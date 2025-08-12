using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// ChecklistTask repository interface
    /// </summary>
    public interface IChecklistTaskRepository : IBaseRepository<ChecklistTask>
    {
        /// <summary>
        /// Get tasks by checklist id
        /// </summary>
        Task<List<ChecklistTask>> GetByChecklistIdAsync(long checklistId);

        /// <summary>
        /// Get tasks for multiple checklist ids in a single query
        /// </summary>
        Task<List<ChecklistTask>> GetByChecklistIdsAsync(List<long> checklistIds);

        /// <summary>
        /// Get completed tasks count
        /// </summary>
        Task<int> GetCompletedCountAsync(long checklistId);

        /// <summary>
        /// Get pending tasks by assignee
        /// </summary>
        Task<List<ChecklistTask>> GetPendingTasksByAssigneeAsync(long assigneeId);

        /// <summary>
        /// Get overdue tasks
        /// </summary>
        Task<List<ChecklistTask>> GetOverdueTasksAsync();

        /// <summary>
        /// Batch complete tasks
        /// </summary>
        Task<bool> BatchCompleteAsync(List<long> taskIds, string completionNotes, int actualHours);

        /// <summary>
        /// Update task order
        /// </summary>
        Task<bool> UpdateOrderAsync(long checklistId, Dictionary<long, int> taskOrders);

        /// <summary>
        /// Get dependent tasks
        /// </summary>
        Task<List<ChecklistTask>> GetDependentTasksAsync(long taskId);

        /// <summary>
        /// Check if task can be completed
        /// </summary>
        Task<bool> CanCompleteAsync(long taskId);

        /// <summary>
        /// Get next order number for checklist
        /// </summary>
        Task<int> GetNextOrderAsync(long checklistId);
    }
}
