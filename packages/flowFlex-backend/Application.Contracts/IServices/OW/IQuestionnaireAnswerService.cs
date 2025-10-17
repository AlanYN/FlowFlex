using System.Collections.Generic;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.QuestionnaireAnswer;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Attr;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Questionnaire Answer Management Service Interface
    /// </summary>
    public interface IQuestionnaireAnswerService : IScopedService
    {
        /// <summary>
        /// Save stage questionnaire answer
        /// </summary>
        /// <param name="input">Input parameters</param>
        /// <returns>Whether successful</returns>
        Task<bool> SaveAnswerAsync(QuestionnaireAnswerInputDto input);

        /// <summary>
        /// Get stage questionnaire answer
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Questionnaire answer</returns>
        Task<QuestionnaireAnswerOutputDto?> GetAnswerAsync(long onboardingId, long stageId);

        /// <summary>
        /// Get all stage questionnaire answers (including multiple versions)
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Questionnaire answer list</returns>
        Task<List<QuestionnaireAnswerOutputDto>> GetAllAnswersAsync(long onboardingId, long stageId);

        /// <summary>
        /// Update questionnaire answer
        /// </summary>
        /// <param name="answerId">Answer ID</param>
        /// <param name="input">Update parameters</param>
        /// <returns>Whether successful</returns>
        Task<bool> UpdateAnswerAsync(long answerId, QuestionnaireAnswerUpdateDto input);

        /// <summary>
        /// Delete questionnaire answer
        /// </summary>
        /// <param name="answerId">Answer ID</param>
        /// <returns>Whether successful</returns>
        Task<bool> DeleteAnswerAsync(long answerId);

        /// <summary>
        /// Get all answers for onboarding
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>Answer list</returns>
        Task<List<QuestionnaireAnswerOutputDto>> GetOnboardingAnswersAsync(long onboardingId);

        /// <summary>
        /// Get answer history versions
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>History version list</returns>
        Task<List<QuestionnaireAnswerOutputDto>> GetAnswerHistoryAsync(long onboardingId, long stageId);

        /// <summary>
        /// Submit answer for review
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <param name="questionnaireId">Questionnaire ID</param>
        /// <returns>Whether successful</returns>
        Task<bool> SubmitAnswerAsync(long onboardingId, long stageId, long questionnaireId);

        /// <summary>
        /// Review answers
        /// </summary>
        /// <param name="input">Review parameters</param>
        /// <returns>Whether successful</returns>
        Task<bool> ReviewAnswersAsync(QuestionnaireAnswerReviewDto input);

        /// <summary>
        /// Get answer statistics
        /// </summary>
        /// <param name="stageId">Stage ID (optional)</param>
        /// <param name="days">Statistics days</param>
        /// <returns>Statistics information</returns>
        Task<Dictionary<string, object>> GetStatisticsAsync(long? stageId = null, int days = 30);

        /// <summary>
        /// Get answer list by status
        /// </summary>
        /// <param name="status">Status</param>
        /// <param name="days">Recent days</param>
        /// <returns>Answer list</returns>
        Task<List<QuestionnaireAnswerOutputDto>> GetAnswersByStatusAsync(string status, int days = 30);

        /// <summary>
        /// Batch get questionnaire answers for multiple Stages
        /// </summary>
        /// <param name="request">Batch query request</param>
        /// <returns>Batch answer response</returns>
        Task<BatchStageAnswerResponse> GetAnswersBatchAsync(BatchStageAnswerRequest request);
    }
}
