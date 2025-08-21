using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Component mapping synchronization service interface
    /// </summary>
    public interface IComponentMappingService : IScopedService
    {
        /// <summary>
        /// Sync mappings for a specific stage
        /// </summary>
        Task SyncStageMappingsAsync(long stageId);

        /// <summary>
        /// Sync all stage mappings (for initial data migration)
        /// </summary>
        Task SyncAllStageMappingsAsync();

        /// <summary>
        /// Get questionnaire assignments from mapping table (ultra-fast)
        /// </summary>
        Task<Dictionary<long, List<(long WorkflowId, long StageId)>>> GetQuestionnaireAssignmentsAsync(List<long> questionnaireIds);

        /// <summary>
        /// Get checklist assignments from mapping table (ultra-fast)
        /// </summary>
        Task<Dictionary<long, List<(long WorkflowId, long StageId)>>> GetChecklistAssignmentsAsync(List<long> checklistIds);
    }
}