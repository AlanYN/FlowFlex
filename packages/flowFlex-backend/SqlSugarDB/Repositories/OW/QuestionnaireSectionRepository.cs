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
            return await db.Queryable<QuestionnaireSection>()
                .Where(x => x.QuestionnaireId == questionnaireId && x.IsValid)
                .ToListAsync();
        }

        public async Task<List<QuestionnaireSection>> GetOrderedByQuestionnaireIdAsync(long questionnaireId)
        {
            return await db.Queryable<QuestionnaireSection>()
                .Where(x => x.QuestionnaireId == questionnaireId && x.IsValid)
                .OrderBy(x => x.Order)
                .OrderBy(x => x.Id)
                .ToListAsync();
        }

        public async Task<List<QuestionnaireSection>> GetByQuestionnaireIdsAsync(List<long> questionnaireIds)
        {
            if (questionnaireIds == null || !questionnaireIds.Any())
            {
                return new List<QuestionnaireSection>();
            }

            return await db.Queryable<QuestionnaireSection>()
                .Where(x => questionnaireIds.Contains(x.QuestionnaireId) && x.IsValid)
                .OrderBy(x => x.QuestionnaireId)
                .OrderBy(x => x.Order)
                .OrderBy(x => x.Id)
                .ToListAsync();
        }

        public async Task<bool> DeleteByQuestionnaireIdAsync(long questionnaireId)
        {
            var result = await db.Updateable<QuestionnaireSection>()
                .SetColumns(x => new QuestionnaireSection { IsValid = false })
                .Where(x => x.QuestionnaireId == questionnaireId && x.IsValid)
                .ExecuteCommandAsync();

            return result > 0;
        }

        public async Task<bool> UpdateOrderAsync(long sectionId, int newOrder)
        {
            var result = await db.Updateable<QuestionnaireSection>()
                .SetColumns(x => x.Order == newOrder)
                .Where(x => x.Id == sectionId)
                .ExecuteCommandAsync();

            return result > 0;
        }

        public async Task<bool> BatchUpdateOrderAsync(List<(long SectionId, int Order)> orderUpdates)
        {
            if (!orderUpdates.Any())
                return true;

            var updateList = orderUpdates.Select(x => new QuestionnaireSection
            {
                Id = x.SectionId,
                Order = x.Order
            }).ToList();

            var result = await db.Updateable(updateList)
                .UpdateColumns(x => new { x.Order })
                .ExecuteCommandAsync();

            return result > 0;
        }

        public async Task<bool> ExistsTitleInQuestionnaireAsync(long questionnaireId, string title, long? excludeId = null)
        {
            var query = db.Queryable<QuestionnaireSection>()
                .Where(x => x.QuestionnaireId == questionnaireId && x.Title == title && x.IsValid);

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<int> GetNextOrderAsync(long questionnaireId)
        {
            var maxOrder = await db.Queryable<QuestionnaireSection>()
                .Where(x => x.QuestionnaireId == questionnaireId && x.IsValid)
                .MaxAsync(x => x.Order);

            return maxOrder + 1;
        }

        /// <summary>
        /// Get active sections by questionnaire ID (sorted by order)
        /// </summary>
        public async Task<List<QuestionnaireSection>> GetActiveByQuestionnaireIdAsync(long questionnaireId)
        {
            return await db.Queryable<QuestionnaireSection>()
                .Where(x => x.QuestionnaireId == questionnaireId && x.IsValid && x.IsActive == true)
                .OrderBy(x => x.Order)
                .OrderBy(x => x.Id)
                .ToListAsync();
        }

        /// <summary>
        /// Batch update section order
        /// </summary>
        public async Task<bool> UpdateSectionOrdersAsync(List<(long SectionId, int OrderIndex)> sectionOrders)
        {
            if (!sectionOrders.Any())
                return true;

            var updateList = sectionOrders.Select(x => new QuestionnaireSection
            {
                Id = x.SectionId,
                Order = x.OrderIndex
            }).ToList();

            var result = await db.Updateable(updateList)
                .UpdateColumns(x => new { x.Order })
                .ExecuteCommandAsync();

            return result > 0;
        }

        /// <summary>
        /// Update section status
        /// </summary>
        public async Task<bool> UpdateSectionStatusAsync(long sectionId, bool isActive)
        {
            var result = await db.Updateable<QuestionnaireSection>()
                .SetColumns(x => new QuestionnaireSection
                {
                    IsActive = isActive,
                    ModifyDate = DateTimeOffset.Now
                })
                .Where(x => x.Id == sectionId && x.IsValid == true)
                .ExecuteCommandAsync();

            return result > 0;
        }
    }
}
