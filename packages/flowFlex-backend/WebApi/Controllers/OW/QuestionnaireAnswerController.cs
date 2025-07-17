using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net;
using FlowFlex.Application.Contracts.Dtos.OW.QuestionnaireAnswer;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Shared.Attr;
using Item.Internal.StandardApi.Response;
using FlowFlex.WebApi.Model.Response;


namespace FlowFlex.WebApi.Controllers.OW
{
    /// <summary>
    /// Questionnaire Answer Management API
    /// </summary>

    [ApiController]

    [Route("ow/questionnaire-answers/v{version:apiVersion}")]
    [Display(Name = "questionnaire-answers")]

    public class QuestionnaireAnswerController : Controllers.ControllerBase
    {
        private readonly IQuestionnaireAnswerService _questionnaireAnswerService;

        public QuestionnaireAnswerController(IQuestionnaireAnswerService questionnaireAnswerService)
        {
            _questionnaireAnswerService = questionnaireAnswerService;
        }

        /// <summary>
        /// Save questionnaire answer
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <param name="input">Questionnaire answer data</param>
        /// <returns>Save result</returns>
        [HttpPost("{onboardingId}/stage/{stageId}/answer")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SaveAnswerAsync(long onboardingId, long stageId, [FromBody] QuestionnaireAnswerInputDto input)
        {
            // Ensure path parameters match input data
            input.OnboardingId = onboardingId;
            input.StageId = stageId;

            bool result = await _questionnaireAnswerService.SaveAnswerAsync(input);
            return Success(result);
        }

        /// <summary>
        /// 获取指定入职流程和阶段的问卷答案
        /// </summary>
        /// <param name="onboardingId">入职流程ID</param>
        /// <param name="stageId">阶段ID</param>
        /// <returns>问卷答案列表</returns>
        [HttpGet("{onboardingId}/stage/{stageId}/answer")]
        public async Task<IActionResult> GetAnswer(long onboardingId, long stageId)
        {
            var answers = await _questionnaireAnswerService.GetAllAnswersAsync(onboardingId, stageId);
            return Success(answers);
        }

        /// <summary>
        /// Update questionnaire answer
        /// </summary>
        /// <param name="answerId">Answer ID</param>
        /// <param name="input">Update data</param>
        /// <returns>Update result</returns>
        [HttpPut("{answerId}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdateAnswerAsync(long answerId, [FromBody] QuestionnaireAnswerUpdateDto input)
        {
            bool result = await _questionnaireAnswerService.UpdateAnswerAsync(answerId, input);
            return Success(result);
        }

        /// <summary>
        /// Delete questionnaire answer
        /// </summary>
        /// <param name="answerId">Answer ID</param>
        /// <returns>Delete result</returns>
        [HttpDelete("{answerId}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteAnswerAsync(long answerId)
        {
            bool result = await _questionnaireAnswerService.DeleteAnswerAsync(answerId);
            return Success(result);
        }

        /// <summary>
        /// Get all questionnaire answers for an onboarding
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>Answer list</returns>
        [HttpGet("{onboardingId}/answers")]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetOnboardingAnswersAsync(long onboardingId)
        {
            var result = await _questionnaireAnswerService.GetOnboardingAnswersAsync(onboardingId);
            return Success(result);
        }

        /// <summary>
        /// Get answer version history
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Version history list</returns>
        [HttpGet("{onboardingId}/stage/{stageId}/history")]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAnswerHistoryAsync(long onboardingId, long stageId)
        {
            var result = await _questionnaireAnswerService.GetAnswerHistoryAsync(onboardingId, stageId);
            return Success(result);
        }

        /// <summary>
        /// Submit questionnaire answer
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Submission result</returns>
        [HttpPost("{onboardingId}/stage/{stageId}/submit")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SubmitAnswerAsync(long onboardingId, long stageId)
        {
            bool result = await _questionnaireAnswerService.SubmitAnswerAsync(onboardingId, stageId);
            return Success(result);
        }

        /// <summary>
        /// Review questionnaire answers
        /// </summary>
        /// <param name="input">Review data</param>
        /// <returns>Review result</returns>
        [HttpPost("review")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> ReviewAnswersAsync([FromBody] QuestionnaireAnswerReviewDto input)
        {
            bool result = await _questionnaireAnswerService.ReviewAnswersAsync(input);
            return Success(result);
        }

        /// <summary>
        /// Get questionnaire answer statistics
        /// </summary>
        /// <param name="stageId">Stage ID (optional)</param>
        /// <param name="days">Statistics days</param>
        /// <returns>Statistics information</returns>
        [HttpGet("statistics")]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetStatisticsAsync([FromQuery] long? stageId = null, [FromQuery] int days = 30)
        {
            var result = await _questionnaireAnswerService.GetStatisticsAsync(stageId, days);
            return Success(result);
        }

        /// <summary>
        /// Get answer list by status
        /// </summary>
        /// <param name="status">Status</param>
        /// <param name="days">Recent days</param>
        /// <returns>Answer list</returns>
        [HttpGet("by-status/{status}")]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAnswersByStatusAsync(string status, [FromQuery] int days = 30)
        {
            var result = await _questionnaireAnswerService.GetAnswersByStatusAsync(status, days);
            return Success(result);
        }

        /// <summary>
        /// Batch get questionnaire answers by stage IDs
        /// </summary>
        /// <param name="request">Batch request</param>
        /// <returns>Batch answer response</returns>
        [HttpPost("batch/by-stages")]
        [ProducesResponseType<SuccessResponse<BatchStageAnswerResponse>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAnswersBatchAsync([FromBody] BatchStageAnswerRequest request)
        {
            var result = await _questionnaireAnswerService.GetAnswersBatchAsync(request);
            return Success(result);
        }
    }
}

