using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// Checklist repository interface
    /// </summary>
    public interface IChecklistRepository : IBaseRepository<Checklist>
    {
        /// <summary>
        /// Get checklists by workflow ID
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <returns></returns>
        Task<List<Checklist>> GetByWorkflowIdAsync(long workflowId);

        /// <summary>
        /// Get checklists by stage ID
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <returns></returns>
        Task<List<Checklist>> GetByStageIdAsync(long stageId);

        /// <summary>
        /// Get checklists by team
        /// </summary>
        /// <param name="team">Team name</param>
        /// <returns></returns>
        Task<List<Checklist>> GetByTeamAsync(string team);

        /// <summary>
        /// Get template checklists
        /// </summary>
        /// <returns></returns>
        Task<List<Checklist>> GetTemplatesAsync();

        /// <summary>
        /// Get checklists by template ID
        /// </summary>
        /// <param name="templateId">Template ID</param>
        /// <returns></returns>
        Task<List<Checklist>> GetByTemplateIdAsync(long templateId);

        /// <summary>
        /// Update completion rate
        /// </summary>
        /// <param name="checklistId">Checklist ID</param>
        /// <param name="completionRate">Completion rate</param>
        /// <param name="completedTasks">Completed tasks count</param>
        /// <returns></returns>
        Task<bool> UpdateCompletionRateAsync(long checklistId, decimal completionRate, int completedTasks);

        /// <summary>
        /// Check if name exists
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="team">Team</param>
        /// <param name="excludeId">Exclude ID</param>
        /// <returns></returns>
        Task<bool> IsNameExistsAsync(string name, string team, long? excludeId = null);

        /// <summary>
        /// Get checklist with tasks by ID
        /// </summary>
        /// <param name="id">Checklist ID</param>
        /// <returns></returns>
        Task<Checklist> GetWithTasksAsync(long id);

        /// <summary>
        /// Get paged data
        /// </summary>
        Task<(List<Checklist> items, int totalCount)> GetPagedAsync(
            int pageIndex,
            int pageSize,
            string name = null,
            string team = null,
            string type = null,
            string status = null,
            bool? isTemplate = null,
            bool? isActive = null,
            long? workflowId = null,
            long? stageId = null,
            string sortField = "CreateDate",
            string sortDirection = "desc");

        /// <summary>
        /// Get statistics by team
        /// </summary>
        /// <param name="team">Team name</param>
        /// <returns></returns>
        Task<Dictionary<string, object>> GetStatisticsByTeamAsync(string team);

        /// <summary>
        /// Get checklists with tasks by stage ID
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <returns></returns>
        Task<List<Checklist>> GetByStageIdWithTasksAsync(long stageId);
    }
} 
