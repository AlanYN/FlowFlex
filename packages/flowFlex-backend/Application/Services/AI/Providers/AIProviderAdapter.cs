using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.IServices.AI;
using FlowFlex.Application.Contracts.Options;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace FlowFlex.Application.Services.AI.Providers
{
    /// <summary>
    /// Unified AI provider adapter that encapsulates all provider-specific call logic.
    /// Supports ZhipuAI, OpenAI, Gemini, Claude, DeepSeek, LLMGateway (Item Gateway),
    /// and generic OpenAI-compatible providers.
    /// </summary>
    public class AIProviderAdapter : IAIProviderAdapter, IScopedService
    {
        private readonly AIOptions _aiOptions;
        private readonly ILogger<AIProviderAdapter> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAIModelConfigService _configService;

        // JWT Token cache for Item Gateway (provider -> token with expiry)
        private static readonly ConcurrentDictionary<string, (string Token, DateTime Expiry)> _jwtTokenCache = new();
        private static readonly TimeSpan _jwtTokenLifetime = TimeSpan.FromHours(1);

        public AIProviderAdapter(
            IOptions<AIOptions> aiOptions,
            ILogger<AIProviderAdapter> logger,
            IHttpClientFactory httpClientFactory,
            IAIModelConfigService configService)
        {
            _aiOptions = aiOptions.Value;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configService = configService;
        }

        #region Public Interface Methods

        /// <inheritdoc />
        public async Task<AIProviderResponse> CallAsync(AIProviderRequest request)
        {
            return await CallAIProviderAsync(
                request.Prompt,
                request.ModelId,
                request.Provider,
                request.ModelName,
                request.MaxTokensOverride);
        }

        /// <inheritdoc />
        public async Task<AIProviderResponse> CallChatAsync(AIChatProviderRequest request)
        {
            return await CallAIProviderForChatAsync(request.Messages, request.Config, request.Provider);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<string> StreamChatAsync(AIChatProviderRequest request)
        {
            await foreach (var chunk in CallAIProviderForStreamChatAsync(request.Messages, request.Config))
            {
                yield return chunk;
            }
        }

        /// <inheritdoc />
        public async Task<AIProviderResponse> CallWithFallbackAsync(AIProviderRequest request)
        {
            return await CallAIProviderWithFallbackAsync(
                request.Prompt,
                request.ModelId,
                request.Provider,
                request.ModelName);
        }

        #endregion

        #region Core Provider Routing

        /// <summary>
        /// Route AI provider call based on provider name with default config resolution
        /// </summary>
        private async Task<AIProviderResponse> CallAIProviderAsync(
            string prompt,
            string? modelId,
            string? modelProvider,
            string? modelName,
            int? maxTokensOverride = null)
        {
            try
            {
                // Prefer specific model if provided; otherwise try user default AI model config, finally fallback to app options
                string? effectiveModelId = modelId;
                string? effectiveProvider = modelProvider;
                string? effectiveModelName = modelName;

                if (string.IsNullOrWhiteSpace(effectiveProvider))
                {
                    try
                    {
                        var defaultConfig = await _configService.GetUserDefaultConfigAsync(0);
                        if (defaultConfig != null && !string.IsNullOrWhiteSpace(defaultConfig.Provider))
                        {
                            effectiveProvider = defaultConfig.Provider;
                            effectiveModelId = defaultConfig.Id.ToString();
                            effectiveModelName = string.IsNullOrWhiteSpace(modelName) ? defaultConfig.ModelName : modelName;
                            _logger.LogInformation("Using tenant default AI config: Provider={Provider}, Model={Model}, ConfigId={Id}",
                                defaultConfig.Provider, defaultConfig.ModelName, defaultConfig.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get default AI model config, will fallback to app-level options");
                    }
                }

                // Fallback to app settings if still missing
                var provider = (effectiveProvider ?? _aiOptions.Provider).ToLower();

                _logger.LogInformation("Using AI provider: {Provider}, Model: {ModelName} (ID: {ModelId})",
                    provider, effectiveModelName, effectiveModelId);

                switch (provider)
                {
                    case "zhipuai":
                        {
                            var r = await CallZhipuAIAsync(prompt, effectiveModelId, effectiveModelName, maxTokensOverride);
                            r.Provider = "zhipuai";
                            r.ModelName = effectiveModelName ?? string.Empty;
                            r.ModelId = effectiveModelId ?? string.Empty;
                            return r;
                        }
                    case "openai":
                        {
                            var r = await CallOpenAIAsync(prompt, effectiveModelId, effectiveModelName, maxTokensOverride);
                            r.Provider = "openai";
                            r.ModelName = effectiveModelName ?? string.Empty;
                            r.ModelId = effectiveModelId ?? string.Empty;
                            return r;
                        }
                    case "gemini":
                        {
                            var r = await CallGeminiAsync(prompt, effectiveModelId, effectiveModelName, maxTokensOverride);
                            r.Provider = "gemini";
                            r.ModelName = effectiveModelName ?? string.Empty;
                            r.ModelId = effectiveModelId ?? string.Empty;
                            return r;
                        }
                    case "claude":
                    case "anthropic":
                        {
                            var r = await CallClaudeAsync(prompt, effectiveModelId, effectiveModelName, maxTokensOverride);
                            r.Provider = "claude";
                            r.ModelName = effectiveModelName ?? string.Empty;
                            r.ModelId = effectiveModelId ?? string.Empty;
                            return r;
                        }
                    case "deepseek":
                        {
                            var r = await CallDeepSeekAsync(prompt, effectiveModelId, effectiveModelName, maxTokensOverride);
                            r.Provider = "deepseek";
                            r.ModelName = effectiveModelName ?? string.Empty;
                            r.ModelId = effectiveModelId ?? string.Empty;
                            return r;
                        }
                    default:
                        // Try to call using generic OpenAI-compatible API
                        _logger.LogInformation("Unknown provider {Provider}, attempting to use OpenAI-compatible API", provider);
                        {
                            var r = await CallGenericOpenAICompatibleAsync(prompt, effectiveModelId, effectiveModelName, provider, maxTokensOverride);
                            r.Provider = provider;
                            r.ModelName = effectiveModelName ?? string.Empty;
                            r.ModelId = effectiveModelId ?? string.Empty;
                            return r;
                        }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling AI provider: {Provider}", modelProvider ?? _aiOptions.Provider);
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"AI provider call failed: {ex.Message}"
                };
            }
        }

        #endregion

        #region Chat Provider Routing

        /// <summary>
        /// Route chat provider call based on config or provider name
        /// </summary>
        private async Task<AIProviderResponse> CallAIProviderForChatAsync(
            List<object> messages,
            AIModelConfig? userConfig,
            string? providerOverride)
        {
            try
            {
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
                        var provider = (providerOverride ?? userConfig.Provider)?.ToLower();
                        response = provider switch
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
                        _logger.LogWarning("Primary model hit rate limit, attempting fallback to ZhipuAI");

                        try
                        {
                            var fallbackResponse = await CallZhipuAIChatAsync(messages);
                            if (fallbackResponse.Success)
                            {
                                _logger.LogInformation("Successfully used ZhipuAI fallback");
                                return fallbackResponse;
                            }
                        }
                        catch (Exception fallbackEx)
                        {
                            _logger.LogWarning(fallbackEx, "Fallback to ZhipuAI also failed");
                        }
                    }
                }
                else
                {
                    // No specific model config found, use default ZhipuAI configuration
                    _logger.LogInformation("No specific model config found, using default ZhipuAI configuration");
                    response = await CallZhipuAIChatAsync(messages);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling AI provider for chat");
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"AI provider call failed: {ex.Message}"
                };
            }
        }

        #endregion

        #region Stream Chat Provider Routing

        /// <summary>
        /// Route streaming chat call based on config
        /// </summary>
        private async IAsyncEnumerable<string> CallAIProviderForStreamChatAsync(List<object> messages, AIModelConfig? userConfig)
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
                else if (provider == "item" || provider == "llmgateway" || provider == "gemini")
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

        #endregion

        #region Fallback Logic

        /// <summary>
        /// Call AI provider with automatic fallback strategy.
        /// Tries: preferred model -> user default -> all available configs -> system default (ZhipuAI)
        /// </summary>
        private async Task<AIProviderResponse> CallAIProviderWithFallbackAsync(
            string prompt,
            string? preferredModelId,
            string? preferredProvider,
            string? preferredModelName)
        {
            try
            {
                // Step 1: Try preferred model if specified
                if (!string.IsNullOrEmpty(preferredProvider) && !string.IsNullOrEmpty(preferredModelName))
                {
                    _logger.LogInformation("Trying preferred AI model: {Provider} - {ModelName} (ID: {ModelId})",
                        preferredProvider, preferredModelName, preferredModelId ?? "Unknown");

                    var response = await CallAIProviderAsync(prompt, preferredModelId, preferredProvider, preferredModelName);
                    if (response.Success)
                    {
                        _logger.LogInformation("Successfully used preferred AI model");
                        return response;
                    }

                    _logger.LogWarning("Preferred AI model failed: {Error}. Trying fallback options...", response.ErrorMessage);
                }
                else if (!string.IsNullOrEmpty(preferredModelId) && long.TryParse(preferredModelId, out var modelId))
                {
                    var preferredConfig = await _configService.GetConfigByIdAsync(modelId);
                    if (preferredConfig != null)
                    {
                        _logger.LogInformation("Trying preferred AI model: {Provider} - {ModelName} (ID: {ModelId})",
                            preferredConfig.Provider, preferredConfig.ModelName, modelId);

                        var response = await CallAIProviderAsync(prompt, preferredModelId, preferredConfig.Provider, preferredConfig.ModelName);
                        if (response.Success)
                        {
                            _logger.LogInformation("Successfully used preferred AI model");
                            return response;
                        }

                        _logger.LogWarning("Preferred AI model failed: {Error}. Trying fallback options...", response.ErrorMessage);
                    }
                }

                // Step 2: Try user's default configuration
                try
                {
                    var defaultConfig = await _configService.GetUserDefaultConfigAsync(0);
                    if (defaultConfig != null && defaultConfig.Id.ToString() != preferredModelId)
                    {
                        _logger.LogInformation("Trying user default AI model: {Provider} - {ModelName} (ID: {ModelId})",
                            defaultConfig.Provider, defaultConfig.ModelName, defaultConfig.Id);

                        var response = await CallAIProviderAsync(prompt, defaultConfig.Id.ToString(), defaultConfig.Provider, defaultConfig.ModelName);
                        if (response.Success)
                        {
                            _logger.LogInformation("Successfully used user default AI model");
                            return response;
                        }

                        _logger.LogWarning("User default AI model failed: {Error}. Trying other available models...", response.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get user default AI config, trying other available models");
                }

                // Step 3: Try all other available user configurations
                try
                {
                    var allConfigs = await _configService.GetUserAIModelConfigsAsync(0);
                    var availableConfigs = allConfigs.Where(c =>
                        c.Id.ToString() != preferredModelId &&
                        !string.IsNullOrEmpty(c.Provider) &&
                        !string.IsNullOrEmpty(c.ApiKey)).ToList();

                    foreach (var config in availableConfigs)
                    {
                        _logger.LogInformation("Trying available AI model: {Provider} - {ModelName} (ID: {ModelId})",
                            config.Provider, config.ModelName, config.Id);

                        var response = await CallAIProviderAsync(prompt, config.Id.ToString(), config.Provider, config.ModelName);
                        if (response.Success)
                        {
                            _logger.LogInformation("Successfully used fallback AI model: {Provider} - {ModelName}",
                                config.Provider, config.ModelName);
                            return response;
                        }

                        _logger.LogWarning("AI model {Provider} - {ModelName} failed: {Error}",
                            config.Provider, config.ModelName, response.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get user AI configs, trying system defaults");
                }

                // Step 4: Fallback to system default (ZhipuAI)
                _logger.LogInformation("Trying system default ZhipuAI configuration");
                var systemResponse = await CallZhipuAIAsync(prompt, null, null, null);
                if (systemResponse.Success)
                {
                    _logger.LogInformation("Successfully used system default ZhipuAI");
                    return systemResponse;
                }

                // Step 5: All options exhausted
                _logger.LogError("All AI providers failed, generating fallback response");
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = "All AI providers failed. Please check your AI model configurations and try again.",
                    Provider = "fallback",
                    ModelName = "fallback",
                    ModelId = "fallback"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AI provider fallback strategy");
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"AI provider fallback failed: {ex.Message}",
                    Provider = "error",
                    ModelName = "error",
                    ModelId = "error"
                };
            }
        }

        #endregion

        #region ZhipuAI Provider

        /// <summary>
        /// Call ZhipuAI with single prompt (for generation/parsing scenarios)
        /// </summary>
        private async Task<AIProviderResponse> CallZhipuAIAsync(string prompt, string? modelId, string? modelName, int? maxTokensOverride = null)
        {
            try
            {
                // Get user's AI model configuration if modelId is provided
                AIModelConfig? userConfig = null;
                if (!string.IsNullOrEmpty(modelId) && long.TryParse(modelId, out var configId))
                {
                    userConfig = await _configService.GetConfigByIdAsync(configId);
                    _logger.LogInformation("Using user's AI model configuration: {ConfigId}", configId);
                }

                // Use user config if available, otherwise fall back to default configuration
                var apiKey = userConfig?.ApiKey ?? _aiOptions.ZhipuAI.ApiKey;
                var baseUrl = userConfig?.BaseUrl ?? _aiOptions.ZhipuAI.BaseUrl;
                var model = userConfig?.ModelName ?? modelName ?? _aiOptions.ZhipuAI.Model;
                var temperature = userConfig?.Temperature ?? _aiOptions.ZhipuAI.Temperature;
                var maxTokens = maxTokensOverride ?? userConfig?.MaxTokens ?? _aiOptions.ZhipuAI.MaxTokens;

                _logger.LogInformation("ZhipuAI Request - Model: {Model}, BaseUrl: {BaseUrl}, MaxTokens: {MaxTokens}", model, baseUrl, maxTokens);

                var requestBody = new
                {
                    model = model,
                    messages = new[]
                    {
                        new { role = "system", content = "You are a professional workflow design expert. Please generate structured workflow definitions based on user requirements. Output the result according to the language input by the user." },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = maxTokens,
                    temperature = temperature,
                    stream = false
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Construct the API URL, avoiding duplication if baseUrl already contains the endpoint
                var apiUrl = baseUrl.TrimEnd('/');
                if (!apiUrl.EndsWith("/chat/completions"))
                {
                    apiUrl = $"{apiUrl}/chat/completions";
                }

                // Build request with HTTP/1.1 and per-call timeout
                using var httpClient = _httpClientFactory.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
                {
                    Version = new Version(1, 1),
                    Content = content,
                };
                request.Headers.Add("Authorization", $"Bearer {apiKey}");

                var timeoutSeconds = Math.Max(10, Math.Min(60, _aiOptions.ConnectionTest.TimeoutSeconds));
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                var response = await httpClient.SendAsync(request, cts.Token);
                var responseContent = await response.Content.ReadAsStringAsync(cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("ZhipuAI API call failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    return new AIProviderResponse
                    {
                        Success = false,
                        ErrorMessage = $"ZhipuAI API error: {response.StatusCode}"
                    };
                }

                var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var messageContent = responseData.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                return new AIProviderResponse
                {
                    Success = true,
                    Content = messageContent ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling ZhipuAI: {Error}", ex.Message);
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"ZhipuAI call failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Call ZhipuAI API using default configuration with chat messages
        /// </summary>
        private async Task<AIProviderResponse> CallZhipuAIChatAsync(List<object> messages)
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
        /// Call ZhipuAI API using user configuration with chat messages
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

            if (baseUrl.Contains("/chat/completions"))
            {
                apiUrl = baseUrl;
            }
            else
            {
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

        #endregion

        #region OpenAI Provider

        /// <summary>
        /// Call OpenAI with single prompt (for generation/parsing scenarios)
        /// </summary>
        private async Task<AIProviderResponse> CallOpenAIAsync(string prompt, string? modelId, string? modelName, int? maxTokensOverride = null)
        {
            try
            {
                // Get user's AI model configuration if modelId is provided
                AIModelConfig? userConfig = null;
                if (!string.IsNullOrEmpty(modelId) && long.TryParse(modelId, out var configId))
                {
                    _logger.LogInformation("Attempting to get OpenAI configuration for ID: {ConfigId}", configId);
                    userConfig = await _configService.GetConfigByIdAsync(configId);

                    if (userConfig != null)
                    {
                        _logger.LogInformation("Successfully retrieved OpenAI configuration: {ConfigId}, Provider: {Provider}, ModelName: {ModelName}",
                            configId, userConfig.Provider, userConfig.ModelName);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to retrieve OpenAI configuration for ID: {ConfigId} - configuration not found or access denied", configId);
                    }
                }

                // Use user config if available, otherwise fall back to default configuration
                if (userConfig == null)
                {
                    _logger.LogWarning("No OpenAI configuration found for model ID: {ModelId}. Attempting to use ZhipuAI as fallback.", modelId);

                    try
                    {
                        _logger.LogInformation("Falling back to ZhipuAI for workflow generation due to missing OpenAI configuration");
                        return await CallZhipuAIAsync(prompt, null, null, maxTokensOverride);
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger.LogError(fallbackEx, "Fallback to ZhipuAI also failed");
                        return new AIProviderResponse
                        {
                            Success = false,
                            ErrorMessage = $"OpenAI configuration not found (ID: {modelId}) and ZhipuAI fallback failed. Please configure your AI models properly."
                        };
                    }
                }

                var apiKey = userConfig.ApiKey;
                var baseUrl = userConfig.BaseUrl;
                var rawModel = userConfig.ModelName ?? modelName ?? "gpt-4o-mini";
                var temperature = userConfig.Temperature > 0 ? userConfig.Temperature : 0.7;
                var maxTokens = maxTokensOverride ?? (userConfig.MaxTokens > 0 ? userConfig.MaxTokens : 2000);

                _logger.LogInformation("OpenAI Request - Model: {Model}, BaseUrl: {BaseUrl}, MaxTokens: {MaxTokens}", rawModel, baseUrl, maxTokens);

                // Check if using Item Gateway
                var isItemGateway = !string.IsNullOrEmpty(baseUrl) &&
                                  baseUrl.Contains("aiop-gateway.item.com", StringComparison.OrdinalIgnoreCase);

                if (isItemGateway)
                {
                    return await CallItemGatewayForMainAsync(prompt, userConfig);
                }

                // For native OpenAI API, strip provider prefix from model name
                var model = GetNativeModelName(rawModel, userConfig.Provider);

                var requestBody = new
                {
                    model = model,
                    messages = new[]
                    {
                        new { role = "system", content = "You are a professional workflow design expert. Please generate structured workflow definitions based on user requirements. Output the result according to the language input by the user." },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = maxTokens,
                    temperature = temperature,
                    stream = false
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Construct the API URL for OpenAI, avoiding duplication
                var apiUrl = baseUrl.TrimEnd('/');
                if (!apiUrl.EndsWith("/v1/chat/completions") && !apiUrl.EndsWith("/chat/completions"))
                {
                    apiUrl = $"{apiUrl}/v1/chat/completions";
                }

                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                var response = await httpClient.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("OpenAI API call failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    return new AIProviderResponse
                    {
                        Success = false,
                        ErrorMessage = $"OpenAI API error: {response.StatusCode} - {responseContent}"
                    };
                }

                var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                // Parse OpenAI response format
                if (responseData.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var messageContent))
                    {
                        var contentStr = messageContent.GetString();
                        return new AIProviderResponse
                        {
                            Success = true,
                            Content = contentStr ?? string.Empty
                        };
                    }
                }

                _logger.LogError("Invalid OpenAI response format: {Response}", responseContent);
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid response format from OpenAI API"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OpenAI: {Error}", ex.Message);
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"OpenAI call failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Call OpenAI API using user configuration with chat messages
        /// </summary>
        private async Task<AIProviderResponse> CallOpenAIWithConfigAsync(List<object> messages, AIModelConfig config)
        {
            // Check if using Item Gateway - if so, keep the original model name with prefix
            var isItemGateway = !string.IsNullOrEmpty(config.BaseUrl) &&
                              config.BaseUrl.Contains("aiop-gateway.item.com", StringComparison.OrdinalIgnoreCase);

            // For native OpenAI API, strip provider prefix from model name
            var modelName = isItemGateway ? config.ModelName : GetNativeModelName(config.ModelName, config.Provider);

            var requestBody = new
            {
                model = modelName,
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
        /// Call OpenAI API with real streaming support
        /// </summary>
        private async IAsyncEnumerable<string> CallOpenAIStreamAsync(List<object> messages, AIModelConfig config)
        {
            // Check if using Item Gateway
            var isItemGateway = !string.IsNullOrEmpty(config.BaseUrl) &&
                              config.BaseUrl.Contains("aiop-gateway.item.com", StringComparison.OrdinalIgnoreCase);

            if (isItemGateway)
            {
                await foreach (var chunk in CallItemGatewayStreamAsync(messages, config))
                {
                    yield return chunk;
                }
                yield break;
            }

            // For native OpenAI API, strip provider prefix from model name
            var modelName = GetNativeModelName(config.ModelName, config.Provider);

            var requestBody = new
            {
                model = modelName,
                messages = messages.ToArray(),
                temperature = config.Temperature > 0 ? config.Temperature : 0.7,
                max_tokens = config.MaxTokens > 0 ? config.MaxTokens : 1000,
                stream = true
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
                            var chunkContent = contentProp.GetString();
                            if (!string.IsNullOrEmpty(chunkContent))
                            {
                                yield return chunkContent;
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Gemini Provider

        /// <summary>
        /// Call Gemini with single prompt (for generation/parsing scenarios)
        /// </summary>
        private async Task<AIProviderResponse> CallGeminiAsync(string prompt, string? modelId, string? modelName, int? maxTokensOverride = null)
        {
            try
            {
                // Get user's AI model configuration if modelId is provided
                AIModelConfig? userConfig = null;
                if (!string.IsNullOrEmpty(modelId) && long.TryParse(modelId, out var configId))
                {
                    _logger.LogInformation("Attempting to get Gemini configuration for ID: {ConfigId}", configId);
                    userConfig = await _configService.GetConfigByIdAsync(configId);

                    if (userConfig != null)
                    {
                        _logger.LogInformation("Successfully retrieved Gemini configuration: {ConfigId}, Provider: {Provider}, ModelName: {ModelName}",
                            configId, userConfig.Provider, userConfig.ModelName);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to retrieve Gemini configuration for ID: {ConfigId} - configuration not found or access denied", configId);
                    }
                }

                // Use user config if available
                if (userConfig == null)
                {
                    _logger.LogWarning("No Gemini configuration found for model ID: {ModelId}. Attempting to use OpenAI-compatible fallback.", modelId);
                    return await CallGenericOpenAICompatibleAsync(prompt, modelId, modelName, "gemini", maxTokensOverride);
                }

                var apiKey = userConfig.ApiKey;
                var baseUrl = userConfig.BaseUrl;
                var model = userConfig.ModelName ?? modelName ?? "gemini-2.5-flash";
                var temperature = userConfig.Temperature > 0 ? userConfig.Temperature : 0.7;
                var maxTokens = maxTokensOverride ?? (userConfig.MaxTokens > 0 ? userConfig.MaxTokens : 2000);

                _logger.LogInformation("Gemini Request - Model: {Model}, BaseUrl: {BaseUrl}, MaxTokens: {MaxTokens}", model, baseUrl, maxTokens);

                // Check if using Item Gateway
                var isItemGateway = !string.IsNullOrEmpty(baseUrl) &&
                                  baseUrl.Contains("aiop-gateway.item.com", StringComparison.OrdinalIgnoreCase);

                if (isItemGateway)
                {
                    return await CallItemGatewayForMainAsync(prompt, userConfig);
                }

                var requestBody = new
                {
                    model = model,
                    messages = new[]
                    {
                        new { role = "system", content = "You are a professional workflow design expert. Please generate structured workflow definitions based on user requirements. Output the result according to the language input by the user." },
                        new { role = "user", content = prompt }
                    },
                    temperature = temperature,
                    max_tokens = maxTokens
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Gemini uses OpenAI-compatible API format
                var apiUrl = $"{baseUrl.TrimEnd('/')}/v1/chat/completions";

                _logger.LogInformation("Calling Gemini API: {Url} with model: {Model}", apiUrl, model);

                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                var response = await httpClient.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Gemini API Response: {StatusCode} - {Content}",
                    response.StatusCode, responseContent.Length > 200 ? responseContent.Substring(0, 200) + "..." : responseContent);

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
                        ErrorMessage = $"Gemini API call failed: {response.StatusCode} - {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API");
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"Gemini call failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Call Gemini API using user configuration with chat messages
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

        #endregion

        #region Claude Provider

        /// <summary>
        /// Call Claude with single prompt (for generation/parsing scenarios)
        /// </summary>
        private async Task<AIProviderResponse> CallClaudeAsync(string prompt, string? modelId, string? modelName, int? maxTokensOverride = null)
        {
            try
            {
                // Get user's AI model configuration if modelId is provided
                AIModelConfig? userConfig = null;
                if (!string.IsNullOrEmpty(modelId) && long.TryParse(modelId, out var configId))
                {
                    _logger.LogInformation("Attempting to get Claude configuration for ID: {ConfigId}", configId);
                    userConfig = await _configService.GetConfigByIdAsync(configId);

                    if (userConfig != null)
                    {
                        _logger.LogInformation("Successfully retrieved Claude configuration: {ConfigId}, Provider: {Provider}, ModelName: {ModelName}",
                            configId, userConfig.Provider, userConfig.ModelName);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to retrieve Claude configuration for ID: {ConfigId} - configuration not found", configId);
                    }
                }

                // Use user config if available, otherwise fall back to default or fail
                if (userConfig == null)
                {
                    _logger.LogWarning("No Claude configuration found for model ID: {ModelId}. Attempting to use ZhipuAI as fallback.", modelId);

                    try
                    {
                        _logger.LogInformation("Falling back to ZhipuAI for workflow generation due to missing Claude configuration");
                        return await CallZhipuAIAsync(prompt, null, null, maxTokensOverride);
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger.LogError(fallbackEx, "Fallback to ZhipuAI also failed");
                        return new AIProviderResponse
                        {
                            Success = false,
                            ErrorMessage = $"Claude configuration not found (ID: {modelId}) and ZhipuAI fallback failed. Please configure your AI models properly."
                        };
                    }
                }

                var apiKey = userConfig.ApiKey;
                var baseUrl = userConfig.BaseUrl;
                var model = userConfig.ModelName ?? modelName ?? "claude-3-sonnet-20240229";
                var temperature = userConfig.Temperature > 0 ? userConfig.Temperature : 0.7;
                var maxTokens = maxTokensOverride ?? (userConfig.MaxTokens > 0 ? userConfig.MaxTokens : 2000);

                _logger.LogInformation("Claude Request - Model: {Model}, BaseUrl: {BaseUrl}, MaxTokens: {MaxTokens}", model, baseUrl, maxTokens);

                var requestBody = new
                {
                    model = model,
                    max_tokens = maxTokens,
                    temperature = temperature,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var httpClient = _httpClientFactory.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/v1/messages")
                {
                    Version = new Version(1, 1),
                    Content = content,
                };
                request.Headers.Add("x-api-key", apiKey);
                request.Headers.Add("anthropic-version", "2023-06-01");

                var timeoutSeconds = Math.Max(10, Math.Min(60, _aiOptions.ConnectionTest.TimeoutSeconds));
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                var response = await httpClient.SendAsync(request, cts.Token);
                var responseContent = await response.Content.ReadAsStringAsync(cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Claude API call failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    return new AIProviderResponse
                    {
                        Success = false,
                        ErrorMessage = $"Claude API error: {response.StatusCode} - {responseContent}"
                    };
                }

                var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                // Parse Claude response format
                if (responseData.TryGetProperty("content", out var contentArray) && contentArray.GetArrayLength() > 0)
                {
                    var firstContent = contentArray[0];
                    if (firstContent.TryGetProperty("text", out var textContent))
                    {
                        var contentStr = textContent.GetString();
                        return new AIProviderResponse
                        {
                            Success = true,
                            Content = contentStr ?? string.Empty
                        };
                    }
                }

                _logger.LogError("Invalid Claude response format: {Response}", responseContent);
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid response format from Claude API"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Claude: {Error}", ex.Message);
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"Claude call failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Call Claude API using user configuration with chat messages
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

            if (baseUrl.Contains("/messages"))
            {
                apiUrl = baseUrl;
            }
            else
            {
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

        #endregion

        #region DeepSeek Provider

        /// <summary>
        /// Call DeepSeek with single prompt (for generation/parsing scenarios)
        /// </summary>
        private async Task<AIProviderResponse> CallDeepSeekAsync(string prompt, string? modelId, string? modelName, int? maxTokensOverride = null)
        {
            try
            {
                // Get user's AI model configuration if modelId is provided
                AIModelConfig? userConfig = null;
                if (!string.IsNullOrEmpty(modelId) && long.TryParse(modelId, out var configId))
                {
                    _logger.LogInformation("Attempting to get DeepSeek configuration for ID: {ConfigId}", configId);
                    userConfig = await _configService.GetConfigByIdAsync(configId);

                    if (userConfig != null)
                    {
                        _logger.LogInformation("Successfully retrieved DeepSeek configuration: {ConfigId}, Provider: {Provider}, ModelName: {ModelName}",
                            configId, userConfig.Provider, userConfig.ModelName);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to retrieve DeepSeek configuration for ID: {ConfigId} - configuration not found", configId);
                    }
                }

                // Use user config if available, otherwise fall back to default or fail
                if (userConfig == null)
                {
                    _logger.LogWarning("No DeepSeek configuration found for model ID: {ModelId}. Attempting to use ZhipuAI as fallback.", modelId);

                    try
                    {
                        _logger.LogInformation("Falling back to ZhipuAI for workflow generation due to missing DeepSeek configuration");
                        return await CallZhipuAIAsync(prompt, null, null, maxTokensOverride);
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger.LogError(fallbackEx, "Fallback to ZhipuAI also failed");
                        return new AIProviderResponse
                        {
                            Success = false,
                            ErrorMessage = $"DeepSeek configuration not found (ID: {modelId}) and ZhipuAI fallback failed. Please configure your AI models properly."
                        };
                    }
                }

                var apiKey = userConfig.ApiKey;
                var baseUrl = userConfig.BaseUrl;
                var model = userConfig.ModelName ?? modelName ?? "deepseek-chat";
                var temperature = userConfig.Temperature > 0 ? userConfig.Temperature : 0.7;
                var maxTokens = maxTokensOverride ?? (userConfig.MaxTokens > 0 ? userConfig.MaxTokens : 2000);

                _logger.LogInformation("DeepSeek Request - Model: {Model}, BaseUrl: {BaseUrl}, MaxTokens: {MaxTokens}", model, baseUrl, maxTokens);

                // DeepSeek uses OpenAI-compatible API format
                var requestBody = new
                {
                    model = model,
                    messages = new[]
                    {
                        new { role = "system", content = "You are a professional workflow design expert. Please generate structured workflow definitions based on user requirements. Output the result according to the language input by the user." },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = maxTokens,
                    temperature = temperature,
                    stream = false
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Construct the API URL for DeepSeek, avoiding duplication
                var apiUrl = baseUrl.TrimEnd('/');
                if (!apiUrl.EndsWith("/v1/chat/completions") && !apiUrl.EndsWith("/chat/completions"))
                {
                    apiUrl = $"{apiUrl}/v1/chat/completions";
                }

                using var httpClient = _httpClientFactory.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
                {
                    Version = new Version(1, 1),
                    Content = content,
                };
                request.Headers.Add("Authorization", $"Bearer {apiKey}");

                var timeoutSeconds = Math.Max(10, Math.Min(60, _aiOptions.ConnectionTest.TimeoutSeconds));
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                var response = await httpClient.SendAsync(request, cts.Token);
                var responseContent = await response.Content.ReadAsStringAsync(cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("DeepSeek API call failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    return new AIProviderResponse
                    {
                        Success = false,
                        ErrorMessage = $"DeepSeek API error: {response.StatusCode} - {responseContent}"
                    };
                }

                var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                // Parse OpenAI-compatible response format
                if (responseData.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var messageContent))
                    {
                        var contentStr = messageContent.GetString();
                        return new AIProviderResponse
                        {
                            Success = true,
                            Content = contentStr ?? string.Empty
                        };
                    }
                }

                _logger.LogError("Invalid DeepSeek response format: {Response}", responseContent);
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid response format from DeepSeek API"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling DeepSeek: {Error}", ex.Message);
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"DeepSeek call failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Call DeepSeek API using user configuration with chat messages
        /// </summary>
        private async Task<AIProviderResponse> CallDeepSeekWithConfigAsync(List<object> messages, AIModelConfig config)
        {
            // For native DeepSeek API, strip provider prefix from model name
            var modelName = GetNativeModelName(config.ModelName, config.Provider);

            var requestBody = new
            {
                model = modelName,
                messages = messages.ToArray(),
                temperature = config.Temperature > 0 ? config.Temperature : 0.7,
                max_tokens = config.MaxTokens > 0 ? config.MaxTokens : 4000
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Intelligently handle API endpoints, avoid path duplication
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

        /// <summary>
        /// Call DeepSeek API with real streaming support
        /// </summary>
        private async IAsyncEnumerable<string> CallDeepSeekStreamAsync(List<object> messages, AIModelConfig config)
        {
            // For native DeepSeek API, strip provider prefix from model name
            var modelName = GetNativeModelName(config.ModelName, config.Provider);

            var requestBody = new
            {
                model = modelName,
                messages = messages.ToArray(),
                temperature = config.Temperature > 0 ? config.Temperature : 0.7,
                max_tokens = config.MaxTokens > 0 ? config.MaxTokens : 4000,
                stream = true
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
                    _logger.LogDebug("DeepSeek: Starting to read line with {Timeout}s timeout", lineTimeout.TotalSeconds);
                    readLine = await reader.ReadLineAsync().WaitAsync(cts.Token);
                    var readDuration = (DateTime.UtcNow - readStartTime).TotalMilliseconds;
                    const double slowReadInfoThresholdMs = 50d;
                    if (readDuration >= slowReadInfoThresholdMs)
                        _logger.LogInformation("DeepSeek: Line read completed in {Duration}ms", readDuration);
                    else
                        _logger.LogDebug("DeepSeek: Line read completed in {Duration}ms", readDuration);
                }
                catch (OperationCanceledException)
                {
                    var readDuration = (DateTime.UtcNow - readStartTime).TotalMilliseconds;
                    _logger.LogWarning("DeepSeek stream line read timeout after {Duration}ms (expected {Timeout}s), breaking stream",
                        readDuration, lineTimeout.TotalSeconds);
                    shouldBreak = true;
                }
                catch (Exception ex)
                {
                    var readDuration = (DateTime.UtcNow - readStartTime).TotalMilliseconds;
                    _logger.LogWarning(ex, "Error reading DeepSeek stream line after {Duration}ms, breaking stream", readDuration);
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
                                var chunkContent = contentProp.GetString();
                                if (!string.IsNullOrEmpty(chunkContent))
                                {
                                    contentToYield = chunkContent;
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

                if (!string.IsNullOrEmpty(contentToYield))
                {
                    yield return contentToYield;
                }
            }
        }

        #endregion

        #region Item Gateway / LLM Gateway Provider

        /// <summary>
        /// Call Item Gateway API for Main service (non-streaming, single prompt)
        /// </summary>
        private async Task<AIProviderResponse> CallItemGatewayForMainAsync(string prompt, AIModelConfig config)
        {
            try
            {
                // Get JWT token
                var jwtToken = await GetLLMGatewayJwtTokenAsync(config);

                var requestBody = new
                {
                    model = config.ModelName,
                    messages = new[]
                    {
                        new { role = "system", content = "You are a professional workflow design expert. Please generate structured workflow definitions based on user requirements. Output the result according to the language input by the user." },
                        new { role = "user", content = prompt }
                    },
                    temperature = config.Temperature > 0 ? config.Temperature : 0.7,
                    max_tokens = config.MaxTokens > 0 ? config.MaxTokens : 2000,
                    stream = false
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

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
                        Content = messageContent
                    };
                }
                else
                {
                    _logger.LogError("Item Gateway API call failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    return new AIProviderResponse
                    {
                        Success = false,
                        ErrorMessage = $"Item Gateway API error: {response.StatusCode} - {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Item Gateway API");
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"Item Gateway call failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Call Item Gateway API using user configuration with chat messages
        /// </summary>
        private async Task<AIProviderResponse> CallLLMGatewayWithConfigAsync(List<object> messages, AIModelConfig config)
        {
            try
            {
                // Get JWT token
                var jwtToken = await GetLLMGatewayJwtTokenAsync(config);

                var requestBody = new
                {
                    model = config.ModelName,
                    messages = messages.ToArray(),
                    temperature = config.Temperature > 0 ? config.Temperature : 0.7,
                    max_tokens = config.MaxTokens > 0 ? config.MaxTokens : 4000
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

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
                max_tokens = Math.Max(config.MaxTokens > 0 ? config.MaxTokens : 10000, 10000),
                stream = true
            };

            var json = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

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
                    _logger.LogDebug("Item Gateway: Starting to read line with {Timeout}s timeout", lineTimeout.TotalSeconds);
                    readLine = await reader.ReadLineAsync().WaitAsync(cts.Token);
                    var readDuration = (DateTime.UtcNow - readStartTime).TotalMilliseconds;
                    const double slowReadInfoThresholdMs = 50d;
                    if (readDuration >= slowReadInfoThresholdMs)
                        _logger.LogInformation("Item Gateway: Line read completed in {Duration}ms", readDuration);
                    else
                        _logger.LogDebug("Item Gateway: Line read completed in {Duration}ms", readDuration);
                }
                catch (OperationCanceledException)
                {
                    var readDuration = (DateTime.UtcNow - readStartTime).TotalMilliseconds;
                    _logger.LogWarning("Item Gateway stream line read timeout after {Duration}ms (expected {Timeout}s), breaking stream",
                        readDuration, lineTimeout.TotalSeconds);
                    shouldBreak = true;
                }
                catch (Exception ex)
                {
                    var readDuration = (DateTime.UtcNow - readStartTime).TotalMilliseconds;
                    _logger.LogWarning(ex, "Error reading Item Gateway stream line after {Duration}ms, breaking stream", readDuration);
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
                                var chunkContent = contentProp.GetString();
                                if (!string.IsNullOrEmpty(chunkContent))
                                {
                                    contentToYield = chunkContent;
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
        /// Get or refresh JWT token for Item Gateway
        /// </summary>
        private async Task<string> GetLLMGatewayJwtTokenAsync(AIModelConfig config)
        {
            var cacheKey = $"{config.Provider}_{config.ApiKey}";

            // Check if we have a valid cached token
            if (_jwtTokenCache.TryGetValue(cacheKey, out var cachedToken))
            {
                if (cachedToken.Expiry > DateTime.UtcNow.AddMinutes(5))
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

        #endregion

        #region Generic OpenAI-Compatible Provider

        /// <summary>
        /// Call generic OpenAI-compatible provider with single prompt
        /// </summary>
        private async Task<AIProviderResponse> CallGenericOpenAICompatibleAsync(
            string prompt, string? modelId, string? modelName, string providerName, int? maxTokensOverride = null)
        {
            try
            {
                // Get user's AI model configuration if modelId is provided
                AIModelConfig? userConfig = null;
                if (!string.IsNullOrEmpty(modelId) && long.TryParse(modelId, out var configId))
                {
                    _logger.LogInformation("Attempting to get {Provider} configuration for ID: {ConfigId}", providerName, configId);
                    userConfig = await _configService.GetConfigByIdAsync(configId);

                    if (userConfig != null)
                    {
                        _logger.LogInformation("Successfully retrieved {Provider} configuration: {ConfigId}, Provider: {Provider}, ModelName: {ModelName}",
                            providerName, configId, userConfig.Provider, userConfig.ModelName);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to retrieve {Provider} configuration for ID: {ConfigId} - configuration not found", providerName, configId);
                    }
                }

                // Use user config if available, otherwise fall back to default or fail
                if (userConfig == null)
                {
                    _logger.LogWarning("No {Provider} configuration found for model ID: {ModelId}. Attempting to use ZhipuAI as fallback.", providerName, modelId);

                    try
                    {
                        _logger.LogInformation("Falling back to ZhipuAI for workflow generation due to missing {Provider} configuration", providerName);
                        return await CallZhipuAIAsync(prompt, null, null, maxTokensOverride);
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger.LogError(fallbackEx, "Fallback to ZhipuAI also failed");
                        return new AIProviderResponse
                        {
                            Success = false,
                            ErrorMessage = $"{providerName} configuration not found (ID: {modelId}) and ZhipuAI fallback failed. Please configure your AI models properly."
                        };
                    }
                }

                var apiKey = userConfig.ApiKey;
                var baseUrl = userConfig.BaseUrl;
                var model = userConfig.ModelName ?? modelName ?? "default-model";
                var temperature = userConfig.Temperature > 0 ? userConfig.Temperature : 0.7;
                var maxTokens = maxTokensOverride ?? (userConfig.MaxTokens > 0 ? userConfig.MaxTokens : 2000);

                _logger.LogInformation("{Provider} Request - Model: {Model}, BaseUrl: {BaseUrl}, MaxTokens: {MaxTokens}", providerName, model, baseUrl, maxTokens);

                // Use OpenAI-compatible API format (most providers support this)
                var requestBody = new
                {
                    model = model,
                    messages = new[]
                    {
                        new { role = "system", content = "You are a professional workflow design expert. Please generate structured workflow definitions based on user requirements. Output the result according to the language input by the user." },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = maxTokens,
                    temperature = temperature,
                    stream = false
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Try both common endpoints
                var endpoints = new[] { "/v1/chat/completions", "/chat/completions" };
                AIProviderResponse? lastResponse = null;

                foreach (var endpoint in endpoints)
                {
                    try
                    {
                        using var httpClient = _httpClientFactory.CreateClient();
                        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}{endpoint}")
                        {
                            Version = new Version(1, 1),
                            Content = content,
                        };
                        request.Headers.Add("Authorization", $"Bearer {apiKey}");
                        request.Headers.Add("X-App-Code", $"wfe");
                        var timeoutSeconds = Math.Max(10, Math.Min(60, _aiOptions.ConnectionTest.TimeoutSeconds));
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                        var response = await httpClient.SendAsync(request, cts.Token);
                        var responseContent = await response.Content.ReadAsStringAsync(cts.Token);

                        if (response.IsSuccessStatusCode)
                        {
                            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                            // Parse OpenAI-compatible response format
                            if (responseData.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                            {
                                var firstChoice = choices[0];
                                if (firstChoice.TryGetProperty("message", out var message) &&
                                    message.TryGetProperty("content", out var messageContent))
                                {
                                    var contentStr = messageContent.GetString();
                                    return new AIProviderResponse
                                    {
                                        Success = true,
                                        Content = contentStr ?? string.Empty
                                    };
                                }
                            }

                            _logger.LogError("Invalid {Provider} response format: {Response}", providerName, responseContent);
                            lastResponse = new AIProviderResponse
                            {
                                Success = false,
                                ErrorMessage = $"Invalid response format from {providerName} API"
                            };
                        }
                        else
                        {
                            _logger.LogWarning("{Provider} API call failed with endpoint {Endpoint}: {StatusCode} - {Content}",
                                providerName, endpoint, response.StatusCode, responseContent);
                            lastResponse = new AIProviderResponse
                            {
                                Success = false,
                                ErrorMessage = $"{providerName} API error: {response.StatusCode} - {responseContent}"
                            };
                        }
                    }
                    catch (Exception endpointEx)
                    {
                        _logger.LogWarning(endpointEx, "Failed to call {Provider} with endpoint {Endpoint}", providerName, endpoint);
                        lastResponse = new AIProviderResponse
                        {
                            Success = false,
                            ErrorMessage = $"{providerName} endpoint {endpoint} failed: {endpointEx.Message}"
                        };
                    }
                }

                return lastResponse ?? new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"All {providerName} endpoints failed"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling {Provider}: {Error}", providerName, ex.Message);
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"{providerName} call failed: {ex.Message}"
                };
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Get native model name by stripping provider prefix if present.
        /// For native API calls (not via gateway), model names like "openai/gpt-4o-mini" 
        /// need to be converted to "gpt-4o-mini".
        /// </summary>
        /// <param name="modelName">Original model name (may contain provider prefix)</param>
        /// <param name="provider">Provider name</param>
        /// <returns>Native model name without provider prefix</returns>
        internal static string GetNativeModelName(string modelName, string? provider)
        {
            if (string.IsNullOrEmpty(modelName))
                return modelName;

            // Common provider prefixes that need to be stripped for native API calls
            var prefixes = new[] { "openai/", "deepseek/", "claude/", "anthropic/", "gemini/" };

            foreach (var prefix in prefixes)
            {
                if (modelName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return modelName.Substring(prefix.Length);
                }
            }

            return modelName;
        }

        #endregion
    }
}
