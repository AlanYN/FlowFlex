using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.IServices.AI;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace FlowFlex.Application.Services.AI.Chat
{
    /// <summary>
    /// AI chat service implementation.
    /// Responsible for chat message sending and streaming.
    /// Migrated from AIService.Chat.cs
    /// </summary>
    public class AIChatService : AIServiceBase, IAIChatService, IScopedService
    {
        private readonly IAIProviderAdapter _providerAdapter;
        private readonly IAIPromptBuilder _promptBuilder;
        private readonly IAIResponseParser _responseParser;
        private readonly IAIModelConfigService _configService;

        public AIChatService(
            IAIProviderAdapter providerAdapter,
            IAIPromptBuilder promptBuilder,
            IAIResponseParser responseParser,
            IAIModelConfigService configService,
            // Base class dependencies
            ILogger<AIChatService> logger,
            IAIPromptHistoryRepository promptHistoryRepository,
            IOperatorContextService operatorContextService,
            IHttpContextAccessor httpContextAccessor,
            IBackgroundTaskQueue backgroundTaskQueue)
            : base(logger, promptHistoryRepository, operatorContextService, httpContextAccessor, backgroundTaskQueue)
        {
            _providerAdapter = providerAdapter;
            _promptBuilder = promptBuilder;
            _responseParser = responseParser;
            _configService = configService;
        }

        #region SendChatMessageAsync

        /// <summary>
        /// Send message to AI chat and get response
        /// </summary>
        public async Task<AIChatResponse> SendChatMessageAsync(AIChatInput input)
        {
            var startTime = DateTime.UtcNow;
            string prompt = null;
            AIProviderResponse response = null;

            try
            {
                Logger.LogInformation("Processing AI chat message for session: {SessionId}", input.SessionId);

                // Build prompt for history tracking
                prompt = _promptBuilder.BuildChatPrompt(input);
                response = await CallAIProviderForChatAsync(input);

                // Save prompt history to database (fire-and-forget)
                QueuePromptHistorySave(
                    "ChatMessage", "Chat", prompt, response, startTime,
                    input.ModelProvider, input.ModelName, input.ModelId,
                    () => new
                    {
                        sessionId = input.SessionId,
                        mode = input.Mode,
                        messageCount = input.Messages?.Count ?? 0,
                        lastMessage = input.Messages?.LastOrDefault()?.Content?.Substring(0, Math.Min(200, input.Messages?.LastOrDefault()?.Content?.Length ?? 0))
                    });

                if (response.Success)
                {
                    var chatResponse = _responseParser.ParseChatResponse(response.Content, input);

                    Logger.LogInformation("AI chat response generated successfully for session: {SessionId}", input.SessionId);
                    return chatResponse;
                }
                else
                {
                    Logger.LogWarning("AI chat failed, using fallback response: {Error}", response.ErrorMessage);
                    return _responseParser.GenerateFallbackChatResponse(input);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in AI chat processing for session: {SessionId}", input.SessionId);

                // Save failed prompt history (fire-and-forget)
                if (!string.IsNullOrEmpty(prompt))
                {
                    QueueFailedPromptHistorySave(
                        "ChatMessage", "Chat", prompt, ex, startTime,
                        input.ModelProvider, input.ModelName, input.ModelId,
                        () => new
                        {
                            sessionId = input.SessionId,
                            mode = input.Mode,
                            messageCount = input.Messages?.Count ?? 0,
                            error = ex.Message
                        });
                }

                return _responseParser.GenerateErrorChatResponse(input, ex.Message);
            }
        }

        #endregion

        #region StreamChatAsync

        /// <summary>
        /// Stream chat conversation with AI
        /// </summary>
        public async IAsyncEnumerable<AIChatStreamResult> StreamChatAsync(AIChatInput input)
        {
            var startTime = DateTime.UtcNow;
            var sessionId = input.SessionId;

            yield return new AIChatStreamResult
            {
                Type = "start",
                Content = "",
                IsComplete = false,
                SessionId = sessionId
            };

            // Use Channel producer-consumer pattern to avoid yield in try/catch
            var channel = System.Threading.Channels.Channel.CreateUnbounded<AIChatStreamResult>();
            BackgroundTaskQueue.QueueBackgroundWorkItem(async token =>
            {
                await ProduceChatStreamAsync(input, startTime, channel.Writer);
            });

            await foreach (var result in channel.Reader.ReadAllAsync())
            {
                yield return result;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Produce chat stream results into the channel writer
        /// </summary>
        private async Task ProduceChatStreamAsync(AIChatInput input, DateTime startTime, System.Threading.Channels.ChannelWriter<AIChatStreamResult> writer)
        {
            var streamingContent = new StringBuilder();
            string prompt = null;
            var sessionId = input.SessionId;

            try
            {
                // Build prompt for logging
                prompt = _promptBuilder.GetChatSystemPrompt(input.Mode, input.Messages.LastOrDefault()?.Content ?? "");
                foreach (var message in input.Messages.TakeLast(5))
                {
                    prompt += $"\n{message.Role}: {message.Content}";
                }

                // Stream each chunk in real-time via IAIProviderAdapter
                await foreach (var chunk in CallAIProviderForStreamChatAsync(input))
                {
                    if (!string.IsNullOrEmpty(chunk))
                    {
                        streamingContent.Append(chunk);
                    }

                    await writer.WriteAsync(new AIChatStreamResult
                    {
                        Type = "delta",
                        Content = chunk,
                        IsComplete = false,
                        SessionId = sessionId
                    });
                }

                // Send completion signal
                await writer.WriteAsync(new AIChatStreamResult
                {
                    Type = "complete",
                    Content = "",
                    IsComplete = true,
                    SessionId = sessionId
                });

                // Save successful chat prompt history (fire-and-forget)
                QueuePromptHistorySave(
                    "ChatMessageStream", "Chat", prompt,
                    new AIProviderResponse
                    {
                        Success = true,
                        Content = streamingContent.ToString(),
                        Provider = input.ModelProvider ?? "Unknown",
                        ModelName = input.ModelName ?? "Unknown",
                        ModelId = input.ModelId ?? "Unknown"
                    },
                    startTime, input.ModelProvider, input.ModelName, input.ModelId,
                    () => new
                    {
                        sessionId = input.SessionId,
                        mode = input.Mode,
                        messageCount = input.Messages?.Count ?? 0,
                        streamingMode = true,
                        contentLength = streamingContent.Length
                    });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in streaming chat for session: {SessionId}", sessionId);

                // Save failed prompt history (fire-and-forget)
                if (!string.IsNullOrEmpty(prompt))
                {
                    QueueFailedPromptHistorySave(
                        "ChatMessageStream", "Chat", prompt, ex, startTime,
                        input.ModelProvider, input.ModelName, input.ModelId,
                        () => new
                        {
                            sessionId = input.SessionId,
                            mode = input.Mode,
                            messageCount = input.Messages?.Count ?? 0,
                            streamingMode = true,
                            error = ex.Message
                        });
                }

                await writer.WriteAsync(new AIChatStreamResult
                {
                    Type = "error",
                    Content = $"Stream error: {ex.Message}",
                    IsComplete = true,
                    SessionId = sessionId
                });
            }
            finally
            {
                writer.TryComplete();
            }
        }

        /// <summary>
        /// Call AI provider for chat using IAIProviderAdapter.CallChatAsync
        /// Handles model config resolution and delegates to the provider adapter
        /// </summary>
        private async Task<AIProviderResponse> CallAIProviderForChatAsync(AIChatInput input)
        {
            try
            {
                // Build message array with system prompt and conversation history
                var messages = new List<object>();

                // Add system prompt
                messages.Add(new { role = "system", content = _promptBuilder.GetChatSystemPrompt(input.Mode, input.Messages.LastOrDefault()?.Content ?? "") });

                // Add conversation history (last 5 messages to reduce token usage)
                foreach (var message in input.Messages.TakeLast(5))
                {
                    messages.Add(new { role = message.Role, content = message.Content });
                }

                // Get user configuration
                AIModelConfig userConfig = null;

                // If model ID is specified, use that configuration
                if (!string.IsNullOrEmpty(input.ModelId) && long.TryParse(input.ModelId, out var modelId))
                {
                    // Use tenant isolation to get configuration
                    userConfig = await _configService.GetConfigByIdAsync(modelId);
                    if (userConfig != null)
                    {
                        Logger.LogInformation("Using specified model config: {Provider} - {ModelName} for session: {SessionId}",
                            userConfig.Provider, userConfig.ModelName, input.SessionId);
                    }
                }

                // Delegate to IAIProviderAdapter for the actual AI call
                var response = await _providerAdapter.CallChatAsync(new AIChatProviderRequest
                {
                    Messages = messages,
                    Config = userConfig,
                    Provider = userConfig?.Provider
                });

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error calling AI provider for chat with session: {SessionId}", input.SessionId);
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"AI provider call failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Call AI provider for streaming chat using IAIProviderAdapter.StreamChatAsync
        /// Handles model config resolution, message building, and fallback to non-streaming
        /// </summary>
        private async IAsyncEnumerable<string> CallAIProviderForStreamChatAsync(AIChatInput input)
        {
            // Build message array with system prompt and conversation history
            var messages = new List<object>();

            // Add system prompt
            messages.Add(new { role = "system", content = _promptBuilder.GetChatSystemPrompt(input.Mode, input.Messages.LastOrDefault()?.Content ?? "") });

            // Add conversation history (last 5 messages to reduce token usage)
            foreach (var message in input.Messages.TakeLast(5))
            {
                messages.Add(new { role = message.Role, content = message.Content });
            }

            // Get user configuration
            AIModelConfig userConfig = null;

            // If model ID is specified, use that configuration
            if (!string.IsNullOrEmpty(input.ModelId) && long.TryParse(input.ModelId, out var modelId))
            {
                userConfig = await _configService.GetConfigByIdAsync(modelId);
            }

            // Try streaming via IAIProviderAdapter if user config is available
            if (userConfig != null)
            {
                await foreach (var chunk in _providerAdapter.StreamChatAsync(new AIChatProviderRequest
                {
                    Messages = messages,
                    Config = userConfig,
                    Provider = userConfig.Provider
                }))
                {
                    yield return chunk;
                }
                yield break;
            }

            // Fallback to non-streaming response with simulated streaming
            var response = await CallAIProviderForChatAsync(input);

            if (response.Success && !string.IsNullOrEmpty(response.Content))
            {
                var words = response.Content.Split(' ');
                foreach (var word in words)
                {
                    yield return word + " ";
                    await Task.Delay(20); // Reduced delay for better UX
                }
            }
            else
            {
                // Check if it's a rate limit error
                if (response.ErrorMessage?.Contains("rate_limit_exceeded") == true ||
                    response.ErrorMessage?.Contains("Rate limit reached") == true)
                {
                    yield return "I'm currently experiencing high demand and have reached the API rate limit. ";
                    yield return "Please try again in a few minutes. ";
                    yield return "In the meantime, you can continue planning your workflow by describing your requirements, ";
                    yield return "and I'll help you once the limit resets.";
                }
                else
                {
                    yield return "I apologize, but I'm having trouble processing your message right now. ";
                    yield return "Please try again in a moment.";
                }
            }
        }

        #endregion
    }
}
