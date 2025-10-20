using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Enums.Permission;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Models.Permission;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PermissionOperationType = FlowFlex.Domain.Shared.Enums.Permission.OperationTypeEnum;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Permission service implementation
    /// Function: Implement permission verification logic based on state machine design
    /// </summary>
    public class PermissionService : IPermissionService, IScopedService
    {
        private readonly ILogger<PermissionService> _logger;
        private readonly UserContext _userContext;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IStageRepository _stageRepository;
        private readonly IOnboardingRepository _onboardingRepository;

        public PermissionService(
            ILogger<PermissionService> logger,
            UserContext userContext,
            IWorkflowRepository workflowRepository,
            IStageRepository stageRepository,
            IOnboardingRepository onboardingRepository)
        {
            _logger = logger;
            _userContext = userContext;
            _workflowRepository = workflowRepository;
            _stageRepository = stageRepository;
            _onboardingRepository = onboardingRepository;
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
                // Step 1: Validate input
                if (userId <= 0 || workflowId <= 0)
                {
                    return PermissionResult.CreateFailure(
                        "Invalid user ID or workflow ID",
                        "INVALID_INPUT");
                }

                // Step 2: Get user groups (from UserContext)
                var userGroups = GetUserGroups();
                if (userGroups == null || !userGroups.Any())
                {
                    _logger.LogInformation("User {UserId} does not belong to any Group, skipping Group permission check", userId);
                    // If user has no Group, skip Group permission check and allow access
                    // This is a temporary solution until Group management is fully implemented
                }
                else
                {
                    // Step 3: Check Group permission switch (前置检查) - only if user has groups
                    var groupCheckResult = await CheckGroupPermissionSwitchAsync(
                        userGroups,
                        PermissionEntityTypeEnum.Workflow,
                        operationType);

                    if (!groupCheckResult.Success)
                    {
                        _logger.LogWarning(
                            "Group permission switch check failed for user {UserId} - {ErrorMessage}",
                            userId, groupCheckResult.ErrorMessage);
                        return groupCheckResult;
                    }
                }

                // Step 4: Load Workflow entity
                var workflow = await _workflowRepository.GetByIdAsync(workflowId);
                if (workflow == null)
                {
                    return PermissionResult.CreateFailure(
                        "Workflow not found",
                        "WORKFLOW_NOT_FOUND");
                }

                // Step 5: Check permission based on ViewPermissionMode and operation type
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
                // Step 1: Validate input
                if (userId <= 0 || stageId <= 0)
                {
                    return PermissionResult.CreateFailure(
                        "Invalid user ID or stage ID",
                        "INVALID_INPUT");
                }

                // Step 2: Get user groups
                var userGroups = GetUserGroups();
                if (userGroups == null || !userGroups.Any())
                {
                    _logger.LogInformation("User {UserId} does not belong to any Group, skipping Group permission check", userId);
                    // If user has no Group, skip Group permission check and allow access
                }
                else
                {
                    // Step 3: Check Group permission switch - only if user has groups
                    var groupCheckResult = await CheckGroupPermissionSwitchAsync(
                        userGroups,
                        PermissionEntityTypeEnum.Stage,
                        operationType);

                    if (!groupCheckResult.Success)
                    {
                        return groupCheckResult;
                    }
                }

                // Step 4: Load Stage permission configuration
                // TODO: Implement actual permission loading from database
                
                return PermissionResult.CreateSuccess(
                    true,  // canView
                    true,  // canOperate
                    "StageTeam");
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
                // Step 1: Validate input
                if (userId <= 0 || caseId <= 0)
                {
                    return PermissionResult.CreateFailure(
                        "Invalid user ID or case ID",
                        "INVALID_INPUT");
                }

                // Step 2: Get user groups
                var userGroups = GetUserGroups();
                if (userGroups == null || !userGroups.Any())
                {
                    _logger.LogInformation("User {UserId} does not belong to any Group, skipping Group permission check", userId);
                    // If user has no Group, skip Group permission check and allow access
                }
                else
                {
                    // Step 3: Check Group permission switch - only if user has groups
                    var groupCheckResult = await CheckGroupPermissionSwitchAsync(
                        userGroups,
                        PermissionEntityTypeEnum.Case,
                        operationType);

                    if (!groupCheckResult.Success)
                    {
                        return groupCheckResult;
                    }
                }

                // Step 4: Load Case permission configuration
                // TODO: Implement actual permission loading from database
                
                return PermissionResult.CreateSuccess(
                    true,  // canView
                    true,  // canOperate
                    "CasePermission");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Case access");
                return PermissionResult.CreateFailure(
                    "Internal error during permission check",
                    "PERMISSION_CHECK_ERROR");
            }
        }

        /// <summary>
        /// Get user groups from UserContext
        /// </summary>
        private List<string> GetUserGroups()
        {
            // Get user groups from UserContext.UserTeams
            if (_userContext?.UserTeams == null)
            {
                return new List<string>();
            }

            // Convert UserTeams to group list
            var groups = new List<string>();
            
            // TODO: Implement actual group extraction logic based on UserTeams structure
            // For now, return empty list as placeholder
            
            return groups;
        }

        /// <summary>
        /// Check Group permission switch (前置检查)
        /// </summary>
        private async Task<PermissionResult> CheckGroupPermissionSwitchAsync(
            List<string> userGroups,
            PermissionEntityTypeEnum entityType,
            PermissionOperationType operationType)
        {
            _logger.LogDebug(
                "Checking Group permission switch - EntityType: {EntityType}, Operation: {Operation}",
                entityType, operationType);

            // TODO: Implement actual Group permission switch check from database
            // For now, return success as default behavior
            
            // According to the design document:
            // - Query user's Groups
            // - Check if any Group has the permission switch enabled
            // - If all Groups have the switch disabled, return failure
            
            // Temporary implementation: allow all operations
            return PermissionResult.CreateSuccess(true, true, "GroupPermissionSwitch");
        }

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

            // Step 1: Check View Permission based on ViewPermissionMode
            bool canView = CheckViewPermission(workflow, userTeamIds);

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
                    var visibleTeams = JsonConvert.DeserializeObject<List<string>>(workflow.ViewTeams) ?? new List<string>();
                    return userTeamIds.Any(teamId => visibleTeams.Contains(teamId));

                case ViewPermissionModeEnum.InvisibleToTeams:
                    // Invisible to specific teams: check if user does NOT belong to any of the listed teams
                    if (string.IsNullOrWhiteSpace(workflow.ViewTeams))
                    {
                        // No teams specified, allow access to all
                        return true;
                    }
                    var invisibleTeams = JsonConvert.DeserializeObject<List<string>>(workflow.ViewTeams) ?? new List<string>();
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
            // If no operate teams specified, deny operate permission
            if (string.IsNullOrWhiteSpace(workflow.OperateTeams))
            {
                return false;
            }

            var operateTeams = JsonConvert.DeserializeObject<List<string>>(workflow.OperateTeams) ?? new List<string>();
            
            // Check if user belongs to any of the operate teams
            return userTeamIds.Any(teamId => operateTeams.Contains(teamId));
        }

        /// <summary>
        /// Get user team IDs from UserContext
        /// </summary>
        private List<string> GetUserTeamIds()
        {
            if (_userContext?.UserTeams == null)
            {
                return new List<string>();
            }

            // Get all team IDs (including sub-teams)
            var teamIds = _userContext.UserTeams.GetAllTeamIds();
            return teamIds.Select(id => id.ToString()).ToList();
        }
    }
}

