using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.Permission;
using FlowFlex.Application.Services.Shared;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Models.Permission;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PermissionOperationType = FlowFlex.Domain.Shared.Enums.Permission.OperationTypeEnum;

namespace FlowFlex.Application.Services.OW.Permission
{
    /// <summary>
    /// Stage permission verification service - STRICT MODE (Scheme 1)
    /// Handles all Stage-specific permission checks
    /// 
    /// STRICT MODE: Stage permission is the SECOND layer of permission control.
    /// User must have BOTH Workflow AND Stage permission to access the Stage.
    /// Stage permission = Workflow ∩ Stage (intersection)
    /// 
    /// Stage can inherit (NULL) or narrow (subset) Workflow permissions.
    /// </summary>
    public class StagePermissionService : IScopedService
    {
        private readonly ILogger<StagePermissionService> _logger;
        private readonly UserContext _userContext;
        private readonly IStageRepository _stageRepository;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly PermissionHelpers _helpers;
        private readonly WorkflowPermissionService _workflowPermissionService;

        public StagePermissionService(
            ILogger<StagePermissionService> logger,
            UserContext userContext,
            IStageRepository stageRepository,
            IWorkflowRepository workflowRepository,
            PermissionHelpers helpers,
            WorkflowPermissionService workflowPermissionService)
        {
            _logger = logger;
            _userContext = userContext;
            _stageRepository = stageRepository;
            _workflowRepository = workflowRepository;
            _helpers = helpers;
            _workflowPermissionService = workflowPermissionService;
        }

        #region Main Permission Check

        /// <summary>
        /// Check Stage permission based on inheritance or narrowing from Workflow
        /// Stage can inherit (NULL) or narrow (subset) Workflow permissions
        /// </summary>
        public PermissionResult CheckStagePermission(
            Stage stage,
            Workflow workflow,
            long userId,
            PermissionOperationType operationType)
        {
            return CheckStagePermission(stage, workflow, userId, operationType, null);
        }

        /// <summary>
        /// Check Stage permission with pre-fetched user teams (performance-optimized)
        /// </summary>
        public PermissionResult CheckStagePermission(
            Stage stage,
            Workflow workflow,
            long userId,
            PermissionOperationType operationType,
            List<string> userTeamIds)
        {
            // PERFORMANCE OPTIMIZATION: Use pre-fetched user teams if provided
            userTeamIds ??= _helpers.GetUserTeamIds();

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
            bool hasWorkflowViewPermission = _workflowPermissionService.CheckViewPermission(workflow, userTeamIds);
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
            // Note: Delete operation also requires Operate permission
            bool canOperate = false;
            string operateReason = null;

            if (canView && (operationType == PermissionOperationType.Operate || operationType == PermissionOperationType.Delete))
            {
                // First, check Workflow operate permission (always required)
                bool hasWorkflowOperatePermission = _workflowPermissionService.CheckOperatePermission(workflow, userTeamIds);
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
                    // User has view permission but not operate permission
                    var result = PermissionResult.CreateFailure(
                        "User has view permission but not operate permission for this stage",
                        "OPERATE_PERMISSION_DENIED");
                    result.CanView = true; // Set CanView to true since user can view
                    return result;
                }
                else
                {
                    return PermissionResult.CreateFailure(
                        "User does not have permission for this stage",
                        "PERMISSION_DENIED");
                }
            }
            else if (operationType == PermissionOperationType.Delete)
            {
                // Delete requires Operate permission (delete is a form of operation)
                if (canOperate)
                {
                    return PermissionResult.CreateSuccess(true, true, operateReason);
                }
                else if (canView)
                {
                    var result = PermissionResult.CreateFailure(
                        "User has view permission but not delete permission for this stage",
                        "DELETE_PERMISSION_DENIED");
                    result.CanView = true;
                    return result;
                }
                else
                {
                    return PermissionResult.CreateFailure(
                        "User does not have permission to delete this stage",
                        "PERMISSION_DENIED");
                }
            }

            return PermissionResult.CreateFailure(
                "Unsupported operation type",
                "UNSUPPORTED_OPERATION");
        }

        #endregion

        #region View Permission

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
                ViewPermissionModeEnum.VisibleToTeams => _helpers.CheckTeamWhitelist(stage.ViewTeams, userTeamIds),
                ViewPermissionModeEnum.InvisibleToTeams => _helpers.CheckTeamBlacklist(stage.ViewTeams, userTeamIds),
                ViewPermissionModeEnum.Private => _helpers.IsCurrentUserOwner(stage.CreateUserId),
                _ => false
            };
        }

        #endregion

        #region Operate Permission

        /// <summary>
        /// Check Stage's own operate permission (narrowed from Workflow)
        /// IMPORTANT: Operate permission is ALWAYS whitelist-based, regardless of ViewPermissionMode
        /// ViewPermissionMode (blacklist/whitelist) only affects View permissions
        /// </summary>
        private bool CheckStageOperatePermission(Stage stage, List<string> userTeamIds)
        {
            _logger.LogDebug(
                "CheckStageOperatePermission - StageId: {StageId}, ViewMode: {ViewMode}, OperateTeams: {OperateTeams}",
                stage.Id,
                stage.ViewPermissionMode,
                stage.OperateTeams ?? "NULL");

            // Special handling for Public mode: empty OperateTeams means everyone can operate
            if (stage.ViewPermissionMode == ViewPermissionModeEnum.Public)
            {
                return _helpers.CheckOperateTeamsPublicMode(stage.OperateTeams, userTeamIds);
            }

            // For all other modes: OperateTeams is ALWAYS whitelist
            return _helpers.CheckTeamWhitelist(stage.OperateTeams, userTeamIds);
        }

        #endregion

        #region Assigned User Check

        /// <summary>
        /// Check if user is assigned to this stage (special privilege)
        /// </summary>
        public bool CheckAssignedUser(Stage stage, long userId)
        {
            if (string.IsNullOrWhiteSpace(stage.DefaultAssignee))
            {
                _logger.LogDebug("Stage {StageId} has no DefaultAssignee configured", stage.Id);
                return false;
            }

            try
            {
                _logger.LogDebug(
                    "Checking DefaultAssignee for Stage {StageId} - Raw value: {DefaultAssignee}",
                    stage.Id,
                    stage.DefaultAssignee);

                // Reuse PermissionHelpers' robust JSON deserialization
                var assignedUserIds = _helpers.DeserializeTeamList(stage.DefaultAssignee);
                var currentUserIdString = _userContext?.UserId;

                _logger.LogDebug(
                    "Parsed {Count} assigned users for Stage {StageId}, current user: {UserId}",
                    assignedUserIds.Count,
                    stage.Id,
                    currentUserIdString);

                if (!string.IsNullOrEmpty(currentUserIdString) && assignedUserIds.Contains(currentUserIdString))
                {
                    _logger.LogInformation(
                        "User {UserId} is assigned to Stage {StageId}",
                        userId, stage.Id);
                    return true;
                }

                _logger.LogDebug(
                    "User {UserId} is not in the assigned users list for Stage {StageId}",
                    userId, stage.Id);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error checking assigned user for Stage {StageId}. DefaultAssignee value: {Value}",
                    stage.Id, stage.DefaultAssignee);
                return false;
            }
        }

        #endregion

        #region Optimized Methods for List APIs

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
            bool canOperate = false;
            if (hasOperateModulePermission)
            {
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
        /// Get permission info for Stage (ultra-optimized for list APIs with pre-loaded entities)
        /// Accepts entity objects to avoid database queries - SYNCHRONOUS method for performance
        /// </summary>
        public PermissionInfoDto GetStagePermissionInfoForList(
            Stage stage,
            Workflow workflow,
            long userId,
            bool hasViewModulePermission,
            bool hasOperateModulePermission)
        {
            return GetStagePermissionInfoForList(stage, workflow, userId, hasViewModulePermission, hasOperateModulePermission, null);
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

            // Check module permission first
            if (!hasViewModulePermission)
            {
                return new PermissionInfoDto
                {
                    CanView = false,
                    CanOperate = false,
                    ErrorMessage = "User does not have module permission to view stages"
                };
            }

            // Check entity-level view permission (includes workflow inheritance check)
            // PERFORMANCE OPTIMIZATION: Pass pre-fetched userTeamIds to avoid repeated calls
            var viewResult = CheckStagePermission(stage, workflow, userId, PermissionOperationType.View, userTeamIds);
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
            bool canOperate = false;
            if (hasOperateModulePermission)
            {
                var operateResult = CheckStagePermission(stage, workflow, userId, PermissionOperationType.Operate, userTeamIds);
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

        #region Three-Layer Permission Check (OW-736)

        /// <summary>
        /// Check Stage permission with three-layer intersection:
        ///   Layer 1: Workflow Runtime snapshot (Onboarding.MaxViewPermissionMode / MaxViewTeams)
        ///   Layer 2: Stage Effective Runtime (PermissionCalculator.ComputeStageEffectiveRuntime)
        ///   Layer 3: Case Stage permission (MaxStageViewTeams snapshot OR StageViewTeams independent)
        /// All three layers must pass for access to be granted.
        /// The existing two-parameter overloads are kept unchanged for backward compatibility.
        /// </summary>
        public PermissionResult CheckStagePermissionWithCaseStage(
            Stage stage,
            Workflow workflow,
            Onboarding onboarding,
            OnboardingStageProgress stageProgress,
            long userId,
            PermissionOperationType operationType,
            List<string> userTeamIds = null)
        {
            userTeamIds ??= _helpers.GetUserTeamIds();

            // ── Layer 1: Workflow Runtime snapshot ───────────────────────────────
            if (onboarding.MaxViewPermissionMode.HasValue)
            {
                var maxViewMode = onboarding.MaxViewPermissionMode.Value;
                var maxViewTeams = DeserializeJsonTeams(onboarding.MaxViewTeams);

                bool layer1ViewPass = maxViewMode switch
                {
                    ViewPermissionModeEnum.Public => true,
                    ViewPermissionModeEnum.VisibleToTeams => maxViewTeams.Count == 0
                        || userTeamIds.Any(t => maxViewTeams.Contains(t, StringComparer.OrdinalIgnoreCase)),
                    ViewPermissionModeEnum.InvisibleToTeams => !userTeamIds.Any(t => maxViewTeams.Contains(t, StringComparer.OrdinalIgnoreCase)),
                    ViewPermissionModeEnum.Private => false,
                    _ => false
                };

                if (!layer1ViewPass)
                {
                    _logger.LogDebug("CheckStagePermissionWithCaseStage - Layer 1 (Workflow snapshot) denied for Stage {StageId}", stage.Id);
                    return PermissionResult.CreateFailure(
                        "User does not have view permission on Workflow Runtime snapshot",
                        "WORKFLOW_RUNTIME_SNAPSHOT_DENIED");
                }

                if (operationType == PermissionOperationType.Operate || operationType == PermissionOperationType.Delete)
                {
                    var maxOperateTeams = DeserializeJsonTeams(onboarding.MaxOperateTeams);
                    bool layer1OperatePass = maxOperateTeams.Count == 0
                        || userTeamIds.Any(t => maxOperateTeams.Contains(t, StringComparer.OrdinalIgnoreCase));

                    if (!layer1OperatePass)
                    {
                        _logger.LogDebug("CheckStagePermissionWithCaseStage - Layer 1 operate (Workflow snapshot) denied for Stage {StageId}", stage.Id);
                        var res = PermissionResult.CreateFailure(
                            "User has view permission but not operate permission on Workflow Runtime snapshot",
                            "WORKFLOW_RUNTIME_SNAPSHOT_OPERATE_DENIED");
                        res.CanView = true;
                        return res;
                    }
                }
            }

            // ── Layer 2: Stage Effective Runtime ─────────────────────────────────
            WorkflowEffectiveRuntime wfRuntime;
            StageEffectiveRuntime stageRuntime;
            try
            {
                wfRuntime = PermissionCalculator.ComputeWorkflowEffectiveRuntime(workflow);
                stageRuntime = PermissionCalculator.ComputeStageEffectiveRuntime(stage, wfRuntime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CheckStagePermissionWithCaseStage - Failed to compute Stage runtime for Stage {StageId}", stage.Id);
                return PermissionResult.CreateFailure("Error computing Stage effective runtime", "STAGE_RUNTIME_ERROR");
            }

            bool layer2ViewPass = stageRuntime.ViewMode switch
            {
                ViewPermissionModeEnum.Public => true,
                ViewPermissionModeEnum.VisibleToTeams => stageRuntime.ViewTeams.Count == 0
                    || userTeamIds.Any(t => stageRuntime.ViewTeams.Contains(t, StringComparer.OrdinalIgnoreCase)),
                ViewPermissionModeEnum.InvisibleToTeams => !userTeamIds.Any(t => stageRuntime.ViewTeams.Contains(t, StringComparer.OrdinalIgnoreCase)),
                ViewPermissionModeEnum.Private => false,
                _ => false
            };

            if (!layer2ViewPass)
            {
                _logger.LogDebug("CheckStagePermissionWithCaseStage - Layer 2 (Stage runtime) view denied for Stage {StageId}", stage.Id);
                return PermissionResult.CreateFailure(
                    "User does not have view permission on Stage Effective Runtime",
                    "STAGE_RUNTIME_VIEW_DENIED");
            }

            bool layer2OperatePass = true;
            if (operationType == PermissionOperationType.Operate || operationType == PermissionOperationType.Delete)
            {
                layer2OperatePass = stageRuntime.OperateTeams.Count == 0
                    || userTeamIds.Any(t => stageRuntime.OperateTeams.Contains(t, StringComparer.OrdinalIgnoreCase));

                if (!layer2OperatePass)
                {
                    _logger.LogDebug("CheckStagePermissionWithCaseStage - Layer 2 (Stage runtime) operate denied for Stage {StageId}", stage.Id);
                    var res = PermissionResult.CreateFailure(
                        "User has view permission but not operate permission on Stage Effective Runtime",
                        "STAGE_RUNTIME_OPERATE_DENIED");
                    res.CanView = true;
                    return res;
                }
            }

            // ── Layer 3: Case Stage permission ────────────────────────────────────
            bool inheritFromWorkflowStage = stageProgress.StagePermissionInheritFromWorkflowStage != false; // null or true = inherit

            if (inheritFromWorkflowStage)
            {
                // Use MaxStageViewTeams snapshot
                var maxStageViewTeams = stageProgress.MaxStageViewTeams ?? new List<string>();
                var maxStageViewMode = stageProgress.MaxStageViewPermissionMode;

                bool layer3ViewPass = true;
                if (maxStageViewMode.HasValue && maxStageViewMode.Value != ViewPermissionModeEnum.Public)
                {
                    layer3ViewPass = maxStageViewMode.Value switch
                    {
                        ViewPermissionModeEnum.VisibleToTeams => maxStageViewTeams.Count == 0
                            || userTeamIds.Any(t => maxStageViewTeams.Contains(t, StringComparer.OrdinalIgnoreCase)),
                        ViewPermissionModeEnum.InvisibleToTeams => !userTeamIds.Any(t => maxStageViewTeams.Contains(t, StringComparer.OrdinalIgnoreCase)),
                        _ => true
                    };
                }

                if (!layer3ViewPass)
                {
                    _logger.LogDebug("CheckStagePermissionWithCaseStage - Layer 3 (Case stage snapshot) view denied for Stage {StageId}", stage.Id);
                    return PermissionResult.CreateFailure(
                        "User does not have view permission on Case Stage snapshot",
                        "CASE_STAGE_SNAPSHOT_VIEW_DENIED");
                }

                if (operationType == PermissionOperationType.Operate || operationType == PermissionOperationType.Delete)
                {
                    var maxStageOperateTeams = stageProgress.MaxStageOperateTeams ?? new List<string>();
                    bool layer3OperatePass = maxStageOperateTeams.Count == 0
                        || userTeamIds.Any(t => maxStageOperateTeams.Contains(t, StringComparer.OrdinalIgnoreCase));

                    if (!layer3OperatePass)
                    {
                        _logger.LogDebug("CheckStagePermissionWithCaseStage - Layer 3 (Case stage snapshot) operate denied for Stage {StageId}", stage.Id);
                        var res = PermissionResult.CreateFailure(
                            "User has view permission but not operate permission on Case Stage snapshot",
                            "CASE_STAGE_SNAPSHOT_OPERATE_DENIED");
                        res.CanView = true;
                        return res;
                    }
                }
            }
            else
            {
                // Use independent StageViewTeams / StageViewUsers
                var stageViewTeams = stageProgress.StageViewTeams ?? new List<string>();
                bool layer3ViewPass;

                if (stageProgress.StageViewPermissionSubjectType == Domain.Shared.Enums.OW.PermissionSubjectTypeEnum.User)
                {
                    var stageViewUsers = stageProgress.StageViewUsers ?? new List<string>();
                    layer3ViewPass = stageViewUsers.Count == 0
                        || stageViewUsers.Contains(userId.ToString(), StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    layer3ViewPass = stageViewTeams.Count == 0
                        || userTeamIds.Any(t => stageViewTeams.Contains(t, StringComparer.OrdinalIgnoreCase));
                }

                if (!layer3ViewPass)
                {
                    _logger.LogDebug("CheckStagePermissionWithCaseStage - Layer 3 (Case stage independent) view denied for Stage {StageId}", stage.Id);
                    return PermissionResult.CreateFailure(
                        "User does not have view permission on Case Stage independent configuration",
                        "CASE_STAGE_INDEPENDENT_VIEW_DENIED");
                }

                if (operationType == PermissionOperationType.Operate || operationType == PermissionOperationType.Delete)
                {
                    bool layer3OperatePass;
                    if (stageProgress.StageUseSameTeamForOperate)
                    {
                        // Reuse view result
                        layer3OperatePass = true; // already passed layer3 view
                    }
                    else if (stageProgress.StageOperatePermissionSubjectType == Domain.Shared.Enums.OW.PermissionSubjectTypeEnum.User)
                    {
                        var stageOperateUsers = stageProgress.StageOperateUsers ?? new List<string>();
                        layer3OperatePass = stageOperateUsers.Count == 0
                            || stageOperateUsers.Contains(userId.ToString(), StringComparer.OrdinalIgnoreCase);
                    }
                    else
                    {
                        var stageOperateTeams = stageProgress.StageOperateTeams ?? new List<string>();
                        layer3OperatePass = stageOperateTeams.Count == 0
                            || userTeamIds.Any(t => stageOperateTeams.Contains(t, StringComparer.OrdinalIgnoreCase));
                    }

                    if (!layer3OperatePass)
                    {
                        _logger.LogDebug("CheckStagePermissionWithCaseStage - Layer 3 (Case stage independent) operate denied for Stage {StageId}", stage.Id);
                        var res = PermissionResult.CreateFailure(
                            "User has view permission but not operate permission on Case Stage independent configuration",
                            "CASE_STAGE_INDEPENDENT_OPERATE_DENIED");
                        res.CanView = true;
                        return res;
                    }
                }
            }

            // All layers passed
            bool finalCanOperate = operationType == PermissionOperationType.Operate || operationType == PermissionOperationType.Delete;
            return PermissionResult.CreateSuccess(true, finalCanOperate, "CaseStageThreeLayerPermission");
        }

        #endregion

        #region Roll Back Permission Check (OW-736)

        /// <summary>
        /// Check Roll Back permission for a stage within a Case.
        /// Three paths based on StageRollBackInherit and StageRollBackUseSameAsOperate:
        ///   Path 1 (StageRollBackInherit = true): use MaxStageRollBackTeams snapshot
        ///   Path 2 (StageRollBackUseSameAsOperate = true): use the effective Case Stage operate teams passed in
        ///   Path 3: use StageRollBackTeams / StageRollBackUsers based on subject type
        /// </summary>
        /// <exception cref="CRMException">HTTP 403 if user is not in the effective roll back set.</exception>
        public void CheckRollBackPermission(
            OnboardingStageProgress stageProgress,
            List<string> effectiveCaseStageOperateTeams,
            List<string> userTeamIds,
            string userId)
        {
            List<string> effectiveRollBackTeams;

            if (stageProgress.StageRollBackInherit)
            {
                // Path 1: Use MaxStageRollBackTeams snapshot
                effectiveRollBackTeams = stageProgress.MaxStageRollBackTeams ?? new List<string>();
            }
            else if (stageProgress.StageRollBackUseSameAsOperate)
            {
                // Path 2: Use effective Case Stage Operate Teams (passed as parameter)
                effectiveRollBackTeams = effectiveCaseStageOperateTeams ?? new List<string>();
            }
            else
            {
                // Path 3: Use StageRollBackTeams or StageRollBackUsers based on subject type
                if (stageProgress.StageRollBackPermissionSubjectType == Domain.Shared.Enums.OW.PermissionSubjectTypeEnum.User)
                {
                    // Check user-based roll back permission
                    var rollBackUsers = stageProgress.StageRollBackUsers ?? new List<string>();
                    if (rollBackUsers.Count > 0 && !rollBackUsers.Contains(userId, StringComparer.OrdinalIgnoreCase))
                    {
                        throw new CRMException(ErrorCodeEnum.RollBackPermissionDenied,
                            "User does not have roll back permission for this stage.");
                    }
                    return; // Access granted or no restriction configured
                }

                effectiveRollBackTeams = stageProgress.StageRollBackTeams ?? new List<string>();
            }

            // Check team membership
            if (effectiveRollBackTeams.Count > 0)
            {
                var teamSet = new HashSet<string>(effectiveRollBackTeams, StringComparer.OrdinalIgnoreCase);
                if (userTeamIds == null || !userTeamIds.Any(t => teamSet.Contains(t)))
                {
                    throw new CRMException(ErrorCodeEnum.RollBackPermissionDenied,
                        "User does not have roll back permission for this stage.");
                }
            }
            // effectiveRollBackTeams.Count == 0 means no restriction — all users can roll back
        }

        #endregion

        #region Authorized Teams for User Tree

        /// <summary>
        /// Get authorized team IDs for a Stage (used for filtering user tree)
        /// Returns the set of team IDs that have view permission for the specified Stage
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <returns>
        /// AuthorizedTeamsResult containing:
        /// - IsPublicAccess: true if all teams are authorized (Public mode with no restrictions)
        /// - IsBlacklistMode: true if using blacklist mode (InvisibleToTeams)
        /// - AuthorizedTeamIds: set of authorized team IDs (whitelist) or blacklisted team IDs (blacklist)
        /// </returns>
        public async Task<AuthorizedTeamsResult> GetAuthorizedTeamIdsAsync(long stageId)
        {
            _logger.LogInformation("GetAuthorizedTeamIdsAsync - StageId: {StageId}", stageId);

            // Load Stage entity
            var stage = await _stageRepository.GetByIdAsync(stageId);
            if (stage == null)
            {
                _logger.LogWarning("Stage not found with ID: {StageId}", stageId);
                return new AuthorizedTeamsResult
                {
                    IsPublicAccess = false,
                    IsBlacklistMode = false,
                    TeamIds = new HashSet<string>(),
                    ErrorMessage = $"Stage with ID {stageId} not found"
                };
            }

            // Load parent Workflow entity (for permission inheritance check)
            var workflow = await _workflowRepository.GetByIdAsync(stage.WorkflowId);
            if (workflow == null)
            {
                _logger.LogWarning("Parent Workflow not found with ID: {WorkflowId}", stage.WorkflowId);
                return new AuthorizedTeamsResult
                {
                    IsPublicAccess = false,
                    IsBlacklistMode = false,
                    TeamIds = new HashSet<string>(),
                    ErrorMessage = $"Parent Workflow with ID {stage.WorkflowId} not found"
                };
            }

            return GetAuthorizedTeamIds(stage, workflow);
        }

        /// <summary>
        /// Get authorized team IDs based on Stage and Workflow permission settings
        /// </summary>
        public AuthorizedTeamsResult GetAuthorizedTeamIds(Stage stage, Workflow workflow)
        {
            _logger.LogDebug("GetAuthorizedTeamIds - Stage ViewMode: {StageViewMode}, ViewTeams: {StageViewTeams}, Workflow ViewMode: {WorkflowViewMode}, ViewTeams: {WorkflowViewTeams}",
                stage.ViewPermissionMode, stage.ViewTeams ?? "NULL",
                workflow.ViewPermissionMode, workflow.ViewTeams ?? "NULL");

            var result = new AuthorizedTeamsResult
            {
                IsPublicAccess = false,
                IsBlacklistMode = false,
                TeamIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            };

            // Check if Stage inherits from Workflow (ViewTeams is NULL or empty)
            bool stageInheritsView = string.IsNullOrWhiteSpace(stage.ViewTeams);

            if (stageInheritsView)
            {
                // Stage inherits from Workflow - use Workflow's permission settings
                _logger.LogDebug("Stage inherits view permission from Workflow");
                return GetAuthorizedTeamIdsFromEntity(workflow.ViewPermissionMode, workflow.ViewTeams, "Workflow");
            }
            else
            {
                // Stage has its own permission settings (narrowed from Workflow)
                _logger.LogDebug("Stage has its own view permission settings");
                return GetAuthorizedTeamIdsFromEntity(stage.ViewPermissionMode, stage.ViewTeams, "Stage");
            }
        }

        /// <summary>
        /// Get authorized team IDs from entity permission settings
        /// </summary>
        private AuthorizedTeamsResult GetAuthorizedTeamIdsFromEntity(
            ViewPermissionModeEnum viewMode,
            string viewTeamsJson,
            string entityName)
        {
            var result = new AuthorizedTeamsResult
            {
                IsPublicAccess = false,
                IsBlacklistMode = false,
                TeamIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            };

            switch (viewMode)
            {
                case ViewPermissionModeEnum.Public:
                    // Public mode - check if there are specific teams
                    var publicTeams = _helpers.DeserializeTeamList(viewTeamsJson);
                    if (!publicTeams.Any())
                    {
                        // No specific teams - all teams are authorized
                        result.IsPublicAccess = true;
                        _logger.LogDebug("{EntityName} is Public mode with no team restrictions - all teams authorized", entityName);
                    }
                    else
                    {
                        // Public mode with specific teams (whitelist)
                        foreach (var teamId in publicTeams)
                        {
                            result.TeamIds.Add(teamId);
                        }
                        _logger.LogDebug("{EntityName} Public with specific teams (whitelist): [{Teams}]", entityName, string.Join(", ", result.TeamIds));
                    }
                    break;

                case ViewPermissionModeEnum.VisibleToTeams:
                    // Whitelist mode - only specified teams are authorized
                    var whitelistTeams = _helpers.DeserializeTeamList(viewTeamsJson);
                    foreach (var teamId in whitelistTeams)
                    {
                        result.TeamIds.Add(teamId);
                    }
                    _logger.LogDebug("{EntityName} VisibleToTeams (whitelist): [{Teams}]", entityName, string.Join(", ", result.TeamIds));
                    break;

                case ViewPermissionModeEnum.InvisibleToTeams:
                    // Blacklist mode - all teams except specified ones are authorized
                    result.IsBlacklistMode = true;
                    var blacklistTeams = _helpers.DeserializeTeamList(viewTeamsJson);
                    foreach (var teamId in blacklistTeams)
                    {
                        result.TeamIds.Add(teamId);
                    }
                    _logger.LogDebug("{EntityName} InvisibleToTeams (blacklist): [{Teams}]", entityName, string.Join(", ", result.TeamIds));
                    break;

                case ViewPermissionModeEnum.Private:
                    // Private mode - only owner can view, no teams authorized
                    _logger.LogDebug("{EntityName} is Private mode - no teams authorized", entityName);
                    break;

                default:
                    _logger.LogWarning("Unknown ViewPermissionMode: {ViewMode}", viewMode);
                    break;
            }

            return result;
        }

        #endregion

        #region Private Helpers

        private static List<string> DeserializeJsonTeams(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<string>();
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        #endregion
    }

    /// <summary>
    /// Result of authorized teams calculation for user tree filtering
    /// </summary>
    public class AuthorizedTeamsResult
    {
        /// <summary>
        /// True if all teams are authorized (Public mode with no restrictions)
        /// </summary>
        public bool IsPublicAccess { get; set; }

        /// <summary>
        /// True if using blacklist mode (InvisibleToTeams)
        /// When true, TeamIds contains teams to EXCLUDE
        /// When false, TeamIds contains teams to INCLUDE (whitelist)
        /// </summary>
        public bool IsBlacklistMode { get; set; }

        /// <summary>
        /// Set of team IDs
        /// - If IsPublicAccess is true, this is ignored
        /// - If IsBlacklistMode is true, these are teams to EXCLUDE
        /// - If IsBlacklistMode is false, these are teams to INCLUDE
        /// </summary>
        public HashSet<string> TeamIds { get; set; } = new HashSet<string>();

        /// <summary>
        /// Error message if any error occurred
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Check if there was an error
        /// </summary>
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    }
}

