using FlowFlex.Application.Contracts.Dtos.OW.Permission;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Const;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Enums.Permission;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Models.Permission;
using Item.ThirdParty.IdentityHub;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PermissionOperationType = FlowFlex.Domain.Shared.Enums.Permission.OperationTypeEnum;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Permission service implementation
    /// Implements permission verification logic with multi-layered security:
    /// - Layer 1 (Controller): WFEAuthorize - Module-level permission check
    /// - Layer 2 (Service): Entity-level permission check (ViewPermissionMode, Teams, Ownership)
    /// </summary>
    public class PermissionService : IPermissionService, IScopedService
    {
        #region Fields and Constructor

        private readonly ILogger<PermissionService> _logger;
        private readonly UserContext _userContext;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IStageRepository _stageRepository;
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IdentityHubClient _identityHubClient;

        public PermissionService(
            ILogger<PermissionService> logger,
            UserContext userContext,
            IWorkflowRepository workflowRepository,
            IStageRepository stageRepository,
            IOnboardingRepository onboardingRepository,
            IdentityHubClient identityHubClient)
        {
            _logger = logger;
            _userContext = userContext;
            _workflowRepository = workflowRepository;
            _stageRepository = stageRepository;
            _onboardingRepository = onboardingRepository;
            _identityHubClient = identityHubClient;
        }

        #endregion

        #region Public API Methods

        /// <summary>
        /// Check resource permission (unified interface for HTTP API)
        /// Checks both View and Operate permissions for the specified resource
        /// First checks View permission, then checks Operate permission if View succeeds
        /// </summary>
        public async Task<CheckPermissionResponse> CheckResourcePermissionAsync(
            long userId,
            long resourceId,
            PermissionEntityTypeEnum resourceType)
        {
            _logger.LogInformation(
                "CheckResourcePermissionAsync - UserId: {UserId}, ResourceId: {ResourceId}, ResourceType: {ResourceType}",
                userId, resourceId, resourceType);

            try
            {
                // Step 1: Check View permission first
                PermissionResult viewResult;

                switch (resourceType)
                {
                    case PermissionEntityTypeEnum.Workflow:
                        viewResult = await CheckWorkflowAccessAsync(userId, resourceId, PermissionOperationType.View);
                        break;

                    case PermissionEntityTypeEnum.Stage:
                        viewResult = await CheckStageAccessAsync(userId, resourceId, PermissionOperationType.View);
                        break;

                    case PermissionEntityTypeEnum.Case:
                        viewResult = await CheckCaseAccessAsync(userId, resourceId, PermissionOperationType.View);
                        break;

                    default:
                        _logger.LogWarning(
                            "Unsupported resource type: {ResourceType}",
                            resourceType);
                        return new CheckPermissionResponse
                        {
                            CanView = false,
                            CanOperate = false,
                            ErrorMessage = $"Unsupported resource type: {resourceType}"
                        };
                }

                // If View permission check failed, return immediately
                if (!viewResult.Success)
                {
                    _logger.LogInformation(
                        "CheckResourcePermissionAsync - View permission check failed, skipping Operate check");
                    return new CheckPermissionResponse
                    {
                        CanView = false,
                        CanOperate = false,
                        GrantReason = null,
                        ErrorMessage = viewResult.ErrorMessage
                    };
                }

                // Step 2: Check Operate permission (only if View succeeded)
                PermissionResult operateResult;

                switch (resourceType)
                {
                    case PermissionEntityTypeEnum.Workflow:
                        operateResult = await CheckWorkflowAccessAsync(userId, resourceId, PermissionOperationType.Operate);
                        break;

                    case PermissionEntityTypeEnum.Stage:
                        operateResult = await CheckStageAccessAsync(userId, resourceId, PermissionOperationType.Operate);
                        break;

                    case PermissionEntityTypeEnum.Case:
                        operateResult = await CheckCaseAccessAsync(userId, resourceId, PermissionOperationType.Operate);
                        break;

                    default:
                        // Should not reach here, but handle it anyway
                        operateResult = PermissionResult.CreateFailure(
                            "Unsupported resource type",
                            "UNSUPPORTED_RESOURCE_TYPE");
                        break;
                }

                // Build response with both View and Operate results
                var response = new CheckPermissionResponse
                {
                    CanView = viewResult.Success,
                    CanOperate = operateResult.Success,
                    GrantReason = operateResult.Success ? operateResult.GrantReason : viewResult.GrantReason,
                    ErrorMessage = null // No error if at least View succeeded
                };

                _logger.LogInformation(
                    "CheckResourcePermissionAsync result - CanView: {CanView}, CanOperate: {CanOperate}, Reason: {Reason}",
                    response.CanView, response.CanOperate, response.GrantReason);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error checking resource permission - UserId: {UserId}, ResourceId: {ResourceId}, ResourceType: {ResourceType}",
                    userId, resourceId, resourceType);

                return new CheckPermissionResponse
                {
                    CanView = false,
                    CanOperate = false,
                    ErrorMessage = "Internal error during permission check"
                };
            }
        }

        #endregion

        #region Core Permission Check Methods (Workflow, Stage, Case)

        /// <summary>
        /// Check module-level permission via IdentityHub
        /// This enforces WFEAuthorize permissions at the service layer
        /// </summary>
        private async Task<PermissionResult> CheckModulePermissionAsync(
            long userId,
            PermissionEntityTypeEnum entityType,
            PermissionOperationType operationType)
        {
            // If no IAM token, deny access
            if (_userContext?.IamToken == null)
            {
                _logger.LogWarning(
                    "Module permission check failed: No IAM token available for user {UserId}",
                    userId);
                return PermissionResult.CreateFailure(
                    "No IAM token available for permission check",
                    "NO_IAM_TOKEN");
            }

            // Map entity type and operation to module permission code
            var permissionCode = GetModulePermissionCode(entityType, operationType);
            if (string.IsNullOrEmpty(permissionCode))
            {
                _logger.LogWarning(
                    "Module permission check failed: Unsupported entity type {EntityType} or operation {Operation}",
                    entityType, operationType);
                return PermissionResult.CreateFailure(
                    "Unsupported entity type or operation",
                    "UNSUPPORTED_PERMISSION");
            }

            _logger.LogInformation(
                "Checking module permission - UserId: {UserId}, Permission: {Permission}",
                userId, permissionCode);

            try
            {
                // Call IdentityHub to check module permission
                var hasPermission = await _identityHubClient.UserRolePermissionCheck(
                    _userContext.IamToken,
                    new List<string> { permissionCode });

                if (!hasPermission)
                {
                    _logger.LogWarning(
                        "Module permission check failed: User {UserId} does not have permission {Permission}",
                        userId, permissionCode);
                    return PermissionResult.CreateFailure(
                        $"User does not have required module permission: {permissionCode}",
                        "MODULE_PERMISSION_DENIED");
                }

                _logger.LogInformation(
                    "Module permission check passed: User {UserId} has permission {Permission}",
                    userId, permissionCode);
                return PermissionResult.CreateSuccess(true, false, "ModulePermission");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error checking module permission for user {UserId}, permission {Permission}",
                    userId, permissionCode);
                return PermissionResult.CreateFailure(
                    "Internal error during module permission check",
                    "MODULE_PERMISSION_CHECK_ERROR");
            }
        }

        /// <summary>
        /// Get module permission code from PermissionConsts based on entity type and operation
        /// </summary>
        private string GetModulePermissionCode(
            PermissionEntityTypeEnum entityType,
            PermissionOperationType operationType)
        {
            return entityType switch
            {
                PermissionEntityTypeEnum.Workflow => operationType switch
                {
                    PermissionOperationType.View => PermissionConsts.Workflow.Read,
                    PermissionOperationType.Operate => PermissionConsts.Workflow.Update,
                    PermissionOperationType.Delete => PermissionConsts.Workflow.Delete,
                    _ => null
                },
                PermissionEntityTypeEnum.Stage => operationType switch
                {
                    PermissionOperationType.View => PermissionConsts.Workflow.Read,
                    PermissionOperationType.Operate => PermissionConsts.Workflow.Update,
                    PermissionOperationType.Delete => PermissionConsts.Workflow.Delete,
                    _ => null
                },
                PermissionEntityTypeEnum.Case => operationType switch
                {
                    PermissionOperationType.View => PermissionConsts.Case.Read,
                    PermissionOperationType.Operate => PermissionConsts.Case.Update,
                    PermissionOperationType.Delete => PermissionConsts.Case.Delete,
                    _ => null
                },
                _ => null
            };
        }

        /// <summary>
        /// Check Workflow access permission
        /// </summary>
        public async Task<PermissionResult> CheckWorkflowAccessAsync(
            long userId,
            long workflowId,
            PermissionOperationType operationType)
        {
            _logger.LogInformation(
                "Checking Workflow access - UserId: {UserId}, WorkflowId: {WorkflowId}, Operation: {Operation}",
                userId, workflowId, operationType);

            try
            {
                // Step 0: Check if user is System Admin or Tenant Admin - bypass all permission checks
                if (_userContext?.IsSystemAdmin == true)
                {
                    _logger.LogInformation(
                        "User {UserId} is System Admin, bypassing all permission checks",
                        userId);
                    return PermissionResult.CreateSuccess(true, true, "SystemAdmin");
                }

                // Get current tenant ID to check tenant admin privileges
                var currentTenantId = GetCurrentTenantId();
                if (_userContext != null && _userContext.HasAdminPrivileges(currentTenantId))
                {
                    _logger.LogInformation(
                        "User {UserId} is Tenant Admin for tenant {TenantId}, bypassing all permission checks",
                        userId, currentTenantId);
                    return PermissionResult.CreateSuccess(true, true, "TenantAdmin");
                }

                // Step 1: Validate input
                if (userId <= 0 || workflowId <= 0)
                {
                    return PermissionResult.CreateFailure(
                        "Invalid user ID or workflow ID",
                        "INVALID_INPUT");
                }

                // Step 2: Check module-level permission (WFEAuthorize)
                // This enforces that user has the required module permission before checking entity-level permissions
                var modulePermissionResult = await CheckModulePermissionAsync(
                    userId,
                    PermissionEntityTypeEnum.Workflow,
                    operationType);

                if (!modulePermissionResult.Success)
                {
                    _logger.LogWarning(
                        "Module permission check failed for user {UserId} - {ErrorMessage}",
                        userId, modulePermissionResult.ErrorMessage);
                    return modulePermissionResult;
                }

                // Step 3: Load Workflow entity
                var workflow = await _workflowRepository.GetByIdAsync(workflowId);
                if (workflow == null)
                {
                    return PermissionResult.CreateFailure(
                        "Workflow not found",
                        "WORKFLOW_NOT_FOUND");
                }

                // Step 4: Check permission based on ViewPermissionMode and operation type
                var permissionCheck = CheckWorkflowPermission(workflow, userId, operationType);
                
                if (permissionCheck.Success)
                {
                _logger.LogInformation(
                        "Workflow permission check passed for user {UserId}, Reason: {Reason}",
                        userId, permissionCheck.GrantReason);
                }
                else
                {
                    _logger.LogWarning(
                        "Workflow permission check failed for user {UserId}, Reason: {Reason}",
                        userId, permissionCheck.ErrorMessage);
                }

                return permissionCheck;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Workflow access");
                return PermissionResult.CreateFailure(
                    "Internal error during permission check",
                    "PERMISSION_CHECK_ERROR");
            }
        }

        /// <summary>
        /// Check Stage access permission
        /// Stage inherits or narrows Workflow permissions
        /// </summary>
        public async Task<PermissionResult> CheckStageAccessAsync(
            long userId,
            long stageId,
            PermissionOperationType operationType)
        {
            _logger.LogInformation(
                "Checking Stage access - UserId: {UserId}, StageId: {StageId}, Operation: {Operation}",
                userId, stageId, operationType);

            try
            {
                // Step 0: Check if user is System Admin or Tenant Admin - bypass all permission checks
                if (_userContext?.IsSystemAdmin == true)
                {
                    _logger.LogInformation(
                        "User {UserId} is System Admin, bypassing all permission checks",
                        userId);
                    return PermissionResult.CreateSuccess(true, true, "SystemAdmin");
                }

                // Get current tenant ID to check tenant admin privileges
                var currentTenantId = GetCurrentTenantId();
                if (_userContext != null && _userContext.HasAdminPrivileges(currentTenantId))
                {
                    _logger.LogInformation(
                        "User {UserId} is Tenant Admin for tenant {TenantId}, bypassing all permission checks",
                        userId, currentTenantId);
                    return PermissionResult.CreateSuccess(true, true, "TenantAdmin");
                }

                // Step 1: Validate input
                if (userId <= 0 || stageId <= 0)
                {
                    return PermissionResult.CreateFailure(
                        "Invalid user ID or stage ID",
                        "INVALID_INPUT");
                }

                // Step 2: Check module-level permission (WFEAuthorize)
                // Stage uses WORKFLOW permissions since it's part of workflow
                var modulePermissionResult = await CheckModulePermissionAsync(
                    userId,
                    PermissionEntityTypeEnum.Stage,
                    operationType);

                if (!modulePermissionResult.Success)
                {
                    _logger.LogWarning(
                        "Module permission check failed for user {UserId} - {ErrorMessage}",
                        userId, modulePermissionResult.ErrorMessage);
                    return modulePermissionResult;
                }

                // Step 3: Load Stage entity
                var stage = await _stageRepository.GetByIdAsync(stageId);
                if (stage == null)
                {
                    return PermissionResult.CreateFailure(
                        "Stage not found",
                        "STAGE_NOT_FOUND");
                }

                // Step 4: Load parent Workflow entity
                var workflow = await _workflowRepository.GetByIdAsync(stage.WorkflowId);
                if (workflow == null)
                {
                    return PermissionResult.CreateFailure(
                        "Parent workflow not found",
                        "WORKFLOW_NOT_FOUND");
                }

                // Step 5: Check if user is the assigned user for this stage (special privilege)
                if (!string.IsNullOrWhiteSpace(stage.DefaultAssignee))
                {
                    try
                    {
                        _logger.LogDebug(
                            "Checking DefaultAssignee - Raw value: {DefaultAssignee}",
                            stage.DefaultAssignee);
                        
                        // Handle double-escaped JSON: first deserialize to string, then to list
                        List<string> assignedUserIds;
                        try
                        {
                            // Try direct deserialization first
                            assignedUserIds = JsonConvert.DeserializeObject<List<string>>(stage.DefaultAssignee) ?? new List<string>();
                        }
                        catch
                        {
                            // If that fails, try double deserialization (for double-escaped JSON)
                            var jsonString = JsonConvert.DeserializeObject<string>(stage.DefaultAssignee);
                            assignedUserIds = JsonConvert.DeserializeObject<List<string>>(jsonString) ?? new List<string>();
                        }
                        
                        var currentUserIdString = _userContext?.UserId;
                        
                        _logger.LogDebug(
                            "Parsed {Count} assigned users, current user: {UserId}",
                            assignedUserIds.Count,
                            currentUserIdString);
                        
                        if (!string.IsNullOrEmpty(currentUserIdString) && assignedUserIds.Contains(currentUserIdString))
                        {
                            _logger.LogInformation(
                                "User {UserId} is assigned to Stage {StageId}, granting access",
                                userId, stageId);
                            return PermissionResult.CreateSuccess(true, true, "AssignedTo");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to parse DefaultAssignee JSON for Stage {StageId}, skipping assigned user check. Value: {Value}",
                            stageId, stage.DefaultAssignee);
                        // Continue with normal permission check
                    }
                }

                // Step 6: Check Stage permission based on inheritance or narrowing
                var permissionCheck = CheckStagePermission(stage, workflow, userId, operationType);
                
                if (permissionCheck.Success)
                {
                    _logger.LogInformation(
                        "Stage permission check passed for user {UserId}, Reason: {Reason}",
                        userId, permissionCheck.GrantReason);
                }
                else
                {
                    _logger.LogWarning(
                        "Stage permission check failed for user {UserId}, Reason: {Reason}",
                        userId, permissionCheck.ErrorMessage);
                }

                return permissionCheck;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Stage access");
                return PermissionResult.CreateFailure(
                    "Internal error during permission check",
                    "PERMISSION_CHECK_ERROR");
            }
        }

        /// <summary>
        /// Check Case access permission
        /// </summary>
        public async Task<PermissionResult> CheckCaseAccessAsync(
            long userId,
            long caseId,
            PermissionOperationType operationType)
        {
            _logger.LogInformation(
                "Checking Case access - UserId: {UserId}, CaseId: {CaseId}, Operation: {Operation}",
                userId, caseId, operationType);

            try
            {
                // Step 0: Check if user is System Admin or Tenant Admin - bypass all permission checks
                if (_userContext?.IsSystemAdmin == true)
                {
                    _logger.LogInformation(
                        "User {UserId} is System Admin, bypassing all permission checks",
                        userId);
                    return PermissionResult.CreateSuccess(true, true, "SystemAdmin");
                }

                // Get current tenant ID to check tenant admin privileges
                var currentTenantId = GetCurrentTenantId();
                if (_userContext != null && _userContext.HasAdminPrivileges(currentTenantId))
                {
                    _logger.LogInformation(
                        "User {UserId} is Tenant Admin for tenant {TenantId}, bypassing all permission checks",
                        userId, currentTenantId);
                    return PermissionResult.CreateSuccess(true, true, "TenantAdmin");
                }

                // Step 1: Validate input
                if (userId <= 0 || caseId <= 0)
                {
                    return PermissionResult.CreateFailure(
                        "Invalid user ID or case ID",
                        "INVALID_INPUT");
                }

                // Step 2: Check module-level permission (WFEAuthorize)
                var modulePermissionResult = await CheckModulePermissionAsync(
                    userId,
                    PermissionEntityTypeEnum.Case,
                    operationType);

                if (!modulePermissionResult.Success)
                {
                    _logger.LogWarning(
                        "Module permission check failed for user {UserId} - {ErrorMessage}",
                        userId, modulePermissionResult.ErrorMessage);
                    return modulePermissionResult;
                }

                // Step 3: Load Case (Onboarding) entity
                var onboarding = await _onboardingRepository.GetByIdAsync(caseId);
                if (onboarding == null)
                {
                    return PermissionResult.CreateFailure(
                        "Case not found",
                        "CASE_NOT_FOUND");
                }

                // Step 4: Check Case permission
                var permissionCheck = CheckCasePermission(onboarding, userId, operationType);
                
                if (permissionCheck.Success)
                {
                    _logger.LogInformation(
                        "Case permission check passed for user {UserId}, Reason: {Reason}",
                        userId, permissionCheck.GrantReason);
                }
                else
                {
                    _logger.LogWarning(
                        "Case permission check failed for user {UserId}, Reason: {Reason}",
                        userId, permissionCheck.ErrorMessage);
                }

                return permissionCheck;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Case access");
                return PermissionResult.CreateFailure(
                    "Internal error during permission check",
                    "PERMISSION_CHECK_ERROR");
            }
        }

        #endregion

        #region Workflow Permission Methods

        /// <summary>
        /// Check Workflow permission based on ViewPermissionMode and operation type
        /// </summary>
        private PermissionResult CheckWorkflowPermission(
            Workflow workflow,
            long userId,
            PermissionOperationType operationType)
        {
            // Get user teams
            var userTeamIds = GetUserTeamIds();

            _logger.LogDebug(
                "CheckWorkflowPermission - WorkflowId: {WorkflowId}, ViewMode: {ViewMode}, ViewTeams: {ViewTeams}, OperateTeams: {OperateTeams}, UserTeams: {UserTeams}",
                workflow.Id,
                workflow.ViewPermissionMode,
                workflow.ViewTeams ?? "NULL",
                workflow.OperateTeams ?? "NULL",
                string.Join(", ", userTeamIds));

            // Step 1: Check View Permission based on ViewPermissionMode
            bool canView = CheckViewPermission(workflow, userTeamIds);
            
            _logger.LogDebug(
                "View permission check result: {CanView}",
                canView);

            // Step 2: Check Operate Permission
            bool canOperate = false;
            if (canView && operationType == PermissionOperationType.Operate)
            {
                canOperate = CheckOperatePermission(workflow, userTeamIds);
            }

            // Step 3: Return result based on operation type
            if (operationType == PermissionOperationType.View)
            {
                if (canView)
                {
                    return PermissionResult.CreateSuccess(true, false, "ViewPermission");
                }
                else
                {
                    return PermissionResult.CreateFailure(
                        "User does not have view permission for this workflow",
                        "VIEW_PERMISSION_DENIED");
                }
            }
            else if (operationType == PermissionOperationType.Operate)
            {
                if (canOperate)
                {
                    return PermissionResult.CreateSuccess(true, true, "OperatePermission");
                }
                else if (canView)
                {
                    return PermissionResult.CreateFailure(
                        "User has view permission but not operate permission",
                        "OPERATE_PERMISSION_DENIED");
                }
                else
                {
                    return PermissionResult.CreateFailure(
                        "User does not have permission for this workflow",
                        "PERMISSION_DENIED");
                }
            }

            return PermissionResult.CreateFailure(
                "Unsupported operation type",
                "UNSUPPORTED_OPERATION");
        }

        /// <summary>
        /// Check view permission based on ViewPermissionMode
        /// </summary>
        private bool CheckViewPermission(Workflow workflow, List<string> userTeamIds)
        {
            switch (workflow.ViewPermissionMode)
            {
                case ViewPermissionModeEnum.Public:
                    // Public mode: everyone can view
                    return true;

                case ViewPermissionModeEnum.VisibleToTeams:
                    // Visible to specific teams: check if user belongs to any of the listed teams
                    if (string.IsNullOrWhiteSpace(workflow.ViewTeams))
                    {
                        // No teams specified, deny access
                        return false;
                    }
                    var visibleTeams = DeserializeTeamList(workflow.ViewTeams);
                    return userTeamIds.Any(teamId => visibleTeams.Contains(teamId));

                case ViewPermissionModeEnum.InvisibleToTeams:
                    // Invisible to specific teams: check if user does NOT belong to any of the listed teams
                    if (string.IsNullOrWhiteSpace(workflow.ViewTeams))
                    {
                        // No teams specified, allow access to all
                        return true;
                    }
                    var invisibleTeams = DeserializeTeamList(workflow.ViewTeams);
                    return !userTeamIds.Any(teamId => invisibleTeams.Contains(teamId));

                case ViewPermissionModeEnum.Private:
                    // Private mode: only creator/owner can view
                    if (string.IsNullOrEmpty(_userContext?.UserId))
                    {
                        return false;
                    }
                    return long.TryParse(_userContext.UserId, out var currentUserId) && 
                           workflow.CreateUserId == currentUserId;

                default:
                    // Default: deny access
                    return false;
            }
        }

        /// <summary>
        /// Check operate permission
        /// </summary>
        private bool CheckOperatePermission(Workflow workflow, List<string> userTeamIds)
        {
            _logger.LogDebug(
                "CheckOperatePermission - ViewPermissionMode: {ViewMode}, OperateTeams: {OperateTeams}",
                workflow.ViewPermissionMode,
                workflow.OperateTeams ?? "NULL");

            // Special case: If ViewPermissionMode is Public and OperateTeams is NULL or empty,
            // it means "Use same groups that have view permission" was checked for Public mode
            // In this case, everyone who can view (everyone) can also operate
            if (workflow.ViewPermissionMode == ViewPermissionModeEnum.Public)
            {
                if (string.IsNullOrWhiteSpace(workflow.OperateTeams))
                {
                    _logger.LogInformation(
                        "Public mode with NULL OperateTeams - granting operate permission to all");
                    return true;
                }
                
                var operateTeams = DeserializeTeamList(workflow.OperateTeams);
                if (operateTeams.Count == 0)
                {
                    _logger.LogInformation(
                        "Public mode with empty OperateTeams array - granting operate permission to all");
                    return true;
                }
                
                // Public mode with specific teams - check membership
                _logger.LogDebug(
                    "Public mode with specific OperateTeams: {Teams}",
                    string.Join(", ", operateTeams));
                return userTeamIds.Any(teamId => operateTeams.Contains(teamId));
            }

            // Non-Public modes: OperateTeams must be specified
            if (string.IsNullOrWhiteSpace(workflow.OperateTeams))
            {
                _logger.LogDebug("Non-Public mode with NULL OperateTeams - denying operate permission");
                return false;
            }

            var teams = DeserializeTeamList(workflow.OperateTeams);
            
            _logger.LogDebug(
                "Deserialized OperateTeams count: {Count}, Teams: {Teams}",
                teams.Count,
                string.Join(", ", teams));
            
            // Check if user belongs to any of the operate teams
            var hasPermission = userTeamIds.Any(teamId => teams.Contains(teamId));
            _logger.LogDebug(
                "User teams: {UserTeams}, Has permission: {HasPermission}",
                string.Join(", ", userTeamIds),
                hasPermission);
            
            return hasPermission;
        }

        #endregion

        #region Stage Permission Methods

        /// <summary>
        /// Get user team IDs from UserContext
        /// </summary>
        private List<string> GetUserTeamIds()
        {
            _logger.LogDebug(
                "GetUserTeamIds - UserContext: {HasContext}, UserTeams: {HasTeams}, UserId: {UserId}",
                _userContext != null,
                _userContext?.UserTeams != null,
                _userContext?.UserId);

            if (_userContext?.UserTeams == null)
            {
                _logger.LogWarning(
                    "UserContext.UserTeams is null for user {UserId}. " +
                    "Team information is not loaded in the current context. " +
                    "This may cause permission checks to fail for team-based permissions. " +
                    "Consider loading team information during authentication or using a middleware to populate UserTeams.",
                    _userContext?.UserId);
                
                // TODO: Implement dynamic team loading from IDM or cache
                // For now, return empty list which will cause team-based permissions to fail
                return new List<string>();
            }

            // Get all team IDs (including sub-teams)
            var teamIds = _userContext.UserTeams.GetAllTeamIds();
            var teamIdStrings = teamIds.Select(id => id.ToString()).ToList();
            
            _logger.LogDebug(
                "GetUserTeamIds - Found {Count} teams: {Teams}",
                teamIdStrings.Count,
                string.Join(", ", teamIdStrings));
            
            return teamIdStrings;
        }

        /// <summary>
        /// Check Stage permission based on inheritance or narrowing from Workflow
        /// Stage can inherit (NULL) or narrow (subset) Workflow permissions
        /// </summary>
        private PermissionResult CheckStagePermission(
            Stage stage,
            Workflow workflow,
            long userId,
            PermissionOperationType operationType)
        {
            // Get user teams
            var userTeamIds = GetUserTeamIds();

            _logger.LogDebug(
                "CheckStagePermission - StageId: {StageId}, ViewMode: {ViewMode}, ViewTeams: {ViewTeams}, OperateTeams: {OperateTeams}, UserTeams: {UserTeams}",
                stage.Id,
                stage.ViewPermissionMode,
                stage.ViewTeams ?? "NULL",
                stage.OperateTeams ?? "NULL",
                string.Join(", ", userTeamIds));

            // Step 1: Determine if Stage inherits or has its own permissions
            bool stageInheritsView = string.IsNullOrWhiteSpace(stage.ViewTeams);
            bool stageInheritsOperate = string.IsNullOrWhiteSpace(stage.OperateTeams);
            
            _logger.LogDebug(
                "Stage inheritance - InheritsView: {InheritsView}, InheritsOperate: {InheritsOperate}",
                stageInheritsView,
                stageInheritsOperate);

            // Step 2: Check View Permission (STRICT MODE)
            // User must have BOTH Workflow AND Stage view permission
            bool canView = false;
            string viewReason = null;

            // First, check Workflow permission (always required)
            bool hasWorkflowViewPermission = CheckViewPermission(workflow, userTeamIds);
            _logger.LogDebug(
                "Stage strict check - Workflow view permission: {HasWorkflowPermission}",
                hasWorkflowViewPermission);

            if (!hasWorkflowViewPermission)
            {
                // If user doesn't have Workflow permission, deny immediately
                _logger.LogDebug("Stage strict check - User denied: No Workflow view permission");
                canView = false;
                viewReason = "NoWorkflowViewPermission";
            }
            else
            {
                // User has Workflow permission, now check Stage permission
                if (stageInheritsView)
                {
                    // Stage inherits view permission from Workflow
                    canView = true;
                    viewReason = "InheritedFromWorkflow";
                    _logger.LogDebug("Stage strict check - Stage inherits Workflow permission, granted");
                }
                else
                {
                    // Stage has its own view permission (narrowed)
                    // User must ALSO satisfy Stage's permission
                    bool hasStageViewPermission = CheckStageViewPermission(stage, userTeamIds);
                    _logger.LogDebug(
                        "Stage strict check - Stage view permission: {HasStagePermission}",
                        hasStageViewPermission);
                    
                    canView = hasStageViewPermission;
                    viewReason = hasStageViewPermission ? "WorkflowAndStageViewPermission" : "NoStageViewPermission";
                }
            }

            // Step 3: Check Operate Permission (STRICT MODE)
            // User must have BOTH Workflow AND Stage operate permission
            bool canOperate = false;
            string operateReason = null;

            if (canView && operationType == PermissionOperationType.Operate)
            {
                // First, check Workflow operate permission (always required)
                bool hasWorkflowOperatePermission = CheckOperatePermission(workflow, userTeamIds);
                _logger.LogDebug(
                    "Stage strict check - Workflow operate permission: {HasWorkflowOperatePermission}",
                    hasWorkflowOperatePermission);

                if (!hasWorkflowOperatePermission)
                {
                    // If user doesn't have Workflow operate permission, deny immediately
                    _logger.LogDebug("Stage strict check - User denied: No Workflow operate permission");
                    canOperate = false;
                    operateReason = "NoWorkflowOperatePermission";
                }
                else
                {
                    // User has Workflow operate permission, now check Stage permission
                    if (stageInheritsOperate)
                    {
                        // Stage inherits operate permission from Workflow
                        canOperate = true;
                        operateReason = "InheritedFromWorkflow";
                        _logger.LogDebug("Stage strict check - Stage inherits Workflow operate permission, granted");
                    }
                    else
                    {
                        // Stage has its own operate permission (narrowed)
                        // User must ALSO satisfy Stage's permission
                        bool hasStageOperatePermission = CheckStageOperatePermission(stage, userTeamIds);
                        _logger.LogDebug(
                            "Stage strict check - Stage operate permission: {HasStageOperatePermission}",
                            hasStageOperatePermission);
                        
                        canOperate = hasStageOperatePermission;
                        operateReason = hasStageOperatePermission ? "WorkflowAndStageOperatePermission" : "NoStageOperatePermission";
                    }
                }
            }

            // Step 4: Return result based on operation type
            if (operationType == PermissionOperationType.View)
            {
                if (canView)
                {
                    return PermissionResult.CreateSuccess(true, false, viewReason);
                }
                else
                {
                    return PermissionResult.CreateFailure(
                        "User does not have view permission for this stage",
                        "VIEW_PERMISSION_DENIED");
                }
            }
            else if (operationType == PermissionOperationType.Operate)
            {
                if (canOperate)
                {
                    return PermissionResult.CreateSuccess(true, true, operateReason);
                }
                else if (canView)
                {
                    return PermissionResult.CreateFailure(
                        "User has view permission but not operate permission for this stage",
                        "OPERATE_PERMISSION_DENIED");
                }
                else
                {
                    return PermissionResult.CreateFailure(
                        "User does not have permission for this stage",
                        "PERMISSION_DENIED");
                }
            }

            return PermissionResult.CreateFailure(
                "Unsupported operation type",
                "UNSUPPORTED_OPERATION");
        }

        /// <summary>
        /// Check Stage's own view permission (narrowed from Workflow)
        /// </summary>
        private bool CheckStageViewPermission(Stage stage, List<string> userTeamIds)
        {
            _logger.LogDebug(
                "CheckStageViewPermission - StageId: {StageId}, ViewMode: {ViewMode}, ViewTeams: {ViewTeams}, UserTeams: [{UserTeams}]",
                stage.Id,
                stage.ViewPermissionMode,
                stage.ViewTeams ?? "NULL",
                string.Join(", ", userTeamIds));

            switch (stage.ViewPermissionMode)
            {
                case ViewPermissionModeEnum.Public:
                    _logger.LogDebug("Stage view permission: Public mode - granting access");
                    return true;

                case ViewPermissionModeEnum.VisibleToTeams:
                    if (string.IsNullOrWhiteSpace(stage.ViewTeams))
                    {
                        _logger.LogDebug("Stage view permission: VisibleToTeams mode but ViewTeams is empty - denying access");
                        return false;
                    }
                    var visibleTeams = DeserializeTeamList(stage.ViewTeams);
                    var hasVisibleTeam = userTeamIds.Any(teamId => visibleTeams.Contains(teamId));
                    _logger.LogDebug(
                        "Stage view permission: VisibleToTeams mode - Required teams: [{RequiredTeams}], User has access: {HasAccess}",
                        string.Join(", ", visibleTeams),
                        hasVisibleTeam);
                    return hasVisibleTeam;

                case ViewPermissionModeEnum.InvisibleToTeams:
                    if (string.IsNullOrWhiteSpace(stage.ViewTeams))
                    {
                        _logger.LogDebug("Stage view permission: InvisibleToTeams mode but ViewTeams is empty - granting access");
                        return true;
                    }
                    var invisibleTeams = DeserializeTeamList(stage.ViewTeams);
                    var isInvisible = !userTeamIds.Any(teamId => invisibleTeams.Contains(teamId));
                    _logger.LogDebug(
                        "Stage view permission: InvisibleToTeams mode - Blocked teams: [{BlockedTeams}], User has access: {HasAccess}",
                        string.Join(", ", invisibleTeams),
                        isInvisible);
                    return isInvisible;

                case ViewPermissionModeEnum.Private:
                    if (string.IsNullOrEmpty(_userContext?.UserId))
                    {
                        _logger.LogDebug("Stage view permission: Private mode but UserId is empty - denying access");
                        return false;
                    }
                    var isOwner = long.TryParse(_userContext.UserId, out var currentUserId) && 
                           stage.CreateUserId == currentUserId;
                    _logger.LogDebug(
                        "Stage view permission: Private mode - CreateUserId: {CreateUserId}, CurrentUserId: {CurrentUserId}, Is owner: {IsOwner}",
                        stage.CreateUserId,
                        currentUserId,
                        isOwner);
                    return isOwner;

                default:
                    _logger.LogWarning("Stage view permission: Unknown ViewPermissionMode: {ViewMode} - denying access", stage.ViewPermissionMode);
                    return false;
            }
        }

        /// <summary>
        /// Check Stage's own operate permission (narrowed from Workflow)
        /// </summary>
        private bool CheckStageOperatePermission(Stage stage, List<string> userTeamIds)
        {
            _logger.LogDebug(
                "CheckStageOperatePermission - StageId: {StageId}, ViewMode: {ViewMode}, OperateTeams: {OperateTeams}, UserTeams: [{UserTeams}]",
                stage.Id,
                stage.ViewPermissionMode,
                stage.OperateTeams ?? "NULL",
                string.Join(", ", userTeamIds));

            // Special case: If ViewPermissionMode is Public and OperateTeams is NULL or empty,
            // it means "Use same groups that have view permission" was checked for Public mode
            // In this case, everyone who can view (everyone) can also operate
            if (stage.ViewPermissionMode == ViewPermissionModeEnum.Public)
            {
                if (string.IsNullOrWhiteSpace(stage.OperateTeams))
                {
                    _logger.LogDebug("Stage operate permission: Public mode with NULL OperateTeams - granting access to all");
                    return true;
                }
                
                var operateTeams = DeserializeTeamList(stage.OperateTeams);
                if (operateTeams.Count == 0)
                {
                    _logger.LogDebug("Stage operate permission: Public mode with empty OperateTeams array - granting access to all");
                    return true;
                }
                
                // Public mode with specific teams - check membership
                var hasOperateTeam = userTeamIds.Any(teamId => operateTeams.Contains(teamId));
                _logger.LogDebug(
                    "Stage operate permission: Public mode with specific teams - Required teams: [{RequiredTeams}], User has access: {HasAccess}",
                    string.Join(", ", operateTeams),
                    hasOperateTeam);
                return hasOperateTeam;
            }

            // Non-Public modes: OperateTeams must be specified
            if (string.IsNullOrWhiteSpace(stage.OperateTeams))
            {
                _logger.LogDebug("Stage operate permission: Non-Public mode with NULL OperateTeams - denying access");
                return false;
            }

            var teams = DeserializeTeamList(stage.OperateTeams);
            var hasTeamAccess = userTeamIds.Any(teamId => teams.Contains(teamId));
            _logger.LogDebug(
                "Stage operate permission: Non-Public mode - Required teams: [{RequiredTeams}], User has access: {HasAccess}",
                string.Join(", ", teams),
                hasTeamAccess);
            return hasTeamAccess;
        }

        #endregion

        #region Case Permission Methods

        /// <summary>
        /// Check Case permission (independent from Workflow/Stage)
        /// Case has completely independent permission control including Ownership
        /// </summary>
        private PermissionResult CheckCasePermission(
            Onboarding onboarding,
            long userId,
            PermissionOperationType operationType)
        {
            // Get user teams or user IDs based on PermissionSubjectType
            var userTeamIds = GetUserTeamIds();
            var userIdString = userId.ToString();

            _logger.LogDebug(
                "CheckCasePermission - CaseId: {CaseId}, ViewMode: {ViewMode}, ViewSubjectType: {ViewSubjectType}, OperateSubjectType: {OperateSubjectType}, " +
                "ViewTeams: {ViewTeams}, ViewUsers: {ViewUsers}, OperateTeams: {OperateTeams}, OperateUsers: {OperateUsers}, " +
                "Ownership: {Ownership}, UserTeams: [{UserTeams}], UserId: {UserId}",
                onboarding.Id,
                onboarding.ViewPermissionMode,
                onboarding.ViewPermissionSubjectType,
                onboarding.OperatePermissionSubjectType,
                onboarding.ViewTeams ?? "NULL",
                onboarding.ViewUsers ?? "NULL",
                onboarding.OperateTeams ?? "NULL",
                onboarding.OperateUsers ?? "NULL",
                onboarding.Ownership,
                string.Join(", ", userTeamIds),
                userId);

            // Step 1: Check Ownership (highest priority)
            if (onboarding.Ownership.HasValue && onboarding.Ownership.Value == userId)
            {
                _logger.LogDebug("Case permission: User {UserId} is the owner - granting full access", userId);
                return PermissionResult.CreateSuccess(true, true, "Owner");
            }

            // Step 2: Check View Permission
            bool canView = CheckCaseViewPermission(onboarding, userTeamIds, userIdString);
            
            if (!canView)
            {
                return PermissionResult.CreateFailure(
                    "User does not have view permission for this case",
                    "VIEW_PERMISSION_DENIED");
            }

            // Step 3: Check Operate Permission (only if user can view)
            if (operationType == PermissionOperationType.Operate || 
                operationType == PermissionOperationType.Delete)
            {
                bool canOperate = CheckCaseOperatePermission(onboarding, userTeamIds, userIdString);
                
                if (canOperate)
                {
                    return PermissionResult.CreateSuccess(true, true, "CaseOperatePermission");
                }
                else
                {
                    return PermissionResult.CreateFailure(
                        "User has view permission but not operate permission for this case",
                        "OPERATE_PERMISSION_DENIED");
                }
            }

            // View operation
            return PermissionResult.CreateSuccess(true, false, "CaseViewPermission");
        }

        /// <summary>
        /// Check Case view permission
        /// </summary>
        private bool CheckCaseViewPermission(Onboarding onboarding, List<string> userTeamIds, string userId)
        {
            _logger.LogDebug(
                "CheckCaseViewPermission - CaseId: {CaseId}, ViewMode: {ViewMode}, ViewSubjectType: {ViewSubjectType}",
                onboarding.Id,
                onboarding.ViewPermissionMode,
                onboarding.ViewPermissionSubjectType);

            switch (onboarding.ViewPermissionMode)
            {
                case ViewPermissionModeEnum.Public:
                    _logger.LogDebug("Case view permission: Public mode - granting access");
                    return true;

                case ViewPermissionModeEnum.VisibleToTeams:
                    if (onboarding.ViewPermissionSubjectType == PermissionSubjectTypeEnum.Team)
                    {
                        // Team-based permission
                        if (string.IsNullOrWhiteSpace(onboarding.ViewTeams))
                        {
                            _logger.LogDebug("Case view permission: VisibleToTeams mode but ViewTeams is empty - denying access");
                            return false;
                        }
                        var visibleTeams = DeserializeTeamList(onboarding.ViewTeams);
                        var hasVisibleTeam = userTeamIds.Any(teamId => visibleTeams.Contains(teamId));
                        _logger.LogDebug(
                            "Case view permission: VisibleToTeams (Team) - Required teams: [{RequiredTeams}], User has access: {HasAccess}",
                            string.Join(", ", visibleTeams),
                            hasVisibleTeam);
                        return hasVisibleTeam;
                    }
                    else
                    {
                        // User-based permission
                        if (string.IsNullOrWhiteSpace(onboarding.ViewUsers))
                        {
                            _logger.LogDebug("Case view permission: VisibleToTeams mode but ViewUsers is empty - denying access");
                            return false;
                        }
                        var visibleUsers = DeserializeTeamList(onboarding.ViewUsers);
                        var hasAccess = visibleUsers.Contains(userId);
                        _logger.LogDebug(
                            "Case view permission: VisibleToTeams (User) - Required users: [{RequiredUsers}], User has access: {HasAccess}",
                            string.Join(", ", visibleUsers),
                            hasAccess);
                        return hasAccess;
                    }

                case ViewPermissionModeEnum.InvisibleToTeams:
                    if (onboarding.ViewPermissionSubjectType == PermissionSubjectTypeEnum.Team)
                    {
                        // Team-based permission
                        if (string.IsNullOrWhiteSpace(onboarding.ViewTeams))
                        {
                            _logger.LogDebug("Case view permission: InvisibleToTeams mode but ViewTeams is empty - granting access");
                            return true;
                        }
                        var invisibleTeams = DeserializeTeamList(onboarding.ViewTeams);
                        var isInvisible = !userTeamIds.Any(teamId => invisibleTeams.Contains(teamId));
                        _logger.LogDebug(
                            "Case view permission: InvisibleToTeams (Team) - Blocked teams: [{BlockedTeams}], User has access: {HasAccess}",
                            string.Join(", ", invisibleTeams),
                            isInvisible);
                        return isInvisible;
                    }
                    else
                    {
                        // User-based permission
                        if (string.IsNullOrWhiteSpace(onboarding.ViewUsers))
                        {
                            _logger.LogDebug("Case view permission: InvisibleToTeams mode but ViewUsers is empty - granting access");
                            return true;
                        }
                        var invisibleUsers = DeserializeTeamList(onboarding.ViewUsers);
                        var hasAccess = !invisibleUsers.Contains(userId);
                        _logger.LogDebug(
                            "Case view permission: InvisibleToTeams (User) - Blocked users: [{BlockedUsers}], User has access: {HasAccess}",
                            string.Join(", ", invisibleUsers),
                            hasAccess);
                        return hasAccess;
                    }

                case ViewPermissionModeEnum.Private:
                    // Private mode: Only owner can view (already checked in CheckCasePermission)
                    _logger.LogDebug("Case view permission: Private mode - denying access (not owner)");
                    return false;

                default:
                    _logger.LogWarning("Case view permission: Unknown ViewPermissionMode: {ViewMode} - denying access", onboarding.ViewPermissionMode);
                    return false;
            }
        }

        /// <summary>
        /// Check Case operate permission
        /// </summary>
        private bool CheckCaseOperatePermission(Onboarding onboarding, List<string> userTeamIds, string userId)
        {
            _logger.LogDebug(
                "CheckCaseOperatePermission - CaseId: {CaseId}, OperateSubjectType: {OperateSubjectType}, OperateTeams: {OperateTeams}, OperateUsers: {OperateUsers}",
                onboarding.Id,
                onboarding.OperatePermissionSubjectType,
                onboarding.OperateTeams ?? "NULL",
                onboarding.OperateUsers ?? "NULL");

            if (onboarding.OperatePermissionSubjectType == PermissionSubjectTypeEnum.Team)
            {
                // Team-based permission
                // Special case: If ViewPermissionMode is Public and OperateTeams is NULL or empty,
                // everyone who can view can also operate
                if (onboarding.ViewPermissionMode == ViewPermissionModeEnum.Public)
                {
                    if (string.IsNullOrWhiteSpace(onboarding.OperateTeams))
                    {
                        _logger.LogDebug("Case operate permission: Public mode with NULL OperateTeams - granting access to all");
                        return true;
                    }
                    
                    var operateTeams = DeserializeTeamList(onboarding.OperateTeams);
                    if (operateTeams.Count == 0)
                    {
                        _logger.LogDebug("Case operate permission: Public mode with empty OperateTeams array - granting access to all");
                        return true;
                    }
                    
                    // Public mode with specific teams - check membership
                    var hasOperateTeam = userTeamIds.Any(teamId => operateTeams.Contains(teamId));
                    _logger.LogDebug(
                        "Case operate permission: Public mode with specific teams - Required teams: [{RequiredTeams}], User has access: {HasAccess}",
                        string.Join(", ", operateTeams),
                        hasOperateTeam);
                    return hasOperateTeam;
                }

                // Non-Public modes: OperateTeams must be specified
                if (string.IsNullOrWhiteSpace(onboarding.OperateTeams))
                {
                    _logger.LogDebug("Case operate permission: Non-Public mode with NULL OperateTeams - denying access");
                    return false;
                }

                var teams = DeserializeTeamList(onboarding.OperateTeams);
                var hasTeamAccess = userTeamIds.Any(teamId => teams.Contains(teamId));
                _logger.LogDebug(
                    "Case operate permission: Team-based - Required teams: [{RequiredTeams}], User has access: {HasAccess}",
                    string.Join(", ", teams),
                    hasTeamAccess);
                return hasTeamAccess;
            }
            else
            {
                // User-based permission
                // Special case: If ViewPermissionMode is Public and OperateUsers is NULL or empty,
                // everyone who can view can also operate
                if (onboarding.ViewPermissionMode == ViewPermissionModeEnum.Public)
                {
                    if (string.IsNullOrWhiteSpace(onboarding.OperateUsers))
                    {
                        _logger.LogDebug("Case operate permission: Public mode with NULL OperateUsers - granting access to all");
                        return true;
                    }
                    
                    var operateUsers = DeserializeTeamList(onboarding.OperateUsers);
                    if (operateUsers.Count == 0)
                    {
                        _logger.LogDebug("Case operate permission: Public mode with empty OperateUsers array - granting access to all");
                        return true;
                    }
                    
                    // Public mode with specific users - check membership
                    var hasAccess = operateUsers.Contains(userId);
                    _logger.LogDebug(
                        "Case operate permission: Public mode with specific users - Required users: [{RequiredUsers}], User has access: {HasAccess}",
                        string.Join(", ", operateUsers),
                        hasAccess);
                    return hasAccess;
                }

                // Non-Public modes: OperateUsers must be specified
                if (string.IsNullOrWhiteSpace(onboarding.OperateUsers))
                {
                    _logger.LogDebug("Case operate permission: Non-Public mode with NULL OperateUsers - denying access");
                    return false;
                }

                var users = DeserializeTeamList(onboarding.OperateUsers);
                var hasUserAccess = users.Contains(userId);
                _logger.LogDebug(
                    "Case operate permission: User-based - Required users: [{RequiredUsers}], User has access: {HasAccess}",
                    string.Join(", ", users),
                    hasUserAccess);
                return hasUserAccess;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get current tenant ID from UserContext
        /// </summary>
        private string GetCurrentTenantId()
        {
            return _userContext?.TenantId ?? "DEFAULT";
        }

        /// <summary>
        /// Deserialize team list from JSON, handling double-escaped JSON
        /// </summary>
        private List<string> DeserializeTeamList(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<string>();
            }

            try
            {
                // Try direct deserialization first
                return JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                try
                {
                    // If that fails, try double deserialization (for double-escaped JSON)
                    var jsonString = JsonConvert.DeserializeObject<string>(json);
                    return JsonConvert.DeserializeObject<List<string>>(jsonString) ?? new List<string>();
                }
                catch
                {
                    _logger.LogWarning("Failed to deserialize team list: {Json}", json);
                    return new List<string>();
                }
            }
        }

        #endregion
    }
}

