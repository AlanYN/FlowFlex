using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Domain.Entities.OW;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;

namespace FlowFlex.Application.Services.AI
{
    public partial class AIService
    {
        // JWT Token cache for Item Gateway (provider -> token with expiry)
        private static readonly ConcurrentDictionary<string, (string Token, DateTime Expiry)> _jwtTokenCache = new();
        private static readonly TimeSpan _jwtTokenLifetime = TimeSpan.FromHours(1); // JWT tokens typically expire after 1 hour
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
                _logger.LogInformation("Processing AI chat message for session: {SessionId}", input.SessionId);

                // Store conversation context in MCP
                await _mcpService.StoreContextAsync(
                    $"chat_session_{input.SessionId}",
                    JsonSerializer.Serialize(input.Messages),
                    new Dictionary<string, object>
                    {
                        { "mode", input.Mode },
                        { "timestamp", DateTime.UtcNow },
                        { "message_count", input.Messages.Count }
                    }
                );

                // Build prompt for history tracking
                prompt = BuildChatPrompt(input);
                response = await CallAIProviderForChatAsync(input);

                // Save prompt history to database (fire-and-forget)
                // Background task queued
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        var metadata = JsonSerializer.Serialize(new
                        {
                            sessionId = input.SessionId,
                            mode = input.Mode,
                            messageCount = input.Messages?.Count ?? 0,
                            lastMessage = input.Messages?.LastOrDefault()?.Content?.Substring(0, Math.Min(200, input.Messages?.LastOrDefault()?.Content?.Length ?? 0))
                        });
                        await SavePromptHistoryAsync("ChatMessage", "Chat", null, null,
                            prompt, response, startTime, input.ModelProvider, input.ModelName, input.ModelId, metadata);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to save chat message prompt history");
                    }
                });

                if (response.Success)
                {
                    var chatResponse = ParseChatResponse(response.Content, input);

                    _logger.LogInformation("AI chat response generated successfully for session: {SessionId}", input.SessionId);
                    return chatResponse;
                }
                else
                {
                    _logger.LogWarning("AI chat failed, using fallback response: {Error}", response.ErrorMessage);
                    return GenerateFallbackChatResponse(input);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AI chat processing for session: {SessionId}", input.SessionId);

                // Save failed prompt history (fire-and-forget)
                if (!string.IsNullOrEmpty(prompt))
                {
                    // Background task queued
                    _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                        {
                            try
                            {
                                var failedResponse = new AIProviderResponse
                                {
                                    Success = false,
                                    ErrorMessage = ex.Message,
                                    Content = ""
                                };
                                var metadata = JsonSerializer.Serialize(new
                                {
                                    sessionId = input.SessionId,
                                    mode = input.Mode,
                                    messageCount = input.Messages?.Count ?? 0,
                                    error = ex.Message
                                });
                                await SavePromptHistoryAsync("ChatMessage", "Chat", null, null,
                                    prompt, failedResponse, startTime, input.ModelProvider, input.ModelName, input.ModelId, metadata);
                            }
                            catch (Exception saveEx)
                            {
                                _logger.LogWarning(saveEx, "Failed to save failed chat message prompt history");
                            }
                        });
                }

                return GenerateErrorChatResponse(input, ex.Message);
            }
        }

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

            // 使用Channel生产者-消费者模式，避免在try/catch中使用yield
            var channel = System.Threading.Channels.Channel.CreateUnbounded<AIChatStreamResult>();
            // Background task queued
            _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
        {
            await ProduceChatStreamAsync(input, startTime, channel.Writer);
        });

            await foreach (var result in channel.Reader.ReadAllAsync())
            {
                yield return result;
            }
        }

        private async Task ProduceChatStreamAsync(AIChatInput input, DateTime startTime, System.Threading.Channels.ChannelWriter<AIChatStreamResult> writer)
        {
            var streamingContent = new StringBuilder();
            string prompt = null;
            var sessionId = input.SessionId;

            try
            {
                // Build prompt for logging
                prompt = GetChatSystemPrompt(input.Mode, input.Messages.LastOrDefault()?.Content ?? "");
                foreach (var message in input.Messages.TakeLast(5))
                {
                    prompt += $"\n{message.Role}: {message.Content}";
                }

                // 实时流式传输每个数据块
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

                // 发送完成信号
                await writer.WriteAsync(new AIChatStreamResult
                {
                    Type = "complete",
                    Content = "",
                    IsComplete = true,
                    SessionId = sessionId
                });

                // Save successful chat prompt history (fire-and-forget)
                // Background task queued
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        var response = new AIProviderResponse
                        {
                            Success = true,
                            Content = streamingContent.ToString(),
                            Provider = input.ModelProvider ?? "Unknown",
                            ModelName = input.ModelName ?? "Unknown",
                            ModelId = input.ModelId ?? "Unknown"
                        };
                        var metadata = JsonSerializer.Serialize(new
                        {
                            sessionId = input.SessionId,
                            mode = input.Mode,
                            messageCount = input.Messages?.Count ?? 0,
                            streamingMode = true,
                            contentLength = streamingContent.Length
                        });
                        await SavePromptHistoryAsync("ChatMessageStream", "Chat", null, null,
                            prompt, response, startTime, input.ModelProvider, input.ModelName, input.ModelId, metadata);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to save streaming chat prompt history");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in streaming chat for session: {SessionId}", sessionId);

                // Save failed prompt history (fire-and-forget)
                if (!string.IsNullOrEmpty(prompt))
                {
                    // Background task queued
                    _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                        {
                            try
                            {
                                var failedResponse = new AIProviderResponse
                                {
                                    Success = false,
                                    ErrorMessage = ex.Message,
                                    Content = "",
                                    Provider = input.ModelProvider ?? "Unknown",
                                    ModelName = input.ModelName ?? "Unknown",
                                    ModelId = input.ModelId ?? "Unknown"
                                };
                                var metadata = JsonSerializer.Serialize(new
                                {
                                    sessionId = input.SessionId,
                                    mode = input.Mode,
                                    messageCount = input.Messages?.Count ?? 0,
                                    streamingMode = true,
                                    error = ex.Message
                                });
                                await SavePromptHistoryAsync("ChatMessageStream", "Chat", null, null,
                                    prompt, failedResponse, startTime, input.ModelProvider, input.ModelName, input.ModelId, metadata);
                            }
                            catch (Exception saveEx)
                            {
                                _logger.LogWarning(saveEx, "Failed to save failed streaming chat prompt history");
                            }
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

        private async Task<AIProviderResponse> CallAIProviderForChatAsync(AIChatInput input)
        {
            try
            {
                // Build message array, directly use conversation history
                var messages = new List<object>();

                // Add system prompt
                messages.Add(new { role = "system", content = GetChatSystemPrompt(input.Mode, input.Messages.LastOrDefault()?.Content ?? "") });

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
                    // Use tenant isolation to get configuration, no need to manually pass user ID
                    userConfig = await _configService.GetConfigByIdAsync(modelId);
                    if (userConfig != null)
                    {
                        _logger.LogInformation("Using specified model config: {Provider} - {ModelName} for session: {SessionId}",
                            userConfig.Provider, userConfig.ModelName, input.SessionId);
                    }
                }

                // Try primary model first
                AIProviderResponse response = null;

                if (userConfig != null)
                {
                    // Check if using Item Gateway based on BaseURL
                    var isItemGateway = !string.IsNullOrEmpty(userConfig.BaseUrl) &&
                                      userConfig.BaseUrl.Contains("aiop-gateway.item.com", StringComparison.OrdinalIgnoreCase);

                    if (isItemGateway)
                    {
                        // Use Item Gateway for any provider when BaseURL is Item Gateway
                        response = await CallLLMGatewayWithConfigAsync(messages, userConfig);
                    }
                    else
                    {
                        // Call corresponding API based on provider
                        response = userConfig.Provider?.ToLower() switch
                        {
                            "zhipuai" => await CallZhipuAIWithConfigAsync(messages, userConfig),
                            "openai" => await CallOpenAIWithConfigAsync(messages, userConfig),
                            "gemini" => await CallGeminiWithConfigAsync(messages, userConfig),
                            "claude" => await CallClaudeWithConfigAsync(messages, userConfig),
                            "deepseek" => await CallDeepSeekWithConfigAsync(messages, userConfig),
                            _ => await CallOpenAIWithConfigAsync(messages, userConfig) // Default to OpenAI compatible
                        };
                    }

                    // Check if it's a rate limit error and try fallback
                    if (!response.Success && (response.ErrorMessage?.Contains("rate_limit_exceeded") == true ||
                                            response.ErrorMessage?.Contains("Rate limit reached") == true ||
                                            response.ErrorMessage?.Contains("429") == true))
                    {
                        _logger.LogWarning("Primary model hit rate limit, attempting fallback to ZhipuAI for session: {SessionId}", input.SessionId);

                        try
                        {
                            var fallbackResponse = await CallZhipuAIAsync(messages);
                            if (fallbackResponse.Success)
                            {
                                _logger.LogInformation("Successfully used ZhipuAI fallback for session: {SessionId}", input.SessionId);
                                return fallbackResponse;
                            }
                        }
                        catch (Exception fallbackEx)
                        {
                            _logger.LogWarning(fallbackEx, "Fallback to ZhipuAI also failed for session: {SessionId}", input.SessionId);
                        }
                    }
                }
                else
                {
                    // No specific model config found, use default ZhipuAI configuration
                    _logger.LogInformation("No specific model config found, using default ZhipuAI configuration");
                    response = await CallZhipuAIAsync(messages);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling AI provider for chat with session: {SessionId}", input.SessionId);
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"AI provider call failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Call API using default ZhipuAI configuration
        /// </summary>
        private async Task<AIProviderResponse> CallZhipuAIAsync(List<object> messages)
        {
            var requestBody = new
            {
                model = _aiOptions.ZhipuAI.Model,
                messages = messages.ToArray(),
                temperature = 0.7,
                max_tokens = 1000
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var apiUrl = $"{_aiOptions.ZhipuAI.BaseUrl}/chat/completions";
            _logger.LogInformation("Calling ZhipuAI API: {Url} with {MessageCount} messages", apiUrl, messages.Count);

            using var httpClient = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Version = new Version(1, 1),
                Content = content,
            };
            request.Headers.Add("Authorization", $"Bearer {_aiOptions.ZhipuAI.ApiKey}");

            var timeoutSeconds = Math.Max(10, Math.Min(60, _aiOptions.ConnectionTest.TimeoutSeconds));
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            var response = await httpClient.SendAsync(request, cts.Token);
            var responseContent = await response.Content.ReadAsStringAsync(cts.Token);

            _logger.LogInformation("ZhipuAI API Response: {StatusCode} - {Content}", response.StatusCode, responseContent);

            if (response.IsSuccessStatusCode)
            {
                var aiResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var messageContent = aiResponse
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";

                _logger.LogInformation("ZhipuAI generated response: {Response}", messageContent);

                return new AIProviderResponse
                {
                    Success = true,
                    Content = messageContent
                };
            }
            else
            {
                _logger.LogWarning("ZhipuAI API call failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"API call failed: {response.StatusCode} - {responseContent}"
                };
            }
        }

        /// <summary>
        /// Call ZhipuAI API using user configuration
        /// </summary>
        private async Task<AIProviderResponse> CallZhipuAIWithConfigAsync(List<object> messages, AIModelConfig config)
        {
            var requestBody = new
            {
                model = config.ModelName,
                messages = messages.ToArray(),
                temperature = config.Temperature > 0 ? config.Temperature : 0.7,
                max_tokens = config.MaxTokens > 0 ? config.MaxTokens : 1000
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Intelligently handle API endpoints, avoid path duplication
            var baseUrl = config.BaseUrl.TrimEnd('/');
            string apiUrl;

            // If BaseUrl already contains the complete endpoint path, use directly
            if (baseUrl.Contains("/chat/completions"))
            {
                apiUrl = baseUrl;
            }
            else
            {
                // Otherwise add endpoint path
                apiUrl = $"{baseUrl}/chat/completions";
            }

            _logger.LogInformation("Calling ZhipuAI API with user config: {Url} - Model: {Model}", apiUrl, config.ModelName);

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");
            var response = await httpClient.PostAsync(apiUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var aiResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var messageContent = aiResponse
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";

                return new AIProviderResponse
                {
                    Success = true,
                    Content = messageContent
                };
            }
            else
            {
                _logger.LogWarning("ZhipuAI API call failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"API call failed: {response.StatusCode} - {responseContent}"
                };
            }
        }

        /// <summary>
        /// Call OpenAI API using user configuration
        /// </summary>
        private async Task<AIProviderResponse> CallOpenAIWithConfigAsync(List<object> messages, AIModelConfig config)
        {
            var requestBody = new
            {
                model = config.ModelName,
                messages = messages.ToArray(),
                temperature = config.Temperature > 0 ? config.Temperature : 0.7,
                max_tokens = config.MaxTokens > 0 ? config.MaxTokens : 4000
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Ensure OpenAI API endpoint contains correct version path, avoid duplication
            var baseUrl = config.BaseUrl.TrimEnd('/');
            string apiUrl;
            if (baseUrl.EndsWith("/v1/chat/completions") || baseUrl.EndsWith("/chat/completions"))
            {
                apiUrl = baseUrl;
            }
            else if (baseUrl.Contains("/v1"))
            {
                apiUrl = $"{baseUrl}/chat/completions";
            }
            else
            {
                apiUrl = $"{baseUrl}/v1/chat/completions";
            }
            _logger.LogInformation("Calling OpenAI API with user config: {Url} - Model: {Model}", apiUrl, config.ModelName);

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");
            var response = await httpClient.PostAsync(apiUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var aiResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var messageContent = aiResponse
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";

                return new AIProviderResponse
                {
                    Success = true,
                    Content = messageContent
                };
            }
            else
            {
                _logger.LogWarning("OpenAI API call failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"API call failed: {response.StatusCode} - {responseContent}"
                };
            }
        }

        /// <summary>
        /// Call Gemini API using user configuration
        /// </summary>
        private async Task<AIProviderResponse> CallGeminiWithConfigAsync(List<object> messages, AIModelConfig config)
        {
            var requestBody = new
            {
                model = config.ModelName,
                messages = messages.ToArray(),
                temperature = config.Temperature > 0 ? config.Temperature : 0.7,
                max_tokens = config.MaxTokens > 0 ? config.MaxTokens : 4000
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Gemini uses OpenAI-compatible API format
            var baseUrl = config.BaseUrl.TrimEnd('/');
            string apiUrl;
            if (baseUrl.EndsWith("/v1/chat/completions") || baseUrl.EndsWith("/chat/completions"))
            {
                apiUrl = baseUrl;
            }
            else if (baseUrl.Contains("/v1"))
            {
                apiUrl = $"{baseUrl}/chat/completions";
            }
            else
            {
                apiUrl = $"{baseUrl}/v1/chat/completions";
            }
            _logger.LogInformation("Calling Gemini API with user config: {Url} - Model: {Model}", apiUrl, config.ModelName);

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");
            var response = await httpClient.PostAsync(apiUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var aiResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var messageContent = aiResponse
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";

                return new AIProviderResponse
                {
                    Success = true,
                    Content = messageContent
                };
            }
            else
            {
                _logger.LogWarning("Gemini API call failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"API call failed: {response.StatusCode} - {responseContent}"
                };
            }
        }

        /// <summary>
        /// Call Claude API using user configuration
        /// </summary>
        private async Task<AIProviderResponse> CallClaudeWithConfigAsync(List<object> messages, AIModelConfig config)
        {
            // Claude API format is slightly different
            var claudeMessages = messages.Skip(1).Select(m => new
            {
                role = ((dynamic)m).role == "assistant" ? "assistant" : "user",
                content = ((dynamic)m).content
            }).ToArray();

            var requestBody = new
            {
                model = config.ModelName,
                max_tokens = config.MaxTokens > 0 ? config.MaxTokens : 4000,
                temperature = config.Temperature > 0 ? config.Temperature : 0.7,
                messages = claudeMessages
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Intelligently handle API endpoints, avoid path duplication
            var baseUrl = config.BaseUrl.TrimEnd('/');
            string apiUrl;

            // If BaseUrl already contains the complete endpoint path, use directly
            if (baseUrl.Contains("/messages"))
            {
                apiUrl = baseUrl;
            }
            else
            {
                // Otherwise add endpoint path，Claude使用/v1/messages
                apiUrl = baseUrl.Contains("/v1") ? $"{baseUrl}/messages" : $"{baseUrl}/v1/messages";
            }

            _logger.LogInformation("Calling Claude API with user config: {Url} - Model: {Model}", apiUrl, config.ModelName);

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("x-api-key", config.ApiKey);
            httpClient.DefaultRequestHeaders.Add("anthropic-version", config.ApiVersion ?? "2023-06-01");
            var response = await httpClient.PostAsync(apiUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var aiResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var messageContent = aiResponse
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString() ?? "";

                return new AIProviderResponse
                {
                    Success = true,
                    Content = messageContent
                };
            }
            else
            {
                _logger.LogWarning("Claude API call failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"API call failed: {response.StatusCode} - {responseContent}"
                };
            }
        }

        /// <summary>
        /// Get or refresh JWT token for Item Gateway
        /// </summary>
        private async Task<string> GetLLMGatewayJwtTokenAsync(AIModelConfig config)
        {
            var cacheKey = $"{config.Provider}_{config.ApiKey}";

            // Check if we have a valid cached token
            if (_jwtTokenCache.TryGetValue(cacheKey, out var cachedToken))
            {
                if (cachedToken.Expiry > DateTime.UtcNow.AddMinutes(5)) // Refresh if less than 5 minutes remaining
                {
                    _logger.LogDebug("Using cached JWT token for Item Gateway");
                    return cachedToken.Token;
                }
            }

            // Request new JWT token
            _logger.LogInformation("Requesting new JWT token for Item Gateway");

            var requestBody = new
            {
                apiKey = config.ApiKey,
                tenantId = "",
                agentCode = "w",
                agentName = "w",
                appCode = "wfe",
                userId = "",
                userName = ""
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Use the base URL from config or default to production
            var baseUrl = string.IsNullOrEmpty(config.BaseUrl)
                ? "https://aiop-gateway.item.com"
                : config.BaseUrl.TrimEnd('/');
            var jwtUrl = $"{baseUrl}/admin/api/credentials/jwt";

            using var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.PostAsync(jwtUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var jwtResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                if (jwtResponse.TryGetProperty("code", out var code) && code.GetInt32() == 0)
                {
                    var token = jwtResponse.GetProperty("data").GetString();
                    if (!string.IsNullOrEmpty(token))
                    {
                        // Cache the token
                        var expiry = DateTime.UtcNow.Add(_jwtTokenLifetime);
                        _jwtTokenCache[cacheKey] = (token, expiry);
                        _logger.LogInformation("Successfully obtained JWT token for Item Gateway");
                        return token;
                    }
                }
            }

            _logger.LogError("Failed to obtain JWT token for Item Gateway: {StatusCode} - {Content}",
                response.StatusCode, responseContent);
            throw new Exception($"Failed to obtain JWT token: {response.StatusCode} - {responseContent}");
        }

        /// <summary>
        /// Call Item Gateway API using user configuration
        /// </summary>
        private async Task<AIProviderResponse> CallLLMGatewayWithConfigAsync(List<object> messages, AIModelConfig config)
        {
            try
            {
                // Get JWT token
                var jwtToken = await GetLLMGatewayJwtTokenAsync(config);

                var requestBody = new
                {
                    model = config.ModelName, // e.g., "openai/gpt-4" or "gemini/gemini-2.5-flash"
                    messages = messages.ToArray(),
                    temperature = config.Temperature > 0 ? config.Temperature : 0.7,
                    max_tokens = config.MaxTokens > 0 ? config.MaxTokens : 4000
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Use the base URL from config or default to production
                var baseUrl = string.IsNullOrEmpty(config.BaseUrl)
                    ? "https://aiop-gateway.item.com"
                    : config.BaseUrl.TrimEnd('/');
                var apiUrl = $"{baseUrl}/openai/v1/chat/completions";

                _logger.LogInformation("Calling Item Gateway API: {Url} - Model: {Model}", apiUrl, config.ModelName);

                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {jwtToken}");
                var response = await httpClient.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var aiResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var messageContent = aiResponse
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString() ?? "";

                    return new AIProviderResponse
                    {
                        Success = true,
                        Content = messageContent,
                        Provider = "Item",
                        ModelName = config.ModelName
                    };
                }
                else
                {
                    _logger.LogWarning("Item Gateway API call failed: {StatusCode} - {Content}",
                        response.StatusCode, responseContent);
                    return new AIProviderResponse
                    {
                        Success = false,
                        ErrorMessage = $"API call failed: {response.StatusCode} - {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Item Gateway API");
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"Item Gateway API error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Call Item Gateway API with real streaming support (alias for CallLLMGatewayStreamAsync)
        /// </summary>
        private async IAsyncEnumerable<string> CallItemGatewayStreamAsync(List<object> messages, AIModelConfig config)
        {
            await foreach (var chunk in CallLLMGatewayStreamAsync(messages, config))
            {
                yield return chunk;
            }
        }

        /// <summary>
        /// Call Item Gateway API with real streaming support
        /// </summary>
        private async IAsyncEnumerable<string> CallLLMGatewayStreamAsync(List<object> messages, AIModelConfig config)
        {
            string jwtToken = null;
            string errorMessage = null;

            // Get JWT token outside of the streaming loop
            try
            {
                jwtToken = await GetLLMGatewayJwtTokenAsync(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to obtain JWT token for Item Gateway streaming");
                errorMessage = "Failed to authenticate with Item Gateway. Please check your API key.";
            }

            // If JWT token acquisition failed, yield error and return
            if (!string.IsNullOrEmpty(errorMessage))
            {
                yield return errorMessage;
                yield break;
            }

            var requestBody = new
            {
                model = config.ModelName,
                messages = messages.ToArray(),
                temperature = config.Temperature > 0 ? config.Temperature : 0.7,
                max_tokens = config.MaxTokens > 0 ? config.MaxTokens : 4000,
                stream = true // Enable streaming
            };

            var json = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            // Use the base URL from config or default to production
            var baseUrl = string.IsNullOrEmpty(config.BaseUrl)
                ? "https://aiop-gateway.item.com"
                : config.BaseUrl.TrimEnd('/');
            var apiUrl = $"{baseUrl}/openai/v1/chat/completions";

            _logger.LogInformation("Calling Item Gateway Stream API: {Url} - Model: {Model}", apiUrl, config.ModelName);

            using var httpClient = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Content = httpContent
            };
            request.Headers.Add("Authorization", $"Bearer {jwtToken}");
            request.Headers.Add("Accept", "text/event-stream");

            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Item Gateway Stream API failed: {StatusCode} - {Content}",
                    response.StatusCode, errorContent);
                yield return "I'm having trouble connecting to the Item Gateway service. Please try again.";
                yield break;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            string line;
            var lineTimeout = TimeSpan.FromSeconds(30);

            while (true)
            {
                string readLine = null;
                bool shouldBreak = false;
                string contentToYield = null;

                var readStartTime = DateTime.UtcNow;
                try
                {
                    using var cts = new CancellationTokenSource(lineTimeout);
                    _logger.LogDebug("🔍 Item Gateway: Starting to read line with {Timeout}s timeout", lineTimeout.TotalSeconds);
                    readLine = await reader.ReadLineAsync().WaitAsync(cts.Token);
                    var readDuration = (DateTime.UtcNow - readStartTime).TotalMilliseconds;
                    const double slowReadInfoThresholdMs = 50d;
                    if (readDuration >= slowReadInfoThresholdMs)
                        _logger.LogInformation("✅ Item Gateway: Line read completed in {Duration}ms", readDuration);
                    else
                        _logger.LogDebug("✅ Item Gateway: Line read completed in {Duration}ms", readDuration);
                }
                catch (OperationCanceledException)
                {
                    var readDuration = (DateTime.UtcNow - readStartTime).TotalMilliseconds;
                    _logger.LogWarning("⏰ Item Gateway stream line read timeout after {Duration}ms (expected {Timeout}s), breaking stream",
                        readDuration, lineTimeout.TotalSeconds);
                    shouldBreak = true;
                }
                catch (Exception ex)
                {
                    var readDuration = (DateTime.UtcNow - readStartTime).TotalMilliseconds;
                    _logger.LogWarning(ex, "❌ Error reading Item Gateway stream line after {Duration}ms, breaking stream", readDuration);
                    shouldBreak = true;
                }

                if (shouldBreak)
                    break;

                if (readLine == null)
                    break;

                if (readLine.StartsWith("data: "))
                {
                    var data = readLine.Substring(6).Trim();

                    if (data == "[DONE]")
                        break;

                    if (string.IsNullOrEmpty(data))
                        continue;

                    try
                    {
                        var jsonData = JsonSerializer.Deserialize<JsonElement>(data);

                        if (jsonData.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                        {
                            var choice = choices[0];
                            if (choice.TryGetProperty("delta", out var delta) &&
                                delta.TryGetProperty("content", out var contentProp))
                            {
                                var content = contentProp.GetString();
                                if (!string.IsNullOrEmpty(content))
                                {
                                    contentToYield = content;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing Item Gateway stream JSON, skipping line");
                        continue;
                    }
                }

                if (!string.IsNullOrEmpty(contentToYield))
                {
                    yield return contentToYield;
                }
            }
        }

        /// <summary>
        /// Call DeepSeek API using user configuration
        /// </summary>
        private async Task<AIProviderResponse> CallDeepSeekWithConfigAsync(List<object> messages, AIModelConfig config)
        {
            var requestBody = new
            {
                model = config.ModelName,
                messages = messages.ToArray(),
                temperature = config.Temperature > 0 ? config.Temperature : 0.7,
                max_tokens = config.MaxTokens > 0 ? config.MaxTokens : 4000
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Intelligently handle API endpoints, avoid path duplication
            var baseUrl = config.BaseUrl.TrimEnd('/');
            string apiUrl;

            // If BaseUrl already contains the complete endpoint path, use directly
            if (baseUrl.Contains("/chat/completions"))
            {
                apiUrl = baseUrl;
            }
            else
            {
                // Otherwise add endpoint path，DeepSeek通常需要v1版本
                apiUrl = baseUrl.Contains("/v1") ? $"{baseUrl}/chat/completions" : $"{baseUrl}/v1/chat/completions";
            }

            _logger.LogInformation("Calling DeepSeek API with user config: {Url} - Model: {Model}", apiUrl, config.ModelName);

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");
            var response = await httpClient.PostAsync(apiUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var aiResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var messageContent = aiResponse
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";

                return new AIProviderResponse
                {
                    Success = true,
                    Content = messageContent
                };
            }
            else
            {
                _logger.LogWarning("DeepSeek API call failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"API call failed: {response.StatusCode} - {responseContent}"
                };
            }
        }

        private async IAsyncEnumerable<string> CallAIProviderForStreamChatAsync(List<object> messages, AIModelConfig userConfig)
        {
            // Try to use real streaming API if available
            if (userConfig != null)
            {
                var provider = userConfig.Provider?.ToLower();

                if (provider == "deepseek")
                {
                    await foreach (var chunk in CallDeepSeekStreamAsync(messages, userConfig))
                    {
                        yield return chunk;
                    }
                    yield break;
                }
                else if (provider == "openai")
                {
                    await foreach (var chunk in CallOpenAIStreamAsync(messages, userConfig))
                    {
                        yield return chunk;
                    }
                    yield break;
                }
                else if (provider == "item" || provider == "llmgateway") // Support both new and old names
                {
                    await foreach (var chunk in CallLLMGatewayStreamAsync(messages, userConfig))
                    {
                        yield return chunk;
                    }
                    yield break;
                }
                else if (provider == "zhipuai")
                {
                    // ZhipuAI doesn't have streaming API, use simulated streaming
                    var response = await CallZhipuAIWithConfigAsync(messages, userConfig);
                    if (response.Success && !string.IsNullOrEmpty(response.Content))
                    {
                        var words = response.Content.Split(' ');
                        foreach (var word in words)
                        {
                            yield return word + " ";
                            await Task.Delay(20);
                        }
                    }
                    yield break;
                }
                else if (provider == "claude" || provider == "anthropic")
                {
                    // Claude doesn't have streaming API, use simulated streaming
                    var response = await CallClaudeWithConfigAsync(messages, userConfig);
                    if (response.Success && !string.IsNullOrEmpty(response.Content))
                    {
                        var words = response.Content.Split(' ');
                        foreach (var word in words)
                        {
                            yield return word + " ";
                            await Task.Delay(20);
                        }
                    }
                    yield break;
                }
            }

            // Fallback to error message
            yield return "I apologize, but I'm having trouble processing your request right now. ";
            yield return "Please try again in a moment.";
        }

        private async IAsyncEnumerable<string> CallAIProviderForStreamChatAsync(AIChatInput input)
        {
            // Build message array, directly use conversation history
            var messages = new List<object>();

            // Add system prompt
            messages.Add(new { role = "system", content = GetChatSystemPrompt(input.Mode, input.Messages.LastOrDefault()?.Content ?? "") });

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

            // Try to use real streaming API if available
            if (userConfig != null)
            {
                var provider = userConfig.Provider?.ToLower();

                if (provider == "deepseek")
                {
                    await foreach (var chunk in CallDeepSeekStreamAsync(messages, userConfig))
                    {
                        yield return chunk;
                    }
                    yield break;
                }
                else if (provider == "openai")
                {
                    await foreach (var chunk in CallOpenAIStreamAsync(messages, userConfig))
                    {
                        yield return chunk;
                    }
                    yield break;
                }
                else if (provider == "item" || provider == "llmgateway") // Support both new and old names
                {
                    await foreach (var chunk in CallLLMGatewayStreamAsync(messages, userConfig))
                    {
                        yield return chunk;
                    }
                    yield break;
                }
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

        /// <summary>
        /// Call DeepSeek API with real streaming support
        /// </summary>
        private async IAsyncEnumerable<string> CallDeepSeekStreamAsync(List<object> messages, AIModelConfig config)
        {
            var requestBody = new
            {
                model = config.ModelName,
                messages = messages.ToArray(),
                temperature = config.Temperature > 0 ? config.Temperature : 0.7,
                max_tokens = config.MaxTokens > 0 ? config.MaxTokens : 4000,
                stream = true // Enable streaming
            };

            var json = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            // Intelligently handle API endpoints
            var baseUrl = config.BaseUrl.TrimEnd('/');
            string apiUrl;

            if (baseUrl.Contains("/chat/completions"))
            {
                apiUrl = baseUrl;
            }
            else
            {
                apiUrl = baseUrl.Contains("/v1") ? $"{baseUrl}/chat/completions" : $"{baseUrl}/v1/chat/completions";
            }

            _logger.LogInformation("Calling DeepSeek Stream API: {Url} - Model: {Model}", apiUrl, config.ModelName);

            using var httpClient = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Content = httpContent
            };
            request.Headers.Add("Authorization", $"Bearer {config.ApiKey}");
            request.Headers.Add("Accept", "text/event-stream");

            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("DeepSeek Stream API failed: {StatusCode} - {Content}", response.StatusCode, errorContent);
                yield return "I'm having trouble connecting to the AI service. Please try again.";
                yield break;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            string line;
            var lineTimeout = TimeSpan.FromSeconds(30); // 增加超时时间以支持更长的响应

            while (true)
            {
                // 将try-catch移出yield return
                string readLine = null;
                bool shouldBreak = false;
                bool shouldContinue = false;
                string contentToYield = null;

                var readStartTime = DateTime.UtcNow;
                try
                {
                    // 为每行读取设置超时
                    using var cts = new CancellationTokenSource(lineTimeout);
                    _logger.LogDebug("🔍 DeepSeek: Starting to read line with {Timeout}s timeout", lineTimeout.TotalSeconds);
                    readLine = await reader.ReadLineAsync().WaitAsync(cts.Token);
                    var readDuration = (DateTime.UtcNow - readStartTime).TotalMilliseconds;
                    const double slowReadInfoThresholdMs = 50d;
                    if (readDuration >= slowReadInfoThresholdMs)
                        _logger.LogInformation("✅ DeepSeek: Line read completed in {Duration}ms", readDuration);
                    else
                        _logger.LogDebug("✅ DeepSeek: Line read completed in {Duration}ms", readDuration);
                }
                catch (OperationCanceledException)
                {
                    var readDuration = (DateTime.UtcNow - readStartTime).TotalMilliseconds;
                    _logger.LogWarning("⏰ DeepSeek stream line read timeout after {Duration}ms (expected {Timeout}s), breaking stream", readDuration, lineTimeout.TotalSeconds);
                    shouldBreak = true;
                }
                catch (Exception ex)
                {
                    var readDuration = (DateTime.UtcNow - readStartTime).TotalMilliseconds;
                    _logger.LogWarning(ex, "❌ Error reading DeepSeek stream line after {Duration}ms, breaking stream", readDuration);
                    shouldBreak = true;
                }

                if (shouldBreak)
                    break;

                if (readLine == null)
                    break;

                if (readLine.StartsWith("data: "))
                {
                    var data = readLine.Substring(6).Trim();

                    if (data == "[DONE]")
                        break;

                    if (string.IsNullOrEmpty(data))
                        continue;

                    try
                    {
                        var jsonData = JsonSerializer.Deserialize<JsonElement>(data);

                        if (jsonData.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                        {
                            var choice = choices[0];
                            if (choice.TryGetProperty("delta", out var delta) &&
                                delta.TryGetProperty("content", out var contentProp))
                            {
                                var content = contentProp.GetString();
                                if (!string.IsNullOrEmpty(content))
                                {
                                    contentToYield = content;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing DeepSeek stream JSON, skipping line");
                        continue;
                    }
                }

                // yield return在try-catch外部
                if (!string.IsNullOrEmpty(contentToYield))
                {
                    yield return contentToYield;
                }
            }
        }

        /// <summary>
        /// Call OpenAI API with real streaming support
        /// </summary>
        private async IAsyncEnumerable<string> CallOpenAIStreamAsync(List<object> messages, AIModelConfig config)
        {
            // Check if using Item Gateway
            var isItemGateway = !string.IsNullOrEmpty(config.BaseUrl) &&
                              config.BaseUrl.Contains("aiop-gateway.item.com", StringComparison.OrdinalIgnoreCase);

            if (isItemGateway)
            {
                // Use Item Gateway streaming method
                await foreach (var chunk in CallItemGatewayStreamAsync(messages, config))
                {
                    yield return chunk;
                }
                yield break;
            }

            // Standard OpenAI API call
            var requestBody = new
            {
                model = config.ModelName,
                messages = messages.ToArray(),
                temperature = config.Temperature > 0 ? config.Temperature : 0.7,
                max_tokens = config.MaxTokens > 0 ? config.MaxTokens : 1000,
                stream = true // Enable streaming
            };

            var json = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var baseUrl = config.BaseUrl.TrimEnd('/');
            string apiUrl;

            if (baseUrl.Contains("/chat/completions"))
            {
                apiUrl = baseUrl;
            }
            else
            {
                apiUrl = baseUrl.Contains("/v1") ? $"{baseUrl}/chat/completions" : $"{baseUrl}/v1/chat/completions";
            }

            _logger.LogInformation("Calling OpenAI Stream API: {Url} - Model: {Model}", apiUrl, config.ModelName);

            using var httpClient = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Content = httpContent
            };
            request.Headers.Add("Authorization", $"Bearer {config.ApiKey}");
            request.Headers.Add("Accept", "text/event-stream");

            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("OpenAI Stream API failed: {StatusCode} - {Content}", response.StatusCode, errorContent);

                // Check for rate limit and throw exception to trigger fallback
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new HttpRequestException($"Rate limit exceeded: {errorContent}");
                }

                yield return "I'm having trouble connecting to the AI service. Please try again.";
                yield break;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (line.StartsWith("data: "))
                {
                    var data = line.Substring(6).Trim();

                    if (data == "[DONE]")
                        break;

                    if (string.IsNullOrEmpty(data))
                        continue;

                    var jsonData = JsonSerializer.Deserialize<JsonElement>(data);

                    if (jsonData.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var choice = choices[0];
                        if (choice.TryGetProperty("delta", out var delta) &&
                            delta.TryGetProperty("content", out var contentProp))
                        {
                            var content = contentProp.GetString();
                            if (!string.IsNullOrEmpty(content))
                            {
                                yield return content;
                            }
                        }
                    }
                }
            }
        }

        private string BuildChatPrompt(AIChatInput input)
        {
            var prompt = new StringBuilder();

            if (input.Mode == "workflow_planning")
            {
                prompt.AppendLine("You are an AI Workflow Assistant helping users design business workflows.");
                prompt.AppendLine("Your goal is to understand their requirements through conversation and help them create effective workflows.");
                prompt.AppendLine();
            }

            prompt.AppendLine("Conversation History:");
            foreach (var message in input.Messages.TakeLast(10)) // Limit context to last 10 messages
            {
                prompt.AppendLine($"{message.Role}: {message.Content}");
            }

            if (!string.IsNullOrEmpty(input.Context))
            {
                prompt.AppendLine();
                prompt.AppendLine($"Context: {input.Context}");
            }

            prompt.AppendLine();
            prompt.AppendLine("Please provide a helpful, conversational response that continues the discussion and gathers more information about their workflow needs.");

            return prompt.ToString();
        }

        private string GetChatSystemPrompt(string mode, string input)
        {
            return mode switch
            {
                "workflow_planning" => @"You are an expert AI Workflow Assistant specialized in business process design. Output the result according to the language input by the user. Your role is to:

1. **Understand User Needs**: Engage in natural conversation to deeply understand the user's workflow requirements
2. **Ask Smart Questions**: Ask relevant, specific questions to gather essential information about:
   - Process type and business context
   - Key stakeholders and their roles
   - Timeline and urgency requirements
   - Specific requirements, documents, or approvals needed
   - Compliance or regulatory considerations
3. **Provide Expert Guidance**: Offer professional insights and best practices for workflow design
4. **Be Conversational**: Maintain a friendly, helpful tone while being thorough and professional
5. **Progressive Discovery**: Gradually build understanding through multiple exchanges rather than overwhelming with too many questions at once
6. **Completion Detection**: When you have sufficient information (typically after 3-4 meaningful exchanges), indicate readiness to proceed with workflow creation

Guidelines:
- Ask 1-2 focused questions per response
- Acknowledge and build upon previous answers
- Provide brief explanations of why certain information is important
- Use business terminology appropriately
- Be encouraging and supportive throughout the conversation
- Respond in the same language as the user's input

Remember: Your goal is to collect enough detailed information to create a comprehensive, practical workflow that meets the user's specific needs.",

                "generate_code" => GetGenerateCodePrompt(input),

                _ => @"You are a helpful, knowledgeable AI assistant. Output the result according to the language input by the user. Provide clear, accurate, and helpful responses to user questions. Be conversational, friendly, and thorough in your explanations."
            };
        }

        private AIChatResponse ParseChatResponse(string content, AIChatInput input)
        {
            // Analyze the response to determine if conversation is complete
            var isComplete = DetermineChatCompletion(content, input);

            return new AIChatResponse
            {
                Success = true,
                Message = "Chat response generated successfully",
                Response = new AIChatResponseData
                {
                    Content = content,
                    IsComplete = isComplete,
                    Suggestions = ExtractSuggestions(content),
                    NextQuestions = ExtractNextQuestions(content)
                },
                SessionId = input.SessionId
            };
        }

        private bool DetermineChatCompletion(string content, AIChatInput input)
        {
            // Simple heuristics to determine if conversation is complete
            var completionKeywords = new[] { "enough information", "ready to create", "comprehensive workflow", "proceed with generation" };
            var lowerContent = content.ToLower();

            return completionKeywords.Any(keyword => lowerContent.Contains(keyword)) ||
                   input.Messages.Count(m => m.Role == "user") >= 4;
        }

        private List<string> ExtractSuggestions(string content)
        {
            // Extract suggestions from AI response (simple implementation)
            var suggestions = new List<string>();

            if (content.Contains("consider", StringComparison.OrdinalIgnoreCase))
            {
                suggestions.Add("Consider the suggestions mentioned in the response");
            }

            return suggestions;
        }

        private List<string> ExtractNextQuestions(string content)
        {
            // Extract questions from AI response
            var questions = new List<string>();
            var sentences = content.Split('.', '?', '!');

            foreach (var sentence in sentences)
            {
                if (sentence.Trim().Contains('?'))
                {
                    questions.Add(sentence.Trim() + "?");
                }
            }

            return questions.Take(2).ToList(); // Limit to 2 questions
        }

        private AIChatResponse GenerateFallbackChatResponse(AIChatInput input)
        {
            var userMessageCount = input.Messages.Count(m => m.Role == "user");

            string fallbackContent = userMessageCount switch
            {
                1 => "I'm currently experiencing some technical issues with the AI service. However, I'd be happy to help you with your workflow! Could you tell me more about the teams or people who will be involved in this process?",
                2 => "Thanks for the details! While I'm working through some technical issues, I can still assist you. How many main stages or steps do you think this workflow should have? And what's your expected timeline?",
                3 => "Great information! Despite some temporary service issues, I'm here to help. Are there any specific requirements, documents, or approvals that need to be included in this workflow?",
                _ => "Thank you for all the information! Even with current technical challenges, I believe we have enough details to help you create a comprehensive workflow. Would you like to proceed?"
            };

            var suggestions = new List<string>
            {
                "Continue describing your workflow requirements",
                "Try again in a few moments",
                "Check if your AI model configuration is correct"
            };

            var nextQuestions = userMessageCount switch
            {
                1 => new List<string> { "Who are the key stakeholders?", "What is the main goal of this workflow?" },
                2 => new List<string> { "What are the key milestones?", "Are there any critical dependencies?" },
                3 => new List<string> { "What documents need approval?", "Who has decision-making authority?" },
                _ => new List<string> { "Ready to generate the workflow?", "Any final requirements to add?" }
            };

            return new AIChatResponse
            {
                Success = true,
                Message = "AI service temporarily unavailable, using intelligent fallback response",
                Response = new AIChatResponseData
                {
                    Content = fallbackContent,
                    IsComplete = userMessageCount >= 4,
                    Suggestions = suggestions,
                    NextQuestions = nextQuestions
                },
                SessionId = input.SessionId
            };
        }

        private AIChatResponse GenerateErrorChatResponse(AIChatInput input, string errorMessage)
        {
            var isModelNotFoundError = errorMessage.Contains("model_not_found") || errorMessage.Contains("does not exist");
            var isAuthError = errorMessage.Contains("Unauthorized") || errorMessage.Contains("authentication");

            string errorContent;
            List<string> errorSuggestions;

            if (isModelNotFoundError)
            {
                errorContent = "I'm having trouble accessing the specified AI model. This might be because the model doesn't exist or your API key doesn't have access to it. I'll try to help you with an alternative approach.";
                errorSuggestions = new List<string>
                {
                    "Check your AI model configuration",
                    "Verify your API key permissions",
                    "Try using a different model (e.g., gpt-3.5-turbo)",
                    "Continue with manual workflow creation"
                };
            }
            else if (isAuthError)
            {
                errorContent = "There seems to be an authentication issue with the AI service. Please check your API configuration.";
                errorSuggestions = new List<string>
                {
                    "Verify your API key is correct",
                    "Check if your API key has expired",
                    "Ensure your account has sufficient credits"
                };
            }
            else
            {
                errorContent = "I'm experiencing technical difficulties right now. Let me try to assist you manually while we resolve this issue.";
                errorSuggestions = new List<string>
                {
                    "Try again in a few moments",
                    "Continue describing your workflow manually",
                    "Check system status and try again"
                };
            }

            return new AIChatResponse
            {
                Success = false,
                Message = $"AI service error: {errorMessage}",
                Response = new AIChatResponseData
                {
                    Content = errorContent,
                    IsComplete = false,
                    Suggestions = errorSuggestions,
                    NextQuestions = new List<string>
                    {
                        "Would you like to continue manually?",
                        "Should I help you configure a different AI model?"
                    }
                },
                SessionId = input.SessionId
            };
        }

        private string GetGenerateCodePrompt(string instruction, string codeLanguage = "python")
        {
            var promptBuilder = new StringBuilder();

            promptBuilder.AppendLine("You are an expert programmer. Generate code based on the following instructions:");
            promptBuilder.AppendLine();

            promptBuilder.AppendLine("Instructions: {{INSTRUCTION}}");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Write the code in {{CODE_LANGUAGE}}.");
            promptBuilder.AppendLine();

            promptBuilder.AppendLine("Please ensure that you meet the following requirements:");
            promptBuilder.AppendLine("1. Define a function named 'main'.");
            promptBuilder.AppendLine("2. The 'main' function must return a dictionary (dict).");
            promptBuilder.AppendLine("3. You may modify the arguments of the 'main' function, but include appropriate type hints.");
            promptBuilder.AppendLine("4. The returned dictionary should contain at least one key-value pair.");
            promptBuilder.AppendLine();

            promptBuilder.AppendLine("5. You may ONLY use the following libraries in your code:");
            var allowedLibraries = new[]
            {
                "json", "datetime", "math", "random", "re", "string", "sys", "time", "traceback",
                "uuid", "os", "base64", "hashlib", "hmac", "binascii", "collections", "functools",
                "operator", "itertools","urllib.request","urllib.parse"
            };

            foreach (var library in allowedLibraries)
            {
                promptBuilder.AppendLine($"- {library}");
            }
            promptBuilder.AppendLine();

            promptBuilder.AppendLine("Example:");
            promptBuilder.AppendLine("def main(arg1: str, arg2: int) -> dict:");
            promptBuilder.AppendLine("    return {");
            promptBuilder.AppendLine("        \"result\": arg1 * arg2,");
            promptBuilder.AppendLine("    }");
            promptBuilder.AppendLine();

            promptBuilder.AppendLine("IMPORTANT:");
            promptBuilder.AppendLine("- Provide ONLY the code without any additional explanations, comments, or markdown formatting.");
            promptBuilder.AppendLine("- DO NOT use markdown code blocks (``` or ``` python). Return the raw code directly.");
            promptBuilder.AppendLine("- The code should start immediately after this instruction, without any preceding newlines or spaces.");
            promptBuilder.AppendLine("- The code should be complete, functional, and follow best practices for {{CODE_LANGUAGE}}.");
            promptBuilder.AppendLine("- Always use the format return {'result': ...} for the output.");
            promptBuilder.AppendLine();

            promptBuilder.AppendLine("Generated Code:");

            return ProcessTemplateVariables(promptBuilder.ToString(), instruction, codeLanguage);
        }

        private string ProcessTemplateVariables(string template, string instruction, string codeLanguage)
        {
            if (string.IsNullOrEmpty(template))
                return template;

            var result = template;

            var variableMappings = new Dictionary<string, string>
            {
                { "{{INSTRUCTION}}", instruction },
                { "{{CODE_LANGUAGE}}", codeLanguage }
            };

            foreach (var mapping in variableMappings)
            {
                result = result.Replace(mapping.Key, mapping.Value);
            }

            return result;
        }


    }
}
