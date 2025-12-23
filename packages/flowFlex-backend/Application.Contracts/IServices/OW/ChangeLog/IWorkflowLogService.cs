using FlowFlex.Application.Contracts.Dtos.OW.OperationChangeLog;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.IServices.OW.ChangeLog
{
    /// <summary>
    /// Workflow operation log service interface
    /// </summary>
    public interface IWorkflowLogService : IBaseOperationLogService
    {
        // Workflow-specific operations
        Task<bool> LogWorkflowCreateAsync(long workflowId, string workflowName, string workflowDescription = null, string afterData = null, string extendedData = null);
        Task<bool> LogWorkflowUpdateAsync(long workflowId, string workflowName, string beforeData, string afterData, List<string> changedFields, string extendedData = null);
        Task<bool> LogWorkflowDeleteAsync(long workflowId, string workflowName, string reason = null, string extendedData = null);
        Task<bool> LogWorkflowPublishAsync(long workflowId, string workflowName, string version = null, string extendedData = null);
        Task<bool> LogWorkflowUnpublishAsync(long workflowId, string workflowName, string reason = null, string extendedData = null);
        Task<bool> LogWorkflowActivateAsync(long workflowId, string workflowName, string extendedData = null);
        Task<bool> LogWorkflowDeactivateAsync(long workflowId, string workflowName, string reason = null, string extendedData = null);

        // Workflow-specific queries
        Task<PagedResult<OperationChangeLogOutputDto>> GetWorkflowLogsAsync(long workflowId, int pageIndex = 1, int pageSize = 20);
        Task<Dictionary<string, int>> GetWorkflowOperationStatisticsAsync(long workflowId);
    }
}