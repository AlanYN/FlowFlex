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
    }
}

