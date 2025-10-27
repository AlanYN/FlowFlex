using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.Permission;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Const;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Models.Permission;
using Microsoft.Extensions.Logging;
using PermissionOperationType = FlowFlex.Domain.Shared.Enums.Permission.OperationTypeEnum;

namespace FlowFlex.Application.Services.OW.Permission
{
    /// <summary>
    /// Workflow permission verification service
    /// Handles all Workflow-specific permission checks
    /// </summary>
    public class WorkflowPermissionService : IScopedService
    {
        private readonly ILogger<WorkflowPermissionService> _logger;
        private readonly UserContext _userContext;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly PermissionHelpers _helpers;

        public WorkflowPermissionService(
            ILogger<WorkflowPermissionService> logger,
            UserContext userContext,
            IWorkflowRepository workflowRepository,
            PermissionHelpers helpers)
        {
            _logger = logger;
            _userContext = userContext;
            _workflowRepository = workflowRepository;
            _helpers = helpers;
        }

        #region Main Permission Check

        /// <summary>
        /// Check Workflow permission based on ViewPermissionMode and operation type
        /// </summary>
        public PermissionResult CheckWorkflowPermission(
            Workflow workflow,
            long userId,
            PermissionOperationType operationType)
        {
            // Get user teams
            var userTeamIds = _helpers.GetUserTeamIds();

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
                    // User has view permission but not operate permission
                    var result = PermissionResult.CreateFailure(
                        "User has view permission but not operate permission",
                        "OPERATE_PERMISSION_DENIED");
                    result.CanView = true; // Set CanView to true since user can view
                    return result;
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

        #endregion

        #region View Permission

        /// <summary>
        /// Check view permission based on ViewPermissionMode
        /// </summary>
        public bool CheckViewPermission(Workflow workflow, List<string> userTeamIds)
        {
            return workflow.ViewPermissionMode switch
            {
                ViewPermissionModeEnum.Public => true,
                
                ViewPermissionModeEnum.VisibleToTeams => 
                    _helpers.CheckTeamWhitelist(workflow.ViewTeams, userTeamIds),
                
                ViewPermissionModeEnum.InvisibleToTeams => 
                    _helpers.CheckTeamBlacklist(workflow.ViewTeams, userTeamIds),
                
                ViewPermissionModeEnum.Private => 
                    _helpers.IsCurrentUserOwner(workflow.CreateUserId),
                
                _ => false
            };
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
            // Admin bypass
            if (_helpers.HasAdminPrivileges())
            {
                _logger.LogDebug(
                    "User {UserId} has admin privileges, granting view permission for Workflow {WorkflowId}",
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
            var userTeamIds = _helpers.GetUserTeamIds();

            // Check view permission only
            return CheckViewPermission(workflow, userTeamIds);
        }

        #endregion

        #region Operate Permission

        /// <summary>
        /// Check operate permission
        /// </summary>
        public bool CheckOperatePermission(Workflow workflow, List<string> userTeamIds)
        {
            _logger.LogDebug(
                "CheckOperatePermission - ViewMode: {ViewMode}, OperateTeams: {OperateTeams}",
                workflow.ViewPermissionMode,
                workflow.OperateTeams ?? "NULL");

            return workflow.ViewPermissionMode switch
            {
                // Public mode: NULL/empty means everyone can operate, otherwise whitelist
                ViewPermissionModeEnum.Public => 
                    _helpers.CheckOperateTeamsPublicMode(workflow.OperateTeams, userTeamIds),
                
                // InvisibleToTeams mode: OperateTeams is blacklist
                ViewPermissionModeEnum.InvisibleToTeams => 
                    _helpers.CheckTeamBlacklist(workflow.OperateTeams, userTeamIds),
                
                // VisibleToTeams and Private modes: OperateTeams is whitelist (required)
                _ => _helpers.CheckTeamWhitelist(workflow.OperateTeams, userTeamIds)
            };
        }

        #endregion

        #region Optimized Methods for List APIs

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
            var userTeamIds = _helpers.GetUserTeamIds();

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

        #endregion
    }
}

