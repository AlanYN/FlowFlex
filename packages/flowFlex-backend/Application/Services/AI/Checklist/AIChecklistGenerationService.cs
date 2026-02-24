using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.IServices.AI;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Services.AI.Checklist
{
    /// <summary>
    /// AI checklist generation service implementation.
    /// Responsible for checklist generation (synchronous and streaming).
    /// Migrated from AIService.Generation.cs
    /// </summary>
    public class AIChecklistGenerationService : AIServiceBase, IAIChecklistGenerationService, IScopedService
    {
        private readonly IAIProviderAdapter _providerAdapter;
        private readonly IAIPromptBuilder _promptBuilder;
        private readonly IAIResponseParser _responseParser;

        public AIChecklistGenerationService(
            IAIProviderAdapter providerAdapter,
            IAIPromptBuilder promptBuilder,
            IAIResponseParser responseParser,
            // Base class dependencies
            ILogger<AIChecklistGenerationService> logger,
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

        #region GenerateChecklistAsync

        /// <summary>
        /// Generate checklist from natural language description
        /// </summary>
        public async Task<AIChecklistGenerationResult> GenerateChecklistAsync(AIChecklistGenerationInput input)
        {
            return await ExecuteGenerationAsync<AIChecklistGenerationResult>(
                providerAdapter: _providerAdapter,
                operationType: "ChecklistGeneration",
                entityType: "Checklist",
                buildPrompt: () => _promptBuilder.BuildChecklistGenerationPrompt(input),
                parseResponse: content => _responseParser.ParseChecklistResponse(content),
                calculateConfidence: result => _responseParser.CalculateChecklistConfidenceScore(result.GeneratedChecklist),
                buildMetadata: () => new
                {
                    processName = input.ProcessName,
                    team = input.Team,
                    requiredStepsCount = input.RequiredSteps?.Count ?? 0
                },
                contextDescription: $"Process: {input.ProcessName}"
            );
        }

        #endregion

        #region StreamGenerateChecklistAsync

        /// <summary>
        /// Stream generate checklist with real-time updates
        /// </summary>
        public async IAsyncEnumerable<AIChecklistStreamResult> StreamGenerateChecklistAsync(AIChecklistGenerationInput input)
        {
            Logger.LogInformation("Starting streaming checklist generation: {ProcessName}", input.ProcessName);

            yield return new AIChecklistStreamResult
            {
                Type = "start",
                Message = "Starting checklist generation...",
                IsComplete = false
            };

            string prompt = null;
            AIProviderResponse aiResponse = null;
            AIChecklistGenerationResult result = null;
            Exception caughtException = null;
            var startTime = DateTime.UtcNow;

            try
            {
                prompt = _promptBuilder.BuildChecklistGenerationPrompt(input);
                aiResponse = await _providerAdapter.CallAsync(new AIProviderRequest
                {
                    Prompt = prompt
                });

                if (aiResponse.Success)
                {
                    result = _responseParser.ParseChecklistResponse(aiResponse.Content);
                }

                // Save prompt history (fire-and-forget)
                QueuePromptHistorySave(
                    "ChecklistGeneration", "Checklist", prompt, aiResponse, startTime,
                    null, null, null,
                    () => new
                    {
                        processName = input.ProcessName,
                        team = input.Team,
                        requiredStepsCount = input.RequiredSteps?.Count ?? 0,
                        streaming = true
                    });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in streaming checklist generation");
                caughtException = ex;

                // Save failed prompt history
                if (!string.IsNullOrEmpty(prompt))
                {
                    QueueFailedPromptHistorySave(
                        "ChecklistGeneration", "Checklist", prompt, ex, startTime,
                        null, null, null,
                        () => new
                        {
                            processName = input.ProcessName,
                            team = input.Team,
                            requiredStepsCount = input.RequiredSteps?.Count ?? 0,
                            streaming = true
                        });
                }
            }

            if (caughtException != null)
            {
                yield return new AIChecklistStreamResult
                {
                    Type = "error",
                    Message = $"Error during generation: {caughtException.Message}",
                    IsComplete = true
                };
                yield break;
            }

            if (aiResponse == null || !aiResponse.Success)
            {
                yield return new AIChecklistStreamResult
                {
                    Type = "error",
                    Message = aiResponse?.ErrorMessage ?? "AI service call failed",
                    IsComplete = true
                };
                yield break;
            }

            if (result?.GeneratedChecklist != null)
            {
                yield return new AIChecklistStreamResult
                {
                    Type = "checklist",
                    Data = result.GeneratedChecklist,
                    Message = "Checklist basic information generated",
                    IsComplete = false
                };

                yield return new AIChecklistStreamResult
                {
                    Type = "complete",
                    Data = result,
                    Message = "Checklist generation completed",
                    IsComplete = true
                };
            }
            else
            {
                yield return new AIChecklistStreamResult
                {
                    Type = "error",
                    Message = "Unable to parse AI-generated checklist structure",
                    IsComplete = true
                };
            }
        }

        #endregion
    }
}
