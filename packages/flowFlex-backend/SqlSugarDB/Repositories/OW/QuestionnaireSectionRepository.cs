using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using SqlSugar;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// Questionnaire Section repository implementation
    /// </summary>
    public class QuestionnaireSectionRepository : BaseRepository<QuestionnaireSection>, IQuestionnaireSectionRepository, IScopedService
    {
        public QuestionnaireSectionRepository(ISqlSugarClient context) : base(context)
        {
        }

        public async Task<List<QuestionnaireSection>> GetByQuestionnaireIdAsync(long questionnaireId)
        {
            // Table ff_questionnaire_section has been removed; return empty list to keep backward compatibility
            return await Task.FromResult(new List<QuestionnaireSection>());
        }

        public async Task<List<QuestionnaireSection>> GetOrderedByQuestionnaireIdAsync(long questionnaireId)
        {
            // Table removed; no-op return
            return await Task.FromResult(new List<QuestionnaireSection>());
        }

        public async Task<List<QuestionnaireSection>> GetByQuestionnaireIdsAsync(List<long> questionnaireIds)
        {
            // Table removed; always return empty list
            return await Task.FromResult(new List<QuestionnaireSection>());
        }

        public async Task<bool> DeleteByQuestionnaireIdAsync(long questionnaireId)
        {
            // Table removed; consider delete as success
            return await Task.FromResult(true);
        }

        public async Task<bool> UpdateOrderAsync(long sectionId, int newOrder)
        {
            // Table removed; treat as no-op success
            return await Task.FromResult(true);
        }

        public async Task<bool> BatchUpdateOrderAsync(List<(long SectionId, int Order)> orderUpdates)
        {
            // Table removed; treat as no-op success
            return await Task.FromResult(true);
        }

        public async Task<bool> ExistsTitleInQuestionnaireAsync(long questionnaireId, string title, long? excludeId = null)
        {
            // Table removed; there is no title conflict
            return await Task.FromResult(false);
        }

        public async Task<int> GetNextOrderAsync(long questionnaireId)
        {
            // Table removed; start from 1
            return await Task.FromResult(1);
        }

        /// <summary>
        /// Get active sections by questionnaire ID (sorted by order)
        /// </summary>
        public async Task<List<QuestionnaireSection>> GetActiveByQuestionnaireIdAsync(long questionnaireId)
        {
            // Table removed; return empty
            return await Task.FromResult(new List<QuestionnaireSection>());
        }

        /// <summary>
        /// Batch update section order
        /// </summary>
        public async Task<bool> UpdateSectionOrdersAsync(List<(long SectionId, int OrderIndex)> sectionOrders)
        {
            // Table removed; treat as no-op success
            return await Task.FromResult(true);
        }

        /// <summary>
        /// Update section status
        /// </summary>
        public async Task<bool> UpdateSectionStatusAsync(long sectionId, bool isActive)
        {
            // Table removed; treat as no-op success
            return await Task.FromResult(true);
        }
    }
}
