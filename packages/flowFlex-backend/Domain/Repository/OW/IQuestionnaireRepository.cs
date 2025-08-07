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
        /// Get questionnaires by multiple IDs (batch query)
        /// </summary>
        Task<List<Questionnaire>> GetByIdsAsync(List<long> ids);

        // Template-related methods removed - template functionality discontinued

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
        /// Note: This method is retained for backward compatibility but is no longer used for uniqueness validation
        /// Multiple questionnaires can now be associated with the same workflow-stage combination
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <param name="excludeId">Exclude questionnaire ID (for update scenarios)</param>
        /// <returns>True if association exists</returns>
        Task<bool> IsWorkflowStageAssociationExistsAsync(long? workflowId, long? stageId, long? excludeId = null);

        /// <summary>
        /// Get existing questionnaire with same workflow and stage association
        /// Note: This method returns the first match found, but multiple questionnaires can now exist with the same association
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

        /// <summary>
        /// Get questionnaires by names
        /// </summary>
        /// <param name="names">List of questionnaire names</param>
        /// <returns>List of questionnaires</returns>
        Task<List<Questionnaire>> GetByNamesAsync(List<string> names);

        /// <summary>
        /// 直接查询，使用显式过滤条件
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="appCode">应用代码</param>
        /// <returns>问卷列表</returns>
        Task<List<Questionnaire>> GetListWithExplicitFiltersAsync(string tenantId, string appCode);
    }
}
