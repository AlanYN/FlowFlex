using FlowFlex.Domain.Entities.Action;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Domain.Repository.Action
{
    /// <summary>
    /// Action Trigger Mapping repository interface
    /// </summary>
    public interface IActionTriggerMappingRepository : IBaseRepository<ActionTriggerMapping>, IScopedService
    {
        /// <summary>
        /// Get mappings by trigger source
        /// </summary>
        /// <param name="triggerType">Trigger type (Stage, Task, Question)</param>
        /// <param name="triggerSourceId">Trigger source ID</param>
        /// <param name="triggerEvent">Trigger event (Completed, Answered, etc.)</param>
        /// <returns>Mapping list ordered by execution order</returns>
        Task<List<ActionTriggerMapping>> GetByTriggerAsync(string triggerType, long triggerSourceId, string triggerEvent);

        /// <summary>
        /// Get mappings for a specific trigger
        /// </summary>
        /// <param name="triggerSourceType">Trigger source type (Stage, Task, Question, etc.)</param>
        /// <param name="triggerSourceId">Trigger source ID</param>
        /// <param name="triggerEventType">Trigger event type (Completed, Created, etc.)</param>
        /// <param name="workflowId"></param>
        /// <param name="stageId"></param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Mapping list ordered by execution order</returns>
        Task<List<ActionTriggerMapping>> GetMappingsForTriggerAsync(
            string triggerSourceType,
            long triggerSourceId,
            string triggerEventType,
            long? workflowId = null,
            long? stageId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get mappings by action definition ID
        /// </summary>
        /// <param name="actionDefinitionId">Action definition ID</param>
        /// <returns>Mapping list</returns>
        Task<List<ActionTriggerMapping>> GetByActionDefinitionIdAsync(long actionDefinitionId);

        /// <summary>
        /// Get mappings by trigger type
        /// </summary>
        /// <param name="triggerType">Trigger type</param>
        /// <returns>Mapping list</returns>
        Task<List<ActionTriggerMapping>> GetByTriggerTypeAsync(string triggerType);

        /// <summary>
        /// Check if mapping exists
        /// </summary>
        /// <param name="actionDefinitionId"></param>
        /// <param name="triggerType"></param>
        /// <param name="triggerSourceId"></param>
        /// <param name="workFlowId"></param>
        /// <returns></returns>
        Task<bool> IsMappingExistsAsync(long actionDefinitionId, string triggerType, long triggerSourceId, long workFlowId);

        /// <summary>
        /// Get all enabled mappings
        /// </summary>
        /// <returns>Enabled mapping list</returns>
        Task<List<ActionTriggerMapping>> GetAllEnabledAsync();

        /// <summary>
        /// Get mappings with action details
        /// </summary>
        /// <param name="triggerType">Trigger type</param>
        /// <param name="triggerSourceId">Trigger source ID</param>
        /// <param name="triggerEvent">Trigger event</param>
        /// <returns>Mappings with associated action definitions</returns>
        Task<List<(ActionTriggerMapping Mapping, ActionDefinition Action)>> GetMappingsWithActionsAsync(string triggerType, long triggerSourceId, string triggerEvent);

        /// <summary>
        /// Batch enable or disable mappings
        /// </summary>
        /// <param name="mappingIds">Mapping ID list</param>
        /// <param name="isEnabled">Enable or disable</param>
        /// <returns>Whether successful</returns>
        Task<bool> BatchUpdateEnabledStatusAsync(List<long> mappingIds, bool isEnabled);

        /// <summary>
        /// Delete mappings by action definition ID
        /// </summary>
        /// <param name="actionDefinitionId">Action definition ID</param>
        /// <returns>Whether successful</returns>
        Task<bool> DeleteByActionDefinitionIdAsync(long actionDefinitionId);
    }
}