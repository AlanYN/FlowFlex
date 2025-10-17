using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// Questionnaire answer repository interface
    /// </summary>
    public interface IQuestionnaireAnswerRepository : IBaseRepository<QuestionnaireAnswer>
    {
        /// <summary>
        /// Get answer by Onboarding ID and Stage ID
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <param name="questionnaireId">Questionnaire ID</param>
        /// <returns>Questionnaire answer</returns>
        Task<QuestionnaireAnswer?> GetByOnboardingAndStageAsync(long onboardingId, long stageId, long questionnaireId = 0);

        /// <summary>
        /// Get answer by Onboarding ID, Stage ID and Questionnaire ID
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <param name="questionnaireId">Questionnaire ID</param>
        /// <returns>Questionnaire answer</returns>
        Task<QuestionnaireAnswer?> GetByOnboardingStageAndQuestionnaireAsync(long onboardingId, long stageId, long questionnaireId);

        /// <summary>
        /// Get all answers by Onboarding ID
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>Answer list</returns>
        Task<List<QuestionnaireAnswer>> GetByOnboardingIdAsync(long onboardingId);

        /// <summary>
        /// Get answer list by Questionnaire ID
        /// </summary>
        /// <param name="questionnaireId">Questionnaire ID</param>
        /// <returns>Answer list</returns>
        Task<List<QuestionnaireAnswer>> GetByQuestionnaireIdAsync(long questionnaireId);

        /// <summary>
        /// Get latest version answer
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Latest answer</returns>
        Task<QuestionnaireAnswer?> GetLatestVersionAsync(long onboardingId, long stageId);

        /// <summary>
        /// Get answer version history
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Version history list</returns>
        Task<List<QuestionnaireAnswer>> GetVersionHistoryAsync(long onboardingId, long stageId);

        /// <summary>
        /// Update to non-latest version
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Update result</returns>
        Task<bool> SetNotLatestAsync(long onboardingId, long stageId);

        /// <summary>
        /// Get answer list by status
        /// </summary>
        /// <param name="status">Status</param>
        /// <param name="days">Recent days</param>
        /// <returns>Answer list</returns>
        Task<List<QuestionnaireAnswer>> GetByStatusAsync(string status, int days = 30);

        /// <summary>
        /// Get completion statistics
        /// </summary>
        /// <param name="stageId">Stage ID (optional)</param>
        /// <returns>Statistics information</returns>
        Task<Dictionary<string, object>> GetCompletionStatisticsAsync(long? stageId = null);

        /// <summary>
        /// Batch update status
        /// </summary>
        /// <param name="ids">ID list</param>
        /// <param name="status">New status</param>
        /// <param name="reviewerId">Reviewer ID</param>
        /// <param name="reviewNotes">Review notes</param>
        /// <returns>Update result</returns>
        Task<bool> BatchUpdateStatusAsync(List<long> ids, string status, long? reviewerId = null, string reviewNotes = "");

        /// <summary>
        /// Get answers by Onboarding ID and multiple Stage IDs
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageIds">Stage ID list</param>
        /// <returns>Answer list</returns>
        Task<List<QuestionnaireAnswer>> GetByOnboardingAndStageIdsAsync(long onboardingId, List<long> stageIds);

        /// <summary>
        /// Get all answers by Onboarding ID and Stage ID (including multiple versions)
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Answer list</returns>
        Task<List<QuestionnaireAnswer>> GetAllByOnboardingAndStageAsync(long onboardingId, long stageId);
    }
}
