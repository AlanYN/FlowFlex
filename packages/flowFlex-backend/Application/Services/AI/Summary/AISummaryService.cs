using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.IServices.AI;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Services.AI.Summary
{
    /// <summary>
    /// AI summary service implementation.
    /// Responsible for stage summary generation.
    /// Migrated from AIService.Summary.cs
    /// </summary>
    public class AISummaryService : AIServiceBase, IAISummaryService, IScopedService
    {
        private readonly IAIProviderAdapter _providerAdapter;
        private readonly IAIPromptBuilder _promptBuilder;
        private readonly IAIResponseParser _responseParser;

        public AISummaryService(
            IAIProviderAdapter providerAdapter,
            IAIPromptBuilder promptBuilder,
            IAIResponseParser responseParser,
            // Base class dependencies
            ILogger<AISummaryService> logger,
            IAIPromptHistoryRepository promptHistoryRepository,
            IOperatorContextService operatorContextService,
            IHttpContextAccessor httpContextAccessor,
            IBackgroundTaskQueue backgroundTaskQueue)
            : base(logger, promptHistoryRepository, operatorContextService, httpContextAccessor, backgroundTaskQueue)
        {
            _providerAdapter = providerAdapter;
            _promptBuilder = promptBuilder;
            _responseParser = responseParser;
        }

        #region GenerateStageSummaryAsync

        /// <summary>
        /// Generate AI summary for stage based on checklist tasks and questionnaire questions
        /// </summary>
        public async Task<AIStageSummaryResult> GenerateStageSummaryAsync(AIStageSummaryInput input)
        {
            var startTime = DateTime.UtcNow;
            string prompt = null;
            AIProviderResponse aiResponse = null;

            try
            {
                Logger.LogInformation("Generating AI summary for stage {StageId}: {StageName}", input.StageId, input.StageName);

                // Build the summary generation prompt via prompt builder
                prompt = _promptBuilder.BuildStageSummaryPrompt(input);

                // Try AI providers with fallback strategy via provider adapter
                aiResponse = await _providerAdapter.CallWithFallbackAsync(new AIProviderRequest
                {
                    Prompt = prompt,
                    ModelId = input.ModelId,
                    Provider = input.ModelProvider,
                    ModelName = input.ModelName
                });

                // Save prompt history to database (fire-and-forget)
                QueuePromptHistorySave(
                    "StageSummary", "Stage", prompt, aiResponse, startTime,
                    input.ModelProvider, input.ModelName, input.ModelId,
                    () => new
                    {
                        stageId = input.StageId,
                        onboardingId = input.OnboardingId,
                        stageName = input.StageName,
                        checklistTasksCount = input.ChecklistTasks?.Count ?? 0,
                        questionnaireQuestionsCount = input.QuestionnaireQuestions?.Count ?? 0,
                        staticFieldsCount = input.StaticFields?.Count ?? 0
                    });

                if (!aiResponse.Success)
                {
                    Logger.LogError("All AI providers failed for stage summary: {Error}", aiResponse.ErrorMessage);
                    return new AIStageSummaryResult
                    {
                        Success = false,
                        Message = $"AI service error: {aiResponse.ErrorMessage}"
                    };
                }

                // Parse the AI response via response parser
                var summaryResult = _responseParser.ParseStageSummaryResponse(aiResponse.Content, input);

                Logger.LogInformation("Successfully generated AI summary for stage {StageId}", input.StageId);
                return summaryResult;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error generating AI summary for stage {StageId}: {Error}", input.StageId, ex.Message);

                // Save failed prompt history (fire-and-forget)
                if (!string.IsNullOrEmpty(prompt))
                {
                    QueueFailedPromptHistorySave(
                        "StageSummary", "Stage", prompt, ex, startTime,
                        input.ModelProvider, input.ModelName, input.ModelId,
                        () => new
                        {
                            stageId = input.StageId,
                            onboardingId = input.OnboardingId,
                            stageName = input.StageName
                        });
                }

                return new AIStageSummaryResult
                {
                    Success = false,
                    Message = $"Failed to generate stage summary: {ex.Message}"
                };
            }
        }

        #endregion
    }
}
