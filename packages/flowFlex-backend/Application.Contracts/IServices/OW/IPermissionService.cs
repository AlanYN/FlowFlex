using FlowFlex.Application.Contracts.Dtos.OW.Permission;
using FlowFlex.Domain.Shared.Enums.Permission;
using FlowFlex.Domain.Shared.Models.Permission;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Permission service interface
    /// Function: Provide permission verification for Workflow, Stage, and Case
    /// </summary>
    public interface IPermissionService
    {
        /// <summary>
        /// Check Workflow access permission
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="operationType">Operation type</param>
        /// <returns>Permission result</returns>
        Task<PermissionResult> CheckWorkflowAccessAsync(
            long userId,
            long workflowId,
            OperationTypeEnum operationType);

        /// <summary>
        /// Check Stage access permission
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <param name="operationType">Operation type</param>
        /// <returns>Permission result</returns>
        Task<PermissionResult> CheckStageAccessAsync(
            long userId,
            long stageId,
            OperationTypeEnum operationType);

        /// <summary>
        /// Check Case access permission
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="caseId">Case ID</param>
        /// <param name="operationType">Operation type</param>
        /// <returns>Permission result</returns>
        Task<PermissionResult> CheckCaseAccessAsync(
            long userId,
            long caseId,
            OperationTypeEnum operationType);

        /// <summary>
        /// Check resource permission (unified interface for HTTP API)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="resourceId">Resource ID</param>
        /// <param name="resourceType">Resource type</param>
        /// <returns>Permission check response</returns>
        Task<CheckPermissionResponse> CheckResourcePermissionAsync(
            long userId,
            long resourceId,
            PermissionEntityTypeEnum resourceType);

        /// <summary>
        /// Check user's group permission (module-level permission check)
        /// This method ONLY checks module permission, without Portal token bypass
        /// Used for batch permission checking in list operations
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="permission">Permission code (e.g., "WORKFLOW:READ")</param>
        /// <returns>True if user has the permission, false otherwise</returns>
        Task<bool> CheckGroupPermissionAsync(long userId, string permission);

        /// <summary>
        /// Check Workflow view permission at entity-level ONLY
        /// This method skips module permission check and directly checks entity-level permission
        /// Optimized for batch operations where module permission is already verified
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="workflow">Workflow entity (optional, to avoid database query)</param>
        /// <returns>True if user has view permission, false otherwise</returns>
        Task<bool> CheckWorkflowViewPermissionAsync(
            long userId,
            long workflowId,
            FlowFlex.Domain.Entities.OW.Workflow workflow = null);

        /// <summary>
        /// Check Workflow view permission with pre-fetched user teams (performance-optimized)
        /// Avoids repeated GetUserTeamIds() calls in batch operations
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="workflow">Workflow entity (optional, to avoid database query)</param>
        /// <param name="userTeamIds">Pre-fetched user team IDs (optional)</param>
        /// <returns>True if user has view permission, false otherwise</returns>
        Task<bool> CheckWorkflowViewPermissionAsync(
            long userId,
            long workflowId,
            FlowFlex.Domain.Entities.OW.Workflow workflow,
            System.Collections.Generic.List<string> userTeamIds);

        /// <summary>
        /// Get user team IDs from UserContext
        /// </summary>
        /// <returns>List of team IDs</returns>
        List<string> GetUserTeamIds();

        /// <summary>
        /// Check Stage permission using entity objects (performance-optimized, no DB query)
        /// </summary>
        PermissionResult CheckStagePermission(
            FlowFlex.Domain.Entities.OW.Stage stage,
            FlowFlex.Domain.Entities.OW.Workflow workflow,
            long userId,
            FlowFlex.Domain.Shared.Enums.Permission.OperationTypeEnum operationType);

        /// <summary>
        /// Get permission info for Workflow (optimized for DTO population)
        /// Returns both view and operate permissions in a single call
        /// </summary>
        Task<PermissionInfoDto> GetWorkflowPermissionInfoAsync(long userId, long workflowId);

        /// <summary>
        /// Get permission info for Stage (optimized for DTO population)
        /// Returns both view and operate permissions in a single call
        /// </summary>
        Task<PermissionInfoDto> GetStagePermissionInfoAsync(long userId, long stageId);

        /// <summary>
        /// Get permission info for Case (optimized for DTO population)
        /// Returns both view and operate permissions in a single call
        /// </summary>
        Task<PermissionInfoDto> GetCasePermissionInfoAsync(long userId, long caseId);

        /// <summary>
        /// Get permission info for Workflow (batch-optimized for list APIs)
        /// Skips redundant module permission checks by accepting pre-checked flags
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="hasViewModulePermission">Pre-checked module-level view permission</param>
        /// <param name="hasOperateModulePermission">Pre-checked module-level operate permission</param>
        Task<PermissionInfoDto> GetWorkflowPermissionInfoForListAsync(
            long userId, 
            long workflowId, 
            bool hasViewModulePermission, 
            bool hasOperateModulePermission);

        /// <summary>
        /// Get permission info for Stage (batch-optimized for list APIs)
        /// Skips redundant module permission checks by accepting pre-checked flags
        /// </summary>
        Task<PermissionInfoDto> GetStagePermissionInfoForListAsync(
            long userId, 
            long stageId, 
            bool hasViewModulePermission, 
            bool hasOperateModulePermission);

        /// <summary>
        /// Get permission info for Stage (ultra-optimized for list APIs with pre-loaded entities)
        /// Accepts entity objects to avoid database queries
        /// </summary>
        PermissionInfoDto GetStagePermissionInfoForList(
            FlowFlex.Domain.Entities.OW.Stage stage,
            FlowFlex.Domain.Entities.OW.Workflow workflow,
            long userId,
            bool hasViewModulePermission,
            bool hasOperateModulePermission);

        /// <summary>
        /// Get permission info for Stage (ultra-optimized with pre-fetched user teams)
        /// Avoids repeated GetUserTeamIds() calls in batch operations
        /// </summary>
        PermissionInfoDto GetStagePermissionInfoForList(
            FlowFlex.Domain.Entities.OW.Stage stage,
            FlowFlex.Domain.Entities.OW.Workflow workflow,
            long userId,
            bool hasViewModulePermission,
            bool hasOperateModulePermission,
            System.Collections.Generic.List<string> userTeamIds);

        /// <summary>
        /// Get permission info for Case (batch-optimized for list APIs)
        /// Skips redundant module permission checks by accepting pre-checked flags
        /// </summary>
        Task<PermissionInfoDto> GetCasePermissionInfoForListAsync(
            long userId, 
            long caseId, 
            bool hasViewModulePermission, 
            bool hasOperateModulePermission);
    }
}

