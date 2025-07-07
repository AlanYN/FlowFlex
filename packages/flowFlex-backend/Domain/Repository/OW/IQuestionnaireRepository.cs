using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// Questionnaire repository interface
    /// </summary>
    public interface IQuestionnaireRepository : IBaseRepository<Questionnaire>
    {
        /// <summary>
        /// Get questionnaire list by category
        /// </summary>
        Task<List<Questionnaire>> GetByCategoryAsync(string category);

        /// <summary>
        /// Get template questionnaires
        /// </summary>
        Task<List<Questionnaire>> GetTemplatesAsync();

        /// <summary>
        /// Get questionnaire instances by template
        /// </summary>
        Task<List<Questionnaire>> GetByTemplateIdAsync(long templateId);

        /// <summary>
        /// Check if name exists
        /// </summary>
        Task<bool> IsNameExistsAsync(string name, string category = null, long? excludeId = null);

        /// <summary>
        /// Get questionnaires with pagination and filters
        /// </summary>
        Task<(List<Questionnaire> items, int totalCount)> GetPagedAsync(
            int pageIndex,
            int pageSize,
            string name = null,
            long? workflowId = null,
            long? stageId = null,
            bool? isTemplate = null,
            bool? isActive = null,
            string sortField = "CreateDate",
            string sortDirection = "desc");

        /// <summary>
        /// Get questionnaire statistics by category
        /// </summary>
        Task<Dictionary<string, object>> GetStatisticsByCategoryAsync(string category);

        /// <summary>
        /// Update questionnaire statistics
        /// </summary>
        Task<bool> UpdateStatisticsAsync(long id, int totalQuestions, int requiredQuestions);

        /// <summary>
        /// Get published questionnaires
        /// </summary>
        Task<List<Questionnaire>> GetPublishedAsync();

        /// <summary>
        /// Validate structure JSON
        /// </summary>
        Task<bool> ValidateStructureAsync(string structureJson);

        /// <summary>
        /// Get questionnaires by multiple stage IDs
        /// </summary>
        Task<List<Questionnaire>> GetByStageIdsAsync(List<long> stageIds);

        /// <summary>
        /// Check if workflow and stage association already exists
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <param name="excludeId">Exclude questionnaire ID (for update scenarios)</param>
        /// <returns>True if association exists</returns>
        Task<bool> IsWorkflowStageAssociationExistsAsync(long? workflowId, long? stageId, long? excludeId = null);

        /// <summary>
        /// Get existing questionnaire with same workflow and stage association
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <param name="excludeId">Exclude questionnaire ID (for update scenarios)</param>
        /// <returns>Existing questionnaire or null</returns>
        Task<Questionnaire> GetByWorkflowStageAssociationAsync(long? workflowId, long? stageId, long? excludeId = null);

        /// <summary>
        /// Get questionnaires by workflow ID
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <returns>List of questionnaires</returns>
        Task<List<Questionnaire>> GetByWorkflowIdAsync(long workflowId);

        /// <summary>
        /// Get questionnaires by stage ID
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <returns>List of questionnaires</returns>
        Task<List<Questionnaire>> GetByStageIdAsync(long stageId);
    }
} 
