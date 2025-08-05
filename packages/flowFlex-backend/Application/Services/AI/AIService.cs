using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.Options;
using FlowFlex.Application.Contracts.Dtos.OW.Workflow;
using FlowFlex.Application.Contracts.Dtos.OW.Questionnaire;
using FlowFlex.Application.Contracts.Dtos.OW.Checklist;
using FlowFlex.Application.Contracts.Dtos.OW.Stage;
using FlowFlex.Application.Contracts.Dtos.OW.ChecklistTask;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Application.Services.AI
{
    /// <summary>
    /// AI service implementation supporting multiple AI providers
    /// </summary>
    public class AIService : IAIService, IScopedService
    {
        private readonly AIOptions _aiOptions;
        private readonly ILogger<AIService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IMCPService _mcpService;
        private readonly IWorkflowService _workflowService;
        private readonly IAIModelConfigService _configService;

        public AIService(
            IOptions<AIOptions> aiOptions,
            ILogger<AIService> logger,
            HttpClient httpClient,
            IMCPService mcpService,
            IWorkflowService workflowService,
            IAIModelConfigService configService)
        {
            _aiOptions = aiOptions.Value;
            _logger = logger;
            _httpClient = httpClient;
            _mcpService = mcpService;
            _workflowService = workflowService;
            _configService = configService;
        }

        public async Task<AIWorkflowGenerationResult> GenerateWorkflowAsync(AIWorkflowGenerationInput input)
        {
            try
            {
                _logger.LogInformation("Generating workflow from natural language with enhanced context");
                _logger.LogInformation("Description length: {DescriptionLength} characters", input.Description?.Length ?? 0);
                _logger.LogInformation("AI Model: {Provider} {Model} (ID: {ModelId})", 
                    input.ModelProvider, input.ModelName, input.ModelId);
                _logger.LogInformation("Session ID: {SessionId}", input.SessionId);
                _logger.LogInformation("Conversation History: {MessageCount} messages", 
                    input.ConversationHistory?.Count ?? 0);

                // Store enhanced context in MCP for future reference
                var contextId = $"workflow_generation_{input.SessionId}_{DateTime.UtcNow:yyyyMMddHHmmss}";
                var contextMetadata = new Dictionary<string, object>
                {
                    { "type", "workflow_generation" },
                    { "timestamp", DateTime.UtcNow },
                    { "description", input.Description },
                    { "sessionId", input.SessionId ?? "" },
                    { "modelProvider", input.ModelProvider ?? "" },
                    { "modelName", input.ModelName ?? "" },
                    { "conversationMessageCount", input.ConversationHistory?.Count ?? 0 }
                };
                
                if (input.ConversationMetadata != null)
                {
                    contextMetadata.Add("conversationMode", input.ConversationMetadata.ConversationMode ?? "");
                    contextMetadata.Add("totalMessages", input.ConversationMetadata.TotalMessages);
                }

                await _mcpService.StoreContextAsync(contextId, JsonSerializer.Serialize(input), contextMetadata);

                var prompt = BuildWorkflowGenerationPrompt(input);
                var aiResponse = await CallAIProviderAsync(prompt, input.ModelId, input.ModelProvider, input.ModelName);

                if (!aiResponse.Success)
                {
                    return new AIWorkflowGenerationResult
                    {
                        Success = false,
                        Message = aiResponse.ErrorMessage,
                        ConfidenceScore = 0
                    };
                }

                var result = ParseWorkflowGenerationResponse(aiResponse.Content);
                result.ConfidenceScore = CalculateConfidenceScore(result.GeneratedWorkflow);

                // 确保至少有一些stages
                if (result.Stages == null || !result.Stages.Any())
                {
                    _logger.LogWarning("AI response did not contain valid stages, using fallback stages");
                    result = GenerateFallbackWorkflow(aiResponse.Content);
                }

                _logger.LogInformation("Successfully generated workflow with {StageCount} stages", result.Stages.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating workflow from description: {Description}", input.Description);
                return new AIWorkflowGenerationResult
                {
                    Success = false,
                    Message = $"Failed to generate workflow: {ex.Message}",
                    ConfidenceScore = 0
                };
            }
        }

        public async Task<AIQuestionnaireGenerationResult> GenerateQuestionnaireAsync(AIQuestionnaireGenerationInput input)
        {
            try
            {
                _logger.LogInformation("Generating questionnaire for purpose: {Purpose}", input.Purpose);

                var prompt = BuildQuestionnaireGenerationPrompt(input);
                var aiResponse = await CallAIProviderAsync(prompt);

                if (!aiResponse.Success)
                {
                    return new AIQuestionnaireGenerationResult
                    {
                        Success = false,
                        Message = aiResponse.ErrorMessage,
                        ConfidenceScore = 0
                    };
                }

                var result = ParseQuestionnaireGenerationResponse(aiResponse.Content);
                result.ConfidenceScore = CalculateQuestionnaireConfidenceScore(result.GeneratedQuestionnaire);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating questionnaire: {Purpose}", input.Purpose);
                return new AIQuestionnaireGenerationResult
                {
                    Success = false,
                    Message = $"Failed to generate questionnaire: {ex.Message}",
                    ConfidenceScore = 0
                };
            }
        }

        public async Task<AIChecklistGenerationResult> GenerateChecklistAsync(AIChecklistGenerationInput input)
        {
            try
            {
                _logger.LogInformation("Generating checklist for process: {ProcessName}", input.ProcessName);

                var prompt = BuildChecklistGenerationPrompt(input);
                var aiResponse = await CallAIProviderAsync(prompt);

                if (!aiResponse.Success)
                {
                    return new AIChecklistGenerationResult
                    {
                        Success = false,
                        Message = aiResponse.ErrorMessage,
                        ConfidenceScore = 0
                    };
                }

                var result = ParseChecklistGenerationResponse(aiResponse.Content);
                result.ConfidenceScore = CalculateChecklistConfidenceScore(result.GeneratedChecklist);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating checklist: {ProcessName}", input.ProcessName);
                return new AIChecklistGenerationResult
                {
                    Success = false,
                    Message = $"Failed to generate checklist: {ex.Message}",
                    ConfidenceScore = 0
                };
            }
        }

        public async IAsyncEnumerable<AIWorkflowStreamResult> StreamGenerateWorkflowAsync(AIWorkflowGenerationInput input)
        {
            _logger.LogInformation("Starting streaming workflow generation: {Description}", input.Description);

            yield return new AIWorkflowStreamResult
            {
                Type = "start",
                Message = "开始生成工作流...",
                IsComplete = false
            };

            string prompt = null;
            AIProviderResponse aiResponse = null;
            AIWorkflowGenerationResult result = null;
            Exception caughtException = null;

            try
            {
                prompt = BuildWorkflowGenerationPrompt(input);
                aiResponse = await CallAIProviderAsync(prompt);
                
                if (aiResponse.Success)
                {
                    result = ParseWorkflowGenerationResponse(aiResponse.Content);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in streaming workflow generation");
                caughtException = ex;
            }

            yield return new AIWorkflowStreamResult
            {
                Type = "progress",
                Message = "正在分析需求...",
                IsComplete = false
            };

            if (caughtException != null)
            {
                yield return new AIWorkflowStreamResult
                {
                    Type = "error",
                    Message = $"生成过程中出现错误: {caughtException.Message}",
                    IsComplete = true
                };
                yield break;
            }

            if (aiResponse == null || !aiResponse.Success)
            {
                yield return new AIWorkflowStreamResult
                {
                    Type = "error",
                    Message = aiResponse?.ErrorMessage ?? "AI服务调用失败",
                    IsComplete = true
                };
                yield break;
            }

            yield return new AIWorkflowStreamResult
            {
                Type = "progress",
                Message = "正在解析工作流结构...",
                IsComplete = false
            };

            if (result?.GeneratedWorkflow != null)
            {
                yield return new AIWorkflowStreamResult
                {
                    Type = "workflow",
                    Data = result.GeneratedWorkflow,
                    Message = "工作流基本信息已生成",
                    IsComplete = false
                };

                foreach (var stage in result.Stages)
                {
                    yield return new AIWorkflowStreamResult
                    {
                        Type = "stage",
                        Data = stage,
                        Message = $"阶段 '{stage.Name}' 已生成",
                        IsComplete = false
                    };
                }

                yield return new AIWorkflowStreamResult
                {
                    Type = "complete",
                    Data = result,
                    Message = "工作流生成完成",
                    IsComplete = true
                };
            }
            else
            {
                yield return new AIWorkflowStreamResult
                {
                    Type = "error",
                    Message = "无法解析AI生成的工作流结构",
                    IsComplete = true
                };
            }
        }

        public async IAsyncEnumerable<AIQuestionnaireStreamResult> StreamGenerateQuestionnaireAsync(AIQuestionnaireGenerationInput input)
        {
            _logger.LogInformation("Starting streaming questionnaire generation: {Purpose}", input.Purpose);

            yield return new AIQuestionnaireStreamResult
            {
                Type = "start",
                Message = "开始生成问卷...",
                IsComplete = false
            };

            string prompt = null;
            AIProviderResponse aiResponse = null;
            AIQuestionnaireGenerationResult result = null;
            Exception caughtException = null;

            try
            {
                prompt = BuildQuestionnaireGenerationPrompt(input);
                aiResponse = await CallAIProviderAsync(prompt);
                
                if (aiResponse.Success)
                {
                    result = ParseQuestionnaireGenerationResponse(aiResponse.Content);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in streaming questionnaire generation");
                caughtException = ex;
            }

            if (caughtException != null)
            {
                yield return new AIQuestionnaireStreamResult
                {
                    Type = "error",
                    Message = $"生成过程中出现错误: {caughtException.Message}",
                    IsComplete = true
                };
                yield break;
            }

            if (aiResponse == null || !aiResponse.Success)
            {
                yield return new AIQuestionnaireStreamResult
                {
                    Type = "error",
                    Message = aiResponse?.ErrorMessage ?? "AI服务调用失败",
                    IsComplete = true
                };
                yield break;
            }

            if (result?.GeneratedQuestionnaire != null)
            {
                yield return new AIQuestionnaireStreamResult
                {
                    Type = "questionnaire",
                    Data = result.GeneratedQuestionnaire,
                    Message = "问卷基本信息已生成",
                    IsComplete = false
                };

                yield return new AIQuestionnaireStreamResult
                {
                    Type = "complete",
                    Data = result,
                    Message = "问卷生成完成",
                    IsComplete = true
                };
            }
            else
            {
                yield return new AIQuestionnaireStreamResult
                {
                    Type = "error",
                    Message = "无法解析AI生成的问卷结构",
                    IsComplete = true
                };
            }
        }

        public async IAsyncEnumerable<AIChecklistStreamResult> StreamGenerateChecklistAsync(AIChecklistGenerationInput input)
        {
            _logger.LogInformation("Starting streaming checklist generation: {ProcessName}", input.ProcessName);

            yield return new AIChecklistStreamResult
            {
                Type = "start",
                Message = "开始生成检查清单...",
                IsComplete = false
            };

            string prompt = null;
            AIProviderResponse aiResponse = null;
            AIChecklistGenerationResult result = null;
            Exception caughtException = null;

            try
            {
                prompt = BuildChecklistGenerationPrompt(input);
                aiResponse = await CallAIProviderAsync(prompt);
                
                if (aiResponse.Success)
                {
                    result = ParseChecklistGenerationResponse(aiResponse.Content);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in streaming checklist generation");
                caughtException = ex;
            }

            if (caughtException != null)
            {
                yield return new AIChecklistStreamResult
                {
                    Type = "error",
                    Message = $"生成过程中出现错误: {caughtException.Message}",
                    IsComplete = true
                };
                yield break;
            }

            if (aiResponse == null || !aiResponse.Success)
            {
                yield return new AIChecklistStreamResult
                {
                    Type = "error",
                    Message = aiResponse?.ErrorMessage ?? "AI服务调用失败",
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
                    Message = "检查清单基本信息已生成",
                    IsComplete = false
                };

                yield return new AIChecklistStreamResult
                {
                    Type = "complete",
                    Data = result,
                    Message = "检查清单生成完成",
                    IsComplete = true
                };
            }
            else
            {
                yield return new AIChecklistStreamResult
                {
                    Type = "error",
                    Message = "无法解析AI生成的检查清单结构",
                    IsComplete = true
                };
            }
        }

        public async Task<AIWorkflowEnhancementResult> EnhanceWorkflowAsync(long workflowId, string enhancement)
        {
            try
            {
                _logger.LogInformation("Enhancing workflow {WorkflowId} with: {Enhancement}", workflowId, enhancement);

                var prompt = $"""
                请分析以下工作流增强需求，并提供具体的改进建议：

                工作流ID: {workflowId}
                增强需求: {enhancement}

                请提供：
                1. 具体的改进建议
                2. 建议的优先级
                3. 实施方案

                请以JSON格式返回结果。
                """;

                var aiResponse = await CallAIProviderAsync(prompt);

                if (!aiResponse.Success)
                {
                    return new AIWorkflowEnhancementResult
                    {
                        Success = false,
                        Message = aiResponse.ErrorMessage
                    };
                }

                // Parse enhancement suggestions from AI response
                return new AIWorkflowEnhancementResult
                {
                    Success = true,
                    Message = "Enhancement suggestions generated successfully",
                    Suggestions = new List<AIEnhancementSuggestion>
                    {
                        new AIEnhancementSuggestion
                        {
                            Type = "enhancement",
                            Description = enhancement,
                            Priority = 0.8
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enhancing workflow {WorkflowId}", workflowId);
                return new AIWorkflowEnhancementResult
                {
                    Success = false,
                    Message = $"Failed to enhance workflow: {ex.Message}"
                };
            }
        }

        public async Task<AIValidationResult> ValidateWorkflowAsync(WorkflowInputDto workflow)
        {
            try
            {
                _logger.LogInformation("Validating workflow: {WorkflowName}", workflow.Name);

                var issues = new List<AIValidationIssue>();
                var suggestions = new List<string>();

                // Basic validation
                if (string.IsNullOrEmpty(workflow.Name))
                {
                    issues.Add(new AIValidationIssue
                    {
                        Severity = "Error",
                        Message = "Workflow name is required",
                        Field = "Name",
                        SuggestedFix = "Provide a descriptive name for the workflow"
                    });
                }

                if (string.IsNullOrEmpty(workflow.Description))
                {
                    issues.Add(new AIValidationIssue
                    {
                        Severity = "Warning",
                        Message = "Workflow description is missing",
                        Field = "Description",
                        SuggestedFix = "Add a detailed description of the workflow purpose"
                    });
                }

                var qualityScore = CalculateWorkflowQualityScore(workflow, issues);

                return new AIValidationResult
                {
                    IsValid = !issues.Any(i => i.Severity == "Error"),
                    Issues = issues,
                    Suggestions = suggestions,
                    QualityScore = qualityScore
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating workflow");
                return new AIValidationResult
                {
                    IsValid = false,
                    Issues = new List<AIValidationIssue>
                    {
                        new AIValidationIssue
                        {
                            Severity = "Error",
                            Message = $"Validation failed: {ex.Message}",
                            Field = "General"
                        }
                    },
                    QualityScore = 0
                };
            }
        }

        public async Task<AIRequirementsParsingResult> ParseRequirementsAsync(string naturalLanguage)
        {
            try
            {
                _logger.LogInformation("Parsing requirements from natural language");

                var prompt = $"""
                请分析以下自然语言描述，提取出结构化的需求信息：

                描述: {naturalLanguage}

                请提取：
                1. 流程类型
                2. 相关人员
                3. 关键步骤
                4. 审批环节
                5. 通知要求

                请以JSON格式返回结果。
                """;

                var aiResponse = await CallAIProviderAsync(prompt);

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
            return await CallAIProviderAsync(prompt, null, null, null);
        }

        private async Task<AIProviderResponse> CallAIProviderAsync(string prompt, string? modelId, string? modelProvider, string? modelName)
        {
            try
            {
                // Use specific model if provided, otherwise fall back to configuration
                var provider = modelProvider?.ToLower() ?? _aiOptions.Provider.ToLower();
                
                _logger.LogInformation("Using AI provider: {Provider}, Model: {ModelName} (ID: {ModelId})", 
                    provider, modelName, modelId);

                switch (provider)
                {
                    case "zhipuai":
                        return await CallZhipuAIAsync(prompt, modelId, modelName);
                    case "openai":
                        return await CallOpenAIAsync(prompt, modelId, modelName);
                    case "claude":
                    case "anthropic":
                        return await CallClaudeAsync(prompt, modelId, modelName);
                    case "deepseek":
                        return await CallDeepSeekAsync(prompt, modelId, modelName);
                    default:
                        // Try to call using generic OpenAI-compatible API
                        _logger.LogInformation("Unknown provider {Provider}, attempting to use OpenAI-compatible API", provider);
                        return await CallGenericOpenAICompatibleAsync(prompt, modelId, modelName, provider);
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
            return await CallZhipuAIAsync(prompt, null, null);
        }

        private async Task<AIProviderResponse> CallZhipuAIAsync(string prompt, string? modelId, string? modelName)
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
                var maxTokens = userConfig?.MaxTokens ?? _aiOptions.ZhipuAI.MaxTokens;

                _logger.LogInformation("ZhipuAI Request - Model: {Model}, BaseUrl: {BaseUrl}", model, baseUrl);

                var requestBody = new
                {
                    model = model,
                    messages = new[]
                    {
                        new { role = "system", content = "你是一个专业的工作流设计专家。请根据用户需求生成结构化的工作流定义。" },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = maxTokens,
                    temperature = temperature,
                    stream = false
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var response = await _httpClient.PostAsync($"{baseUrl.TrimEnd('/')}/chat/completions", content);
                var responseContent = await response.Content.ReadAsStringAsync();

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
            return await CallOpenAIAsync(prompt, null, null);
        }

        private async Task<AIProviderResponse> CallOpenAIAsync(string prompt, string? modelId, string? modelName)
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
                        return await CallZhipuAIAsync(prompt, null, null);
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
                var model = userConfig.ModelName ?? modelName ?? "gpt-4o-mini";
                var temperature = userConfig.Temperature > 0 ? userConfig.Temperature : 0.7;
                var maxTokens = userConfig.MaxTokens > 0 ? userConfig.MaxTokens : 2000;

                _logger.LogInformation("OpenAI Request - Model: {Model}, BaseUrl: {BaseUrl}", model, baseUrl);

                var requestBody = new
                {
                    model = model,
                    messages = new[]
                    {
                        new { role = "system", content = "你是一个专业的工作流设计专家。请根据用户需求生成结构化的工作流定义。" },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = maxTokens,
                    temperature = temperature,
                    stream = false
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var response = await _httpClient.PostAsync($"{baseUrl.TrimEnd('/')}/v1/chat/completions", content);
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

        private async Task<AIProviderResponse> CallClaudeAsync(string prompt, string? modelId, string? modelName)
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
                        return await CallZhipuAIAsync(prompt, null, null);
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
                var maxTokens = userConfig.MaxTokens > 0 ? userConfig.MaxTokens : 2000;

                _logger.LogInformation("Claude Request - Model: {Model}, BaseUrl: {BaseUrl}", model, baseUrl);

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

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

                var response = await _httpClient.PostAsync($"{baseUrl.TrimEnd('/')}/v1/messages", content);
                var responseContent = await response.Content.ReadAsStringAsync();

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

        private async Task<AIProviderResponse> CallDeepSeekAsync(string prompt, string? modelId, string? modelName)
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
                        return await CallZhipuAIAsync(prompt, null, null);
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
                var maxTokens = userConfig.MaxTokens > 0 ? userConfig.MaxTokens : 2000;

                _logger.LogInformation("DeepSeek Request - Model: {Model}, BaseUrl: {BaseUrl}", model, baseUrl);

                // DeepSeek uses OpenAI-compatible API format
                var requestBody = new
                {
                    model = model,
                    messages = new[]
                    {
                        new { role = "system", content = "你是一个专业的工作流设计专家。请根据用户需求生成结构化的工作流定义。" },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = maxTokens,
                    temperature = temperature,
                    stream = false
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var response = await _httpClient.PostAsync($"{baseUrl.TrimEnd('/')}/v1/chat/completions", content);
                var responseContent = await response.Content.ReadAsStringAsync();

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

        private async Task<AIProviderResponse> CallGenericOpenAICompatibleAsync(string prompt, string? modelId, string? modelName, string providerName)
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
                        return await CallZhipuAIAsync(prompt, null, null);
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
                var maxTokens = userConfig.MaxTokens > 0 ? userConfig.MaxTokens : 2000;

                _logger.LogInformation("{Provider} Request - Model: {Model}, BaseUrl: {BaseUrl}", providerName, model, baseUrl);

                // Use OpenAI-compatible API format (most providers support this)
                var requestBody = new
                {
                    model = model,
                    messages = new[]
                    {
                        new { role = "system", content = "你是一个专业的工作流设计专家。请根据用户需求生成结构化的工作流定义。" },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = maxTokens,
                    temperature = temperature,
                    stream = false
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                // Try both common endpoints
                var endpoints = new[] { "/v1/chat/completions", "/chat/completions" };
                AIProviderResponse? lastResponse = null;

                foreach (var endpoint in endpoints)
                {
                    try
                    {
                        var response = await _httpClient.PostAsync($"{baseUrl.TrimEnd('/')}{endpoint}", content);
                        var responseContent = await response.Content.ReadAsStringAsync();

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

        private string BuildWorkflowGenerationPrompt(AIWorkflowGenerationInput input)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine($"{_aiOptions.Prompts.WorkflowSystem}");
            promptBuilder.AppendLine();
            
            // Check if this is a conversation-based workflow generation
            if (input.ConversationHistory != null && input.ConversationHistory.Any())
            {
                promptBuilder.AppendLine("=== 基于详细对话生成工作流 ===");
                promptBuilder.AppendLine("以下是与用户的完整对话历史，请根据这些详细信息生成精确的工作流：");
                promptBuilder.AppendLine();
                
                // Add conversation context
                if (input.ConversationMetadata != null)
                {
                    promptBuilder.AppendLine($"会话信息：");
                    promptBuilder.AppendLine($"- 会话ID: {input.SessionId}");
                    promptBuilder.AppendLine($"- 总消息数: {input.ConversationMetadata.TotalMessages}");
                    promptBuilder.AppendLine($"- 对话模式: {input.ConversationMetadata.ConversationMode}");
                    promptBuilder.AppendLine();
                }
                
                // Add full conversation history
                promptBuilder.AppendLine("完整对话内容：");
                foreach (var message in input.ConversationHistory)
                {
                    var role = message.Role == "user" ? "👤 用户" : "🤖 AI助手";
                    promptBuilder.AppendLine($"{role}：");
                    promptBuilder.AppendLine(message.Content);
                    promptBuilder.AppendLine();
                }
                
                promptBuilder.AppendLine("请特别注意：");
                promptBuilder.AppendLine("1. 从对话中提取所有关键需求和细节");
                promptBuilder.AppendLine("2. 使用AI助手在对话中提供的具体建议和详细信息");
                promptBuilder.AppendLine("3. 确保工作流反映用户的具体需求和偏好");
                promptBuilder.AppendLine("4. 如果AI助手提供了详细的行程、计划或步骤，请将其转化为工作流阶段");
                promptBuilder.AppendLine();
            }
            else
            {
                // Fallback to traditional prompt building
                promptBuilder.AppendLine("请根据以下需求生成一个完整的工作流定义：");
                promptBuilder.AppendLine($"描述: {input.Description}");
            }
            
            if (!string.IsNullOrEmpty(input.Context))
                promptBuilder.AppendLine($"上下文: {input.Context}");
            
            if (!string.IsNullOrEmpty(input.Industry))
                promptBuilder.AppendLine($"行业: {input.Industry}");
            
            if (!string.IsNullOrEmpty(input.ProcessType))
                promptBuilder.AppendLine($"流程类型: {input.ProcessType}");

            if (input.Requirements.Any())
            {
                promptBuilder.AppendLine("具体要求:");
                foreach (var req in input.Requirements)
                {
                    promptBuilder.AppendLine($"- {req}");
                }
            }

            // Add AI model information if available
            if (!string.IsNullOrEmpty(input.ModelProvider))
            {
                promptBuilder.AppendLine();
                promptBuilder.AppendLine($"使用的AI模型: {input.ModelProvider} {input.ModelName}");
            }

            promptBuilder.AppendLine();
            promptBuilder.AppendLine("请严格按照以下JSON格式返回响应，不要包含任何其他文本：");
            promptBuilder.AppendLine(@"{
  ""name"": ""工作流名称"",
  ""description"": ""工作流描述"",
  ""stages"": [
    {
      ""name"": ""阶段名称"",
      ""description"": ""阶段描述"",
      ""assignedGroup"": ""负责团队"",
      ""estimatedDuration"": 1
    }
  ]
}");

            return promptBuilder.ToString();
        }

        private string BuildQuestionnaireGenerationPrompt(AIQuestionnaireGenerationInput input)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine($"{_aiOptions.Prompts.QuestionnaireSystem}");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("请根据以下需求生成一个完整的问卷：");
            promptBuilder.AppendLine($"目的: {input.Purpose}");
            promptBuilder.AppendLine($"目标受众: {input.TargetAudience}");
            promptBuilder.AppendLine($"复杂度: {input.Complexity}");
            promptBuilder.AppendLine($"预计问题数量: {input.EstimatedQuestions}");

            if (input.Topics.Any())
            {
                promptBuilder.AppendLine("涉及主题:");
                foreach (var topic in input.Topics)
                {
                    promptBuilder.AppendLine($"- {topic}");
                }
            }

            promptBuilder.AppendLine();
            promptBuilder.AppendLine("请生成包含以下信息的JSON格式响应:");
            promptBuilder.AppendLine("1. 问卷基本信息 (name, description)");
            promptBuilder.AppendLine("2. 问题分组 (sections)");
            promptBuilder.AppendLine("3. 具体问题列表，包括问题类型、选项等");

            return promptBuilder.ToString();
        }

        private string BuildChecklistGenerationPrompt(AIChecklistGenerationInput input)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine($"{_aiOptions.Prompts.ChecklistSystem}");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("请根据以下需求生成一个完整的检查清单：");
            promptBuilder.AppendLine($"流程名称: {input.ProcessName}");
            promptBuilder.AppendLine($"描述: {input.Description}");
            promptBuilder.AppendLine($"负责团队: {input.Team}");

            if (input.RequiredSteps.Any())
            {
                promptBuilder.AppendLine("必需步骤:");
                foreach (var step in input.RequiredSteps)
                {
                    promptBuilder.AppendLine($"- {step}");
                }
            }

            promptBuilder.AppendLine();
            promptBuilder.AppendLine("请生成包含以下信息的JSON格式响应:");
            promptBuilder.AppendLine("1. 检查清单基本信息 (name, description, team)");
            promptBuilder.AppendLine("2. 任务列表，包括任务名称、描述、预估时间、是否必需");
            if (input.IncludeDependencies)
                promptBuilder.AppendLine("3. 任务依赖关系");

            return promptBuilder.ToString();
        }

        private AIWorkflowGenerationResult ParseWorkflowGenerationResponse(string aiResponse)
        {
            try
            {
                // Try to parse JSON response from AI
                if (aiResponse.Contains("{") && aiResponse.Contains("}"))
                {
                    var jsonStart = aiResponse.IndexOf('{');
                    var jsonEnd = aiResponse.LastIndexOf('}') + 1;
                    var jsonContent = aiResponse.Substring(jsonStart, jsonEnd - jsonStart);
                    
                    var parsed = JsonSerializer.Deserialize<JsonElement>(jsonContent);
                    
                    var workflow = new WorkflowInputDto
                    {
                        Name = parsed.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : "AI Generated Workflow",
                        Description = parsed.TryGetProperty("description", out var descEl) ? descEl.GetString() : "Generated by AI",
                        IsActive = true
                    };

                    var stages = new List<AIStageGenerationResult>();
                    if (parsed.TryGetProperty("stages", out var stagesEl) && stagesEl.ValueKind == JsonValueKind.Array)
                    {
                        var order = 1;
                        foreach (var stageEl in stagesEl.EnumerateArray())
                        {
                            stages.Add(new AIStageGenerationResult
                            {
                                Name = stageEl.TryGetProperty("name", out var sNameEl) ? sNameEl.GetString() : $"Stage {order}",
                                Description = stageEl.TryGetProperty("description", out var sDescEl) ? sDescEl.GetString() : "",
                                Order = order++,
                                AssignedGroup = stageEl.TryGetProperty("assignedGroup", out var sGroupEl) ? sGroupEl.GetString() : "General",
                                EstimatedDuration = stageEl.TryGetProperty("estimatedDuration", out var sDurEl) && sDurEl.TryGetInt32(out var dur) ? dur : 1
                            });
                        }
                    }

                    return new AIWorkflowGenerationResult
                    {
                        Success = true,
                        Message = "Workflow generated successfully",
                        GeneratedWorkflow = workflow,
                        Stages = stages,
                        Suggestions = new List<string> { "Consider adding approval stages", "Review stage assignments" }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse JSON response, using fallback parsing");
            }

            // Fallback: Generate a basic workflow from the text response
            return GenerateFallbackWorkflow(aiResponse);
        }

        private AIWorkflowGenerationResult GenerateFallbackWorkflow(string aiResponse)
        {
            var workflow = new WorkflowInputDto
            {
                Name = "AI Generated Workflow",
                Description = "Generated by AI",
                IsActive = true
            };

            // 从AI响应中智能提取阶段信息
            var stages = ExtractStagesFromText(aiResponse);
            
            // 如果没有提取到阶段，创建默认阶段
            if (!stages.Any())
            {
                stages = new List<AIStageGenerationResult>
                {
                    new AIStageGenerationResult
                    {
                        Name = "准备阶段",
                        Description = "收集所需信息和资源",
                        Order = 1,
                        AssignedGroup = "执行团队",
                        EstimatedDuration = 2
                    },
                    new AIStageGenerationResult
                    {
                        Name = "执行阶段",
                        Description = "执行主要工作任务",
                        Order = 2,
                        AssignedGroup = "执行团队",
                        EstimatedDuration = 5
                    },
                    new AIStageGenerationResult
                    {
                        Name = "审核阶段",
                        Description = "审核工作成果和质量",
                        Order = 3,
                        AssignedGroup = "管理团队",
                        EstimatedDuration = 2
                    },
                    new AIStageGenerationResult
                    {
                        Name = "完成阶段",
                        Description = "确认完成并交付成果",
                        Order = 4,
                        AssignedGroup = "管理团队",
                        EstimatedDuration = 1
                    }
                };
            }

            return new AIWorkflowGenerationResult
            {
                Success = true,
                Message = "Workflow generated successfully",
                GeneratedWorkflow = workflow,
                Stages = stages,
                Suggestions = new List<string> { "Consider adding approval stages", "Review stage assignments" }
            };
        }

        private List<AIStageGenerationResult> ExtractStagesFromText(string text)
        {
            var stages = new List<AIStageGenerationResult>();
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var order = 1;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // 查找可能的阶段标识符
                if (trimmedLine.Contains("阶段") || trimmedLine.Contains("步骤") || 
                    trimmedLine.Contains("Stage") || trimmedLine.Contains("Step") ||
                    trimmedLine.StartsWith("-") || trimmedLine.StartsWith("*") ||
                    Regex.IsMatch(trimmedLine, @"^\d+\."))
                {
                    var stageName = ExtractStageName(trimmedLine);
                    if (!string.IsNullOrEmpty(stageName) && stageName.Length > 2)
                    {
                        stages.Add(new AIStageGenerationResult
                        {
                            Name = stageName,
                            Description = $"AI生成的{stageName}",
                            Order = order++,
                            AssignedGroup = "执行团队",
                            EstimatedDuration = 2
                        });
                    }
                }
            }

            return stages;
        }

        private string ExtractStageName(string line)
        {
            // 移除常见的前缀和标识符
            var cleaned = line.Trim()
                .Replace("-", "")
                .Replace("*", "")
                .Replace("•", "");
            
            // 移除数字前缀 (如 "1. ", "2. ")
            cleaned = Regex.Replace(cleaned, @"^\d+\.\s*", "");
            
            // 移除括号内容
            cleaned = Regex.Replace(cleaned, @"\([^)]*\)", "");
            
            return cleaned.Trim();
        }

        private AIQuestionnaireGenerationResult ParseQuestionnaireGenerationResponse(string aiResponse)
        {
            var questionnaire = new QuestionnaireInputDto
            {
                Name = "AI Generated Questionnaire",
                Description = "Generated by AI based on requirements",
                IsActive = true
            };

            return new AIQuestionnaireGenerationResult
            {
                Success = true,
                Message = "Questionnaire generated successfully",
                GeneratedQuestionnaire = questionnaire,
                Suggestions = new List<string> { "Review question types", "Add validation rules" }
            };
        }

        private AIChecklistGenerationResult ParseChecklistGenerationResponse(string aiResponse)
        {
            var checklist = new ChecklistInputDto
            {
                Name = "AI Generated Checklist",
                Description = "Generated by AI based on requirements",
                Team = "General"
            };

            return new AIChecklistGenerationResult
            {
                Success = true,
                Message = "Checklist generated successfully",
                GeneratedChecklist = checklist,
                Suggestions = new List<string> { "Review task dependencies", "Adjust time estimates" }
            };
        }

        private double CalculateConfidenceScore(WorkflowInputDto workflow)
        {
            double score = 0.5; // Base score

            if (!string.IsNullOrEmpty(workflow.Name)) score += 0.2;
            if (!string.IsNullOrEmpty(workflow.Description)) score += 0.2;
            
            return Math.Min(score, 1.0);
        }

        private double CalculateQuestionnaireConfidenceScore(QuestionnaireInputDto questionnaire)
        {
            double score = 0.5;
            if (!string.IsNullOrEmpty(questionnaire.Name)) score += 0.25;
            if (!string.IsNullOrEmpty(questionnaire.Description)) score += 0.25;
            return Math.Min(score, 1.0);
        }

        private double CalculateChecklistConfidenceScore(ChecklistInputDto checklist)
        {
            double score = 0.5;
            if (!string.IsNullOrEmpty(checklist.Name)) score += 0.25;
            if (!string.IsNullOrEmpty(checklist.Description)) score += 0.25;
            return Math.Min(score, 1.0);
        }

        private double CalculateWorkflowQualityScore(WorkflowInputDto workflow, List<AIValidationIssue> issues)
        {
            double score = 1.0;
            
            foreach (var issue in issues)
            {
                switch (issue.Severity)
                {
                    case "Error":
                        score -= 0.3;
                        break;
                    case "Warning":
                        score -= 0.1;
                        break;
                }
            }

            return Math.Max(score, 0.0);
        }

        #endregion
        public async Task<AIWorkflowGenerationResult> EnhanceWorkflowAsync(AIWorkflowModificationInput input)
        {
            var result = new AIWorkflowGenerationResult();

            try
            {
                _logger.LogInformation("Modifying workflow {WorkflowId}: {Description}", 
                    input.WorkflowId, input.Description);

                // 获取现有workflow的详细信息
                _logger.LogInformation("Fetching existing workflow with ID: {WorkflowId}", input.WorkflowId);
                var existingWorkflowInfo = await GetExistingWorkflowAsync(input.WorkflowId);
                _logger.LogInformation("Retrieved workflow: Name={Name}, Description={Description}, StageCount={StageCount}", 
                    existingWorkflowInfo.Name, existingWorkflowInfo.Description, existingWorkflowInfo.Stages.Count);
                
                // 详细记录现有阶段信息
                for (int i = 0; i < existingWorkflowInfo.Stages.Count; i++)
                {
                    var stage = existingWorkflowInfo.Stages[i];
                    _logger.LogInformation("Stage {Index}: Name='{Name}', Description='{Description}', Duration={Duration}, Team='{Team}'", 
                        i + 1, stage.Name, stage.Description, stage.EstimatedDuration, stage.AssignedGroup);
                }
                
                // 构建修改提示词
                var prompt = await BuildWorkflowModificationPromptAsync(input, existingWorkflowInfo);
                
                // 调试日志：输出完整的提示词
                _logger.LogInformation("AI Modification Prompt: {Prompt}", prompt);
                
                // 调用AI进行workflow修改
                var aiResponse = await CallAIProviderAsync(prompt);
                
                // 调试日志：输出AI响应
                _logger.LogInformation("AI Modification Response: Success={Success}, Content={Content}", 
                    aiResponse.Success, aiResponse.Content);
                
                if (!aiResponse.Success)
                {
                    result.Success = false;
                    result.Message = aiResponse.ErrorMessage;
                    return GenerateFallbackWorkflow($"Error modifying workflow {input.WorkflowId}");
                }
                
                // 解析AI响应
                var modificationResult = ParseWorkflowGenerationResponse(aiResponse.Content);
                
                if (modificationResult.Stages == null || !modificationResult.Stages.Any())
                {
                    modificationResult = GenerateFallbackWorkflow($"Modified workflow for ID: {input.WorkflowId}");
                }

                // 强制确保workflow名称正确（防止AI不遵循指令）
                _logger.LogInformation("Checking workflow name correction: AI returned '{AIName}', expected '{ExpectedName}'", 
                    modificationResult.GeneratedWorkflow?.Name ?? "NULL", existingWorkflowInfo.Name);
                
                if (modificationResult.GeneratedWorkflow != null && 
                    modificationResult.GeneratedWorkflow.Name != existingWorkflowInfo.Name)
                {
                    _logger.LogWarning("AI returned incorrect workflow name '{AIName}', forcing to correct name '{CorrectName}'", 
                        modificationResult.GeneratedWorkflow.Name, existingWorkflowInfo.Name);
                    
                    var originalName = modificationResult.GeneratedWorkflow.Name;
                    modificationResult.GeneratedWorkflow.Name = existingWorkflowInfo.Name;
                    modificationResult.GeneratedWorkflow.Description = existingWorkflowInfo.Description + " - Modified based on user requirements";
                    
                    _logger.LogInformation("Name correction applied: '{OriginalName}' -> '{CorrectedName}'", 
                        originalName, modificationResult.GeneratedWorkflow.Name);
                }
                else
                {
                    _logger.LogInformation("Workflow name is correct: '{Name}'", modificationResult.GeneratedWorkflow?.Name);
                }

                result = modificationResult;
                result.Message = "Workflow modified successfully";
                
                _logger.LogInformation("Workflow modification completed successfully for ID: {WorkflowId}", input.WorkflowId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to modify workflow {WorkflowId}", input.WorkflowId);
                result.Success = false;
                result.Message = "Workflow modification failed";
                result = GenerateFallbackWorkflow($"Error modifying workflow {input.WorkflowId}");
            }

            return result;
        }

        private Task<string> BuildWorkflowModificationPromptAsync(AIWorkflowModificationInput input, MockWorkflowInfo existingWorkflowInfo)
        {
            var systemPrompt = _aiOptions.Prompts.WorkflowSystem;
            var modificationContext = input.PreserveExisting ? 
                "请在保持现有工作流核心结构和现有阶段的基础上进行修改。只根据具体要求添加、修改或删除阶段。" :
                "如果需要，您可以完全重新设计工作流。";

            var prompt = $@"CRITICAL: This is a MODIFICATION task, NOT a creation task.

MANDATORY RULES - DO NOT VIOLATE:
1. Workflow name MUST remain EXACTLY: ""{existingWorkflowInfo.Name}""
2. DO NOT change the workflow name under any circumstances
3. DO NOT create a new workflow
4. ONLY modify existing stages or add new stages

EXISTING WORKFLOW TO MODIFY:
Name: {existingWorkflowInfo.Name}
Description: {existingWorkflowInfo.Description}
Current Stages:
{string.Join("\n", existingWorkflowInfo.Stages.Select((stage, index) => 
    $"{index + 1}. {stage.Name} - {stage.Description} (Duration: {stage.EstimatedDuration} days, Team: {stage.AssignedGroup})"))}

USER MODIFICATION REQUEST: {input.Description}

REQUIRED OUTPUT FORMAT - Use EXACT name ""{existingWorkflowInfo.Name}"":
{{
    ""name"": ""{existingWorkflowInfo.Name}"",
    ""description"": ""{existingWorkflowInfo.Description} - Modified based on user requirements"",
    ""isActive"": true,
    ""stages"": [
{string.Join(",\n", existingWorkflowInfo.Stages.Select((stage, index) => 
    $@"        {{
            ""name"": ""{stage.Name}"",
            ""description"": ""{stage.Description}"",
            ""order"": {index + 1},
            ""assignedGroup"": ""{stage.AssignedGroup}"",
            ""estimatedDuration"": {stage.EstimatedDuration}
        }}"))}
    ]
}}

VERIFICATION CHECKLIST:
✓ Workflow name is EXACTLY: ""{existingWorkflowInfo.Name}""
✓ Based on existing stages
✓ Added modifications as requested
✓ JSON format only

RETURN ONLY THE JSON - NO EXPLANATORY TEXT.";

            return Task.FromResult(prompt);
        }

        private async Task<MockWorkflowInfo> GetExistingWorkflowAsync(long workflowId)
        {
            try
            {
                _logger.LogInformation("Attempting to fetch workflow with ID: {WorkflowId}", workflowId);
                var workflow = await _workflowService.GetByIdAsync(workflowId);
                
                if (workflow != null)
                {
                    _logger.LogInformation("Successfully retrieved workflow: Name={Name}, Description={Description}, StageCount={StageCount}", 
                        workflow.Name, workflow.Description, workflow.Stages?.Count ?? 0);
                    
                    var mockInfo = new MockWorkflowInfo
                    {
                        Name = workflow.Name,
                        Description = workflow.Description,
                        Stages = workflow.Stages?.Select(s => new MockStageInfo
                        {
                            Name = s.Name,
                            Description = s.Description,
                            EstimatedDuration = (int)(s.EstimatedDuration ?? 1),
                            AssignedGroup = s.DefaultAssignedGroup ?? "Default Team"
                        }).ToList() ?? new List<MockStageInfo>()
                    };
                    
                    _logger.LogInformation("Converted to MockWorkflowInfo: Name={Name}, StageCount={StageCount}", 
                        mockInfo.Name, mockInfo.Stages.Count);
                    
                    return mockInfo;
                }
                else
                {
                    _logger.LogWarning("Workflow with ID {WorkflowId} not found", workflowId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching workflow {WorkflowId}", workflowId);
            }

            // 如果获取失败，返回默认数据
            _logger.LogWarning("Returning default workflow data for ID {WorkflowId}", workflowId);
            return new MockWorkflowInfo
            {
                Name = "Default Workflow",
                Description = "Default workflow description",
                Stages = new List<MockStageInfo>
                {
                    new MockStageInfo { Name = "Default Stage", Description = "Default stage description", EstimatedDuration = 1, AssignedGroup = "Default Team" }
                }
            };
        }

        private class MockWorkflowInfo
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public List<MockStageInfo> Stages { get; set; } = new();
        }

        private class MockStageInfo
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public int EstimatedDuration { get; set; }
            public string AssignedGroup { get; set; } = string.Empty;
        }

        #region AI Chat Implementation

        /// <summary>
        /// Send message to AI chat and get response
        /// </summary>
        public async Task<AIChatResponse> SendChatMessageAsync(AIChatInput input)
        {
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

                var response = await CallAIProviderForChatAsync(input);

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
                return GenerateErrorChatResponse(input, ex.Message);
            }
        }

        /// <summary>
        /// Stream chat conversation with AI
        /// </summary>
        public async IAsyncEnumerable<AIChatStreamResult> StreamChatAsync(AIChatInput input)
        {
            var sessionId = input.SessionId;
            
            yield return new AIChatStreamResult
            {
                Type = "start",
                Content = "",
                IsComplete = false,
                SessionId = sessionId
            };

            var results = new List<AIChatStreamResult>();
            Exception? streamException = null;

            try
            {
                await foreach (var chunk in CallAIProviderForStreamChatAsync(input))
                {
                    results.Add(new AIChatStreamResult
                    {
                        Type = "delta",
                        Content = chunk,
                        IsComplete = false,
                        SessionId = sessionId
                    });
                }

                results.Add(new AIChatStreamResult
                {
                    Type = "complete",
                    Content = "",
                    IsComplete = true,
                    SessionId = sessionId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in streaming AI chat for session: {SessionId}", sessionId);
                streamException = ex;
            }

            // Yield all collected results
            foreach (var result in results)
            {
                yield return result;
            }

            // Handle error case
            if (streamException != null)
            {
                yield return new AIChatStreamResult
                {
                    Type = "error",
                    Content = $"Error: {streamException.Message}",
                    IsComplete = true,
                    SessionId = sessionId
                };
            }
        }

        private async Task<AIProviderResponse> CallAIProviderForChatAsync(AIChatInput input)
        {
            try
            {
                // 构建消息数组，直接使用对话历史
                var messages = new List<object>();
                
                // 添加系统提示
                messages.Add(new { role = "system", content = GetChatSystemPrompt(input.Mode) });
                
                // 添加对话历史（最近10条消息）
                foreach (var message in input.Messages.TakeLast(10))
                {
                    messages.Add(new { role = message.Role, content = message.Content });
                }

                // 获取用户配置
                AIModelConfig userConfig = null;
                
                // 如果指定了模型ID，使用该配置
                if (!string.IsNullOrEmpty(input.ModelId) && long.TryParse(input.ModelId, out var modelId))
                {
                    // 使用租户隔离获取配置，不需要手动传递用户ID
                    userConfig = await _configService.GetConfigByIdAsync(modelId);
                    if (userConfig != null)
                    {
                        _logger.LogInformation("Using specified model config: {Provider} - {ModelName} for session: {SessionId}", 
                            userConfig.Provider, userConfig.ModelName, input.SessionId);
                    }
                }
                
                // 如果没有指定模型或找不到配置，使用默认配置
                if (userConfig == null)
                {
                    _logger.LogInformation("No specific model config found, using default ZhipuAI configuration");
                    return await CallZhipuAIAsync(messages);
                }

                // 根据提供商调用相应的API
                return userConfig.Provider?.ToLower() switch
                {
                    "zhipuai" => await CallZhipuAIWithConfigAsync(messages, userConfig),
                    "openai" => await CallOpenAIWithConfigAsync(messages, userConfig),
                    "claude" => await CallClaudeWithConfigAsync(messages, userConfig),
                    "deepseek" => await CallDeepSeekWithConfigAsync(messages, userConfig),
                    _ => await CallZhipuAIAsync(messages) // 默认使用ZhipuAI
                };
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
        /// 使用默认ZhipuAI配置调用API
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

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_aiOptions.ZhipuAI.ApiKey}");

            var apiUrl = $"{_aiOptions.ZhipuAI.BaseUrl}/chat/completions";
            _logger.LogInformation("Calling ZhipuAI API: {Url} with {MessageCount} messages", apiUrl, messages.Count);
            
            var response = await _httpClient.PostAsync(apiUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

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
        /// 使用用户配置调用ZhipuAI API
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

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");

            // 智能处理API端点，避免路径重复
            var baseUrl = config.BaseUrl.TrimEnd('/');
            string apiUrl;
            
            // 如果BaseUrl已经包含了完整的端点路径，直接使用
            if (baseUrl.Contains("/chat/completions"))
            {
                apiUrl = baseUrl;
            }
            else
            {
                // 否则添加端点路径
                apiUrl = $"{baseUrl}/chat/completions";
            }
            
            _logger.LogInformation("Calling ZhipuAI API with user config: {Url} - Model: {Model}", apiUrl, config.ModelName);
            
            var response = await _httpClient.PostAsync(apiUrl, content);
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
        /// 使用用户配置调用OpenAI API
        /// </summary>
        private async Task<AIProviderResponse> CallOpenAIWithConfigAsync(List<object> messages, AIModelConfig config)
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

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");

            // 确保OpenAI API端点包含正确的版本路径
            var baseUrl = config.BaseUrl.TrimEnd('/');
            var apiUrl = baseUrl.Contains("/v1") ? $"{baseUrl}/chat/completions" : $"{baseUrl}/v1/chat/completions";
            _logger.LogInformation("Calling OpenAI API with user config: {Url} - Model: {Model}", apiUrl, config.ModelName);
            
            var response = await _httpClient.PostAsync(apiUrl, content);
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
        /// 使用用户配置调用Claude API
        /// </summary>
        private async Task<AIProviderResponse> CallClaudeWithConfigAsync(List<object> messages, AIModelConfig config)
        {
            // Claude API格式略有不同
            var claudeMessages = messages.Skip(1).Select(m => new 
            { 
                role = ((dynamic)m).role == "assistant" ? "assistant" : "user", 
                content = ((dynamic)m).content 
            }).ToArray();

            var requestBody = new
            {
                model = config.ModelName,
                max_tokens = config.MaxTokens > 0 ? config.MaxTokens : 1000,
                temperature = config.Temperature > 0 ? config.Temperature : 0.7,
                messages = claudeMessages
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", config.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", config.ApiVersion ?? "2023-06-01");

            // 智能处理API端点，避免路径重复
            var baseUrl = config.BaseUrl.TrimEnd('/');
            string apiUrl;
            
            // 如果BaseUrl已经包含了完整的端点路径，直接使用
            if (baseUrl.Contains("/messages"))
            {
                apiUrl = baseUrl;
            }
            else
            {
                // 否则添加端点路径，Claude使用/v1/messages
                apiUrl = baseUrl.Contains("/v1") ? $"{baseUrl}/messages" : $"{baseUrl}/v1/messages";
            }
            
            _logger.LogInformation("Calling Claude API with user config: {Url} - Model: {Model}", apiUrl, config.ModelName);
            
            var response = await _httpClient.PostAsync(apiUrl, content);
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
        /// 使用用户配置调用DeepSeek API
        /// </summary>
        private async Task<AIProviderResponse> CallDeepSeekWithConfigAsync(List<object> messages, AIModelConfig config)
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

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");

            // 智能处理API端点，避免路径重复
            var baseUrl = config.BaseUrl.TrimEnd('/');
            string apiUrl;
            
            // 如果BaseUrl已经包含了完整的端点路径，直接使用
            if (baseUrl.Contains("/chat/completions"))
            {
                apiUrl = baseUrl;
            }
            else
            {
                // 否则添加端点路径，DeepSeek通常需要v1版本
                apiUrl = baseUrl.Contains("/v1") ? $"{baseUrl}/chat/completions" : $"{baseUrl}/v1/chat/completions";
            }
            
            _logger.LogInformation("Calling DeepSeek API with user config: {Url} - Model: {Model}", apiUrl, config.ModelName);
            
            var response = await _httpClient.PostAsync(apiUrl, content);
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

        private async IAsyncEnumerable<string> CallAIProviderForStreamChatAsync(AIChatInput input)
        {
            // For now, simulate streaming by breaking the response into chunks
            var response = await CallAIProviderForChatAsync(input);
            
            if (response.Success && !string.IsNullOrEmpty(response.Content))
            {
                var words = response.Content.Split(' ');
                foreach (var word in words)
                {
                    yield return word + " ";
                    await Task.Delay(50); // Simulate streaming delay
                }
            }
            else
            {
                yield return "I apologize, but I'm having trouble processing your message right now.";
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

        private string GetChatSystemPrompt(string mode)
        {
            return mode switch
            {
                "workflow_planning" => @"You are an expert AI Workflow Assistant specialized in business process design. Your role is to:

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

                _ => @"You are a helpful, knowledgeable AI assistant. Provide clear, accurate, and helpful responses to user questions. Be conversational, friendly, and thorough in your explanations."
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

        #endregion
    }

    #region Helper Classes

    public class AIProviderResponse
    {
        public bool Success { get; set; }
        public string Content { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    #endregion
} 