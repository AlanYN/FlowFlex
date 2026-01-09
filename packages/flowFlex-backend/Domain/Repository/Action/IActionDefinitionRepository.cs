using FlowFlex.Domain.Entities.Action;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Domain.Repository.Action
{
    /// <summary>
    /// Action Definition repository interface
    /// </summary>
    public interface IActionDefinitionRepository : IBaseRepository<ActionDefinition>, IScopedService
    {
        /// <summary>
        /// Get action definitions by action type
        /// </summary>
        /// <param name="actionType">Action type (Python, HttpApi, SendEmail)</param>
        /// <returns>Action definition list</returns>
        Task<List<ActionDefinition>> GetByActionTypeAsync(string actionType);

        /// <summary>
        /// Get all enabled action definitions
        /// </summary>
        /// <returns>Enabled action definition list</returns>
        Task<List<ActionDefinition>> GetAllEnabledAsync();

        /// <summary>
        /// Get action definitions by name (fuzzy search)
        /// </summary>
        /// <param name="name">Action name</param>
        /// <returns>Action definition list</returns>
        Task<List<ActionDefinition>> GetByNameAsync(string name);

        /// <summary>
        /// Check if action name exists
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="excludeId">Exclude this ID (for update scenarios)</param>
        /// <returns>Whether exists</returns>
        Task<bool> IsActionNameExistsAsync(string actionName, long? excludeId = null);

        /// <summary>
        /// Get action definitions with pagination
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="actionType"></param>
        /// <param name="keyword"></param>
        /// <param name="isAssignmentStage"></param>
        /// <param name="isAssignmentChecklist"></param>
        /// <param name="isAssignmentQuestionnaire"></param>
        /// <param name="isTools"></param>
        /// <param name="integrationId">Filter by Integration ID</param>
        /// <returns></returns>
        Task<(List<ActionDefinition> Data, int TotalCount)> GetPagedAsync(int pageIndex,
            int pageSize,
            string? actionType = null,
            string? keyword = null,
            bool? isAssignmentStage = null,
            bool? isAssignmentChecklist = null,
            bool? isAssignmentQuestionnaire = null,
            bool? isAssignmentWorkflow = null,
            bool? isTools = null,
            long? integrationId = null);

        /// <summary>
        /// Batch enable or disable actions
        /// </summary>
        /// <param name="actionIds">Action ID list</param>
        /// <param name="isEnabled">Enable or disable</param>
        /// <returns>Whether successful</returns>
        Task<bool> BatchUpdateEnabledStatusAsync(List<long> actionIds, bool isEnabled);

        /// <summary>
        /// Get trigger mappings with related entity names by action definition IDs
        /// </summary>
        /// <param name="actionDefinitionIds">Action definition ID list</param>
        /// <returns>Trigger mappings with entity details</returns>
        Task<List<ActionTriggerMappingWithDetails>> GetTriggerMappingsWithDetailsByActionIdsAsync(List<long> actionDefinitionIds);

        /// <summary>
        /// Get trigger mappings with action details by trigger source id
        /// </summary>
        /// <param name="triggerSourceId"></param>
        /// <returns></returns>
        Task<List<ActionTriggerMappingWithActionDetails>> GetMappingsWithActionDetailsByTriggerSourceIdAsync(long triggerSourceId);

        /// <summary>
        /// Get action definitions by IDs
        /// </summary>
        /// <param name="ids">List of action definition IDs</param>
        /// <returns>List of action definitions</returns>
        Task<List<ActionDefinition>> GetByIdsAsync(List<long> ids);

        /// <summary>
        /// Get all enabled action definitions summary (lightweight)
        /// </summary>
        /// <returns>List of enabled action definitions with basic info only</returns>
        Task<List<ActionDefinition>> GetAllEnabledSummaryAsync();
    }
}