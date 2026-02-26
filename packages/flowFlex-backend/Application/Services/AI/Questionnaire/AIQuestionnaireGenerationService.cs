using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.IServices.AI;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using FlowFlex.Domain.Shared.Const;

namespace FlowFlex.Application.Services.AI.Questionnaire
{
    /// <summary>
    /// AI questionnaire generation service implementation.
    /// Responsible for questionnaire generation (synchronous and streaming).
    /// Migrated from AIService.Generation.cs
    /// </summary>
    public class AIQuestionnaireGenerationService : AIServiceBase, IAIQuestionnaireGenerationService, IScopedService
    {
        private readonly IAIProviderAdapter _providerAdapter;
        private readonly IAIPromptBuilder _promptBuilder;
        private readonly IAIResponseParser _responseParser;

        public AIQuestionnaireGenerationService(
            IAIProviderAdapter providerAdapter,
            IAIPromptBuilder promptBuilder,
            IAIResponseParser responseParser,
            // Base class dependencies
            ILogger<AIQuestionnaireGenerationService> logger,
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

        #region GenerateQuestionnaireAsync

        /// <summary>
        /// Generate questionnaire from natural language description
        /// </summary>
        public async Task<AIQuestionnaireGenerationResult> GenerateQuestionnaireAsync(AIQuestionnaireGenerationInput input)
        {
            return await ExecuteGenerationAsync<AIQuestionnaireGenerationResult>(
                providerAdapter: _providerAdapter,
                operationType: "QuestionnaireGeneration",
                entityType: "Questionnaire",
                buildPrompt: () => _promptBuilder.BuildQuestionnaireGenerationPrompt(input),
                parseResponse: content => _responseParser.ParseQuestionnaireResponse(content),
                calculateConfidence: result => _responseParser.CalculateQuestionnaireConfidenceScore(result.GeneratedQuestionnaire),
                buildMetadata: () => new
                {
                    purpose = input.Purpose,
                    targetAudience = input.TargetAudience,
                    estimatedQuestions = input.EstimatedQuestions
                },
                contextDescription: $"Purpose: {input.Purpose}"
            );
        }

        #endregion

        #region StreamGenerateQuestionnaireAsync

        /// <summary>
        /// Stream generate questionnaire with real-time updates
        /// </summary>
        public async IAsyncEnumerable<AIQuestionnaireStreamResult> StreamGenerateQuestionnaireAsync(AIQuestionnaireGenerationInput input)
        {
            Logger.LogInformation("Starting streaming questionnaire generation: {Purpose}", input.Purpose);

            yield return new AIQuestionnaireStreamResult
            {
                Type = AIStreamResultTypes.Start,
                Message = "Starting questionnaire generation...",
                IsComplete = false
            };

            string prompt = null;
            AIProviderResponse aiResponse = null;
            AIQuestionnaireGenerationResult result = null;
            Exception caughtException = null;
            var startTime = DateTime.UtcNow;

            try
            {
                prompt = _promptBuilder.BuildQuestionnaireGenerationPrompt(input);
                aiResponse = await _providerAdapter.CallAsync(new AIProviderRequest
                {
                    Prompt = prompt
                });

                if (aiResponse.Success)
                {
                    result = _responseParser.ParseQuestionnaireResponse(aiResponse.Content);
                }

                // Save prompt history (fire-and-forget)
                QueuePromptHistorySave(
                    "QuestionnaireGeneration", "Questionnaire", prompt, aiResponse, startTime,
                    null, null, null,
                    () => new
                    {
                        purpose = input.Purpose,
                        targetAudience = input.TargetAudience,
                        estimatedQuestions = input.EstimatedQuestions,
                        streaming = true
                    });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in streaming questionnaire generation");
                caughtException = ex;

                // Save failed prompt history
                if (!string.IsNullOrEmpty(prompt))
                {
                    QueueFailedPromptHistorySave(
                        "QuestionnaireGeneration", "Questionnaire", prompt, ex, startTime,
                        null, null, null,
                        () => new
                        {
                            purpose = input.Purpose,
                            targetAudience = input.TargetAudience,
                            estimatedQuestions = input.EstimatedQuestions,
                            streaming = true
                        });
                }
            }

            if (caughtException != null)
            {
                yield return new AIQuestionnaireStreamResult
                {
                    Type = AIStreamResultTypes.Error,
                    Message = $"Error during generation: {caughtException.Message}",
                    IsComplete = true
                };
                yield break;
            }

            if (aiResponse == null || !aiResponse.Success)
            {
                yield return new AIQuestionnaireStreamResult
                {
                    Type = AIStreamResultTypes.Error,
                    Message = aiResponse?.ErrorMessage ?? "AI service call failed",
                    IsComplete = true
                };
                yield break;
            }

            if (result?.GeneratedQuestionnaire != null)
            {
                yield return new AIQuestionnaireStreamResult
                {
                    Type = AIStreamResultTypes.Questionnaire,
                    Data = result.GeneratedQuestionnaire,
                    Message = "Questionnaire basic information generated",
                    IsComplete = false
                };

                yield return new AIQuestionnaireStreamResult
                {
                    Type = AIStreamResultTypes.Complete,
                    Data = result,
                    Message = "Questionnaire generation completed",
                    IsComplete = true
                };
            }
            else
            {
                yield return new AIQuestionnaireStreamResult
                {
                    Type = AIStreamResultTypes.Error,
                    Message = "Unable to parse AI-generated questionnaire structure",
                    IsComplete = true
                };
            }
        }

        #endregion
    }
}
