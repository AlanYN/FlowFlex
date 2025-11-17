using FlowFlex.Application.Contracts.Dtos.OW.OperationChangeLog;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.IServices.OW.ChangeLog
{
    /// <summary>
    /// Checklist operation log service interface
    /// </summary>
    public interface IChecklistLogService : IBaseOperationLogService
    {
        // Checklist lifecycle operations
        Task<bool> LogChecklistCreateAsync(long checklistId, string checklistName, string afterData = null, string extendedData = null);
        Task<bool> LogChecklistUpdateAsync(long checklistId, string checklistName, string beforeData, string afterData, List<string> changedFields, string extendedData = null);
        Task<bool> LogChecklistDeleteAsync(long checklistId, string checklistName, string reason = null, string extendedData = null);

        // Checklist task operations
        Task<bool> LogChecklistTaskCreateAsync(long taskId, string taskName, long checklistId, string afterData = null, string extendedData = null);
        Task<bool> LogChecklistTaskUpdateAsync(long taskId, string taskName, string beforeData, string afterData, List<string> changedFields, long checklistId, string extendedData = null);
        Task<bool> LogChecklistTaskDeleteAsync(long taskId, string taskName, long checklistId, string reason = null, string extendedData = null);

        // Checklist task runtime operations
        Task<bool> LogChecklistTaskCompleteAsync(long taskId, string taskName, long onboardingId, long? stageId, string completionNotes = null, int actualHours = 0);
        Task<bool> LogChecklistTaskUncompleteAsync(long taskId, string taskName, long onboardingId, long? stageId, string reason = null);

        // Checklist-specific queries
        Task<PagedResult<OperationChangeLogOutputDto>> GetChecklistLogsAsync(long checklistId, int pageIndex = 1, int pageSize = 20);
        Task<PagedResult<OperationChangeLogOutputDto>> GetChecklistTaskLogsAsync(long taskId, long? onboardingId = null, int pageIndex = 1, int pageSize = 20, bool includeActionExecutions = true);
        Task<Dictionary<string, int>> GetChecklistOperationStatisticsAsync(long checklistId);
        Task<Dictionary<string, int>> GetChecklistTaskStatisticsAsync(long taskId, long? onboardingId = null);
    }
}