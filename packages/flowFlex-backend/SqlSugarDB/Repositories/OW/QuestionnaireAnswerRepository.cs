using SqlSugar;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// Questionnaire answer repository implementation
    /// </summary>
    public class QuestionnaireAnswerRepository : BaseRepository<QuestionnaireAnswer>, IQuestionnaireAnswerRepository, IScopedService
    {
        public QuestionnaireAnswerRepository(ISqlSugarClient dbContext) : base(dbContext) { }

        /// <summary>
        /// Get answer by Onboarding ID and Stage ID
        /// </summary>
        public async Task<QuestionnaireAnswer?> GetByOnboardingAndStageAsync(long onboardingId, long stageId)
        {
            return await base.GetFirstAsync(x => x.OnboardingId == onboardingId && x.StageId == stageId && x.IsLatest && x.IsValid);
        }

        /// <summary>
        /// Get answer by Onboarding ID, Stage ID and Questionnaire ID
        /// </summary>
        public async Task<QuestionnaireAnswer?> GetByOnboardingStageAndQuestionnaireAsync(long onboardingId, long stageId, long questionnaireId)
        {
            return await base.GetFirstAsync(x => x.OnboardingId == onboardingId && x.StageId == stageId && x.QuestionnaireId == questionnaireId && x.IsLatest && x.IsValid);
        }

        /// <summary>
        /// Get all answers by Onboarding ID
        /// </summary>
        public async Task<List<QuestionnaireAnswer>> GetByOnboardingIdAsync(long onboardingId)
        {
            return await base.GetListAsync(x => x.OnboardingId == onboardingId && x.IsValid);
        }

        /// <summary>
        /// Get answer list by Questionnaire ID
        /// </summary>
        public async Task<List<QuestionnaireAnswer>> GetByQuestionnaireIdAsync(long questionnaireId)
        {
            return await base.GetListAsync(x => x.QuestionnaireId == questionnaireId && x.IsValid);
        }

        /// <summary>
        /// Get latest version of answer
        /// </summary>
        public async Task<QuestionnaireAnswer?> GetLatestVersionAsync(long onboardingId, long stageId)
        {
            return await base.GetFirstAsync(x => x.OnboardingId == onboardingId && x.StageId == stageId && x.IsLatest && x.IsValid);
        }

        /// <summary>
        /// Get answer version history
        /// </summary>
        public async Task<List<QuestionnaireAnswer>> GetVersionHistoryAsync(long onboardingId, long stageId)
        {
            return await base.GetListAsync(x => x.OnboardingId == onboardingId && x.StageId == stageId && x.IsValid, x => x.CreateDate, SqlSugar.OrderByType.Desc);
        }

        /// <summary>
        /// Update to non-latest version
        /// </summary>
        public async Task<bool> SetNotLatestAsync(long onboardingId, long stageId)
        {
            return await base.UpdateAsync(x => new QuestionnaireAnswer { IsLatest = false },
                x => x.OnboardingId == onboardingId && x.StageId == stageId && x.IsValid);
        }

        /// <summary>
        /// Get answer list by status
        /// </summary>
        public async Task<List<QuestionnaireAnswer>> GetByStatusAsync(string status, int days = 30)
        {
            var startDate = DateTimeOffset.Now.AddDays(-days);
            return await base.GetListAsync(x => x.Status == status && x.CreateDate >= startDate && x.IsValid);
        }

        /// <summary>
        /// Get completion statistics
        /// </summary>
        public async Task<Dictionary<string, object>> GetCompletionStatisticsAsync(long? stageId = null)
        {
            var query = db.Queryable<QuestionnaireAnswer>().Where(x => x.IsValid);

            if (stageId.HasValue)
                query = query.Where(x => x.StageId == stageId.Value);

            var totalCount = await query.CountAsync();
            var completedCount = await query.Where(x => x.Status == "Completed").CountAsync();
            var avgCompletionRate = await query.AvgAsync(x => x.CompletionRate);

            return new Dictionary<string, object>
            {
                ["total"] = totalCount,
                ["completed"] = completedCount,
                ["completionRate"] = totalCount > 0 ? (double)completedCount / totalCount * 100 : 0,
                ["averageCompletionRate"] = avgCompletionRate
            };
        }

        /// <summary>
        /// Batch update status
        /// </summary>
        public async Task<bool> BatchUpdateStatusAsync(List<long> ids, string status, long? reviewerId = null, string reviewNotes = "")
        {
            var updateData = new QuestionnaireAnswer
            {
                Status = status,
                ModifyDate = DateTimeOffset.Now
            };

            if (reviewerId.HasValue)
            {
                updateData.ReviewerId = reviewerId.Value;
                updateData.ReviewNotes = reviewNotes;
                updateData.ReviewTime = DateTimeOffset.Now;
            }

            return await base.UpdateAsync(x => updateData, x => ids.Contains(x.Id) && x.IsValid);
        }

        /// <summary>
        /// Get answers by Onboarding ID and multiple Stage IDs
        /// </summary>
        public async Task<List<QuestionnaireAnswer>> GetByOnboardingAndStageIdsAsync(long onboardingId, List<long> stageIds)
        {
            if (stageIds == null || !stageIds.Any())
            {
                return new List<QuestionnaireAnswer>();
            }

            return await base.GetListAsync(
                x => x.OnboardingId == onboardingId &&
                     stageIds.Contains(x.StageId) &&
                     x.IsLatest &&
                     x.IsValid,
                x => x.StageId,
                SqlSugar.OrderByType.Asc
            );
        }

        /// <summary>
        /// Get all answers by Onboarding ID and Stage ID (including multiple versions)
        /// </summary>
        public async Task<List<QuestionnaireAnswer>> GetAllByOnboardingAndStageAsync(long onboardingId, long stageId)
        {
            return await base.GetListAsync(
                x => x.OnboardingId == onboardingId &&
                     x.StageId == stageId &&
                     x.IsValid,
                x => x.CreateDate,
                SqlSugar.OrderByType.Desc
            );
        }
    }
}
