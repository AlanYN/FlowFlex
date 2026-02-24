using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.IServices.AI;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Services.AI.Requirements
{
    /// <summary>
    /// AI requirements parsing service implementation.
    /// Responsible for parsing natural language into structured requirements.
    /// Migrated from AIService.Main.cs
    /// </summary>
    public class AIRequirementsParsingService : AIServiceBase, IAIRequirementsParsingService, IScopedService
    {
        private readonly IAIProviderAdapter _providerAdapter;
        private readonly IAIPromptBuilder _promptBuilder;
        private readonly IAIResponseParser _responseParser;

        public AIRequirementsParsingService(
            IAIProviderAdapter providerAdapter,
            IAIPromptBuilder promptBuilder,
            IAIResponseParser responseParser,
            // Base class dependencies
            ILogger<AIRequirementsParsingService> logger,
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

        #region ParseRequirementsAsync

        /// <summary>
        /// Parse natural language into structured requirements using default provider config
        /// </summary>
        public async Task<AIRequirementsParsingResult> ParseRequirementsAsync(string naturalLanguage)
        {
            return await ParseRequirementsInternalAsync(naturalLanguage, null, null, null);
        }

        /// <summary>
        /// Parse natural language into structured requirements with explicit AI model override
        /// </summary>
        public async Task<AIRequirementsParsingResult> ParseRequirementsAsync(
            string naturalLanguage, string? modelProvider, string? modelName, string? modelId)
        {
            return await ParseRequirementsInternalAsync(naturalLanguage, modelProvider, modelName, modelId);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Internal implementation for requirements parsing shared by both overloads
        /// </summary>
        private async Task<AIRequirementsParsingResult> ParseRequirementsInternalAsync(
            string naturalLanguage, string? modelProvider, string? modelName, string? modelId)
        {
            var startTime = DateTime.UtcNow;
            string prompt = null;
            AIProviderResponse aiResponse = null;
            var hasExplicitOverride = modelProvider != null || modelName != null || modelId != null;

            try
            {
                if (hasExplicitOverride)
                {
                    Logger.LogInformation(
                        "Parsing requirements with explicit model override: Provider={Provider}, Model={ModelName}, Id={ModelId}",
                        modelProvider, modelName, modelId);
                }
                else
                {
                    Logger.LogInformation("Parsing requirements from natural language");
                }

                // Build prompt via prompt builder
                prompt = _promptBuilder.BuildRequirementsParsingPrompt(naturalLanguage);

                // Call AI provider via provider adapter
                aiResponse = await _providerAdapter.CallAsync(new AIProviderRequest
                {
                    Prompt = prompt,
                    ModelId = modelId,
                    Provider = modelProvider,
                    ModelName = modelName
                });

                // Save prompt history to database (fire-and-forget)
                QueuePromptHistorySave(
                    "RequirementsParsing", "Requirements", prompt, aiResponse, startTime,
                    modelProvider, modelName, modelId,
                    () => new
                    {
                        naturalLanguageLength = naturalLanguage?.Length ?? 0,
                        inputText = naturalLanguage?.Substring(0, Math.Min(200, naturalLanguage?.Length ?? 0)),
                        explicitModelOverride = hasExplicitOverride
                    });

                if (!aiResponse.Success)
                {
                    return new AIRequirementsParsingResult
                    {
                        Success = false,
                        Message = aiResponse.ErrorMessage
                    };
                }

                // Parse the AI response via response parser
                var result = _responseParser.ParseRequirementsResponse(aiResponse.Content);
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error parsing requirements");

                // Save failed prompt history (fire-and-forget)
                if (!string.IsNullOrEmpty(prompt))
                {
                    QueueFailedPromptHistorySave(
                        "RequirementsParsing", "Requirements", prompt, ex, startTime,
                        modelProvider, modelName, modelId,
                        () => new
                        {
                            naturalLanguageLength = naturalLanguage?.Length ?? 0,
                            explicitModelOverride = hasExplicitOverride
                        });
                }

                return new AIRequirementsParsingResult
                {
                    Success = false,
                    Message = $"Failed to parse requirements: {ex.Message}"
                };
            }
        }

        #endregion
    }
}
