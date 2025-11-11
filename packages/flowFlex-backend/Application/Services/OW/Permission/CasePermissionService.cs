using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.Permission;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Enums.Permission;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Models.Permission;
using Microsoft.Extensions.Logging;
using PermissionOperationType = FlowFlex.Domain.Shared.Enums.Permission.OperationTypeEnum;

namespace FlowFlex.Application.Services.OW.Permission
{
    /// <summary>
    /// Case permission verification service
    /// Handles all Case-specific permission checks
    /// In Public mode, Case inherits Workflow permissions
    /// </summary>
    public class CasePermissionService : IScopedService
    {
        private readonly ILogger<CasePermissionService> _logger;
        private readonly UserContext _userContext;
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly PermissionHelpers _helpers;
        private readonly WorkflowPermissionService _workflowPermissionService;

        public CasePermissionService(
            ILogger<CasePermissionService> logger,
            UserContext userContext,
            IOnboardingRepository onboardingRepository,
            IWorkflowRepository workflowRepository,
            PermissionHelpers helpers,
            WorkflowPermissionService workflowPermissionService)
        {
            _logger = logger;
            _userContext = userContext;
            _onboardingRepository = onboardingRepository;
            _workflowRepository = workflowRepository;
            _helpers = helpers;
            _workflowPermissionService = workflowPermissionService;
        }

        #region Main Permission Check

        /// <summary>
        /// Check Case permission
        /// In Public mode, Case inherits Workflow permissions
        /// Case has independent permission control including Ownership
        /// </summary>
        public async Task<PermissionResult> CheckCasePermissionAsync(
            Onboarding onboarding,
            long userId,
            PermissionOperationType operationType)
        {
            // Get user teams or user IDs based on PermissionSubjectType
            var userTeamIds = _helpers.GetUserTeamIds();
            var userIdString = userId.ToString();

            _logger.LogDebug(
                "CheckCasePermission - CaseId: {CaseId}, WorkflowId: {WorkflowId}, ViewMode: {ViewMode}, ViewSubjectType: {ViewSubjectType}, OperateSubjectType: {OperateSubjectType}, " +
                "ViewTeams: {ViewTeams}, ViewUsers: {ViewUsers}, OperateTeams: {OperateTeams}, OperateUsers: {OperateUsers}, " +
                "Ownership: {Ownership}, UserTeams: [{UserTeams}], UserId: {UserId}",
                onboarding.Id,
                onboarding.WorkflowId,
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

            // Step 0: Admin bypass (highest priority)
            if (_helpers.IsSystemAdmin())
            {
                _logger.LogInformation("User {UserId} is System Admin - granting full access to Case {CaseId}", userId, onboarding.Id);
                return PermissionResult.CreateSuccess(true, true, "SystemAdmin");
            }

            if (_helpers.IsTenantAdmin())
            {
                _logger.LogInformation("User {UserId} is Tenant Admin - granting full access to Case {CaseId}", userId, onboarding.Id);
                return PermissionResult.CreateSuccess(true, true, "TenantAdmin");
            }

            // Step 1: Check Ownership
            if (onboarding.Ownership.HasValue && onboarding.Ownership.Value == userId)
            {
                _logger.LogDebug("Case permission: User {UserId} is the owner - granting full access", userId);
                return PermissionResult.CreateSuccess(true, true, "Owner");
            }

            // Step 2: In Public mode, inherit Workflow permissions
            if (onboarding.ViewPermissionMode == ViewPermissionModeEnum.Public)
            {
                return await CheckCasePermissionWithWorkflowInheritanceAsync(onboarding, userId, operationType, userTeamIds, userIdString);
            }

            // Step 3: Check View Permission (non-Public modes)
            bool canView = CheckCaseViewPermission(onboarding, userTeamIds, userIdString);

            if (!canView)
            {
                return PermissionResult.CreateFailure(
                    "User does not have view permission for this case",
                    "VIEW_PERMISSION_DENIED");
            }

            // Step 4: Check Operate Permission (only if user can view)
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
                    // User has view permission but not operate permission
                    var result = PermissionResult.CreateFailure(
                        "User has view permission but not operate permission for this case",
                        "OPERATE_PERMISSION_DENIED");
                    result.CanView = true; // Set CanView to true since user can view
                    return result;
                }
            }

            // View operation
            return PermissionResult.CreateSuccess(true, false, "CaseViewPermission");
        }

        #endregion

        #region Workflow Inheritance (Public Mode)

        /// <summary>
        /// Check Case permission with Workflow inheritance (Public mode only)
        /// In Public mode, Case inherits Workflow's view and operate permissions
        /// </summary>
        private async Task<PermissionResult> CheckCasePermissionWithWorkflowInheritanceAsync(
            Onboarding onboarding,
            long userId,
            PermissionOperationType operationType,
            List<string> userTeamIds,
            string userIdString)
        {
            _logger.LogDebug(
                "CheckCasePermissionWithWorkflowInheritance - CaseId: {CaseId}, WorkflowId: {WorkflowId}, Operation: {Operation}",
                onboarding.Id,
                onboarding.WorkflowId,
                operationType);

            try
            {
                // Load parent Workflow (now properly async)
                var workflow = await _workflowRepository.GetByIdAsync(onboarding.WorkflowId);
                if (workflow == null)
                {
                    _logger.LogWarning(
                        "Workflow {WorkflowId} not found for Case {CaseId} - falling back to Case's own permissions",
                        onboarding.WorkflowId,
                        onboarding.Id);
                    return CheckCasePermissionFallback(onboarding, userTeamIds, userIdString, operationType);
                }

                _logger.LogDebug(
                    "Loaded Workflow {WorkflowId} - ViewMode: {ViewMode}, ViewTeams: {ViewTeams}, OperateTeams: {OperateTeams}",
                    workflow.Id,
                    workflow.ViewPermissionMode,
                    workflow.ViewTeams ?? "NULL",
                    workflow.OperateTeams ?? "NULL");

                // Step 1: Check Workflow View Permission
                bool workflowCanView = _workflowPermissionService.CheckViewPermission(workflow, userTeamIds);

                if (!workflowCanView)
                {
                    _logger.LogDebug(
                        "User {UserId} does not have view permission on parent Workflow {WorkflowId} - denying Case access",
                        userId,
                        workflow.Id);
                    return PermissionResult.CreateFailure(
                        "User does not have view permission on parent workflow",
                        "WORKFLOW_VIEW_PERMISSION_DENIED");
                }

                _logger.LogDebug(
                    "User {UserId} has view permission on parent Workflow {WorkflowId}",
                    userId,
                    workflow.Id);

                // Step 2: Check Workflow Operate Permission (if needed)
                if (operationType == PermissionOperationType.Operate ||
                    operationType == PermissionOperationType.Delete)
                {
                    bool workflowCanOperate = _workflowPermissionService.CheckOperatePermission(workflow, userTeamIds);

                    if (workflowCanOperate)
                    {
                        _logger.LogDebug(
                            "User {UserId} has operate permission on parent Workflow {WorkflowId} - granting Case operate permission",
                            userId,
                            workflow.Id);
                        return PermissionResult.CreateSuccess(true, true, "WorkflowInheritedOperatePermission");
                    }
                    else
                    {
                        _logger.LogDebug(
                            "User {UserId} does not have operate permission on parent Workflow {WorkflowId} - denying Case operate permission",
                            userId,
                            workflow.Id);
                        // User has view permission but not operate permission
                        var result = PermissionResult.CreateFailure(
                            "User has view permission but not operate permission on parent workflow",
                            "WORKFLOW_OPERATE_PERMISSION_DENIED");
                        result.CanView = true; // Set CanView to true since user can view
                        return result;
                    }
                }

                // View operation - user has Workflow view permission
                _logger.LogDebug(
                    "User {UserId} has view permission on parent Workflow {WorkflowId} - granting Case view permission",
                    userId,
                    workflow.Id);
                return PermissionResult.CreateSuccess(true, false, "WorkflowInheritedViewPermission");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error checking Workflow inheritance for Case {CaseId}, WorkflowId {WorkflowId} - falling back to Case's own permissions",
                    onboarding.Id,
                    onboarding.WorkflowId);
                return CheckCasePermissionFallback(onboarding, userTeamIds, userIdString, operationType);
            }
        }

        /// <summary>
        /// Fallback to Case's own permissions when Workflow inheritance fails
        /// </summary>
        private PermissionResult CheckCasePermissionFallback(
            Onboarding onboarding,
            List<string> userTeamIds,
            string userIdString,
            PermissionOperationType operationType)
        {
            // Check View Permission
            bool canView = CheckCaseViewPermission(onboarding, userTeamIds, userIdString);

            if (!canView)
            {
                return PermissionResult.CreateFailure(
                    "User does not have view permission for this case",
                    "VIEW_PERMISSION_DENIED");
            }

            // Check Operate Permission (only if user can view)
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
                    // User has view permission but not operate permission
                    var result = PermissionResult.CreateFailure(
                        "User has view permission but not operate permission for this case",
                        "OPERATE_PERMISSION_DENIED");
                    result.CanView = true; // Set CanView to true since user can view
                    return result;
                }
            }

            // View operation
            return PermissionResult.CreateSuccess(true, false, "CaseViewPermission");
        }

        #endregion

        #region View Permission

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
                        ? _helpers.CheckTeamWhitelist(onboarding.ViewTeams, userTeamIds)
                        : _helpers.CheckUserWhitelist(onboarding.ViewUsers, userId),

                ViewPermissionModeEnum.InvisibleToTeams =>
                    onboarding.ViewPermissionSubjectType == PermissionSubjectTypeEnum.Team
                        ? _helpers.CheckTeamBlacklist(onboarding.ViewTeams, userTeamIds)
                        : _helpers.CheckUserBlacklist(onboarding.ViewUsers, userId),

                ViewPermissionModeEnum.Private => false, // Owner check is handled in CheckCasePermission

                _ => false
            };
        }

        #endregion

        #region Operate Permission

        /// <summary>
        /// Check Case operate permission
        /// IMPORTANT: Operate permission is ALWAYS whitelist-based, regardless of ViewPermissionMode
        /// ViewPermissionMode (blacklist/whitelist) only affects View permissions
        /// </summary>
        private bool CheckCaseOperatePermission(Onboarding onboarding, List<string> userTeamIds, string userId)
        {
            _logger.LogDebug(
                "CheckCaseOperatePermission - CaseId: {CaseId}, OperateSubjectType: {OperateSubjectType}",
                onboarding.Id,
                onboarding.OperatePermissionSubjectType);

            // Team-based permission - ALWAYS whitelist
            if (onboarding.OperatePermissionSubjectType == PermissionSubjectTypeEnum.Team)
            {
                // Special handling for Public mode: empty OperateTeams means everyone can operate
                if (onboarding.ViewPermissionMode == ViewPermissionModeEnum.Public)
                {
                    return _helpers.CheckOperateTeamsPublicMode(onboarding.OperateTeams, userTeamIds);
                }

                // For all other modes: OperateTeams is whitelist
                return _helpers.CheckTeamWhitelist(onboarding.OperateTeams, userTeamIds);
            }

            // User-based permission - ALWAYS whitelist
            // Special handling for Public mode: empty OperateUsers means everyone can operate
            if (onboarding.ViewPermissionMode == ViewPermissionModeEnum.Public)
            {
                return _helpers.CheckOperateUsersPublicMode(onboarding.OperateUsers, userId);
            }

            // For all other modes: OperateUsers is whitelist
            return _helpers.CheckUserWhitelist(onboarding.OperateUsers, userId);
        }

        #endregion

        #region Batch Permission Checking

        /// <summary>
        /// Check case permissions in batch for multiple cases
        /// Optimized for list APIs to avoid N+1 queries
        /// </summary>
        public async Task<Dictionary<long, PermissionInfoDto>> CheckBatchCasePermissionsAsync(
            List<Onboarding> entities,
            long userId,
            bool hasViewModulePermission,
            bool hasOperateModulePermission)
        {
            var permissions = new Dictionary<long, PermissionInfoDto>();

            if (entities == null || !entities.Any())
            {
                return permissions;
            }

            _logger.LogDebug(
                "CheckBatchCasePermissions - Checking permissions for {Count} cases, User: {UserId}",
                entities.Count,
                userId);

            // Fast path: Admin bypass
            if (_helpers.HasAdminPrivileges())
            {
                _logger.LogDebug("User {UserId} has admin privileges - granting full access to all cases", userId);
                foreach (var entity in entities)
                {
                    permissions[entity.Id] = new PermissionInfoDto
                    {
                        CanView = true,
                        CanOperate = true,
                        ErrorMessage = null
                    };
                }
                return permissions;
            }

            // Check each case's permissions
            foreach (var entity in entities)
            {
                // Check View permission
                if (!hasViewModulePermission)
                {
                    permissions[entity.Id] = new PermissionInfoDto
                    {
                        CanView = false,
                        CanOperate = false,
                        ErrorMessage = "User does not have required module permission: CASE:READ"
                    };
                    continue;
                }

                var viewResult = await CheckCasePermissionAsync(entity, userId, PermissionOperationType.View);

                // If user cannot view, deny all access
                if (!viewResult.Success || !viewResult.CanView)
                {
                    permissions[entity.Id] = new PermissionInfoDto
                    {
                        CanView = false,
                        CanOperate = false,
                        ErrorMessage = viewResult.ErrorMessage ?? "User is not allowed to view this case"
                    };
                    continue;
                }

                // Check Operate permission (only if user can view and has module permission)
                bool canOperate = false;
                if (hasOperateModulePermission)
                {
                    var operateResult = await CheckCasePermissionAsync(entity, userId, PermissionOperationType.Operate);
                    canOperate = operateResult.Success && operateResult.CanOperate;
                }

                permissions[entity.Id] = new PermissionInfoDto
                {
                    CanView = true,
                    CanOperate = canOperate,
                    ErrorMessage = null
                };
            }

            _logger.LogDebug(
                "CheckBatchCasePermissions - Completed checking {Count} cases",
                permissions.Count);

            return permissions;
        }

        #endregion

        #region Optimized Methods for List APIs

        /// <summary>
        /// Get permission info for Case (batch-optimized for list APIs)
        /// </summary>
        public async Task<PermissionInfoDto> GetCasePermissionInfoForListAsync(
            long userId,
            long caseId,
            bool hasViewModulePermission,
            bool hasOperateModulePermission)
        {
            // Fast path: Admin bypass
            if (_helpers.HasAdminPrivileges())
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
                    ErrorMessage = "User does not have required module permission: CASE:READ"
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
            var viewResult = await CheckCasePermissionAsync(onboarding, userId, PermissionOperationType.View);
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
                var operateResult = await CheckCasePermissionAsync(onboarding, userId, PermissionOperationType.Operate);
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

