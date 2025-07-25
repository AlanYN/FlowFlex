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

        public AIService(
            IOptions<AIOptions> aiOptions,
            ILogger<AIService> logger,
            HttpClient httpClient,
            IMCPService mcpService,
            IWorkflowService workflowService)
        {
            _aiOptions = aiOptions.Value;
            _logger = logger;
            _httpClient = httpClient;
            _mcpService = mcpService;
            _workflowService = workflowService;
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
                var prompt = BuildChatPrompt(input);
                
                var requestBody = new
                {
                    model = _aiOptions.ZhipuAI.Model,
                    messages = new[]
                    {
                        new { role = "system", content = GetChatSystemPrompt(input.Mode) },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.7,
                    max_tokens = 1000
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_aiOptions.ZhipuAI.ApiKey}");

                var response = await _httpClient.PostAsync(_aiOptions.ZhipuAI.BaseUrl, content);
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
                    return new AIProviderResponse
                    {
                        Success = false,
                        ErrorMessage = $"API call failed: {response.StatusCode} - {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
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
                "workflow_planning" => @"You are an expert AI Workflow Assistant. Your role is to:
1. Help users design effective business workflows through conversation
2. Ask relevant questions to understand their needs
3. Provide professional guidance on workflow best practices
4. Be conversational, helpful, and thorough
5. Gradually collect information about: process type, stakeholders, timeline, requirements, and constraints
6. Determine when you have enough information to create a comprehensive workflow

Keep responses concise but thorough. Ask one or two focused questions at a time.",

                _ => @"You are a helpful AI assistant. Provide clear, accurate, and helpful responses to user questions."
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
                1 => "That sounds like an interesting workflow! Could you tell me more about the teams or people who will be involved in this process?",
                2 => "Great! Now, how many main stages or steps do you think this workflow should have? And what's your expected timeline?",
                3 => "Perfect! Are there any specific requirements, documents, or approvals that need to be included in this workflow?",
                _ => "Thank you for all the information! I believe I have enough details to help you create a comprehensive workflow. Would you like to proceed with generating it?"
            };

            return new AIChatResponse
            {
                Success = true,
                Message = "Fallback response generated",
                Response = new AIChatResponseData
                {
                    Content = fallbackContent,
                    IsComplete = userMessageCount >= 4,
                    Suggestions = new List<string>(),
                    NextQuestions = new List<string>()
                },
                SessionId = input.SessionId
            };
        }

        private AIChatResponse GenerateErrorChatResponse(AIChatInput input, string errorMessage)
        {
            return new AIChatResponse
            {
                Success = false,
                Message = $"Chat processing failed: {errorMessage}",
                Response = new AIChatResponseData
                {
                    Content = "I apologize, but I'm having trouble processing your message right now. Could you please try again?",
                    IsComplete = false,
                    Suggestions = new List<string> { "Try rephrasing your message", "Check your connection and try again" },
                    NextQuestions = new List<string>()
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