using System.Collections.Generic;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.Questionnaire;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Questionnaire Section service interface
    /// </summary>
    public interface IQuestionnaireSectionService : IScopedService
    {
        /// <summary>
        /// Get sections by questionnaire ID
        /// </summary>
        Task<List<QuestionnaireSectionDto>> GetByQuestionnaireIdAsync(long questionnaireId);

        /// <summary>
        /// Get section by ID
        /// </summary>
        Task<QuestionnaireSectionDto> GetByIdAsync(long id);

        /// <summary>
        /// Create section
        /// </summary>
        Task<long> CreateAsync(QuestionnaireSectionInputDto input);

        /// <summary>
        /// Update section
        /// </summary>
        Task<bool> UpdateAsync(long id, QuestionnaireSectionInputDto input);

        /// <summary>
        /// Delete section
        /// </summary>
        Task<bool> DeleteAsync(long id);

        /// <summary>
        /// Update section order
        /// </summary>
        Task<bool> UpdateOrderAsync(long id, int order);

        /// <summary>
        /// Batch update section orders
        /// </summary>
        Task<bool> BatchUpdateOrderAsync(List<(long SectionId, int Order)> orderUpdates);
    }
}
