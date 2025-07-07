using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// Questionnaire Section repository interface
    /// </summary>
    public interface IQuestionnaireSectionRepository : IBaseRepository<QuestionnaireSection>
    {
        /// <summary>
        /// Get sections by questionnaire ID
        /// </summary>
        /// <param name="questionnaireId">Questionnaire ID</param>
        /// <returns>List of sections</returns>
        Task<List<QuestionnaireSection>> GetByQuestionnaireIdAsync(long questionnaireId);

        /// <summary>
        /// Get sections by questionnaire ID with ordering
        /// </summary>
        /// <param name="questionnaireId">Questionnaire ID</param>
        /// <returns>Ordered list of sections</returns>
        Task<List<QuestionnaireSection>> GetOrderedByQuestionnaireIdAsync(long questionnaireId);

        /// <summary>
        /// Get sections by multiple questionnaire IDs
        /// </summary>
        /// <param name="questionnaireIds">List of questionnaire IDs</param>
        /// <returns>List of sections for all questionnaires</returns>
        Task<List<QuestionnaireSection>> GetByQuestionnaireIdsAsync(List<long> questionnaireIds);

        /// <summary>
        /// Delete sections by questionnaire ID
        /// </summary>
        /// <param name="questionnaireId">Questionnaire ID</param>
        /// <returns>True if successful</returns>
        Task<bool> DeleteByQuestionnaireIdAsync(long questionnaireId);

        /// <summary>
        /// Update section order
        /// </summary>
        /// <param name="sectionId">Section ID</param>
        /// <param name="newOrder">New order</param>
        /// <returns>Update result</returns>
        Task<bool> UpdateOrderAsync(long sectionId, int newOrder);

        /// <summary>
        /// Batch update section orders
        /// </summary>
        /// <param name="orderUpdates">List of (SectionId, Order) tuples</param>
        /// <returns>Update result</returns>
        Task<bool> BatchUpdateOrderAsync(List<(long SectionId, int Order)> orderUpdates);

        /// <summary>
        /// Check if section name exists in questionnaire
        /// </summary>
        /// <param name="questionnaireId">Questionnaire ID</param>
        /// <param name="title">Section title</param>
        /// <param name="excludeId">Exclude section ID (for update)</param>
        /// <returns>True if exists</returns>
        Task<bool> ExistsTitleInQuestionnaireAsync(long questionnaireId, string title, long? excludeId = null);

        /// <summary>
        /// Get next order number for questionnaire
        /// </summary>
        /// <param name="questionnaireId">Questionnaire ID</param>
        /// <returns>Next order number</returns>
        Task<int> GetNextOrderAsync(long questionnaireId);

        /// <summary>
        /// Get active sections by questionnaire ID (ordered)
        /// </summary>
        /// <param name="questionnaireId">Questionnaire ID</param>
        /// <returns>List of active sections</returns>
        Task<List<QuestionnaireSection>> GetActiveByQuestionnaireIdAsync(long questionnaireId);
    }
} 
