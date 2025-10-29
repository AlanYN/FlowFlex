using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.Permission;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Services.OW.Permission;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Const;
using FlowFlex.Domain.Shared.Enums.Permission;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Models.Permission;
using Item.ThirdParty.IdentityHub;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PermissionOperationType = FlowFlex.Domain.Shared.Enums.Permission.OperationTypeEnum;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Main permission service - Coordinator pattern - STRICT MODE (Scheme 1)
    /// 
    /// STRICT MODE RULES:
    /// 1. No Workflow permission → Cannot view/modify Stage or Case
    /// 2. No Stage permission → Cannot view/modify Case in that Stage
    /// 3. Final permission = Workflow ∩ Stage ∩ Case (intersection of all three levels)
    /// 
    /// Delegates permission checks to specialized services:
    /// - WorkflowPermissionService: Workflow permission checks
    /// - StagePermissionService: Stage permission checks (requires Workflow permission)
    /// - CasePermissionService: Case permission checks (requires Workflow ∩ Stage permission)
    /// - PermissionHelpers: Common utility methods
    /// 
    /// Special Cases:
    /// - Admin (System/Tenant): Bypasses all permission checks
    /// - Owner: Bypasses all permission checks for owned resources
    /// - Assigned User: Special privilege for assigned Stages
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

        // Specialized permission services
        private readonly PermissionHelpers _helpers;
        private readonly WorkflowPermissionService _workflowPermissionService;
        private readonly StagePermissionService _stagePermissionService;
        private readonly CasePermissionService _casePermissionService;

        public PermissionService(
            ILogger<PermissionService> logger,
            UserContext userContext,
            IWorkflowRepository workflowRepository,
            IStageRepository stageRepository,
            IOnboardingRepository onboardingRepository,
            IdentityHubClient identityHubClient,
            IHttpContextAccessor httpContextAccessor,
            PermissionHelpers helpers,
            WorkflowPermissionService workflowPermissionService,
            StagePermissionService stagePermissionService,
            CasePermissionService casePermissionService)
        {
            _logger = logger;
            _userContext = userContext;
            _workflowRepository = workflowRepository;
            _stageRepository = stageRepository;
            _onboardingRepository = onboardingRepository;
            _identityHubClient = identityHubClient;
            _httpContextAccessor = httpContextAccessor;
            _helpers = helpers;
            _workflowPermissionService = workflowPermissionService;
            _stagePermissionService = stagePermissionService;
            _casePermissionService = casePermissionService;
        }

        #endregion

        #region Public API Methods

        /// <summary>
        /// Check resource permission (unified interface for HTTP API)
        /// Checks both View and Operate permissions for the specified resource
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
                        _logger.LogWarning("Unsupported resource type: {ResourceType}", resourceType);
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
                    _logger.LogInformation("CheckResourcePermissionAsync - View permission check failed, skipping Operate check");
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
                        operateResult = PermissionResult.CreateFailure("Unsupported resource type", "UNSUPPORTED_RESOURCE_TYPE");
                        break;
                }

                // Build response with both View and Operate results
                var response = new CheckPermissionResponse
                {
                    CanView = viewResult.Success,
                    CanOperate = operateResult.Success,
                    GrantReason = operateResult.Success ? operateResult.GrantReason : viewResult.GrantReason,
                    ErrorMessage = null
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

        #region Workflow Permission Methods

        /// <summary>
        /// Check Workflow access permission
        /// Delegates to WorkflowPermissionService
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
                // Step 0: Admin bypass
                if (_helpers.IsSystemAdmin())
                {
                    _logger.LogInformation("User {UserId} is System Admin, bypassing all permission checks", userId);
                    return PermissionResult.CreateSuccess(true, true, "SystemAdmin");
                }

                if (_helpers.IsTenantAdmin())
                {
                    _logger.LogInformation("User {UserId} is Tenant Admin, bypassing all permission checks", userId);
                    return PermissionResult.CreateSuccess(true, true, "TenantAdmin");
                }

                // Step 1: Validate input
                if (userId <= 0 || workflowId <= 0)
                {
                    return PermissionResult.CreateFailure(
                        PermissionErrorMessages.FormatMessage(PermissionErrorMessages.InvalidInput, "workflow"),
                        PermissionErrorCodes.InvalidInput);
                }

                // Step 2: Check module-level permission
                var modulePermissionResult = await CheckModulePermissionAsync(
                    userId,
                    PermissionEntityTypeEnum.Workflow,
                    operationType);

                if (!modulePermissionResult.Success)
                {
                    _logger.LogWarning("Module permission check failed for user {UserId} - {ErrorMessage}",
                        userId, modulePermissionResult.ErrorMessage);
                    return modulePermissionResult;
                }

                // Step 3: Load Workflow entity
                var workflow = await _workflowRepository.GetByIdAsync(workflowId);
                if (workflow == null)
                {
                    return PermissionResult.CreateFailure(
                        PermissionErrorMessages.FormatMessage(PermissionErrorMessages.ResourceNotFound, "Workflow"),
                        PermissionErrorCodes.WorkflowNotFound);
                }

                // Step 4: Delegate to WorkflowPermissionService
                var permissionCheck = _workflowPermissionService.CheckWorkflowPermission(workflow, userId, operationType);
                
                if (permissionCheck.Success)
                {
                    _logger.LogInformation("Workflow permission check passed for user {UserId}, Reason: {Reason}",
                        userId, permissionCheck.GrantReason);
                }
                else
                {
                    _logger.LogWarning("Workflow permission check failed for user {UserId}, Reason: {Reason}",
                        userId, permissionCheck.ErrorMessage);
                }

                return permissionCheck;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Workflow access");
                return PermissionResult.CreateFailure("Internal error during permission check", "PERMISSION_CHECK_ERROR");
            }
        }

        /// <summary>
        /// Get permission info for Workflow (optimized for DTO population)
        /// </summary>
        public async Task<PermissionInfoDto> GetWorkflowPermissionInfoAsync(long userId, long workflowId)
        {
            // Admin bypass
            var adminResult = CheckAdminBypass();
            if (adminResult != null) return adminResult;

            // Check view permission
            var viewResult = await CheckWorkflowAccessAsync(userId, workflowId, PermissionOperationType.View);
            
            if (!viewResult.CanView)
            {
                return new PermissionInfoDto
                {
                    CanView = false,
                    CanOperate = false,
                    ErrorMessage = viewResult.ErrorMessage
                };
            }

            // Check operate permission
            var operateResult = await CheckWorkflowAccessAsync(userId, workflowId, PermissionOperationType.Operate);
            
            return new PermissionInfoDto
            {
                CanView = true,
                CanOperate = operateResult.CanOperate,
                ErrorMessage = null
            };
        }

        /// <summary>
        /// Check Workflow view permission at entity-level ONLY (for batch operations)
        /// </summary>
        public async Task<bool> CheckWorkflowViewPermissionAsync(
            long userId,
            long workflowId,
            Workflow workflow = null)
        {
            return await _workflowPermissionService.CheckWorkflowViewPermissionAsync(userId, workflowId, workflow, null);
        }

        /// <summary>
        /// Check Workflow view permission with pre-fetched user teams (performance-optimized)
        /// </summary>
        public async Task<bool> CheckWorkflowViewPermissionAsync(
            long userId,
            long workflowId,
            Workflow workflow,
            List<string> userTeamIds)
        {
            return await _workflowPermissionService.CheckWorkflowViewPermissionAsync(userId, workflowId, workflow, userTeamIds);
        }

        /// <summary>
        /// Get permission info for Workflow (batch-optimized for list APIs)
        /// </summary>
        public async Task<PermissionInfoDto> GetWorkflowPermissionInfoForListAsync(
            long userId, 
            long workflowId, 
            bool hasViewModulePermission, 
            bool hasOperateModulePermission)
        {
            return await _workflowPermissionService.GetWorkflowPermissionInfoForListAsync(
                userId, workflowId, hasViewModulePermission, hasOperateModulePermission);
        }

        #endregion

        #region Stage Permission Methods

        /// <summary>
        /// Check Stage access permission
        /// Delegates to StagePermissionService
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
                // Step 0: Admin bypass
                if (_helpers.IsSystemAdmin())
                {
                    _logger.LogInformation("User {UserId} is System Admin, bypassing all permission checks", userId);
                    return PermissionResult.CreateSuccess(true, true, "SystemAdmin");
                }

                if (_helpers.IsTenantAdmin())
                {
                    _logger.LogInformation("User {UserId} is Tenant Admin, bypassing all permission checks", userId);
                    return PermissionResult.CreateSuccess(true, true, "TenantAdmin");
                }

                // Step 1: Validate input
                if (userId <= 0 || stageId <= 0)
                {
                    return PermissionResult.CreateFailure(
                        PermissionErrorMessages.FormatMessage(PermissionErrorMessages.InvalidInput, "stage"),
                        PermissionErrorCodes.InvalidInput);
                }

                // Step 2: Check module-level permission
                var modulePermissionResult = await CheckModulePermissionAsync(
                    userId,
                    PermissionEntityTypeEnum.Stage,
                    operationType);

                if (!modulePermissionResult.Success)
                {
                    _logger.LogWarning("Module permission check failed for user {UserId} - {ErrorMessage}",
                        userId, modulePermissionResult.ErrorMessage);
                    return modulePermissionResult;
                }

                // Step 3: Load Stage entity
                var stage = await _stageRepository.GetByIdAsync(stageId);
                if (stage == null)
                {
                    return PermissionResult.CreateFailure(
                        PermissionErrorMessages.FormatMessage(PermissionErrorMessages.ResourceNotFound, "Stage"),
                        PermissionErrorCodes.StageNotFound);
                }

                // Step 4: Load parent Workflow entity
                var workflow = await _workflowRepository.GetByIdAsync(stage.WorkflowId);
                if (workflow == null)
                {
                    return PermissionResult.CreateFailure(
                        PermissionErrorMessages.FormatMessage(PermissionErrorMessages.ResourceNotFound, "Parent workflow"),
                        PermissionErrorCodes.WorkflowNotFound);
                }

                // Step 5: Check if user is assigned to this stage
                if (_stagePermissionService.CheckAssignedUser(stage, userId))
                {
                    _logger.LogInformation("User {UserId} is assigned to Stage {StageId}, granting access", userId, stageId);
                    return PermissionResult.CreateSuccess(true, true, "AssignedTo");
                }

                // Step 6: Delegate to StagePermissionService
                var permissionCheck = _stagePermissionService.CheckStagePermission(stage, workflow, userId, operationType);
                
                if (permissionCheck.Success)
                {
                    _logger.LogInformation("Stage permission check passed for user {UserId}, Reason: {Reason}",
                        userId, permissionCheck.GrantReason);
                }
                else
                {
                    _logger.LogWarning("Stage permission check failed for user {UserId}, Reason: {Reason}",
                        userId, permissionCheck.ErrorMessage);
                }

                return permissionCheck;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Stage access");
                return PermissionResult.CreateFailure("Internal error during permission check", "PERMISSION_CHECK_ERROR");
            }
        }

        /// <summary>
        /// Get permission info for Stage (optimized for DTO population)
        /// </summary>
        public async Task<PermissionInfoDto> GetStagePermissionInfoAsync(long userId, long stageId)
        {
            // Admin bypass
            var adminResult = CheckAdminBypass();
            if (adminResult != null) return adminResult;

            // Check view permission
            var viewResult = await CheckStageAccessAsync(userId, stageId, PermissionOperationType.View);
            
            if (!viewResult.CanView)
            {
                return new PermissionInfoDto
                {
                    CanView = false,
                    CanOperate = false,
                    ErrorMessage = viewResult.ErrorMessage
                };
            }

            // Check operate permission
            var operateResult = await CheckStageAccessAsync(userId, stageId, PermissionOperationType.Operate);
            
            return new PermissionInfoDto
            {
                CanView = true,
                CanOperate = operateResult.CanOperate,
                ErrorMessage = null
            };
        }

        /// <summary>
        /// Check Stage permission (public for direct access by services)
        /// </summary>
        public PermissionResult CheckStagePermission(
            Stage stage,
            Workflow workflow,
            long userId,
            PermissionOperationType operationType)
        {
            return _stagePermissionService.CheckStagePermission(stage, workflow, userId, operationType);
        }

        /// <summary>
        /// Get permission info for Stage (batch-optimized for list APIs)
        /// </summary>
        public async Task<PermissionInfoDto> GetStagePermissionInfoForListAsync(
            long userId, 
            long stageId, 
            bool hasViewModulePermission, 
            bool hasOperateModulePermission)
        {
            return await _stagePermissionService.GetStagePermissionInfoForListAsync(
                userId, stageId, hasViewModulePermission, hasOperateModulePermission);
        }

        /// <summary>
        /// Get permission info for Stage (ultra-optimized with pre-loaded entities)
        /// </summary>
        public PermissionInfoDto GetStagePermissionInfoForList(
            Stage stage,
            Workflow workflow,
            long userId,
            bool hasViewModulePermission,
            bool hasOperateModulePermission)
        {
            return _stagePermissionService.GetStagePermissionInfoForList(
                stage, workflow, userId, hasViewModulePermission, hasOperateModulePermission, null);
        }

        /// <summary>
        /// Get permission info for Stage (ultra-optimized with pre-fetched user teams)
        /// </summary>
        public PermissionInfoDto GetStagePermissionInfoForList(
            Stage stage,
            Workflow workflow,
            long userId,
            bool hasViewModulePermission,
            bool hasOperateModulePermission,
            List<string> userTeamIds)
        {
            return _stagePermissionService.GetStagePermissionInfoForList(
                stage, workflow, userId, hasViewModulePermission, hasOperateModulePermission, userTeamIds);
        }

        #endregion

        #region Case Permission Methods

        /// <summary>
        /// Check Case access permission
        /// Delegates to CasePermissionService
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
                // Step 0: Admin bypass
                if (_helpers.IsSystemAdmin())
                {
                    _logger.LogInformation("User {UserId} is System Admin, bypassing all permission checks", userId);
                    return PermissionResult.CreateSuccess(true, true, "SystemAdmin");
                }

                if (_helpers.IsTenantAdmin())
                {
                    _logger.LogInformation("User {UserId} is Tenant Admin, bypassing all permission checks", userId);
                    return PermissionResult.CreateSuccess(true, true, "TenantAdmin");
                }

                // Step 1: Validate input
                if (userId <= 0 || caseId <= 0)
                {
                    return PermissionResult.CreateFailure(
                        PermissionErrorMessages.FormatMessage(PermissionErrorMessages.InvalidInput, "case"),
                        PermissionErrorCodes.InvalidInput);
                }

                // Step 2: Check module-level permission
                var modulePermissionResult = await CheckModulePermissionAsync(
                    userId,
                    PermissionEntityTypeEnum.Case,
                    operationType);

                if (!modulePermissionResult.Success)
                {
                    _logger.LogWarning("Module permission check failed for user {UserId} - {ErrorMessage}",
                        userId, modulePermissionResult.ErrorMessage);
                    return modulePermissionResult;
                }

                // Step 3: Load Case entity
                var onboarding = await _onboardingRepository.GetByIdAsync(caseId);
                if (onboarding == null)
                {
                    return PermissionResult.CreateFailure(
                        PermissionErrorMessages.FormatMessage(PermissionErrorMessages.ResourceNotFound, "Case"),
                        PermissionErrorCodes.CaseNotFound);
                }

                // Step 4: Delegate to CasePermissionService
                var permissionCheck = await _casePermissionService.CheckCasePermissionAsync(onboarding, userId, operationType);
                
                if (permissionCheck.Success)
                {
                    _logger.LogInformation("Case permission check passed for user {UserId}, Reason: {Reason}",
                        userId, permissionCheck.GrantReason);
                }
                else
                {
                    _logger.LogWarning("Case permission check failed for user {UserId}, Reason: {Reason}",
                        userId, permissionCheck.ErrorMessage);
                }

                return permissionCheck;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Case access");
                return PermissionResult.CreateFailure("Internal error during permission check", "PERMISSION_CHECK_ERROR");
            }
        }

        /// <summary>
        /// Get permission info for Case (optimized for DTO population)
        /// </summary>
        public async Task<PermissionInfoDto> GetCasePermissionInfoAsync(long userId, long caseId)
        {
            // Admin bypass
            var adminResult = CheckAdminBypass();
            if (adminResult != null) return adminResult;

            // Check view permission
            var viewResult = await CheckCaseAccessAsync(userId, caseId, PermissionOperationType.View);
            
            if (!viewResult.CanView)
            {
                return new PermissionInfoDto
                {
                    CanView = false,
                    CanOperate = false,
                    ErrorMessage = viewResult.ErrorMessage
                };
            }

            // Check operate permission
            var operateResult = await CheckCaseAccessAsync(userId, caseId, PermissionOperationType.Operate);
            
            return new PermissionInfoDto
            {
                CanView = true,
                CanOperate = operateResult.CanOperate,
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
            return await _casePermissionService.GetCasePermissionInfoForListAsync(
                userId, caseId, hasViewModulePermission, hasOperateModulePermission);
        }

        #endregion

        #region Module Permission Methods

        /// <summary>
        /// Check module-level permission via IdentityHub
        /// </summary>
        private async Task<PermissionResult> CheckModulePermissionAsync(
            long userId,
            PermissionEntityTypeEnum entityType,
            PermissionOperationType operationType)
        {
            // Check if this is a Portal token accessing a [PortalAccess] endpoint
            if (_helpers.IsPortalTokenWithPortalAccess())
            {
                _logger.LogInformation("Portal token detected - bypassing module permission check for user {UserId}", userId);
                return PermissionResult.CreateSuccess(true, false, "PortalAccess");
            }

            // If no IAM token, deny access
            if (_userContext?.IamToken == null)
            {
                _logger.LogWarning("Module permission check failed: No IAM token available for user {UserId}", userId);
                return PermissionResult.CreateFailure(
                    PermissionErrorMessages.NoIamToken,
                    PermissionErrorCodes.NoIamToken);
            }

            // Map entity type and operation to module permission code
            var permissionCode = GetModulePermissionCode(entityType, operationType);
            if (string.IsNullOrEmpty(permissionCode))
            {
                _logger.LogWarning("Module permission check failed: Unsupported entity type {EntityType} or operation {Operation}",
                    entityType, operationType);
                return PermissionResult.CreateFailure(
                    PermissionErrorMessages.UnsupportedOperation,
                    PermissionErrorCodes.UnsupportedPermission);
            }

            _logger.LogInformation("Checking module permission - UserId: {UserId}, Permission: {Permission}", userId, permissionCode);

            try
            {
                var hasPermission = await _identityHubClient.UserRolePermissionCheck(
                    _userContext.IamToken,
                    new List<string> { permissionCode });

                if (!hasPermission)
                {
                    _logger.LogWarning("Module permission check failed: User {UserId} does not have permission {Permission}",
                        userId, permissionCode);
                    return PermissionResult.CreateFailure(
                        $"User does not have required module permission: {permissionCode}",
                        "MODULE_PERMISSION_DENIED");
                }

                _logger.LogInformation("Module permission check passed: User {UserId} has permission {Permission}", userId, permissionCode);
                return PermissionResult.CreateSuccess(true, false, "ModulePermission");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking module permission for user {UserId}, permission {Permission}", userId, permissionCode);
                return PermissionResult.CreateFailure("Internal error during module permission check", "MODULE_PERMISSION_CHECK_ERROR");
            }
        }

        /// <summary>
        /// Get module permission code from PermissionConsts
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
        /// Check user's group permission (module-level permission check)
        /// Used for batch permission checking in list operations
        /// </summary>
        public async Task<bool> CheckGroupPermissionAsync(long userId, string permission)
        {
            _logger.LogDebug("CheckGroupPermissionAsync - UserId: {UserId}, Permission: {Permission}", userId, permission);

            // System Admin has all permissions
            if (_userContext?.IsSystemAdmin == true)
            {
                _logger.LogInformation("User {UserId} is System Admin, granting permission {Permission}", userId, permission);
                return true;
            }

            // If no IAM token, deny access
            if (_userContext?.IamToken == null)
            {
                _logger.LogWarning("Module permission check failed: No IAM token available for user {UserId}", userId);
                return false;
            }

            try
            {
                var hasPermission = await _identityHubClient.UserRolePermissionCheck(
                    _userContext.IamToken,
                    new List<string> { permission });

                if (hasPermission)
                {
                    _logger.LogDebug("Module permission check passed: User {UserId} has permission {Permission}", userId, permission);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Module permission check failed: User {UserId} does not have permission {Permission}", userId, permission);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking group permission for user {UserId}, permission {Permission}", userId, permission);
                return false;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get user team IDs (delegates to PermissionHelpers)
        /// </summary>
        public List<string> GetUserTeamIds()
        {
            return _helpers.GetUserTeamIds();
        }

        /// <summary>
        /// Check if user has admin bypass privileges
        /// Returns PermissionInfoDto with full permissions if admin, null otherwise
        /// </summary>
        private PermissionInfoDto CheckAdminBypass()
        {
            if (_helpers.HasAdminPrivileges())
            {
                return new PermissionInfoDto
                {
                    CanView = true,
                    CanOperate = true,
                    ErrorMessage = null
                };
            }

            return null;
        }

        #endregion
    }
}
