using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// Checklist repository interface
    /// </summary>
    public interface IChecklistRepository : IBaseRepository<Checklist>
    {
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
        /// Check if name exists
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="team">Team</param>
        /// <param name="excludeId">Exclude ID</param>
        /// <returns></returns>
        Task<bool> IsNameExistsAsync(string name, string team, long? excludeId = null);

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
        /// Get checklists by names
        /// </summary>
        /// <param name="names">List of checklist names</param>
        /// <returns></returns>
        Task<List<Checklist>> GetByNamesAsync(List<string> names);

        /// <summary>
        /// Get checklists by name
        /// </summary>
        /// <param name="name">Checklist name</param>
        /// <returns></returns>
        Task<List<Checklist>> GetByNameAsync(string name);
    }
}
