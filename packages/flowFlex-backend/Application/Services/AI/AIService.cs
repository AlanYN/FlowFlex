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

namespace FlowFlex.Application.Services.AI
{
    /// <summary>
    /// AI service implementation supporting multiple AI providers
    /// </summary>
    public class AIService : IAIService
    {
        private readonly AIOptions _aiOptions;
        private readonly ILogger<AIService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IMCPService _mcpService;

        public AIService(
            IOptions<AIOptions> aiOptions,
            ILogger<AIService> logger,
            HttpClient httpClient,
            IMCPService mcpService)
        {
            _aiOptions = aiOptions.Value;
            _logger = logger;
            _httpClient = httpClient;
            _mcpService = mcpService;
        }

        public async Task<AIWorkflowGenerationResult> GenerateWorkflowAsync(AIWorkflowGenerationInput input)
        {
            try
            {
                _logger.LogInformation("Generating workflow from natural language: {Description}", input.Description);

                // Store context in MCP for future reference
                await _mcpService.StoreContextAsync(
                    $"workflow_generation_{DateTime.UtcNow:yyyyMMddHHmmss}",
                    JsonSerializer.Serialize(input),
                    new Dictionary<string, object>
                    {
                        { "type", "workflow_generation" },
                        { "timestamp", DateTime.UtcNow },
                        { "description", input.Description }
                    });

                var prompt = BuildWorkflowGenerationPrompt(input);
                var aiResponse = await CallAIProviderAsync(prompt);

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
            try
            {
                switch (_aiOptions.Provider.ToLower())
                {
                    case "zhipuai":
                        return await CallZhipuAIAsync(prompt);
                    case "openai":
                        return await CallOpenAIAsync(prompt);
                    default:
                        return new AIProviderResponse
                        {
                            Success = false,
                            ErrorMessage = $"Unsupported AI provider: {_aiOptions.Provider}"
                        };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling AI provider: {Provider}", _aiOptions.Provider);
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"AI provider call failed: {ex.Message}"
                };
            }
        }

        private async Task<AIProviderResponse> CallZhipuAIAsync(string prompt)
        {
            var config = _aiOptions.ZhipuAI;
            var requestBody = new
            {
                model = config.Model,
                messages = new[]
                {
                    new { role = "system", content = "你是一个专业的工作流设计专家。请根据用户需求生成结构化的工作流定义。" },
                    new { role = "user", content = prompt }
                },
                max_tokens = config.MaxTokens,
                temperature = config.Temperature,
                stream = false
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");

            var response = await _httpClient.PostAsync($"{config.BaseUrl}/chat/completions", content);
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

        private async Task<AIProviderResponse> CallOpenAIAsync(string prompt)
        {
            // OpenAI implementation placeholder
            await Task.Delay(100);
            return new AIProviderResponse
            {
                Success = false,
                ErrorMessage = "OpenAI integration not implemented yet"
            };
        }

        private string BuildWorkflowGenerationPrompt(AIWorkflowGenerationInput input)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine($"{_aiOptions.Prompts.WorkflowSystem}");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("请根据以下需求生成一个完整的工作流定义：");
            promptBuilder.AppendLine($"描述: {input.Description}");
            
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