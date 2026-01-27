using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.Permission;
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

