using System.Linq;
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
using Microsoft.AspNetCore.Http;
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
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PermissionService(
            ILogger<PermissionService> logger,
            UserContext userContext,
            IWorkflowRepository workflowRepository,
            IStageRepository stageRepository,
            IOnboardingRepository onboardingRepository,
            IdentityHubClient identityHubClient,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _userContext = userContext;
            _workflowRepository = workflowRepository;
            _stageRepository = stageRepository;
            _onboardingRepository = onboardingRepository;
            _identityHubClient = identityHubClient;
            _httpContextAccessor = httpContextAccessor;
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
            // Check if this is a Portal token accessing a [PortalAccess] endpoint
            // Portal tokens bypass module permission checks (they use endpoint-level authorization)
            if (IsPortalTokenWithPortalAccess())
            {
                _logger.LogInformation(
                    "Portal token detected - bypassing module permission check for user {UserId}",
                    userId);
                return PermissionResult.CreateSuccess(true, false, "PortalAccess");
            }

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
            return workflow.ViewPermissionMode switch
            {
                ViewPermissionModeEnum.Public => true,
                
                ViewPermissionModeEnum.VisibleToTeams => 
                    CheckTeamWhitelist(workflow.ViewTeams, userTeamIds),
                
                ViewPermissionModeEnum.InvisibleToTeams => 
                    CheckTeamBlacklist(workflow.ViewTeams, userTeamIds),
                
                ViewPermissionModeEnum.Private => 
                    IsCurrentUserOwner(workflow.CreateUserId),
                
                _ => false
            };
        }

        /// <summary>
        /// Check operate permission
        /// </summary>
        private bool CheckOperatePermission(Workflow workflow, List<string> userTeamIds)
        {
            _logger.LogDebug(
                "CheckOperatePermission - ViewMode: {ViewMode}, OperateTeams: {OperateTeams}",
                workflow.ViewPermissionMode,
                workflow.OperateTeams ?? "NULL");

            return workflow.ViewPermissionMode switch
            {
                // Public mode: NULL/empty means everyone can operate, otherwise whitelist
                ViewPermissionModeEnum.Public => 
                    CheckOperateTeamsPublicMode(workflow.OperateTeams, userTeamIds),
                
                // InvisibleToTeams mode: OperateTeams is blacklist
                ViewPermissionModeEnum.InvisibleToTeams => 
                    CheckTeamBlacklist(workflow.OperateTeams, userTeamIds),
                
                // VisibleToTeams and Private modes: OperateTeams is whitelist (required)
                _ => CheckTeamWhitelist(workflow.OperateTeams, userTeamIds)
            };
        }

        /// <summary>
        /// Check operate teams in Public mode
        /// NULL or empty means everyone can operate, otherwise whitelist
        /// </summary>
        private bool CheckOperateTeamsPublicMode(string operateTeamsJson, List<string> userTeamIds)
        {
            if (string.IsNullOrWhiteSpace(operateTeamsJson))
            {
                _logger.LogDebug("Public mode with NULL OperateTeams - granting operate permission to all");
                    return true;
                }
                
            var operateTeams = DeserializeTeamList(operateTeamsJson);
                if (operateTeams.Count == 0)
                {
                _logger.LogDebug("Public mode with empty OperateTeams array - granting operate permission to all");
                    return true;
                }
                
            // Public mode with specific teams - check membership (whitelist)
            var hasPermission = userTeamIds.Any(teamId => operateTeams.Contains(teamId));
                _logger.LogDebug(
                "Public mode with specific OperateTeams (whitelist): [{Teams}], Has permission: {HasPermission}",
                string.Join(", ", operateTeams),
                hasPermission);
            return hasPermission;
        }

        #endregion

        #region Stage Permission Methods

        /// <summary>
        /// Get user team IDs from UserContext
        /// </summary>
        public List<string> GetUserTeamIds()
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
                "CheckStageViewPermission - StageId: {StageId}, ViewMode: {ViewMode}, ViewTeams: {ViewTeams}",
                stage.Id,
                stage.ViewPermissionMode,
                stage.ViewTeams ?? "NULL");

            return stage.ViewPermissionMode switch
            {
                ViewPermissionModeEnum.Public => true,
                ViewPermissionModeEnum.VisibleToTeams => CheckTeamWhitelist(stage.ViewTeams, userTeamIds),
                ViewPermissionModeEnum.InvisibleToTeams => CheckTeamBlacklist(stage.ViewTeams, userTeamIds),
                ViewPermissionModeEnum.Private => IsCurrentUserOwner(stage.CreateUserId),
                _ => false
            };
        }

        /// <summary>
        /// Check Stage's own operate permission (narrowed from Workflow)
        /// </summary>
        private bool CheckStageOperatePermission(Stage stage, List<string> userTeamIds)
        {
            _logger.LogDebug(
                "CheckStageOperatePermission - StageId: {StageId}, ViewMode: {ViewMode}, OperateTeams: {OperateTeams}",
                stage.Id,
                stage.ViewPermissionMode,
                stage.OperateTeams ?? "NULL");

            return stage.ViewPermissionMode switch
            {
                ViewPermissionModeEnum.Public => CheckOperateTeamsPublicMode(stage.OperateTeams, userTeamIds),
                ViewPermissionModeEnum.InvisibleToTeams => CheckTeamBlacklist(stage.OperateTeams, userTeamIds),
                _ => CheckTeamWhitelist(stage.OperateTeams, userTeamIds)
            };
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

            return onboarding.ViewPermissionMode switch
            {
                ViewPermissionModeEnum.Public => true,
                
                ViewPermissionModeEnum.VisibleToTeams => 
                    onboarding.ViewPermissionSubjectType == PermissionSubjectTypeEnum.Team
                        ? CheckTeamWhitelist(onboarding.ViewTeams, userTeamIds)
                        : CheckUserWhitelist(onboarding.ViewUsers, userId),
                
                ViewPermissionModeEnum.InvisibleToTeams => 
                    onboarding.ViewPermissionSubjectType == PermissionSubjectTypeEnum.Team
                        ? CheckTeamBlacklist(onboarding.ViewTeams, userTeamIds)
                        : CheckUserBlacklist(onboarding.ViewUsers, userId),
                
                ViewPermissionModeEnum.Private => false, // Owner check is handled in CheckCasePermission
                
                _ => false
            };
        }

        /// <summary>
        /// Check Case operate permission
        /// </summary>
        private bool CheckCaseOperatePermission(Onboarding onboarding, List<string> userTeamIds, string userId)
        {
            _logger.LogDebug(
                "CheckCaseOperatePermission - CaseId: {CaseId}, OperateSubjectType: {OperateSubjectType}",
                onboarding.Id,
                onboarding.OperatePermissionSubjectType);

                        // Team-based permission
            if (onboarding.OperatePermissionSubjectType == PermissionSubjectTypeEnum.Team)
            {
                return onboarding.ViewPermissionMode switch
                {
                    ViewPermissionModeEnum.Public => CheckOperateTeamsPublicMode(onboarding.OperateTeams, userTeamIds),
                    ViewPermissionModeEnum.InvisibleToTeams => CheckTeamBlacklist(onboarding.OperateTeams, userTeamIds),
                    _ => CheckTeamWhitelist(onboarding.OperateTeams, userTeamIds)
                };
            }
            
                        // User-based permission
            return onboarding.ViewPermissionMode switch
            {
                ViewPermissionModeEnum.Public => CheckOperateUsersPublicMode(onboarding.OperateUsers, userId),
                ViewPermissionModeEnum.InvisibleToTeams => CheckUserBlacklist(onboarding.OperateUsers, userId),
                _ => CheckUserWhitelist(onboarding.OperateUsers, userId)
            };
        }

        /// <summary>
        /// Check operate users in Public mode
        /// NULL or empty means everyone can operate, otherwise whitelist
        /// </summary>
        private bool CheckOperateUsersPublicMode(string operateUsersJson, string userId)
        {
            if (string.IsNullOrWhiteSpace(operateUsersJson))
            {
                _logger.LogDebug("Public mode with NULL OperateUsers - granting operate permission to all");
                            return true;
                        }
            
            var operateUsers = DeserializeTeamList(operateUsersJson);
            if (operateUsers.Count == 0)
            {
                _logger.LogDebug("Public mode with empty OperateUsers array - granting operate permission to all");
                            return true;
                        }
            
            // Public mode with specific users - check membership (whitelist)
            var hasPermission = operateUsers.Contains(userId);
                        _logger.LogDebug(
                "Public mode with specific OperateUsers (whitelist): [{Users}], Has permission: {HasPermission}",
                string.Join(", ", operateUsers),
                hasPermission);
            return hasPermission;
        }

        #endregion

        #region Optimized Permission Info Methods (for DTO population)

        /// <summary>
        /// Get permission info for Workflow (optimized for DTO population)
        /// Returns both view and operate permissions in a single call
        /// </summary>
        public async Task<PermissionInfoDto> GetWorkflowPermissionInfoAsync(long userId, long workflowId)
        {
            // Fast path: System Admin
            if (_userContext?.IsSystemAdmin == true)
            {
                return new PermissionInfoDto
                {
                    CanView = true,
                    CanOperate = true,
                    ErrorMessage = null
                };
            }

            // Check view permission (READ)
            var viewResult = await CheckWorkflowAccessAsync(userId, workflowId, Domain.Shared.Enums.Permission.OperationTypeEnum.View);
            
            // If can't view, return immediately
            if (!viewResult.CanView)
            {
                return new PermissionInfoDto
                {
                    CanView = false,
                    CanOperate = false,
                    ErrorMessage = viewResult.ErrorMessage
                };
            }

            // Check operate permission (UPDATE) - only if view is granted
            var operateResult = await CheckWorkflowAccessAsync(userId, workflowId, Domain.Shared.Enums.Permission.OperationTypeEnum.Operate);
            
            return new PermissionInfoDto
            {
                CanView = true,
                CanOperate = operateResult.CanOperate,
                ErrorMessage = null
            };
        }

        /// <summary>
        /// Get permission info for Stage (optimized for DTO population)
        /// Returns both view and operate permissions in a single call
        /// </summary>
        public async Task<PermissionInfoDto> GetStagePermissionInfoAsync(long userId, long stageId)
        {
            // Fast path: System Admin
            if (_userContext?.IsSystemAdmin == true)
            {
                return new PermissionInfoDto
                {
                    CanView = true,
                    CanOperate = true,
                    ErrorMessage = null
                };
            }

            // Check view permission (READ)
            var viewResult = await CheckStageAccessAsync(userId, stageId, Domain.Shared.Enums.Permission.OperationTypeEnum.View);
            
            // If can't view, return immediately
            if (!viewResult.CanView)
            {
                return new PermissionInfoDto
                {
                    CanView = false,
                    CanOperate = false,
                    ErrorMessage = viewResult.ErrorMessage
                };
            }

            // Check operate permission (UPDATE) - only if view is granted
            var operateResult = await CheckStageAccessAsync(userId, stageId, Domain.Shared.Enums.Permission.OperationTypeEnum.Operate);
            
            return new PermissionInfoDto
            {
                CanView = true,
                CanOperate = operateResult.CanOperate,
                ErrorMessage = null
            };
        }

        /// <summary>
        /// Get permission info for Case (optimized for DTO population)
        /// Returns both view and operate permissions in a single call
        /// </summary>
        public async Task<PermissionInfoDto> GetCasePermissionInfoAsync(long userId, long caseId)
        {
            // Fast path: System Admin
            if (_userContext?.IsSystemAdmin == true)
            {
                return new PermissionInfoDto
                {
                    CanView = true,
                    CanOperate = true,
                    ErrorMessage = null
                };
            }

            // Check view permission (READ)
            var viewResult = await CheckCaseAccessAsync(userId, caseId, Domain.Shared.Enums.Permission.OperationTypeEnum.View);
            
            // If can't view, return immediately
            if (!viewResult.CanView)
            {
                return new PermissionInfoDto
                {
                    CanView = false,
                    CanOperate = false,
                    ErrorMessage = viewResult.ErrorMessage
                };
            }

            // Check operate permission (UPDATE) - only if view is granted
            var operateResult = await CheckCaseAccessAsync(userId, caseId, Domain.Shared.Enums.Permission.OperationTypeEnum.Operate);
            
            return new PermissionInfoDto
            {
                CanView = true,
                CanOperate = operateResult.CanOperate,
                ErrorMessage = null
            };
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
        /// Check if user belongs to any team in the whitelist
        /// </summary>
        private bool CheckTeamWhitelist(string teamsJson, List<string> userTeamIds)
        {
            if (string.IsNullOrWhiteSpace(teamsJson))
            {
                return false;
            }
            
            var teams = DeserializeTeamList(teamsJson);
            return userTeamIds.Any(teamId => teams.Contains(teamId));
        }

        /// <summary>
        /// Check if user does NOT belong to any team in the blacklist
        /// </summary>
        private bool CheckTeamBlacklist(string teamsJson, List<string> userTeamIds)
        {
            if (string.IsNullOrWhiteSpace(teamsJson))
            {
                return true; // No blacklist means everyone can access
            }
            
            var teams = DeserializeTeamList(teamsJson);
            return !userTeamIds.Any(teamId => teams.Contains(teamId));
        }

        /// <summary>
        /// Check if user is in the whitelist
        /// </summary>
        private bool CheckUserWhitelist(string usersJson, string userId)
        {
            if (string.IsNullOrWhiteSpace(usersJson))
            {
                    return false;
                }

            var users = DeserializeTeamList(usersJson);
            return users.Contains(userId);
        }

        /// <summary>
        /// Check if user is NOT in the blacklist
        /// </summary>
        private bool CheckUserBlacklist(string usersJson, string userId)
        {
            if (string.IsNullOrWhiteSpace(usersJson))
            {
                return true; // No blacklist means everyone can access
            }
            
            var users = DeserializeTeamList(usersJson);
            return !users.Contains(userId);
        }

        /// <summary>
        /// Check if current user is the owner of the entity
        /// </summary>
        private bool IsCurrentUserOwner(long? createUserId)
        {
            if (string.IsNullOrEmpty(_userContext?.UserId) || !createUserId.HasValue)
            {
                    return false;
                }

            return long.TryParse(_userContext.UserId, out var currentUserId) && 
                   createUserId.Value == currentUserId;
        }

        /// <summary>
        /// Check if current request is using a Portal token and accessing a [PortalAccess] endpoint
        /// Portal tokens should bypass module permission checks on endpoints marked with [PortalAccess]
        /// </summary>
        private bool IsPortalTokenWithPortalAccess()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                _logger.LogDebug("IsPortalTokenWithPortalAccess: HttpContext is null");
                return false;
            }

            var user = httpContext.User;
            if (user == null || !user.Identity.IsAuthenticated)
            {
                _logger.LogDebug("IsPortalTokenWithPortalAccess: User is null or not authenticated");
                return false;
            }

            // Check for Portal token indicators in claims
            var scope = user.Claims.FirstOrDefault(c => c.Type == "scope")?.Value;
            var tokenType = user.Claims.FirstOrDefault(c => c.Type == "token_type")?.Value;

            _logger.LogDebug(
                "IsPortalTokenWithPortalAccess: Checking claims - Scope: {Scope}, TokenType: {TokenType}",
                scope ?? "NULL",
                tokenType ?? "NULL");

            // Portal token has scope="portal" and token_type="portal-access"
            bool isPortalToken = scope == "portal" && tokenType == "portal-access";

            if (!isPortalToken)
            {
                _logger.LogDebug("IsPortalTokenWithPortalAccess: Not a portal token");
                return false;
            }

            // Check if endpoint has [PortalAccess] attribute
            var endpoint = httpContext.GetEndpoint();
            if (endpoint == null)
            {
                _logger.LogDebug("IsPortalTokenWithPortalAccess: Endpoint is null");
                return false;
            }

            var portalAccessAttr = endpoint.Metadata.GetMetadata<FlowFlex.Application.Filter.PortalAccessAttribute>();
            bool hasPortalAccess = portalAccessAttr != null;
            
            _logger.LogDebug(
                "IsPortalTokenWithPortalAccess: Endpoint has [PortalAccess]: {HasPortalAccess}",
                hasPortalAccess);
            
            return hasPortalAccess;
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

        /// <summary>
        /// Check user's group permission (module-level permission check)
        /// This method ONLY checks module permission, without Portal token bypass
        /// Used for batch permission checking in list operations
        /// </summary>
        public async Task<bool> CheckGroupPermissionAsync(long userId, string permission)
        {
            _logger.LogDebug(
                "CheckGroupPermissionAsync - UserId: {UserId}, Permission: {Permission}",
                userId, permission);

            // System Admin has all permissions
            if (_userContext?.IsSystemAdmin == true)
            {
                _logger.LogDebug("User {UserId} is System Admin, granting permission {Permission}", userId, permission);
                return true;
            }

            // If no IAM token, deny access
            if (_userContext?.IamToken == null)
            {
                _logger.LogWarning(
                    "Module permission check failed: No IAM token available for user {UserId}",
                    userId);
                return false;
            }

            try
            {
                // Call IdentityHub to check module permission
                var hasPermission = await _identityHubClient.UserRolePermissionCheck(
                    _userContext.IamToken,
                    new List<string> { permission });

                if (hasPermission)
                {
                    _logger.LogDebug(
                        "Module permission check passed: User {UserId} has permission {Permission}",
                        userId, permission);
                    return true;
                }
                else
                {
                    _logger.LogWarning(
                        "Module permission check failed: User {UserId} does not have permission {Permission}",
                        userId, permission);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error checking group permission for user {UserId}, permission {Permission}",
                    userId, permission);
                return false;
            }
        }

        /// <summary>
        /// Check Workflow view permission at entity-level ONLY
        /// This method skips module permission check and directly checks entity-level permission
        /// Optimized for batch operations where module permission is already verified
        /// </summary>
        public async Task<bool> CheckWorkflowViewPermissionAsync(
            long userId,
            long workflowId,
            Workflow workflow = null)
        {
            // System Admin has all permissions
            if (_userContext?.IsSystemAdmin == true)
            {
                _logger.LogDebug(
                    "User {UserId} is System Admin, granting view permission for Workflow {WorkflowId}",
                    userId, workflowId);
                return true;
            }

            // Load workflow if not provided
            if (workflow == null)
            {
                workflow = await _workflowRepository.GetByIdAsync(workflowId);
                if (workflow == null)
                {
                    _logger.LogWarning("Workflow {WorkflowId} not found", workflowId);
                    return false;
                }
            }

            // Get user teams
            var userTeamIds = GetUserTeamIds();

            // Check view permission only
            return CheckViewPermission(workflow, userTeamIds);
        }

        /// <summary>
        /// Get permission info for Workflow (batch-optimized for list APIs)
        /// Skips redundant module permission checks by accepting pre-checked flags
        /// This significantly improves performance for list queries by avoiding N IDM API calls
        /// </summary>
        public async Task<PermissionInfoDto> GetWorkflowPermissionInfoForListAsync(
            long userId, 
            long workflowId, 
            bool hasViewModulePermission, 
            bool hasOperateModulePermission)
        {
            // System Admin bypass
            if (_userContext?.IsSystemAdmin == true)
            {
                return new PermissionInfoDto 
                { 
                    CanView = true, 
                    CanOperate = true, 
                    ErrorMessage = null 
                };
            }

            // Check View permission (module permission already checked by caller)
            if (!hasViewModulePermission)
            {
                return new PermissionInfoDto
                {
                    CanView = false,
                    CanOperate = false,
                    ErrorMessage = $"User does not have required module permission: {PermissionConsts.Workflow.Read}"
                };
            }

            // Load workflow entity
            var workflow = await _workflowRepository.GetByIdAsync(workflowId);
            if (workflow == null)
            {
                return new PermissionInfoDto
                {
                    CanView = false,
                    CanOperate = false,
                    ErrorMessage = $"Workflow {workflowId} not found"
                };
            }

            // Get user teams
            var userTeamIds = GetUserTeamIds();

            // Check entity-level view permission
            bool canView = CheckViewPermission(workflow, userTeamIds);
            if (!canView)
            {
                return new PermissionInfoDto
                {
                    CanView = false,
                    CanOperate = false,
                    ErrorMessage = "User is not in allowed teams to view this workflow"
                };
            }

            // Check Operate permission (module permission already checked by caller)
            bool canOperate = false;
            if (hasOperateModulePermission)
            {
                // Check entity-level operate permission
                canOperate = CheckOperatePermission(workflow, userTeamIds);
            }

            return new PermissionInfoDto
            {
                CanView = true,
                CanOperate = canOperate,
                ErrorMessage = null
            };
        }

        /// <summary>
        /// Get permission info for Stage (batch-optimized for list APIs)
        /// Stage inherits Workflow view permissions, but requires explicit module permission for operate
        /// </summary>
        public async Task<PermissionInfoDto> GetStagePermissionInfoForListAsync(
            long userId, 
            long stageId, 
            bool hasViewModulePermission, 
            bool hasOperateModulePermission)
        {
            // System Admin bypass
            if (_userContext?.IsSystemAdmin == true)
            {
                return new PermissionInfoDto 
                { 
                    CanView = true, 
                    CanOperate = true, 
                    ErrorMessage = null 
                };
            }

            // Load stage entity
            var stage = await _stageRepository.GetByIdAsync(stageId);
            if (stage == null)
            {
                return new PermissionInfoDto
                {
                    CanView = false,
                    CanOperate = false,
                    ErrorMessage = $"Stage {stageId} not found"
                };
            }

            // Load workflow entity (needed for permission inheritance check)
            var workflow = await _workflowRepository.GetByIdAsync(stage.WorkflowId);
            if (workflow == null)
            {
                return new PermissionInfoDto
                {
                    CanView = false,
                    CanOperate = false,
                    ErrorMessage = $"Workflow {stage.WorkflowId} not found"
                };
            }

            // Check entity-level view permission (includes workflow inheritance check)
            // Stage view permission inherits from Workflow, so we don't strictly require STAGE:READ module permission
            var viewResult = CheckStagePermission(stage, workflow, userId, PermissionOperationType.View);
            if (!viewResult.Success)
            {
                return new PermissionInfoDto
                {
                    CanView = false,
                    CanOperate = false,
                    ErrorMessage = viewResult.ErrorMessage ?? "User is not allowed to view this stage"
                };
            }

            // Check Operate permission
            // Operate requires explicit module permission AND entity-level permission
            bool canOperate = false;
            if (hasOperateModulePermission)
            {
                // Check entity-level operate permission (includes workflow inheritance check)
                var operateResult = CheckStagePermission(stage, workflow, userId, PermissionOperationType.Operate);
                canOperate = operateResult.Success;
            }

            return new PermissionInfoDto
            {
                CanView = true,
                CanOperate = canOperate,
                ErrorMessage = null
            };
        }

        /// <summary>
        /// Get permission info for Case (batch-optimized for list APIs)
        /// </summary>
        public async Task<PermissionInfoDto> GetCasePermissionInfoForListAsync(
            long userId, 
            long caseId, 
            bool hasViewModulePermission, 
            bool hasOperateModulePermission)
        {
            // System Admin bypass
            if (_userContext?.IsSystemAdmin == true)
            {
                return new PermissionInfoDto 
                { 
                    CanView = true, 
                    CanOperate = true, 
                    ErrorMessage = null 
                };
            }

            // Check View permission (module permission already checked by caller)
            if (!hasViewModulePermission)
            {
                return new PermissionInfoDto
                {
                    CanView = false,
                    CanOperate = false,
                    ErrorMessage = $"User does not have required module permission: {PermissionConsts.Case.Read}"
                };
            }

            // Load case entity
            var onboarding = await _onboardingRepository.GetByIdAsync(caseId);
            if (onboarding == null)
            {
                return new PermissionInfoDto
                {
                    CanView = false,
                    CanOperate = false,
                    ErrorMessage = $"Case {caseId} not found"
                };
            }

            // Check entity-level view permission (includes workflow/stage inheritance check)
            var viewResult = CheckCasePermission(onboarding, userId, PermissionOperationType.View);
            if (!viewResult.Success)
            {
                return new PermissionInfoDto
                {
                    CanView = false,
                    CanOperate = false,
                    ErrorMessage = viewResult.ErrorMessage ?? "User is not allowed to view this case"
                };
            }

            // Check Operate permission (module permission already checked by caller)
            bool canOperate = false;
            if (hasOperateModulePermission)
            {
                // Check entity-level operate permission (includes workflow/stage inheritance check)
                var operateResult = CheckCasePermission(onboarding, userId, PermissionOperationType.Operate);
                canOperate = operateResult.Success;
            }

            return new PermissionInfoDto
            {
                CanView = true,
                CanOperate = canOperate,
                ErrorMessage = null
            };
        }

        #endregion
    }
}

