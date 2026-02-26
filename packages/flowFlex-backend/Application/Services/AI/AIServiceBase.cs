using FlowFlex.Application.Contracts.IServices.AI;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Infrastructure.Extensions;
using FlowFlex.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FlowFlex.Application.Services.AI
{
    /// <summary>
    /// Abstract base class for AI services providing cross-cutting concerns:
    /// - Prompt history saving (async via background task queue)
    /// - Client IP address and User-Agent extraction
    /// - Standardized generation execution with error handling
    /// - Failed result creation and confidence score setting
    /// </summary>
    public abstract class AIServiceBase
    {
        protected readonly ILogger Logger;
        protected readonly IAIPromptHistoryRepository PromptHistoryRepository;
        protected readonly IOperatorContextService OperatorContextService;
        protected readonly IHttpContextAccessor HttpContextAccessor;
        protected readonly IBackgroundTaskQueue BackgroundTaskQueue;

        protected AIServiceBase(
            ILogger logger,
            IAIPromptHistoryRepository promptHistoryRepository,
            IOperatorContextService operatorContextService,
            IHttpContextAccessor httpContextAccessor,
            IBackgroundTaskQueue backgroundTaskQueue)
        {
            Logger = logger;
            PromptHistoryRepository = promptHistoryRepository;
            OperatorContextService = operatorContextService;
            HttpContextAccessor = httpContextAccessor;
            BackgroundTaskQueue = backgroundTaskQueue;
        }

        #region Prompt History

        /// <summary>
        /// Save AI prompt history to database
        /// </summary>
        protected async Task SavePromptHistoryAsync(
            string promptType, string entityType, long? entityId, long? onboardingId,
            string promptContent, AIProviderResponse response, DateTime startTime,
            string modelProvider = null, string modelName = null, string modelId = null,
            string metadata = null)
        {
            try
            {
                var responseTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                object tokenUsageObj;
                object metadataObj;
                try
                {
                    tokenUsageObj = string.IsNullOrWhiteSpace(response?.TokenUsage)
                        ? new { }
                        : JsonSerializer.Deserialize<object>(response.TokenUsage);
                }
                catch
                {
                    tokenUsageObj = new { };
                }
                try
                {
                    metadataObj = string.IsNullOrWhiteSpace(metadata)
                        ? new { }
                        : JsonSerializer.Deserialize<object>(metadata);
                }
                catch
                {
                    metadataObj = new { };
                }

                var promptHistory = new AIPromptHistory
                {
                    PromptType = promptType ?? "Unknown",
                    EntityType = entityType ?? "Unknown",
                    EntityId = entityId,
                    OnboardingId = onboardingId,
                    ModelProvider = !string.IsNullOrEmpty(response?.Provider) ? response.Provider : (!string.IsNullOrEmpty(modelProvider) ? modelProvider : "Unknown"),
                    ModelName = !string.IsNullOrEmpty(response?.ModelName) ? response.ModelName : (modelName ?? "Unknown"),
                    ModelId = !string.IsNullOrEmpty(response?.ModelId) ? response.ModelId : (modelId ?? "Unknown"),
                    PromptContent = promptContent ?? "",
                    ResponseContent = response?.Content ?? "",
                    IsSuccess = response?.Success ?? false,
                    ErrorMessage = response?.ErrorMessage ?? "",
                    ResponseTimeMs = responseTime,
                    TokenUsage = tokenUsageObj,
                    Metadata = metadataObj,
                    UserId = OperatorContextService?.GetOperatorId() ?? 0,
                    UserName = OperatorContextService?.GetOperatorDisplayName() ?? "",
                    IpAddress = GetClientIpAddress(),
                    UserAgent = GetUserAgent(),
                    CreateBy = OperatorContextService?.GetOperatorDisplayName() ?? "",
                    ModifyBy = OperatorContextService?.GetOperatorDisplayName() ?? "",
                    CreateUserId = OperatorContextService?.GetOperatorId() ?? 0,
                    ModifyUserId = OperatorContextService?.GetOperatorId() ?? 0,
                    CreateDate = DateTimeOffset.UtcNow,
                    ModifyDate = DateTimeOffset.UtcNow,
                    IsValid = true
                };

                // Initialize ID
                promptHistory.InitNewId();

                // Save to database
                await PromptHistoryRepository.InsertAsync(promptHistory);

                Logger.LogDebug("Saved AI prompt history for {PromptType} - {EntityType}:{EntityId}",
                    promptType, entityType, entityId);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to save AI prompt history: {Error}", ex.Message);
                // Don't throw - this is a background operation
            }
        }

        /// <summary>
        /// Queue prompt history save as background task
        /// </summary>
        protected void QueuePromptHistorySave(
            string operationType,
            string entityType,
            string prompt,
            AIProviderResponse aiResponse,
            DateTime startTime,
            string modelProvider,
            string modelName,
            string modelId,
            Func<object> buildMetadata)
        {
            BackgroundTaskQueue.QueueBackgroundWorkItem(async token =>
            {
                try
                {
                    var metadata = JsonSerializer.Serialize(buildMetadata());
                    await SavePromptHistoryAsync(operationType, entityType, null, null,
                        prompt, aiResponse, startTime, modelProvider, modelName, modelId, metadata);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to save {OperationType} prompt history", operationType);
                }
            });
        }

        /// <summary>
        /// Queue failed prompt history save as background task
        /// </summary>
        protected void QueueFailedPromptHistorySave(
            string operationType,
            string entityType,
            string prompt,
            Exception error,
            DateTime startTime,
            string modelProvider,
            string modelName,
            string modelId,
            Func<object> buildMetadata)
        {
            BackgroundTaskQueue.QueueBackgroundWorkItem(async token =>
            {
                try
                {
                    var failedResponse = new AIProviderResponse
                    {
                        Success = false,
                        ErrorMessage = error.Message,
                        Content = ""
                    };
                    var metadata = JsonSerializer.Serialize(new
                    {
                        originalMetadata = buildMetadata(),
                        error = error.Message
                    });
                    await SavePromptHistoryAsync(operationType, entityType, null, null,
                        prompt, failedResponse, startTime, modelProvider, modelName, modelId, metadata);
                }
                catch (Exception saveEx)
                {
                    Logger.LogWarning(saveEx, "Failed to save failed {OperationType} prompt history", operationType);
                }
            });
        }

        #endregion

        #region HTTP Context Utilities

        /// <summary>
        /// Get client IP address from current HTTP context
        /// </summary>
        protected string GetClientIpAddress() => HttpContextAccessor.GetClientIpAddress();

        /// <summary>
        /// Get User-Agent string from current HTTP context
        /// </summary>
        protected string GetUserAgent() => HttpContextAccessor.GetUserAgent();

        #endregion

        #region Generation Helpers

        /// <summary>
        /// Execute AI generation with standardized error handling and prompt history logging
        /// </summary>
        /// <typeparam name="TResult">Result type</typeparam>
        /// <param name="providerAdapter">AI provider adapter for making calls</param>
        /// <param name="operationType">Type of generation operation (e.g., "WorkflowGeneration")</param>
        /// <param name="entityType">Entity type being generated (e.g., "Workflow")</param>
        /// <param name="buildPrompt">Function to build the prompt</param>
        /// <param name="parseResponse">Function to parse AI response into result</param>
        /// <param name="calculateConfidence">Function to calculate confidence score</param>
        /// <param name="buildMetadata">Function to build metadata for logging</param>
        /// <param name="modelId">Optional model ID</param>
        /// <param name="modelProvider">Optional model provider</param>
        /// <param name="modelName">Optional model name</param>
        /// <param name="maxTokens">Optional max tokens override</param>
        /// <param name="contextDescription">Description for logging context</param>
        /// <returns>Generation result</returns>
        protected async Task<TResult> ExecuteGenerationAsync<TResult>(
            IAIProviderAdapter providerAdapter,
            string operationType,
            string entityType,
            Func<string> buildPrompt,
            Func<string, TResult> parseResponse,
            Func<TResult, double> calculateConfidence,
            Func<object> buildMetadata,
            string modelId = null,
            string modelProvider = null,
            string modelName = null,
            int? maxTokens = null,
            string contextDescription = null) where TResult : class, new()
        {
            var startTime = DateTime.UtcNow;
            string prompt = null;
            AIProviderResponse aiResponse = null;

            try
            {
                Logger.LogInformation("Starting {OperationType}: {Context}", operationType, contextDescription ?? "N/A");

                prompt = buildPrompt();
                aiResponse = await providerAdapter.CallAsync(new AIProviderRequest
                {
                    Prompt = prompt,
                    ModelId = modelId,
                    Provider = modelProvider,
                    ModelName = modelName,
                    MaxTokensOverride = maxTokens
                });

                // Save prompt history to database (fire-and-forget)
                QueuePromptHistorySave(operationType, entityType, prompt, aiResponse, startTime, modelProvider, modelName, modelId, buildMetadata);

                if (!aiResponse.Success)
                {
                    return CreateFailedResult<TResult>(aiResponse.ErrorMessage);
                }

                var result = parseResponse(aiResponse.Content);
                SetConfidenceScore(result, calculateConfidence(result));

                Logger.LogInformation("Successfully completed {OperationType}", operationType);
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during {OperationType}: {Context}", operationType, contextDescription ?? "N/A");

                // Save failed prompt history
                if (!string.IsNullOrEmpty(prompt))
                {
                    QueueFailedPromptHistorySave(operationType, entityType, prompt, ex, startTime, modelProvider, modelName, modelId, buildMetadata);
                }

                return CreateFailedResult<TResult>($"Failed to complete {operationType}: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a failed result with error message
        /// </summary>
        protected static TResult CreateFailedResult<TResult>(string errorMessage) where TResult : class, new()
        {
            var result = new TResult();

            // Use cached reflection delegates for common properties
            var props = ReflectionCache<TResult>.Instance;
            props.SetSuccess?.Invoke(result, false);
            props.SetMessage?.Invoke(result, errorMessage);
            props.SetConfidenceScore?.Invoke(result, 0.0);

            return result;
        }

        /// <summary>
        /// Set confidence score on result using reflection
        /// </summary>
        protected static void SetConfidenceScore<TResult>(TResult result, double score) where TResult : class
        {
            var props = ReflectionCache<TResult>.Instance;
            props.SetConfidenceScore?.Invoke(result, score);
        }

        /// <summary>
        /// Cached reflection delegates per TResult type to avoid repeated reflection lookups
        /// </summary>
        private sealed class ReflectionCache<T> where T : class
        {
            public static readonly ReflectionCache<T> Instance = new();

            public readonly Action<T, bool>? SetSuccess;
            public readonly Action<T, string>? SetMessage;
            public readonly Action<T, double>? SetConfidenceScore;

            private ReflectionCache()
            {
                var successProp = typeof(T).GetProperty("Success");
                if (successProp?.CanWrite == true)
                    SetSuccess = (obj, val) => successProp.SetValue(obj, val);

                var messageProp = typeof(T).GetProperty("Message");
                if (messageProp?.CanWrite == true)
                    SetMessage = (obj, val) => messageProp.SetValue(obj, val);

                var confidenceProp = typeof(T).GetProperty("ConfidenceScore");
                if (confidenceProp?.CanWrite == true)
                    SetConfidenceScore = (obj, val) => confidenceProp.SetValue(obj, val);
            }
        }

        #endregion
    }
}
