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

        /// <summary>
        /// Get tasks by action ID
        /// </summary>
        Task<List<ChecklistTask>> GetTasksByActionIdAsync(long actionId);

        /// <summary>
        /// Check if task name exists in checklist (excluding specific task ID for update scenario)
        /// </summary>
        Task<bool> IsTaskNameExistsAsync(long checklistId, string taskName, long? excludeTaskId = null);

        #region Dashboard Methods

        /// <summary>
        /// Get pending tasks for user (assigned to user or their teams) with pagination
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="userTeamIds">User's team IDs</param>
        /// <param name="category">Optional category filter (Sales, Account, Other)</param>
        /// <param name="pageIndex">Page index (1-based)</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>List of tasks with case information</returns>
        Task<List<DashboardTaskInfo>> GetPendingTasksForUserAsync(long userId, List<long> userTeamIds, string? category, int pageIndex, int pageSize);

        /// <summary>
        /// Get count of pending tasks for user
        /// </summary>
        Task<int> GetPendingTasksCountForUserAsync(long userId, List<long> userTeamIds, string? category);

        /// <summary>
        /// Get tasks with upcoming deadlines for user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="userTeamIds">User's team IDs</param>
        /// <param name="endDate">End date for deadline range</param>
        /// <returns>List of tasks with deadlines</returns>
        Task<List<DashboardTaskInfo>> GetUpcomingDeadlinesAsync(long userId, List<long> userTeamIds, DateTimeOffset endDate);

        #endregion
    }

    /// <summary>
    /// Dashboard task info with joined case data
    /// </summary>
    public class DashboardTaskInfo
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Priority { get; set; } = "Medium";
        public DateTimeOffset? DueDate { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsRequired { get; set; }
        public string? AssignedTeam { get; set; }
        public string? AssigneeName { get; set; }
        public long? AssigneeId { get; set; }
        public string Status { get; set; } = "Pending";
        public long ChecklistId { get; set; }
        public long OnboardingId { get; set; }
        public string? CaseCode { get; set; }
        public string? CaseName { get; set; }
    }
}
