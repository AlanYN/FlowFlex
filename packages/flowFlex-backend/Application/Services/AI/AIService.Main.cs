using DocumentFormat.OpenXml.Spreadsheet;
using FlowFlex.Application.Contracts.Dtos.OW.Checklist;
using FlowFlex.Application.Contracts.Dtos.OW.ChecklistTask;
using FlowFlex.Application.Contracts.Dtos.OW.Questionnaire;
using FlowFlex.Application.Contracts.Dtos.OW.Stage;
using FlowFlex.Application.Contracts.Dtos.OW.Workflow;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.Options;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FlowFlex.Infrastructure.Services;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static FastExpressionCompiler.ExpressionCompiler;

namespace FlowFlex.Application.Services.AI
{
    /// <summary>
    /// AI service implementation supporting multiple AI providers
    /// </summary>
    public partial class AIService : IAIService, IScopedService
    {
        private readonly AIOptions _aiOptions;
        private readonly ILogger<AIService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWorkflowService _workflowService;
        private readonly IAIModelConfigService _configService;
        private readonly IChecklistService _checklistService;
        private readonly IQuestionnaireService _questionnaireService;
        private readonly IChecklistRepository _checklistRepository;
        private readonly IQuestionnaireRepository _questionnaireRepository;
        private readonly IChecklistTaskService _checklistTaskService;
        private readonly IComponentMappingService _componentMappingService;
        private readonly IStageRepository _stageRepository;
        private readonly IAIPromptHistoryRepository _aiPromptHistoryRepository;
        private readonly IOperatorContextService _operatorContextService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;

        public AIService(
            IOptions<AIOptions> aiOptions,
            ILogger<AIService> logger,
            IHttpClientFactory httpClientFactory,
            IWorkflowService workflowService,
            IAIModelConfigService configService,
            IChecklistService checklistService,
            IQuestionnaireService questionnaireService,
            IChecklistRepository checklistRepository,
            IQuestionnaireRepository questionnaireRepository,
            IChecklistTaskService checklistTaskService,
            IComponentMappingService componentMappingService,
            IStageRepository stageRepository,
            IAIPromptHistoryRepository aiPromptHistoryRepository,
            IOperatorContextService operatorContextService,
            IHttpContextAccessor httpContextAccessor,
            IBackgroundTaskQueue backgroundTaskQueue)
        {
            _aiOptions = aiOptions.Value;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _workflowService = workflowService;
            _configService = configService;
            _checklistService = checklistService;
            _questionnaireService = questionnaireService;
            _checklistRepository = checklistRepository;
            _questionnaireRepository = questionnaireRepository;
            _checklistTaskService = checklistTaskService;
            _componentMappingService = componentMappingService;
            _stageRepository = stageRepository;
            _aiPromptHistoryRepository = aiPromptHistoryRepository;
            _operatorContextService = operatorContextService;
            _httpContextAccessor = httpContextAccessor;
            _backgroundTaskQueue = backgroundTaskQueue;
        }


        public async Task<AIRequirementsParsingResult> ParseRequirementsAsync(string naturalLanguage)
        {
            var startTime = DateTime.UtcNow;
            string prompt = null;
            AIProviderResponse aiResponse = null;

            try
            {
                _logger.LogInformation("Parsing requirements from natural language");

                prompt = $"""
                Please analyze the following natural language description and extract structured requirement information:

                Description: {naturalLanguage}

                Please extract:
                1. Process type
                2. Involved personnel
                3. Key steps
                4. Approval processes
                5. Notification requirements

                Please return the results in JSON format.
                """;

                aiResponse = await CallAIProviderAsync(prompt);

                // Save prompt history to database (fire-and-forget)
                // Background task queued
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        var metadata = JsonSerializer.Serialize(new
                        {
                            naturalLanguageLength = naturalLanguage?.Length ?? 0,
                            inputText = naturalLanguage?.Substring(0, Math.Min(200, naturalLanguage?.Length ?? 0))
                        });
                        await SavePromptHistoryAsync("RequirementsParsing", "Requirements", null, null,
                            prompt, aiResponse, startTime, null, null, null, metadata);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to save requirements parsing prompt history");
                    }
                });

                if (!aiResponse.Success)
                {
                    return new AIRequirementsParsingResult
                    {
                        Success = false,
                        Message = aiResponse.ErrorMessage
                    };
                }

                // Parse the AI response and extract requirements
                var requirements = new AIRequirements
                {
                    ProcessType = "General",
                    Stakeholders = new List<string> { "User", "Manager" },
                    Steps = new List<string> { "Start", "Process", "End" },
                    Approvals = new List<string>(),
                    Notifications = new List<string>()
                };

                return new AIRequirementsParsingResult
                {
                    Success = true,
                    Message = "Requirements parsed successfully",
                    Requirements = requirements
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing requirements");

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
                                    naturalLanguageLength = naturalLanguage?.Length ?? 0,
                                    error = ex.Message
                                });
                                await SavePromptHistoryAsync("RequirementsParsing", "Requirements", null, null,
                                    prompt, failedResponse, startTime, null, null, null, metadata);
                            }
                            catch (Exception saveEx)
                            {
                                _logger.LogWarning(saveEx, "Failed to save failed requirements parsing prompt history");
                            }
                        });
                }

                return new AIRequirementsParsingResult
                {
                    Success = false,
                    Message = $"Failed to parse requirements: {ex.Message}"
                };
            }
        }

        public async Task<AIRequirementsParsingResult> ParseRequirementsAsync(string naturalLanguage, string? modelProvider, string? modelName, string? modelId)
        {
            var startTime = DateTime.UtcNow;
            string prompt = null;
            AIProviderResponse aiResponse = null;

            try
            {
                _logger.LogInformation("Parsing requirements with explicit model override: Provider={Provider}, Model={ModelName}, Id={ModelId}", modelProvider, modelName, modelId);

                prompt = $"""
                Please analyze the following natural language description and extract structured requirement information:

                Description: {naturalLanguage}

                Please extract:
                1. Process type
                2. Involved personnel
                3. Key steps
                4. Approval processes
                5. Notification requirements

                Please return the results in JSON format.
                """;

                aiResponse = await CallAIProviderAsync(prompt, modelId, modelProvider, modelName);

                // Save prompt history to database (fire-and-forget)
                // Background task queued
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        var metadata = JsonSerializer.Serialize(new
                        {
                            naturalLanguageLength = naturalLanguage?.Length ?? 0,
                            inputText = naturalLanguage?.Substring(0, Math.Min(200, naturalLanguage?.Length ?? 0)),
                            explicitModelOverride = true
                        });
                        await SavePromptHistoryAsync("RequirementsParsing", "Requirements", null, null,
                            prompt, aiResponse, startTime, modelProvider, modelName, modelId, metadata);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to save requirements parsing prompt history");
                    }
                });

                if (!aiResponse.Success)
                {
                    return new AIRequirementsParsingResult
                    {
                        Success = false,
                        Message = aiResponse.ErrorMessage
                    };
                }

                var requirements = new AIRequirements
                {
                    ProcessType = "General",
                    Stakeholders = new List<string> { "User", "Manager" },
                    Steps = new List<string> { "Start", "Process", "End" },
                    Approvals = new List<string>(),
                    Notifications = new List<string>()
                };

                return new AIRequirementsParsingResult
                {
                    Success = true,
                    Message = "Requirements parsed successfully",
                    Requirements = requirements
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing requirements with override");

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
                                    naturalLanguageLength = naturalLanguage?.Length ?? 0,
                                    explicitModelOverride = true,
                                    error = ex.Message
                                });
                                await SavePromptHistoryAsync("RequirementsParsing", "Requirements", null, null,
                                    prompt, failedResponse, startTime, modelProvider, modelName, modelId, metadata);
                            }
                            catch (Exception saveEx)
                            {
                                _logger.LogWarning(saveEx, "Failed to save failed requirements parsing prompt history");
                            }
                        });
                }

                return new AIRequirementsParsingResult
                {
                    Success = false,
                    Message = $"Failed to parse requirements: {ex.Message}"
                };
            }
        }

        #region Private Methods

        private async Task<AIProviderResponse> CallAIProviderAsync(string prompt)
        {
            return await CallAIProviderAsync(prompt, null, null, null, null);
        }

        private async Task<AIProviderResponse> CallAIProviderAsync(string prompt, string? modelId, string? modelProvider, string? modelName, int? maxTokensOverride = null)
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
                            _logger.LogInformation("Using tenant default AI config: Provider={Provider}, Model={Model}, ConfigId={Id}", defaultConfig.Provider, defaultConfig.ModelName, defaultConfig.Id);
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

        private async Task<AIProviderResponse> CallZhipuAIAsync(string prompt)
        {
            return await CallZhipuAIAsync(prompt, null, null, null);
        }

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

        private async Task<AIProviderResponse> CallOpenAIAsync(string prompt)
        {
            return await CallOpenAIAsync(prompt, null, null, null);
        }

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
                // Note: For OpenAI, we need to use user configuration since there's no default OpenAI config in _aiOptions
                if (userConfig == null)
                {
                    _logger.LogWarning("No OpenAI configuration found for model ID: {ModelId}. Attempting to use ZhipuAI as fallback.", modelId);

                    // Try to fallback to ZhipuAI if OpenAI config is not available
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
                    // Use Item Gateway method
                    return await CallItemGatewayForMainAsync(prompt, userConfig);
                }

                // For native OpenAI API, strip provider prefix from model name
                // e.g., "openai/gpt-4o-mini" -> "gpt-4o-mini"
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

                // Construct the API URL for OpenAI, avoiding duplication if baseUrl already contains the endpoint
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
                        var content_str = messageContent.GetString();
                        return new AIProviderResponse
                        {
                            Success = true,
                            Content = content_str ?? string.Empty
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
                    // Use Item Gateway method
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
        /// Call Item Gateway API for Main service (non-streaming)
        /// </summary>
        private async Task<AIProviderResponse> CallItemGatewayForMainAsync(string prompt, AIModelConfig config)
        {
            try
            {
                // Get JWT token
                var jwtToken = await GetLLMGatewayJwtTokenAsync(config);

                var requestBody = new
                {
                    model = config.ModelName, // e.g., "openai/gpt-4" or "gemini/gemini-2.5-flash"
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

                    // Try to fallback to ZhipuAI if Claude config is not available
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
                if (responseData.TryGetProperty("content", out var content_array) && content_array.GetArrayLength() > 0)
                {
                    var firstContent = content_array[0];
                    if (firstContent.TryGetProperty("text", out var textContent))
                    {
                        var content_str = textContent.GetString();
                        return new AIProviderResponse
                        {
                            Success = true,
                            Content = content_str ?? string.Empty
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

                    // Try to fallback to ZhipuAI if DeepSeek config is not available
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

                // Construct the API URL for DeepSeek, avoiding duplication if baseUrl already contains the endpoint
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
                        var content_str = messageContent.GetString();
                        return new AIProviderResponse
                        {
                            Success = true,
                            Content = content_str ?? string.Empty
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

        private async Task<AIProviderResponse> CallGenericOpenAICompatibleAsync(string prompt, string? modelId, string? modelName, string providerName, int? maxTokensOverride = null)
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

                    // Try to fallback to ZhipuAI if config is not available
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
                                    var content_str = messageContent.GetString();
                                    return new AIProviderResponse
                                    {
                                        Success = true,
                                        Content = content_str ?? string.Empty
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


        #region Helper Classes

        public class AIProviderResponse
        {
            public bool Success { get; set; }
            public string Content { get; set; } = string.Empty;
            public string ErrorMessage { get; set; } = string.Empty;
            public string TokenUsage { get; set; } = string.Empty;
            public string Provider { get; set; } = string.Empty;
            public string ModelName { get; set; } = string.Empty;
            public string ModelId { get; set; } = string.Empty;
        }

        /// <summary>
        /// Save AI prompt history to database
        /// </summary>
        private async Task SavePromptHistoryAsync(string promptType, string entityType, long? entityId, long? onboardingId,
            string promptContent, AIProviderResponse response, DateTime startTime, string modelProvider = null,
            string modelName = null, string modelId = null, string metadata = null)
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
                        : System.Text.Json.JsonSerializer.Deserialize<object>(response.TokenUsage);
                }
                catch
                {
                    tokenUsageObj = new { };
                }
                try
                {
                    metadataObj = string.IsNullOrWhiteSpace(metadata)
                        ? new { }
                        : System.Text.Json.JsonSerializer.Deserialize<object>(metadata);
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
                    UserId = _operatorContextService?.GetOperatorId() ?? 0,
                    UserName = _operatorContextService?.GetOperatorDisplayName() ?? "",
                    IpAddress = GetClientIpAddress(),
                    UserAgent = GetUserAgent(),
                    CreateBy = _operatorContextService?.GetOperatorDisplayName() ?? "",
                    ModifyBy = _operatorContextService?.GetOperatorDisplayName() ?? "",
                    CreateUserId = _operatorContextService?.GetOperatorId() ?? 0,
                    ModifyUserId = _operatorContextService?.GetOperatorId() ?? 0,
                    CreateDate = DateTimeOffset.UtcNow,
                    ModifyDate = DateTimeOffset.UtcNow,
                    IsValid = true
                };

                // Initialize ID
                promptHistory.InitNewId();

                // Save to database
                await _aiPromptHistoryRepository.InsertAsync(promptHistory);

                _logger.LogDebug("Saved AI prompt history for {PromptType} - {EntityType}:{EntityId}",
                    promptType, entityType, entityId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save AI prompt history: {Error}", ex.Message);
                // Don't throw - this is a background operation
            }
        }

        private string GetClientIpAddress()
        {
            try
            {
                var httpContext = _httpContextAccessor?.HttpContext;
                if (httpContext?.Connection?.RemoteIpAddress != null)
                {
                    return httpContext.Connection.RemoteIpAddress.ToString();
                }

                // Check for forwarded IPs from proxies/load balancers
                var forwardedFor = httpContext?.Request?.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(forwardedFor))
                {
                    return forwardedFor.Split(',').FirstOrDefault()?.Trim() ?? "";
                }

                var realIp = httpContext?.Request?.Headers["X-Real-IP"].FirstOrDefault();
                if (!string.IsNullOrEmpty(realIp))
                {
                    return realIp;
                }

                return "";
            }
            catch
            {
                return "";
            }
        }

        private string GetUserAgent()
        {
            try
            {
                var httpContext = _httpContextAccessor?.HttpContext;
                return httpContext?.Request?.Headers["User-Agent"].FirstOrDefault() ?? "";
            }
            catch
            {
                return "";
            }
        }

        #endregion

        #region AI Provider Fallback for Actions

        /// <summary>
        /// Call AI provider with automatic fallback for Action operations
        /// </summary>
        /// <param name="prompt">The prompt to send</param>
        /// <param name="preferredModelId">Preferred model ID</param>
        /// <param name="preferredProvider">Preferred provider</param>
        /// <param name="preferredModelName">Preferred model name</param>
        /// <returns>AI response</returns>
        private async Task<AIProviderResponse> CallAIProviderWithFallbackForActionAsync(string prompt, string? preferredModelId, string? preferredProvider, string? preferredModelName)
        {
            try
            {
                // Step 1: Try preferred model if specified with optimized timeout
                if (!string.IsNullOrEmpty(preferredProvider) && !string.IsNullOrEmpty(preferredModelName))
                {
                    _logger.LogInformation("Trying preferred AI model for Action: {Provider} - {ModelName} (ID: {ModelId})",
                        preferredProvider, preferredModelName, preferredModelId ?? "Unknown");

                    var response = await CallAIProviderForActionAsync(prompt, preferredModelId, preferredProvider, preferredModelName);
                    if (response.Success)
                    {
                        _logger.LogInformation("Successfully used preferred AI model for Action operation");
                        return response;
                    }

                    _logger.LogWarning("Preferred AI model failed for Action: {Error}. Trying fallback options...", response.ErrorMessage);
                }
                else if (!string.IsNullOrEmpty(preferredModelId) && long.TryParse(preferredModelId, out var modelId))
                {
                    var preferredConfig = await _configService.GetConfigByIdAsync(modelId);
                    if (preferredConfig != null)
                    {
                        _logger.LogInformation("Trying preferred AI model for Action: {Provider} - {ModelName} (ID: {ModelId})",
                            preferredConfig.Provider, preferredConfig.ModelName, modelId);

                        var response = await CallAIProviderAsync(prompt, preferredModelId, preferredConfig.Provider, preferredConfig.ModelName);
                        if (response.Success)
                        {
                            _logger.LogInformation("Successfully used preferred AI model for Action operation");
                            return response;
                        }

                        _logger.LogWarning("Preferred AI model failed for Action: {Error}. Trying fallback options...", response.ErrorMessage);
                    }
                }

                // Step 2: Try user's default configuration
                try
                {
                    var defaultConfig = await _configService.GetUserDefaultConfigAsync(0);
                    if (defaultConfig != null && defaultConfig.Id.ToString() != preferredModelId)
                    {
                        _logger.LogInformation("Trying user default AI model for Action: {Provider} - {ModelName} (ID: {ModelId})",
                            defaultConfig.Provider, defaultConfig.ModelName, defaultConfig.Id);

                        var response = await CallAIProviderAsync(prompt, defaultConfig.Id.ToString(), defaultConfig.Provider, defaultConfig.ModelName);
                        if (response.Success)
                        {
                            _logger.LogInformation("Successfully used user default AI model for Action operation");
                            return response;
                        }

                        _logger.LogWarning("User default AI model failed for Action: {Error}. Trying other available models...", response.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get user default AI config for Action, trying other available models");
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
                        _logger.LogInformation("Trying available AI model for Action: {Provider} - {ModelName} (ID: {ModelId})",
                            config.Provider, config.ModelName, config.Id);

                        var response = await CallAIProviderForActionAsync(prompt, config.Id.ToString(), config.Provider, config.ModelName);
                        if (response.Success)
                        {
                            _logger.LogInformation("Successfully used fallback AI model for Action: {Provider} - {ModelName}",
                                config.Provider, config.ModelName);
                            return response;
                        }

                        _logger.LogWarning("AI model {Provider} - {ModelName} failed for Action: {Error}",
                            config.Provider, config.ModelName, response.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get user AI configs for Action, trying system defaults");
                }

                // Step 4: Fallback to system default (ZhipuAI)
                _logger.LogInformation("Trying system default ZhipuAI configuration for Action");
                var systemResponse = await CallZhipuAIAsync(prompt);
                if (systemResponse.Success)
                {
                    _logger.LogInformation("Successfully used system default ZhipuAI for Action operation");
                    return systemResponse;
                }

                // Step 5: All options exhausted, return fallback result
                _logger.LogError("All AI providers failed for Action operation, generating fallback response");
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
                _logger.LogError(ex, "Error in AI provider fallback for Action operation");
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


    }
}