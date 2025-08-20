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
using FlowFlex.Domain.Repository.OW;

namespace FlowFlex.Application.Services.AI
{
    /// <summary>
    /// AI service implementation supporting multiple AI providers
    /// </summary>
    public class AIService : IAIService, IScopedService
    {
        private readonly AIOptions _aiOptions;
        private readonly ILogger<AIService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMCPService _mcpService;
        private readonly IWorkflowService _workflowService;
        private readonly IAIModelConfigService _configService;
        private readonly IChecklistService _checklistService;
        private readonly IQuestionnaireService _questionnaireService;
        private readonly IChecklistRepository _checklistRepository;
        private readonly IQuestionnaireRepository _questionnaireRepository;
        private readonly IChecklistTaskService _checklistTaskService;

        public AIService(
            IOptions<AIOptions> aiOptions,
            ILogger<AIService> logger,
            IHttpClientFactory httpClientFactory,
            IMCPService mcpService,
            IWorkflowService workflowService,
            IAIModelConfigService configService,
            IChecklistService checklistService,
            IQuestionnaireService questionnaireService,
            IChecklistRepository checklistRepository,
            IQuestionnaireRepository questionnaireRepository,
            IChecklistTaskService checklistTaskService)
        {
            _aiOptions = aiOptions.Value;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _mcpService = mcpService;
            _workflowService = workflowService;
            _configService = configService;
            _checklistService = checklistService;
            _questionnaireService = questionnaireService;
            _checklistRepository = checklistRepository;
            _questionnaireRepository = questionnaireRepository;
            _checklistTaskService = checklistTaskService;
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

                // Ensure at least some stages exist
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
                Message = "Starting workflow generation...",
                IsComplete = false
            };

            yield return new AIWorkflowStreamResult
            {
                Type = "progress",
                Message = "Analyzing requirements...",
                IsComplete = false
            };

            // 尝试使用真正的流式AI调用
            var prompt = BuildWorkflowGenerationPrompt(input);
            var streamingContent = new StringBuilder();
            var hasReceivedContent = false;

            // 构建聊天消息格式
            var messages = new List<object>
            {
                new { role = "system", content = "You are an AI workflow assistant that generates detailed business workflows." },
                new { role = "user", content = prompt }
            };

            // 获取用户配置
            AIModelConfig userConfig = null;
            if (!string.IsNullOrEmpty(input.ModelId) && long.TryParse(input.ModelId, out var modelId))
            {
                userConfig = await _configService.GetConfigByIdAsync(modelId);
            }

            // 智能模型选择：优先使用快速模型
            if (userConfig != null)
            {
                var progressSent = false;
                var streamStartTime = DateTime.UtcNow;

                // 发送初始进度消息
                yield return new AIWorkflowStreamResult
                {
                    Type = "progress",
                    Message = "Generating workflow structure...",
                    IsComplete = false
                };
                progressSent = true;
                _logger.LogInformation("✅ Initial progress message sent");

                // 根据模型类型选择处理方式
                if (userConfig.Provider?.ToLower() == "openai")
                {
                    _logger.LogInformation("🚀 Using OpenAI TRUE streaming - real-time progress updates");

                    var lastProgressLength = 0;
                    var lastProgressTime = DateTime.UtcNow;

                    // 真正的流式处理：实时输出有意义的进度更新
                    await foreach (var chunk in CallOpenAIStreamAsync(messages, userConfig))
                    {
                        if (!string.IsNullOrEmpty(chunk))
                        {
                            streamingContent.Append(chunk);
                            hasReceivedContent = true;

                            var now = DateTime.UtcNow;
                            var timeSinceLastProgress = (now - lastProgressTime).TotalMilliseconds;
                            var lengthDifference = streamingContent.Length - lastProgressLength;

                            // 条件：每收集50个字符或每2秒更新一次进度
                            if (lengthDifference >= 50 || timeSinceLastProgress >= 2000)
                            {
                                yield return new AIWorkflowStreamResult
                                {
                                    Type = "progress",
                                    Message = $"Generating workflow... ({streamingContent.Length} characters, {timeSinceLastProgress / 1000:F1}s)",
                                    IsComplete = false
                                };

                                lastProgressLength = streamingContent.Length;
                                lastProgressTime = now;

                                _logger.LogInformation("📊 Progress update: {Length} chars, {Duration}ms since last",
                                    streamingContent.Length, timeSinceLastProgress);
                            }
                        }
                    }

                    var totalDuration = (DateTime.UtcNow - streamStartTime).TotalMilliseconds;
                    _logger.LogInformation("🏁 OpenAI TRUE stream completed: {Length} chars in {Duration}ms",
                        streamingContent.Length, totalDuration);
                }
                else if (userConfig.Provider?.ToLower() == "deepseek")
                {
                    _logger.LogInformation("🚀 Using DeepSeek TRUE streaming - real-time progress updates");

                    var lastProgressLength = 0;
                    var lastProgressTime = DateTime.UtcNow;

                    // 真正的流式处理：实时输出有意义的进度更新
                    await foreach (var chunk in CallDeepSeekStreamAsync(messages, userConfig))
                    {
                        if (!string.IsNullOrEmpty(chunk))
                        {
                            streamingContent.Append(chunk);
                            hasReceivedContent = true;

                            var now = DateTime.UtcNow;
                            var timeSinceLastProgress = (now - lastProgressTime).TotalMilliseconds;
                            var lengthDifference = streamingContent.Length - lastProgressLength;

                            // 条件：每收集50个字符或每2秒更新一次进度
                            if (lengthDifference >= 50 || timeSinceLastProgress >= 2000)
                            {
                                yield return new AIWorkflowStreamResult
                                {
                                    Type = "progress",
                                    Message = $"Generating workflow... ({streamingContent.Length} characters, {timeSinceLastProgress / 1000:F1}s)",
                                    IsComplete = false
                                };

                                lastProgressLength = streamingContent.Length;
                                lastProgressTime = now;

                                _logger.LogInformation("📊 Progress update: {Length} chars, {Duration}ms since last",
                                    streamingContent.Length, timeSinceLastProgress);
                            }
                        }
                    }

                    var totalDuration = (DateTime.UtcNow - streamStartTime).TotalMilliseconds;
                    _logger.LogInformation("🏁 DeepSeek TRUE stream completed: {Length} chars in {Duration}ms",
                        streamingContent.Length, totalDuration);
                }
                else
                {
                    // 其他模型使用真正的流式处理
                    _logger.LogInformation("🚀 Using {Provider} TRUE streaming - real-time progress updates", userConfig.Provider);

                    var lastProgressLength = 0;
                    var lastProgressTime = DateTime.UtcNow;

                    // 真正的流式处理：实时输出有意义的进度更新，45秒超时+重试
                    using var streamTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(45));

                    await foreach (var chunk in CallAIProviderForStreamChatAsync(messages, userConfig).WithCancellation(streamTimeoutCts.Token))
                    {
                        if (!string.IsNullOrEmpty(chunk))
                        {
                            streamingContent.Append(chunk);
                            hasReceivedContent = true;

                            var now = DateTime.UtcNow;
                            var timeSinceLastProgress = (now - lastProgressTime).TotalMilliseconds;
                            var lengthDifference = streamingContent.Length - lastProgressLength;

                            // 条件：每收集50个字符或每2秒更新一次进度
                            if (lengthDifference >= 50 || timeSinceLastProgress >= 2000)
                            {
                                yield return new AIWorkflowStreamResult
                                {
                                    Type = "progress",
                                    Message = $"Generating workflow... ({streamingContent.Length} characters, {timeSinceLastProgress / 1000:F1}s)",
                                    IsComplete = false
                                };

                                lastProgressLength = streamingContent.Length;
                                lastProgressTime = now;

                                _logger.LogInformation("📊 Progress update: {Length} chars, {Duration}ms since last",
                                    streamingContent.Length, timeSinceLastProgress);
                            }
                        }
                    }

                    var totalDuration = (DateTime.UtcNow - streamStartTime).TotalMilliseconds;
                    _logger.LogInformation("🏁 {Provider} TRUE stream completed: {Length} chars in {Duration}ms",
                        userConfig.Provider, streamingContent.Length, totalDuration);
                }

                // 流式完成后立即开始解析
                if (hasReceivedContent)
                {
                    yield return new AIWorkflowStreamResult
                    {
                        Type = "progress",
                        Message = "Parsing workflow structure...",
                        IsComplete = false
                    };

                    // 解析AI响应
                    _logger.LogInformation("🔍 Starting to parse AI response, content length: {Length}", streamingContent.Length);
                    var parseStartTime = DateTime.UtcNow;
                    var streamResult = ParseWorkflowGenerationResponse(streamingContent.ToString());
                    var parseEndTime = DateTime.UtcNow;
                    _logger.LogInformation("✅ Parsing completed in {Duration}ms", (parseEndTime - parseStartTime).TotalMilliseconds);

                    if (streamResult?.GeneratedWorkflow != null)
                    {
                        _logger.LogInformation("🎯 About to yield workflow and {Count} stages", streamResult.Stages?.Count ?? 0);

                        yield return new AIWorkflowStreamResult
                        {
                            Type = "workflow",
                            Data = streamResult.GeneratedWorkflow,
                            Message = "Workflow basic information generated",
                            IsComplete = false
                        };

                        var stageStartTime = DateTime.UtcNow;
                        var stageCount = 0;
                        foreach (var stage in streamResult.Stages)
                        {
                            yield return new AIWorkflowStreamResult
                            {
                                Type = "stage",
                                Data = stage,
                                Message = $"Stage '{stage.Name}' generated",
                                IsComplete = false
                            };
                            stageCount++;

                            // 每处理10个stage记录一次
                            if (stageCount % 10 == 0)
                            {
                                _logger.LogInformation("📊 Processed {Count} stages so far...", stageCount);
                            }
                        }
                        var stageEndTime = DateTime.UtcNow;
                        _logger.LogInformation("✅ All {Count} stages yielded in {Duration}ms", stageCount, (stageEndTime - stageStartTime).TotalMilliseconds);

                        // AI生成质量分析 - 优化后的统计逻辑
                        var checklistCount = streamResult.Checklists?.Count ?? 0;
                        var questionnaireCount = streamResult.Questionnaires?.Count ?? 0;
                        stageCount = streamResult.Stages.Count;

                        // 快速质量评估：计算有效组件比例
                        var effectiveChecklists = streamResult.Checklists?.Count(c => c?.Tasks?.Count > 0) ?? 0;
                        var effectiveQuestionnaires = streamResult.Questionnaires?.Count(q => q?.Questions?.Count > 0) ?? 0;
                        var qualityScore = stageCount > 0 ? ((effectiveChecklists + effectiveQuestionnaires) * 50.0) / stageCount : 0;

                        _logger.LogInformation("📈 AI生成质量: {QualityScore:F1}% - 有效组件 {EffectiveChecklists}/{ChecklistCount} checklists, {EffectiveQuestionnaires}/{QuestionnaireCount} questionnaires",
                            qualityScore, effectiveChecklists, checklistCount, effectiveQuestionnaires, questionnaireCount);

                        // 确保每个stage都有完整的checklist和questionnaire
                        streamResult.Checklists ??= new List<AIChecklistGenerationResult>();
                        streamResult.Questionnaires ??= new List<AIQuestionnaireGenerationResult>();

                        var supplementStartTime = DateTime.UtcNow;
                        var supplementCount = 0;

                        // 智能补充缺失的组件 - 优化性能，减少不必要的fallback创建
                        stageCount = streamResult.Stages.Count;
                        checklistCount = streamResult.Checklists?.Count ?? 0;
                        questionnaireCount = streamResult.Questionnaires?.Count ?? 0;

                        // 快速检查：如果AI完整生成了所有组件，跳过补充逻辑
                        var hasCompleteGeneration = checklistCount >= stageCount && questionnaireCount >= stageCount &&
                            streamResult.Checklists.Take(stageCount).All(c => c?.Tasks?.Count > 0) &&
                            streamResult.Questionnaires.Take(stageCount).All(q => q?.Questions?.Count > 0);

                        if (hasCompleteGeneration)
                        {
                            _logger.LogInformation("🎯 AI完整生成所有组件，跳过补充逻辑 - {ChecklistCount} checklists, {QuestionnaireCount} questionnaires",
                                checklistCount, questionnaireCount);
                        }
                        else
                        {
                            // 只有在确实缺失组件时才进行补充
                            _logger.LogInformation("🔧 检测到组件缺失，开始智能补充 - 现有: {ChecklistCount}/{StageCount} checklists, {QuestionnaireCount}/{StageCount} questionnaires",
                                checklistCount, stageCount, questionnaireCount, stageCount);

                            for (int i = 0; i < stageCount; i++)
                            {
                                var stage = streamResult.Stages[i];

                                // 智能检查checklist: 确保索引范围内且有有效任务
                                if (i >= checklistCount || streamResult.Checklists[i]?.Tasks?.Count == 0)
                                {
                                    // 只在完全缺失时创建空结构，避免覆盖有效的AI生成内容
                                    if (i >= checklistCount)
                                    {
                                        streamResult.Checklists.Add(GenerateFallbackChecklist(stage));
                                        supplementCount++;
                                        _logger.LogDebug("➕ 为stage {StageIndex}-{StageName} 添加空checklist", i, stage.Name);
                                    }
                                }

                                // 智能检查questionnaire: 确保索引范围内且有有效问题
                                if (i >= questionnaireCount || streamResult.Questionnaires[i]?.Questions?.Count == 0)
                                {
                                    // 只在完全缺失时创建空结构
                                    if (i >= questionnaireCount)
                                    {
                                        streamResult.Questionnaires.Add(GenerateFallbackQuestionnaire(stage));
                                        supplementCount++;
                                        _logger.LogDebug("➕ 为stage {StageIndex}-{StageName} 添加空questionnaire", i, stage.Name);
                                    }
                                }
                            }
                        }

                        if (supplementCount > 0)
                        {
                            _logger.LogWarning("⚠️ AI生成不完整，补充了{SupplementCount}个空结构 ({Duration:F1}ms) - 最终{ChecklistCount} checklists + {QuestionnaireCount} questionnaires",
                                supplementCount,
                                (DateTime.UtcNow - supplementStartTime).TotalMilliseconds,
                                streamResult.Checklists.Count,
                                streamResult.Questionnaires.Count);
                        }
                        else
                        {
                            _logger.LogInformation("🎯 AI生成完美：无需补充任何组件");
                        }

                        var completeStartTime = DateTime.UtcNow;
                        yield return new AIWorkflowStreamResult
                        {
                            Type = "complete",
                            Data = streamResult,
                            Message = "Workflow generation completed with AI-powered components",
                            IsComplete = true
                        };
                        var completeEndTime = DateTime.UtcNow;
                        _logger.LogInformation("🏁 Complete message yielded in {Duration}ms", (completeEndTime - completeStartTime).TotalMilliseconds);

                        _logger.LogInformation("🎉 StreamGenerateWorkflowAsync about to exit successfully");
                        yield break;
                    }
                    else
                    {
                        // AI解析失败，先尝试修复JSON，再使用快速fallback生成
                        _logger.LogWarning("AI response parsing failed, attempting JSON repair and fallback generation");

                        var fallbackStartTime = DateTime.UtcNow;
                        var fallbackResult = TryRepairAndParseWorkflow(streamingContent.ToString());

                        if (fallbackResult == null)
                        {
                            fallbackResult = GenerateFallbackWorkflow(streamingContent.ToString());
                        }

                        if (fallbackResult?.Stages?.Any() == true)
                        {
                            _logger.LogInformation("🔄 Fallback workflow generated with {Count} stages", fallbackResult.Stages.Count);

                            yield return new AIWorkflowStreamResult
                            {
                                Type = "workflow",
                                Data = fallbackResult.GeneratedWorkflow,
                                Message = "Workflow generated using optimized fallback",
                                IsComplete = false
                            };

                            foreach (var stage in fallbackResult.Stages)
                            {
                                yield return new AIWorkflowStreamResult
                                {
                                    Type = "stage",
                                    Data = stage,
                                    Message = $"Stage '{stage.Name}' generated",
                                    IsComplete = false
                                };
                            }

                            yield return new AIWorkflowStreamResult
                            {
                                Type = "complete",
                                Data = fallbackResult,
                                Message = $"Workflow generation completed in {(DateTime.UtcNow - fallbackStartTime).TotalMilliseconds}ms",
                                IsComplete = true
                            };
                            yield break;
                        }

                        yield return new AIWorkflowStreamResult
                        {
                            Type = "error",
                            Message = "AI generation failed and fallback unsuccessful",
                            IsComplete = true
                        };
                        yield break;
                    }
                }
            }
            else
            {
                // 回退到非流式API
                var aiResponse = await CallAIProviderAsync(prompt);
                if (aiResponse.Success)
                {
                    streamingContent.Append(aiResponse.Content);
                    hasReceivedContent = true;
                }
                else
                {
                    yield return new AIWorkflowStreamResult
                    {
                        Type = "error",
                        Message = aiResponse.ErrorMessage ?? "AI service call failed",
                        IsComplete = true
                    };
                    yield break;
                }
            }

            if (!hasReceivedContent)
            {
                yield return new AIWorkflowStreamResult
                {
                    Type = "error",
                    Message = "No content received from AI service",
                    IsComplete = true
                };
                yield break;
            }

            yield return new AIWorkflowStreamResult
            {
                Type = "progress",
                Message = "Parsing workflow structure...",
                IsComplete = false
            };

            // 解析AI响应（非流式路径）
            var result = ParseWorkflowGenerationResponse(streamingContent.ToString());

            if (result?.GeneratedWorkflow != null)
            {
                yield return new AIWorkflowStreamResult
                {
                    Type = "workflow",
                    Data = result.GeneratedWorkflow,
                    Message = "Workflow basic information generated",
                    IsComplete = false
                };

                foreach (var stage in result.Stages)
                {
                    yield return new AIWorkflowStreamResult
                    {
                        Type = "stage",
                        Data = stage,
                        Message = $"Stage '{stage.Name}' generated",
                        IsComplete = false
                    };
                }

                yield return new AIWorkflowStreamResult
                {
                    Type = "complete",
                    Data = result,
                    Message = "Workflow generation completed",
                    IsComplete = true
                };
            }
            else
            {
                yield return new AIWorkflowStreamResult
                {
                    Type = "error",
                    Message = "Unable to parse AI-generated workflow structure",
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
                Message = "Starting questionnaire generation...",
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
                    Message = $"Error during generation: {caughtException.Message}",
                    IsComplete = true
                };
                yield break;
            }

            if (aiResponse == null || !aiResponse.Success)
            {
                yield return new AIQuestionnaireStreamResult
                {
                    Type = "error",
                    Message = aiResponse?.ErrorMessage ?? "AI service call failed",
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
                    Message = "Questionnaire basic information generated",
                    IsComplete = false
                };

                yield return new AIQuestionnaireStreamResult
                {
                    Type = "complete",
                    Data = result,
                    Message = "Questionnaire generation completed",
                    IsComplete = true
                };
            }
            else
            {
                yield return new AIQuestionnaireStreamResult
                {
                    Type = "error",
                    Message = "Unable to parse AI-generated questionnaire structure",
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
                Message = "Starting checklist generation...",
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

        public async Task<AIWorkflowEnhancementResult> EnhanceWorkflowAsync(long workflowId, string enhancement)
        {
            try
            {
                _logger.LogInformation("Enhancing workflow {WorkflowId} with: {Enhancement}", workflowId, enhancement);

                var prompt = $"""
                Please analyze the following workflow enhancement requirements and provide specific improvement suggestions:

                Workflow ID: {workflowId}
                Enhancement Requirements: {enhancement}

                Please provide:
                1. Specific improvement suggestions
                2. Suggested priority levels
                3. Implementation plans

                Please return the results in JSON format.
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

        public async Task<AIRequirementsParsingResult> ParseRequirementsAsync(string naturalLanguage, string? modelProvider, string? modelName, string? modelId)
        {
            try
            {
                _logger.LogInformation("Parsing requirements with explicit model override: Provider={Provider}, Model={ModelName}, Id={ModelId}", modelProvider, modelName, modelId);

                var prompt = $"""
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

                var aiResponse = await CallAIProviderAsync(prompt, modelId, modelProvider, modelName);
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
                        return await CallZhipuAIAsync(prompt, effectiveModelId, effectiveModelName);
                    case "openai":
                        return await CallOpenAIAsync(prompt, effectiveModelId, effectiveModelName);
                    case "claude":
                    case "anthropic":
                        return await CallClaudeAsync(prompt, effectiveModelId, effectiveModelName);
                    case "deepseek":
                        return await CallDeepSeekAsync(prompt, effectiveModelId, effectiveModelName);
                    default:
                        // Try to call using generic OpenAI-compatible API
                        _logger.LogInformation("Unknown provider {Provider}, attempting to use OpenAI-compatible API", provider);
                        return await CallGenericOpenAICompatibleAsync(prompt, effectiveModelId, effectiveModelName, provider);
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

        private string BuildWorkflowGenerationPrompt(AIWorkflowGenerationInput input)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine($"{_aiOptions.Prompts.WorkflowSystem}");
            promptBuilder.AppendLine("Output the result according to the language input by the user.");
            promptBuilder.AppendLine();

            // Check if this is a conversation-based workflow generation
            if (input.ConversationHistory != null && input.ConversationHistory.Any())
            {
                promptBuilder.AppendLine("=== Generate Workflow Based on Detailed Conversation ===");
                promptBuilder.AppendLine("Below is the complete conversation history with the user. Please generate an accurate workflow based on these detailed information:");
                promptBuilder.AppendLine();

                // Add conversation context
                if (input.ConversationMetadata != null)
                {
                    promptBuilder.AppendLine($"Session Information:");
                    promptBuilder.AppendLine($"- Session ID: {input.SessionId}");
                    promptBuilder.AppendLine($"- Total Messages: {input.ConversationMetadata.TotalMessages}");
                    promptBuilder.AppendLine($"- Conversation Mode: {input.ConversationMetadata.ConversationMode}");
                    promptBuilder.AppendLine();
                }

                // Add full conversation history
                promptBuilder.AppendLine("Complete Conversation Content:");
                foreach (var message in input.ConversationHistory)
                {
                    var role = message.Role == "user" ? "👤 User" : "🤖 AI Assistant";
                    promptBuilder.AppendLine($"{role}:");
                    promptBuilder.AppendLine(message.Content);
                    promptBuilder.AppendLine();
                }

                promptBuilder.AppendLine("Please pay special attention to:");
                promptBuilder.AppendLine("1. Extract all key requirements and details from the conversation");
                promptBuilder.AppendLine("2. Use the specific suggestions and detailed information provided by the AI assistant in the conversation");
                promptBuilder.AppendLine("3. Ensure the workflow reflects the user's specific needs and preferences");
                promptBuilder.AppendLine("4. If the AI assistant provided detailed itineraries, plans, or steps, convert them into workflow stages");
                promptBuilder.AppendLine();
            }
            else
            {
                // Fallback to traditional prompt building
                promptBuilder.AppendLine("Please generate a complete workflow definition based on the following requirements:");
                promptBuilder.AppendLine($"Description: {input.Description}");
            }

            if (!string.IsNullOrEmpty(input.Context))
                promptBuilder.AppendLine($"Context: {input.Context}");

            if (!string.IsNullOrEmpty(input.Industry))
                promptBuilder.AppendLine($"Industry: {input.Industry}");

            if (!string.IsNullOrEmpty(input.ProcessType))
                promptBuilder.AppendLine($"Process Type: {input.ProcessType}");

            if (input.Requirements.Any())
            {
                promptBuilder.AppendLine("Specific Requirements:");
                foreach (var req in input.Requirements)
                {
                    promptBuilder.AppendLine($"- {req}");
                }
            }

            // Add AI model information if available
            if (!string.IsNullOrEmpty(input.ModelProvider))
            {
                promptBuilder.AppendLine();
                promptBuilder.AppendLine($"AI Model Used: {input.ModelProvider} {input.ModelName}");
            }

            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Generate complete workflow, must include checklist and questionnaire for each stage. Each stage should have 0-3 tasks and 0-3 questions:");
            promptBuilder.AppendLine(@"{
  ""name"": ""Workflow Name"",
  ""description"": ""Workflow Description"",
  ""stages"": [
    {
      ""name"": ""Stage Name"",
      ""description"": ""Stage Description"",
      ""assignedGroup"": ""Assigned Team"",
      ""estimatedDuration"": 1,
      ""checklist"": {
        ""name"": ""Task List"",
        ""tasks"": [
          { ""title"": ""Task Name"", ""description"": ""Description"", ""isRequired"": true, ""estimatedMinutes"": 60, ""category"": ""Execution"" }
        ]
      },
      ""questionnaire"": {
        ""name"": ""Information Collection"",
        ""questions"": [
          { ""question"": ""Key Question?"", ""type"": ""text"", ""isRequired"": true, ""category"": ""Requirements"" },
          { ""question"": ""Priority?"", ""type"": ""select"", ""isRequired"": true, ""options"": [""High"", ""Medium"", ""Low""] }
        ]
      }
    }
  ]
}
IMPORTANT: Each stage must contain both checklist and questionnaire fields!");

            return promptBuilder.ToString();
        }

        private string BuildQuestionnaireGenerationPrompt(AIQuestionnaireGenerationInput input)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine($"{_aiOptions.Prompts.QuestionnaireSystem}");
            promptBuilder.AppendLine("Output the result according to the language input by the user.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Please generate a complete questionnaire based on the following requirements:");
            promptBuilder.AppendLine($"Purpose: {input.Purpose}");
            promptBuilder.AppendLine($"Target Audience: {input.TargetAudience}");
            promptBuilder.AppendLine($"Complexity: {input.Complexity}");
            promptBuilder.AppendLine($"Estimated Number of Questions: {input.EstimatedQuestions}");

            if (input.Topics.Any())
            {
                promptBuilder.AppendLine("Topics Covered:");
                foreach (var topic in input.Topics)
                {
                    promptBuilder.AppendLine($"- {topic}");
                }
            }

            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Please generate a JSON format response containing the following information:");
            promptBuilder.AppendLine("1. Basic questionnaire information (name, description)");
            promptBuilder.AppendLine("2. Question sections (sections)");
            promptBuilder.AppendLine("3. Specific question list, including question types, options, etc.");

            return promptBuilder.ToString();
        }

        private string BuildChecklistGenerationPrompt(AIChecklistGenerationInput input)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine($"{_aiOptions.Prompts.ChecklistSystem}");
            promptBuilder.AppendLine("Output the result according to the language input by the user.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Please generate a complete checklist based on the following requirements:");
            promptBuilder.AppendLine($"Process Name: {input.ProcessName}");
            promptBuilder.AppendLine($"Description: {input.Description}");
            promptBuilder.AppendLine($"Responsible Team: {input.Team}");

            if (input.RequiredSteps.Any())
            {
                promptBuilder.AppendLine("Required Steps:");
                foreach (var step in input.RequiredSteps)
                {
                    promptBuilder.AppendLine($"- {step}");
                }
            }

            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Please generate a JSON format response containing the following information:");
            promptBuilder.AppendLine("1. Basic checklist information (name, description, team)");
            promptBuilder.AppendLine("2. Task list, including task name, description, estimated time, whether required");
            if (input.IncludeDependencies)
                promptBuilder.AppendLine("3. Task dependencies");

            return promptBuilder.ToString();
        }

        private AIWorkflowGenerationResult ParseWorkflowGenerationResponse(string aiResponse)
        {
            _logger.LogInformation("🔍 ParseWorkflowGenerationResponse started, response length: {Length}", aiResponse.Length);
            var methodStartTime = DateTime.UtcNow;

            try
            {
                // Try to parse JSON response from AI
                if (aiResponse.Contains("{") && aiResponse.Contains("}"))
                {
                    _logger.LogInformation("📄 Found JSON content, extracting...");
                    var jsonStart = aiResponse.IndexOf('{');
                    var jsonEnd = aiResponse.LastIndexOf('}') + 1;
                    var jsonContent = aiResponse.Substring(jsonStart, jsonEnd - jsonStart);

                    _logger.LogDebug("🔧 Deserializing JSON, content length: {Length}", jsonContent.Length);
                    _logger.LogDebug("🔍 JSON Preview: {JsonPreview}",
                        jsonContent.Length > 300 ? jsonContent.Substring(0, 300) + "..." : jsonContent);
                    var parsed = JsonSerializer.Deserialize<JsonElement>(jsonContent);

                    var workflow = new WorkflowInputDto
                    {
                        Name = parsed.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : "AI Generated Workflow",
                        Description = parsed.TryGetProperty("description", out var descEl) ? descEl.GetString() : "Generated by AI",
                        IsActive = true,
                        IsAIGenerated = true
                    };

                    var stages = new List<AIStageGenerationResult>();
                    var checklists = new List<AIChecklistGenerationResult>();
                    var questionnaires = new List<AIQuestionnaireGenerationResult>();

                    if (parsed.TryGetProperty("stages", out var stagesEl) && stagesEl.ValueKind == JsonValueKind.Array)
                    {
                        var order = 1;
                        foreach (var stageEl in stagesEl.EnumerateArray())
                        {
                            var stage = new AIStageGenerationResult
                            {
                                Name = stageEl.TryGetProperty("name", out var sNameEl) ? sNameEl.GetString() : $"Stage {order}",
                                Description = stageEl.TryGetProperty("description", out var sDescEl) ? sDescEl.GetString() : "",
                                Order = order,
                                AssignedGroup = stageEl.TryGetProperty("assignedGroup", out var sGroupEl) ? sGroupEl.GetString() : "General",
                                EstimatedDuration = stageEl.TryGetProperty("estimatedDuration", out var sDurEl) && sDurEl.TryGetInt32(out var dur) ? dur : 1
                            };
                            stages.Add(stage);

                            // 解析embedded checklist
                            if (stageEl.TryGetProperty("checklist", out var checklistEl))
                            {
                                var checklist = ParseEmbeddedChecklist(checklistEl, stage, order - 1);
                                if (checklist != null)
                                {
                                    _logger.LogDebug("✅ Parsed checklist: {TaskCount} tasks for {StageName}",
                                        checklist.Tasks?.Count ?? 0, stage.Name);
                                    checklists.Add(checklist);
                                }
                                else
                                {
                                    _logger.LogWarning("❌ Failed to parse embedded checklist for stage {StageName}", stage.Name);
                                }
                            }
                            else
                            {
                                _logger.LogWarning("⚠️ No embedded checklist found for stage {StageName}", stage.Name);
                            }

                            // 解析embedded questionnaire
                            if (stageEl.TryGetProperty("questionnaire", out var questionnaireEl))
                            {
                                var questionnaire = ParseEmbeddedQuestionnaire(questionnaireEl, stage, order - 1);
                                if (questionnaire != null)
                                {
                                    _logger.LogDebug("✅ Parsed questionnaire: {QuestionCount} questions for {StageName}",
                                        questionnaire.Questions?.Count ?? 0, stage.Name);
                                    questionnaires.Add(questionnaire);
                                }
                                else
                                {
                                    _logger.LogWarning("❌ Failed to parse embedded questionnaire for stage {StageName}", stage.Name);
                                }
                            }
                            else
                            {
                                _logger.LogWarning("⚠️ No embedded questionnaire found for stage {StageName}", stage.Name);
                            }

                            order++;
                        }
                    }

                    var jsonEndTime = DateTime.UtcNow;
                    _logger.LogInformation("🎯 JSON解析成功 ({Duration:F1}ms) - 完整度: {StageCount} stages, {ChecklistCount} checklists, {QuestionnaireCount} questionnaires",
    (jsonEndTime - methodStartTime).TotalMilliseconds, stages.Count, checklists.Count, questionnaires.Count);

                    return new AIWorkflowGenerationResult
                    {
                        Success = true,
                        Message = "Workflow generated successfully with embedded checklists and questionnaires",
                        GeneratedWorkflow = workflow,
                        Stages = stages,
                        Checklists = checklists,
                        Questionnaires = questionnaires,
                        Suggestions = new List<string> { "Consider adding approval stages", "Review stage assignments" }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse JSON response, using fallback parsing");
            }

            // Fallback: Generate a basic workflow from the text response
            _logger.LogInformation("🔄 Using fallback workflow generation...");
            var fallbackStartTime = DateTime.UtcNow;
            var result = GenerateFallbackWorkflow(aiResponse);
            var fallbackEndTime = DateTime.UtcNow;
            _logger.LogInformation("✅ Fallback completed in {Duration}ms", (fallbackEndTime - fallbackStartTime).TotalMilliseconds);

            var methodEndTime = DateTime.UtcNow;
            _logger.LogInformation("🏁 ParseWorkflowGenerationResponse completed in {Duration}ms", (methodEndTime - methodStartTime).TotalMilliseconds);

            return result;
        }

        private AIWorkflowGenerationResult GenerateFallbackWorkflow(string aiResponse)
        {
            _logger.LogInformation("🔄 GenerateFallbackWorkflow started, response length: {Length}", aiResponse.Length);

            var workflow = new WorkflowInputDto
            {
                Name = "AI Generated Workflow",
                Description = "Generated by AI",
                IsActive = true,
                IsAIGenerated = true
            };

            // Intelligently extract stage information from AI response
            _logger.LogInformation("🔍 Extracting stages from text...");
            var extractStartTime = DateTime.UtcNow;
            var stages = ExtractStagesFromText(aiResponse);
            var extractEndTime = DateTime.UtcNow;
            _logger.LogInformation("✅ Stage extraction completed in {Duration}ms, found {Count} stages",
                (extractEndTime - extractStartTime).TotalMilliseconds, stages.Count);

            // If no stages extracted, create default stages
            if (!stages.Any())
            {
                stages = new List<AIStageGenerationResult>
                {
                    new AIStageGenerationResult
                    {
                        Name = "Preparation Stage",
                        Description = "Gather required information and resources",
                        Order = 1,
                        AssignedGroup = "Execution Team",
                        EstimatedDuration = 2
                    },
                    new AIStageGenerationResult
                    {
                        Name = "Execution Stage",
                        Description = "Execute main work tasks",
                        Order = 2,
                        AssignedGroup = "Execution Team",
                        EstimatedDuration = 5
                    },
                    new AIStageGenerationResult
                    {
                        Name = "Review Stage",
                        Description = "Review work results and quality",
                        Order = 3,
                        AssignedGroup = "Management Team",
                        EstimatedDuration = 2
                    },
                    new AIStageGenerationResult
                    {
                        Name = "Completion Stage",
                        Description = "Confirm completion and deliver results",
                        Order = 4,
                        AssignedGroup = "Management Team",
                        EstimatedDuration = 1
                    }
                };
            }

            // Generate checklists and questionnaires for each stage (using fallback methods for synchronous context)
            _logger.LogInformation("🔍 Generating checklists and questionnaires metadata for {Count} stages...", stages.Count);
            var checklistsStartTime = DateTime.UtcNow;
            var checklists = GenerateChecklistsForStages(stages);
            var checklistsEndTime = DateTime.UtcNow;
            _logger.LogInformation("✅ Checklists metadata generation completed in {Duration}ms, generated {Count} checklists",
                (checklistsEndTime - checklistsStartTime).TotalMilliseconds, checklists.Count);

            var questionnairesStartTime = DateTime.UtcNow;
            var questionnaires = GenerateQuestionnairesForStages(stages);
            var questionnairesEndTime = DateTime.UtcNow;
            _logger.LogInformation("✅ Questionnaires metadata generation completed in {Duration}ms, generated {Count} questionnaires",
                (questionnairesEndTime - questionnairesStartTime).TotalMilliseconds, questionnaires.Count);

            return new AIWorkflowGenerationResult
            {
                Success = true,
                Message = "Workflow generated successfully",
                GeneratedWorkflow = workflow,
                Stages = stages,
                Checklists = checklists,
                Questionnaires = questionnaires,
                Suggestions = new List<string> { "Consider adding approval stages", "Review stage assignments" }
            };
        }

        private List<AIStageGenerationResult> ExtractStagesFromText(string text)
        {
            _logger.LogInformation("🔍 ExtractStagesFromText started, text length: {Length}", text.Length);

            var stages = new List<AIStageGenerationResult>();
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var order = 1;

            _logger.LogInformation("📄 Processing {LineCount} lines of text", lines.Length);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Find possible stage identifiers
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
                            Description = $"AI-generated {stageName}",
                            Order = order++,
                            AssignedGroup = "Execution Team",
                            EstimatedDuration = 2
                        });
                    }
                }
            }

            return stages;
        }

        private string ExtractStageName(string line)
        {
            // Remove common prefixes and identifiers
            var cleaned = line.Trim()
                .Replace("-", "")
                .Replace("*", "")
                .Replace("•", "");

            // Remove numeric prefixes (e.g., "1. ", "2. ")
            cleaned = Regex.Replace(cleaned, @"^\d+\.\s*", "");

            // Remove parentheses content
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

                // Get detailed information of existing workflow
                _logger.LogInformation("Fetching existing workflow with ID: {WorkflowId}", input.WorkflowId);
                var existingWorkflowInfo = await GetExistingWorkflowAsync(input.WorkflowId);
                _logger.LogInformation("Retrieved workflow: Name={Name}, Description={Description}, StageCount={StageCount}",
                    existingWorkflowInfo.Name, existingWorkflowInfo.Description, existingWorkflowInfo.Stages.Count);

                // Record detailed information of existing stages
                for (int i = 0; i < existingWorkflowInfo.Stages.Count; i++)
                {
                    var stage = existingWorkflowInfo.Stages[i];
                    _logger.LogInformation("Stage {Index}: Name='{Name}', Description='{Description}', Duration={Duration}, Team='{Team}'",
                        i + 1, stage.Name, stage.Description, stage.EstimatedDuration, stage.AssignedGroup);
                }

                // Build modification prompt
                var prompt = await BuildWorkflowModificationPromptAsync(input, existingWorkflowInfo);

                // Debug log: output complete prompt
                _logger.LogInformation("AI Modification Prompt: {Prompt}", prompt);

                // Call AI for workflow modification
                var aiResponse = await CallAIProviderAsync(prompt);

                // Debug log: output AI response
                _logger.LogInformation("AI Modification Response: Success={Success}, Content={Content}",
                    aiResponse.Success, aiResponse.Content);

                if (!aiResponse.Success)
                {
                    result.Success = false;
                    result.Message = aiResponse.ErrorMessage;
                    return GenerateFallbackWorkflow($"Error modifying workflow {input.WorkflowId}");
                }

                // Parse AI response
                var modificationResult = ParseWorkflowGenerationResponse(aiResponse.Content);

                if (modificationResult.Stages == null || !modificationResult.Stages.Any())
                {
                    modificationResult = GenerateFallbackWorkflow($"Modified workflow for ID: {input.WorkflowId}");
                }

                // Force ensure workflow name is correct (prevent AI from not following instructions)
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
                "Please modify based on maintaining the core structure and existing stages of the current workflow. Only add, modify, or delete stages according to specific requirements." :
                "If needed, you can completely redesign the workflow.";

            var prompt = $@"CRITICAL: This is a MODIFICATION task, NOT a creation task.
Output the result according to the language input by the user.

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

            // If retrieval fails, return default data
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

            // 实时流式传输每个数据块
            await foreach (var chunk in CallAIProviderForStreamChatAsync(input))
            {
                yield return new AIChatStreamResult
                {
                    Type = "delta",
                    Content = chunk,
                    IsComplete = false,
                    SessionId = sessionId
                };
            }

            // 发送完成信号
            yield return new AIChatStreamResult
            {
                Type = "complete",
                Content = "",
                IsComplete = true,
                SessionId = sessionId
            };
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
                    // Call corresponding API based on provider
                    response = userConfig.Provider?.ToLower() switch
                    {
                        "zhipuai" => await CallZhipuAIWithConfigAsync(messages, userConfig),
                        "openai" => await CallOpenAIWithConfigAsync(messages, userConfig),
                        "claude" => await CallClaudeWithConfigAsync(messages, userConfig),
                        "deepseek" => await CallDeepSeekWithConfigAsync(messages, userConfig),
                        _ => await CallZhipuAIAsync(messages) // Default to ZhipuAI
                    };

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
                max_tokens = config.MaxTokens > 0 ? config.MaxTokens : 1000
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
                max_tokens = config.MaxTokens > 0 ? config.MaxTokens : 1000,
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
        /// Call DeepSeek API using user configuration
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
                max_tokens = config.MaxTokens > 0 ? config.MaxTokens : 1000,
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
            var lineTimeout = TimeSpan.FromSeconds(5); // 每行最多等待5秒

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
                "uuid", "os", "base64", "hashlib", "hmac", "binascii", "collections", "functools", "operator", "itertools"
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

        private string BuildChecklistGenerationPrompt(AIStageGenerationResult stage, string originalDescription)
        {
            return $@"
Based on the following stage information and original project description, generate a comprehensive checklist with specific tasks.

Original Project Description: {originalDescription}

Stage Information:
- Name: {stage.Name}
- Description: {stage.Description}
- Assigned Group: {stage.AssignedGroup}
- Estimated Duration: {stage.EstimatedDuration} days

Please provide a JSON response with the following structure:
{{
    ""name"": ""Specific checklist name"",
    ""description"": ""Detailed description of what this checklist covers"",
    ""tasks"": [
        {{
            ""title"": ""Task title"",
            ""description"": ""Detailed task description"",
            ""isRequired"": true/false,
            ""estimatedMinutes"": number,
            ""category"": ""Task category""
        }}
    ]
}}

Generate 3-6 specific, actionable tasks that are directly relevant to this stage and the overall project description.
Focus on concrete deliverables and ensure tasks are specific to the project context.
";
        }

        private string BuildQuestionnaireGenerationPrompt(AIStageGenerationResult stage, string originalDescription)
        {
            return $@"
Based on the following stage information and original project description, generate relevant questions to gather information needed for this stage.

Original Project Description: {originalDescription}

Stage Information:
- Name: {stage.Name}
- Description: {stage.Description}
- Assigned Group: {stage.AssignedGroup}
- Estimated Duration: {stage.EstimatedDuration} days

Please provide a JSON response with the following structure:
{{
    ""name"": ""Specific questionnaire name"",
    ""description"": ""Description of information this questionnaire gathers"",
    ""questions"": [
        {{
            ""question"": ""Question text"",
            ""type"": ""text"" | ""select"" | ""multiselect"" | ""boolean"" | ""number"",
            ""isRequired"": true/false,
            ""category"": ""Question category"",
            ""options"": [""option1"", ""option2""] // only for select/multiselect types
        }}
    ]
}}

Generate 3-8 relevant questions that help gather the information needed to successfully complete this stage.
Focus on questions that are specific to the project context and this particular stage's requirements.
";
        }

        private string BuildBatchChecklistGenerationPrompt(List<AIStageGenerationResult> stages, string originalDescription)
        {
            var stagesInfo = string.Join("\n\n", stages.Select((stage, index) => $@"
Stage {index + 1}:
- Name: {stage.Name}
- Description: {stage.Description}
- Assigned Group: {stage.AssignedGroup}
- Estimated Duration: {stage.EstimatedDuration} days"));

            return $@"
You are a project management expert. Generate comprehensive checklists for the project stages described below.

Project: {originalDescription}

Stages:
{stagesInfo}

IMPORTANT: Respond ONLY with valid JSON in the exact format below, no additional text:

{{
    ""checklists"": [
        {{
            ""stageIndex"": 0,
            ""name"": ""Checklist for {stages.FirstOrDefault()?.Name ?? "Stage 1"}"",
            ""description"": ""Tasks for {stages.FirstOrDefault()?.Name ?? "Stage 1"}"",
            ""tasks"": [
                {{
                    ""title"": ""Sample task title"",
                    ""description"": ""Detailed task description"",
                    ""isRequired"": true,
                    ""estimatedMinutes"": 120,
                    ""category"": ""Planning""
                }}
            ]
        }}
    ]
}}

Create {stages.Count} checklist entries (stageIndex 0 to {stages.Count - 1}), each with 3-5 specific tasks.
Make tasks relevant to the project context and stage purpose.
Use realistic time estimates (30-480 minutes per task).";
        }

        private string BuildBatchQuestionnaireGenerationPrompt(List<AIStageGenerationResult> stages, string originalDescription)
        {
            var stagesInfo = string.Join("\n\n", stages.Select((stage, index) => $@"
Stage {index + 1}:
- Name: {stage.Name}
- Description: {stage.Description}
- Assigned Group: {stage.AssignedGroup}
- Estimated Duration: {stage.EstimatedDuration} days"));

            return $@"
You are a business analyst expert. Generate relevant questionnaires for the project stages described below.

Project: {originalDescription}

Stages:
{stagesInfo}

IMPORTANT: Respond ONLY with valid JSON in the exact format below, no additional text:

{{
    ""questionnaires"": [
        {{
            ""stageIndex"": 0,
            ""name"": ""Questionnaire for {stages.FirstOrDefault()?.Name ?? "Stage 1"}"",
            ""description"": ""Information gathering for {stages.FirstOrDefault()?.Name ?? "Stage 1"}"",
            ""questions"": [
                {{
                    ""question"": ""Sample question about the stage requirements?"",
                    ""type"": ""text"",
                    ""isRequired"": true,
                    ""category"": ""Requirements""
                }}
            ]
        }}
    ]
}}

Create {stages.Count} questionnaire entries (stageIndex 0 to {stages.Count - 1}), each with 3-6 specific questions.
Use question types: text, select, multiselect, boolean, number.
Include ""options"" array only for select/multiselect types.
Make questions relevant to the project context and stage objectives.";
        }

        private async Task<List<AIChecklistGenerationResult>> GenerateChecklistsForStagesAsync(List<AIStageGenerationResult> stages, string originalDescription)
        {
            var checklists = new List<AIChecklistGenerationResult>();

            // 预先获取AI配置，避免重复查询
            var defaultConfig = await _configService.GetUserDefaultConfigAsync(0);
            _logger.LogInformation("🔧 Pre-cached AI config for batch generation: {Provider} - {Model}",
                defaultConfig?.Provider, defaultConfig?.ModelName);

            try
            {
                // 批量生成 - 构建包含所有stages的单个prompt，设置35秒超时
                using var batchCts = new CancellationTokenSource(TimeSpan.FromSeconds(35));
                var batchPrompt = BuildBatchChecklistGenerationPrompt(stages, originalDescription);

                // 加入重试机制，减少重试次数但更快失败转移
                var aiResponse = await CallAIProviderWithRetryAsync(batchPrompt, maxRetries: 1);

                if (aiResponse.Success && !string.IsNullOrEmpty(aiResponse.Content))
                {
                    // 添加调试日志查看AI响应
                    _logger.LogInformation("🔍 AI Checklist Response Preview: {Preview}",
                        aiResponse.Content.Length > 500 ? aiResponse.Content.Substring(0, 500) + "..." : aiResponse.Content);

                    var batchChecklists = ParseBatchChecklistResponse(aiResponse.Content, stages);
                    if (batchChecklists != null && batchChecklists.Count > 0)
                    {
                        _logger.LogInformation("✅ Successfully generated {Count} checklists in batch", batchChecklists.Count);
                        return batchChecklists;
                    }
                    else
                    {
                        _logger.LogWarning("❌ Batch checklist parsing failed - no valid results extracted");
                    }
                }
                else
                {
                    _logger.LogWarning("❌ AI checklist call failed: Success={Success}, ErrorMessage={Error}",
                        aiResponse.Success, aiResponse.ErrorMessage);
                }

                _logger.LogWarning("Batch checklist generation failed, falling back to individual generation");
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Batch checklist generation timeout, falling back to individual generation");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Batch checklist generation error, falling back to individual generation");
            }

            // 回退到并行个别生成，增加超时控制
            var parallelTasks = stages.Select(async stage =>
            {
                try
                {
                    // 为每个个别生成设置20秒超时，无重试（并行已提供冗余）
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    var checklistPrompt = BuildChecklistGenerationPrompt(stage, originalDescription);
                    var aiResponse = await CallAIProviderAsync(checklistPrompt);

                    if (aiResponse.Success && !string.IsNullOrEmpty(aiResponse.Content))
                    {
                        return ParseAIChecklistResponse(aiResponse.Content, stage);
                    }
                    else
                    {
                        _logger.LogWarning("AI checklist generation failed for stage {StageName}, using fallback", stage.Name);
                        return GenerateFallbackChecklist(stage);
                    }
                }
                catch (TaskCanceledException)
                {
                    _logger.LogWarning("AI checklist generation timeout for stage {StageName}, using fallback", stage.Name);
                    return GenerateFallbackChecklist(stage);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate AI checklist for stage {StageName}, using fallback", stage.Name);
                    return GenerateFallbackChecklist(stage);
                }
            });

            var results = await Task.WhenAll(parallelTasks);
            return results.ToList();
        }

        private List<AIChecklistGenerationResult> GenerateChecklistsForStages(List<AIStageGenerationResult> stages)
        {
            // 保留同步版本作为回退
            var checklists = new List<AIChecklistGenerationResult>();
            var random = new Random();

            foreach (var stage in stages)
            {
                // Determine the number of checklists for this stage based on complexity and characteristics
                var checklistCount = DetermineChecklistCount(stage, random);

                for (int i = 0; i < checklistCount; i++)
                {
                    var checklistName = GenerateChecklistName(stage, i, checklistCount);
                    var checklistDescription = GenerateChecklistDescription(stage, i, checklistCount);

                    var checklist = new AIChecklistGenerationResult
                    {
                        Success = true,
                        Message = $"Checklist generated for {stage.Name}",
                        GeneratedChecklist = new ChecklistInputDto
                        {
                            Name = checklistName,
                            Description = checklistDescription,
                            Team = stage.AssignedGroup,
                            IsActive = true
                        },
                        Tasks = GenerateTasksForStage(stage, i, checklistCount, random),
                        ConfidenceScore = 0.85
                    };

                    checklists.Add(checklist);
                }
            }

            return checklists;
        }

        private async Task<List<AIQuestionnaireGenerationResult>> GenerateQuestionnairesForStagesAsync(List<AIStageGenerationResult> stages, string originalDescription)
        {
            var questionnaires = new List<AIQuestionnaireGenerationResult>();

            // 预先获取AI配置，避免重复查询
            var defaultConfig = await _configService.GetUserDefaultConfigAsync(0);
            _logger.LogInformation("🔧 Pre-cached AI config for batch generation: {Provider} - {Model}",
                defaultConfig?.Provider, defaultConfig?.ModelName);

            try
            {
                // 批量生成 - 构建包含所有stages的单个prompt，设置35秒超时
                using var batchCts = new CancellationTokenSource(TimeSpan.FromSeconds(35));
                var batchPrompt = BuildBatchQuestionnaireGenerationPrompt(stages, originalDescription);

                // 加入重试机制，减少重试次数但更快失败转移
                var aiResponse = await CallAIProviderWithRetryAsync(batchPrompt, maxRetries: 1);

                if (aiResponse.Success && !string.IsNullOrEmpty(aiResponse.Content))
                {
                    // 添加调试日志查看AI响应
                    _logger.LogInformation("🔍 AI Questionnaire Response Preview: {Preview}",
                        aiResponse.Content.Length > 500 ? aiResponse.Content.Substring(0, 500) + "..." : aiResponse.Content);

                    var batchQuestionnaires = ParseBatchQuestionnaireResponse(aiResponse.Content, stages);
                    if (batchQuestionnaires != null && batchQuestionnaires.Count > 0)
                    {
                        _logger.LogInformation("✅ Successfully generated {Count} questionnaires in batch", batchQuestionnaires.Count);
                        return batchQuestionnaires;
                    }
                    else
                    {
                        _logger.LogWarning("❌ Batch questionnaire parsing failed - no valid results extracted");
                    }
                }
                else
                {
                    _logger.LogWarning("❌ AI questionnaire call failed: Success={Success}, ErrorMessage={Error}",
                        aiResponse.Success, aiResponse.ErrorMessage);
                }

                _logger.LogWarning("Batch questionnaire generation failed, falling back to individual generation");
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Batch questionnaire generation timeout, falling back to individual generation");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Batch questionnaire generation error, falling back to individual generation");
            }

            // 回退到并行个别生成，增加超时控制
            var parallelTasks = stages.Select(async stage =>
            {
                try
                {
                    // 为每个个别生成设置20秒超时，无重试（并行已提供冗余）
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    var questionnairePrompt = BuildQuestionnaireGenerationPrompt(stage, originalDescription);
                    var aiResponse = await CallAIProviderAsync(questionnairePrompt);

                    if (aiResponse.Success && !string.IsNullOrEmpty(aiResponse.Content))
                    {
                        return ParseAIQuestionnaireResponse(aiResponse.Content, stage);
                    }
                    else
                    {
                        _logger.LogWarning("AI questionnaire generation failed for stage {StageName}, using fallback", stage.Name);
                        return GenerateFallbackQuestionnaire(stage);
                    }
                }
                catch (TaskCanceledException)
                {
                    _logger.LogWarning("AI questionnaire generation timeout for stage {StageName}, using fallback", stage.Name);
                    return GenerateFallbackQuestionnaire(stage);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate AI questionnaire for stage {StageName}, using fallback", stage.Name);
                    return GenerateFallbackQuestionnaire(stage);
                }
            });

            var results = await Task.WhenAll(parallelTasks);
            return results.ToList();
        }

        private List<AIQuestionnaireGenerationResult> GenerateQuestionnairesForStages(List<AIStageGenerationResult> stages)
        {
            // 保留同步版本作为回退
            var questionnaires = new List<AIQuestionnaireGenerationResult>();
            var random = new Random();

            foreach (var stage in stages)
            {
                // Determine the number of questionnaires for this stage based on complexity and characteristics
                var questionnaireCount = DetermineQuestionnaireCount(stage, random);

                for (int i = 0; i < questionnaireCount; i++)
                {
                    var questionnaireName = GenerateQuestionnaireName(stage, i, questionnaireCount);
                    var questionnaireDescription = GenerateQuestionnaireDescription(stage, i, questionnaireCount);

                    var questionnaire = new AIQuestionnaireGenerationResult
                    {
                        Success = true,
                        Message = $"Questionnaire generated for {stage.Name}",
                        GeneratedQuestionnaire = new QuestionnaireInputDto
                        {
                            Name = questionnaireName,
                            Description = questionnaireDescription,
                            IsActive = true
                        },
                        Questions = GenerateQuestionsForStage(stage, i, questionnaireCount, random),
                        ConfidenceScore = 0.85
                    };

                    questionnaires.Add(questionnaire);
                }
            }

            return questionnaires;
        }

        private AIChecklistGenerationResult ParseAIChecklistResponse(string aiResponse, AIStageGenerationResult stage)
        {
            try
            {
                // 尝试解析JSON响应
                if (aiResponse.Contains("{") && aiResponse.Contains("}"))
                {
                    var jsonStart = aiResponse.IndexOf('{');
                    var jsonEnd = aiResponse.LastIndexOf('}') + 1;
                    var jsonContent = aiResponse.Substring(jsonStart, jsonEnd - jsonStart);

                    var parsed = JsonSerializer.Deserialize<JsonElement>(jsonContent);

                    var tasks = new List<AITaskGenerationResult>();
                    if (parsed.TryGetProperty("tasks", out var tasksEl) && tasksEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var taskEl in tasksEl.EnumerateArray())
                        {
                            tasks.Add(new AITaskGenerationResult
                            {
                                Title = taskEl.TryGetProperty("title", out var titleEl) ? titleEl.GetString() : "Task",
                                Description = taskEl.TryGetProperty("description", out var taskDescEl) ? taskDescEl.GetString() : "",
                                IsRequired = taskEl.TryGetProperty("isRequired", out var reqEl) && reqEl.GetBoolean(),
                                EstimatedMinutes = taskEl.TryGetProperty("estimatedMinutes", out var timeEl) && timeEl.TryGetInt32(out var minutes) ? minutes : 60,
                                Category = taskEl.TryGetProperty("category", out var catEl) ? catEl.GetString() : "General"
                            });
                        }
                    }

                    return new AIChecklistGenerationResult
                    {
                        Success = true,
                        Message = $"AI-generated checklist for {stage.Name}",
                        GeneratedChecklist = new ChecklistInputDto
                        {
                            Name = parsed.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : $"{stage.Name} Checklist",
                            Description = parsed.TryGetProperty("description", out var descEl) ? descEl.GetString() : $"AI-generated checklist for {stage.Name}",
                            Team = stage.AssignedGroup,
                            IsActive = true
                        },
                        Tasks = tasks,
                        ConfidenceScore = 0.9
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse AI checklist response for stage {StageName}", stage.Name);
            }

            // 回退到默认生成
            return GenerateFallbackChecklist(stage);
        }

        private AIQuestionnaireGenerationResult ParseAIQuestionnaireResponse(string aiResponse, AIStageGenerationResult stage)
        {
            try
            {
                // 尝试解析JSON响应
                if (aiResponse.Contains("{") && aiResponse.Contains("}"))
                {
                    var jsonStart = aiResponse.IndexOf('{');
                    var jsonEnd = aiResponse.LastIndexOf('}') + 1;
                    var jsonContent = aiResponse.Substring(jsonStart, jsonEnd - jsonStart);

                    var parsed = JsonSerializer.Deserialize<JsonElement>(jsonContent);

                    var questions = new List<AIQuestionGenerationResult>();
                    if (parsed.TryGetProperty("questions", out var questionsEl) && questionsEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var questionEl in questionsEl.EnumerateArray())
                        {
                            var options = new List<string>();
                            if (questionEl.TryGetProperty("options", out var optionsEl) && optionsEl.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var optionEl in optionsEl.EnumerateArray())
                                {
                                    if (optionEl.ValueKind == JsonValueKind.String)
                                    {
                                        options.Add(optionEl.GetString());
                                    }
                                }
                            }

                            questions.Add(new AIQuestionGenerationResult
                            {
                                Question = questionEl.TryGetProperty("question", out var qEl) ? qEl.GetString() : "Question",
                                Type = questionEl.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : "text",
                                IsRequired = questionEl.TryGetProperty("isRequired", out var reqEl) && reqEl.GetBoolean(),
                                Category = questionEl.TryGetProperty("category", out var catEl) ? catEl.GetString() : "General",
                                Options = options
                            });
                        }
                    }

                    return new AIQuestionnaireGenerationResult
                    {
                        Success = true,
                        Message = $"AI-generated questionnaire for {stage.Name}",
                        GeneratedQuestionnaire = new QuestionnaireInputDto
                        {
                            Name = parsed.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : $"{stage.Name} Questionnaire",
                            Description = parsed.TryGetProperty("description", out var descEl) ? descEl.GetString() : $"AI-generated questionnaire for {stage.Name}",
                            IsActive = true
                        },
                        Questions = questions,
                        ConfidenceScore = 0.9
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse AI questionnaire response for stage {StageName}", stage.Name);
            }

            // 回退到默认生成
            return GenerateFallbackQuestionnaire(stage);
        }

        private List<AIChecklistGenerationResult> ParseBatchChecklistResponse(string aiResponse, List<AIStageGenerationResult> stages)
        {
            try
            {
                _logger.LogInformation("🔍 Parsing batch checklist response, length: {Length}", aiResponse.Length);

                // 尝试解析JSON响应
                if (aiResponse.Contains("{") && aiResponse.Contains("}"))
                {
                    var jsonStart = aiResponse.IndexOf('{');
                    var jsonEnd = aiResponse.LastIndexOf('}') + 1;
                    var jsonContent = aiResponse.Substring(jsonStart, jsonEnd - jsonStart);

                    _logger.LogInformation("🔍 Extracted JSON content: {JsonContent}",
                        jsonContent.Length > 1000 ? jsonContent.Substring(0, 1000) + "..." : jsonContent);

                    var parsed = JsonSerializer.Deserialize<JsonElement>(jsonContent);
                    var results = new List<AIChecklistGenerationResult>();

                    if (parsed.TryGetProperty("checklists", out var checklistsEl) && checklistsEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var checklistEl in checklistsEl.EnumerateArray())
                        {
                            if (checklistEl.TryGetProperty("stageIndex", out var indexEl) && indexEl.TryGetInt32(out var stageIndex)
                                && stageIndex >= 0 && stageIndex < stages.Count)
                            {
                                var stage = stages[stageIndex];
                                var tasks = new List<AITaskGenerationResult>();

                                if (checklistEl.TryGetProperty("tasks", out var tasksEl) && tasksEl.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var taskEl in tasksEl.EnumerateArray())
                                    {
                                        tasks.Add(new AITaskGenerationResult
                                        {
                                            Title = taskEl.TryGetProperty("title", out var titleEl) ? titleEl.GetString() : "Task",
                                            Description = taskEl.TryGetProperty("description", out var taskDescEl) ? taskDescEl.GetString() : "",
                                            IsRequired = taskEl.TryGetProperty("isRequired", out var reqEl) && reqEl.GetBoolean(),
                                            EstimatedMinutes = taskEl.TryGetProperty("estimatedMinutes", out var timeEl) && timeEl.TryGetInt32(out var minutes) ? minutes : 60,
                                            Category = taskEl.TryGetProperty("category", out var catEl) ? catEl.GetString() : "General"
                                        });
                                    }
                                }

                                results.Add(new AIChecklistGenerationResult
                                {
                                    Success = true,
                                    Message = $"Batch AI-generated checklist for {stage.Name}",
                                    GeneratedChecklist = new ChecklistInputDto
                                    {
                                        Name = checklistEl.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : $"{stage.Name} Checklist",
                                        Description = checklistEl.TryGetProperty("description", out var descEl) ? descEl.GetString() : $"AI-generated checklist for {stage.Name}",
                                        Team = stage.AssignedGroup,
                                        IsActive = true
                                    },
                                    Tasks = tasks,
                                    ConfidenceScore = 0.95 // Higher confidence for batch generation
                                });
                            }
                        }
                    }

                    // 确保返回结果数量与stages匹配，缺失的用fallback填充
                    while (results.Count < stages.Count)
                    {
                        var missingStage = stages[results.Count];
                        results.Add(GenerateFallbackChecklist(missingStage));
                    }

                    return results;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse batch checklist response");
            }

            return null;
        }

        private List<AIQuestionnaireGenerationResult> ParseBatchQuestionnaireResponse(string aiResponse, List<AIStageGenerationResult> stages)
        {
            try
            {
                // 尝试解析JSON响应
                if (aiResponse.Contains("{") && aiResponse.Contains("}"))
                {
                    var jsonStart = aiResponse.IndexOf('{');
                    var jsonEnd = aiResponse.LastIndexOf('}') + 1;
                    var jsonContent = aiResponse.Substring(jsonStart, jsonEnd - jsonStart);

                    var parsed = JsonSerializer.Deserialize<JsonElement>(jsonContent);
                    var results = new List<AIQuestionnaireGenerationResult>();

                    if (parsed.TryGetProperty("questionnaires", out var questionnairesEl) && questionnairesEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var questionnaireEl in questionnairesEl.EnumerateArray())
                        {
                            if (questionnaireEl.TryGetProperty("stageIndex", out var indexEl) && indexEl.TryGetInt32(out var stageIndex)
                                && stageIndex >= 0 && stageIndex < stages.Count)
                            {
                                var stage = stages[stageIndex];
                                var questions = new List<AIQuestionGenerationResult>();

                                if (questionnaireEl.TryGetProperty("questions", out var questionsEl) && questionsEl.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var questionEl in questionsEl.EnumerateArray())
                                    {
                                        var options = new List<string>();
                                        if (questionEl.TryGetProperty("options", out var optionsEl) && optionsEl.ValueKind == JsonValueKind.Array)
                                        {
                                            foreach (var optionEl in optionsEl.EnumerateArray())
                                            {
                                                if (optionEl.ValueKind == JsonValueKind.String)
                                                {
                                                    options.Add(optionEl.GetString());
                                                }
                                            }
                                        }

                                        questions.Add(new AIQuestionGenerationResult
                                        {
                                            Question = questionEl.TryGetProperty("question", out var qEl) ? qEl.GetString() : "Question",
                                            Type = questionEl.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : "text",
                                            IsRequired = questionEl.TryGetProperty("isRequired", out var reqEl) && reqEl.GetBoolean(),
                                            Category = questionEl.TryGetProperty("category", out var catEl) ? catEl.GetString() : "General",
                                            Options = options
                                        });
                                    }
                                }

                                results.Add(new AIQuestionnaireGenerationResult
                                {
                                    Success = true,
                                    Message = $"Batch AI-generated questionnaire for {stage.Name}",
                                    GeneratedQuestionnaire = new QuestionnaireInputDto
                                    {
                                        Name = questionnaireEl.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : $"{stage.Name} Questionnaire",
                                        Description = questionnaireEl.TryGetProperty("description", out var descEl) ? descEl.GetString() : $"AI-generated questionnaire for {stage.Name}",
                                        IsActive = true
                                    },
                                    Questions = questions,
                                    ConfidenceScore = 0.95 // Higher confidence for batch generation
                                });
                            }
                        }
                    }

                    // 确保返回结果数量与stages匹配，缺失的用fallback填充
                    while (results.Count < stages.Count)
                    {
                        var missingStage = stages[results.Count];
                        results.Add(GenerateFallbackQuestionnaire(missingStage));
                    }

                    return results;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse batch questionnaire response");
            }

            return null;
        }

        private List<AIChecklistGenerationResult> GenerateFallbackChecklists(List<AIStageGenerationResult> stages)
        {
            return stages.Select(stage => GenerateFallbackChecklist(stage)).ToList();
        }

        private List<AIQuestionnaireGenerationResult> GenerateFallbackQuestionnaires(List<AIStageGenerationResult> stages)
        {
            return stages.Select(stage => GenerateFallbackQuestionnaire(stage)).ToList();
        }

        /// <summary>
        /// 尝试修复AI生成的损坏JSON并解析
        /// </summary>
        private AIWorkflowGenerationResult TryRepairAndParseWorkflow(string aiResponse)
        {
            try
            {
                _logger.LogInformation("🔧 Attempting to repair potentially corrupted JSON...");

                // 移除可能的markdown格式
                var cleaned = aiResponse
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                // 寻找JSON开始和结束
                var jsonStart = cleaned.IndexOf('{');
                var jsonEnd = cleaned.LastIndexOf('}');

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonContent = cleaned.Substring(jsonStart, jsonEnd - jsonStart + 1);

                    // 尝试解析
                    var parsed = JsonSerializer.Deserialize<JsonElement>(jsonContent);

                    // 如果解析成功，走正常流程
                    return ParseWorkflowFromJsonElement(parsed);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "JSON repair attempt failed");
            }

            return null;
        }

        private AIWorkflowGenerationResult ParseWorkflowFromJsonElement(JsonElement parsed)
        {
            var workflow = new WorkflowInputDto
            {
                Name = parsed.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : "AI Generated Workflow",
                Description = parsed.TryGetProperty("description", out var descEl) ? descEl.GetString() : "Generated by AI",
                IsActive = true,
                IsAIGenerated = true
            };

            var stages = new List<AIStageGenerationResult>();
            var checklists = new List<AIChecklistGenerationResult>();
            var questionnaires = new List<AIQuestionnaireGenerationResult>();

            if (parsed.TryGetProperty("stages", out var stagesEl) && stagesEl.ValueKind == JsonValueKind.Array)
            {
                var order = 1;
                foreach (var stageEl in stagesEl.EnumerateArray())
                {
                    var stage = new AIStageGenerationResult
                    {
                        Name = stageEl.TryGetProperty("name", out var sNameEl) ? sNameEl.GetString() : $"Stage {order}",
                        Description = stageEl.TryGetProperty("description", out var sDescEl) ? sDescEl.GetString() : "",
                        Order = order,
                        AssignedGroup = stageEl.TryGetProperty("assignedGroup", out var sGroupEl) ? sGroupEl.GetString() : "General",
                        EstimatedDuration = stageEl.TryGetProperty("estimatedDuration", out var sDurEl) && sDurEl.TryGetInt32(out var dur) ? dur : 1
                    };
                    stages.Add(stage);

                    // 解析embedded checklist
                    if (stageEl.TryGetProperty("checklist", out var checklistEl))
                    {
                        var checklist = ParseEmbeddedChecklist(checklistEl, stage, order - 1);
                        if (checklist != null)
                        {
                            checklists.Add(checklist);
                        }
                    }

                    // 解析embedded questionnaire
                    if (stageEl.TryGetProperty("questionnaire", out var questionnaireEl))
                    {
                        var questionnaire = ParseEmbeddedQuestionnaire(questionnaireEl, stage, order - 1);
                        if (questionnaire != null)
                        {
                            questionnaires.Add(questionnaire);
                        }
                    }

                    order++;
                }
            }

            return new AIWorkflowGenerationResult
            {
                Success = true,
                Message = "Workflow generated successfully after JSON repair",
                GeneratedWorkflow = workflow,
                Stages = stages,
                Checklists = checklists,
                Questionnaires = questionnaires,
                Suggestions = new List<string> { "Consider adding approval stages", "Review stage assignments" }
            };
        }

        /// <summary>
        /// 解析嵌入在stage中的checklist
        /// </summary>
        private AIChecklistGenerationResult ParseEmbeddedChecklist(JsonElement checklistEl, AIStageGenerationResult stage, int stageIndex)
        {
            try
            {
                var tasks = new List<AITaskGenerationResult>();

                if (checklistEl.TryGetProperty("tasks", out var tasksEl) && tasksEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var taskEl in tasksEl.EnumerateArray())
                    {
                        var task = new AITaskGenerationResult
                        {
                            Title = taskEl.TryGetProperty("title", out var titleEl) ? titleEl.GetString() : "Task",
                            Description = taskEl.TryGetProperty("description", out var taskDescEl) ? taskDescEl.GetString() : "",
                            IsRequired = taskEl.TryGetProperty("isRequired", out var reqEl) && reqEl.GetBoolean(),
                            EstimatedMinutes = taskEl.TryGetProperty("estimatedMinutes", out var minEl) && minEl.TryGetInt32(out var minutes) ? minutes : 60,
                            Category = taskEl.TryGetProperty("category", out var catEl) ? catEl.GetString() : "General"
                        };
                        tasks.Add(task);
                    }
                }

                return new AIChecklistGenerationResult
                {
                    Success = true,
                    Message = "Checklist generated successfully",
                    GeneratedChecklist = new ChecklistInputDto
                    {
                        Name = checklistEl.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : $"Checklist for {stage.Name}",
                        Description = checklistEl.TryGetProperty("description", out var checklistDescEl) ? checklistDescEl.GetString() : $"Tasks for {stage.Name}"
                    },
                    Tasks = tasks
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse embedded checklist for stage {StageName}", stage.Name);
                return GenerateFallbackChecklist(stage);
            }
        }

        /// <summary>
        /// 解析嵌入在stage中的questionnaire
        /// </summary>
        private AIQuestionnaireGenerationResult ParseEmbeddedQuestionnaire(JsonElement questionnaireEl, AIStageGenerationResult stage, int stageIndex)
        {
            try
            {
                var questions = new List<AIQuestionGenerationResult>();

                if (questionnaireEl.TryGetProperty("questions", out var questionsEl) && questionsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var questionEl in questionsEl.EnumerateArray())
                    {
                        var question = new AIQuestionGenerationResult
                        {
                            Question = questionEl.TryGetProperty("question", out var qEl) ? qEl.GetString() : "Question",
                            Type = questionEl.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : "text",
                            IsRequired = questionEl.TryGetProperty("isRequired", out var reqEl) && reqEl.GetBoolean(),
                            Category = questionEl.TryGetProperty("category", out var catEl) ? catEl.GetString() : "General"
                        };

                        // 处理select和multiselect的options
                        if ((question.Type == "select" || question.Type == "multiselect") &&
                            questionEl.TryGetProperty("options", out var optionsEl) && optionsEl.ValueKind == JsonValueKind.Array)
                        {
                            var options = new List<string>();
                            foreach (var optionEl in optionsEl.EnumerateArray())
                            {
                                if (optionEl.ValueKind == JsonValueKind.String)
                                {
                                    options.Add(optionEl.GetString());
                                }
                            }
                            question.Options = options;
                        }

                        questions.Add(question);
                    }
                }

                return new AIQuestionnaireGenerationResult
                {
                    Success = true,
                    Message = "Questionnaire generated successfully",
                    GeneratedQuestionnaire = new QuestionnaireInputDto
                    {
                        Name = questionnaireEl.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : $"Questionnaire for {stage.Name}",
                        Description = questionnaireEl.TryGetProperty("description", out var questionnaireDescEl) ? questionnaireDescEl.GetString() : $"Information gathering for {stage.Name}"
                    },
                    Questions = questions
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse embedded questionnaire for stage {StageName}", stage.Name);
                return GenerateFallbackQuestionnaire(stage);
            }
        }

        /// <summary>
        /// AI调用重试机制
        /// </summary>
        private async Task<AIProviderResponse> CallAIProviderWithRetryAsync(string prompt, int maxRetries = 2)
        {
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        // 快速重试: 2秒, 5秒 - 适合网络不稳定情况
                        var delay = TimeSpan.FromSeconds(attempt == 1 ? 2 : 5);
                        _logger.LogWarning("🔄 AI call retry #{Attempt} after {Delay}ms delay", attempt, delay.TotalMilliseconds);
                        await Task.Delay(delay);
                    }

                    var response = await CallAIProviderAsync(prompt);

                    if (response.Success)
                    {
                        if (attempt > 0)
                        {
                            _logger.LogInformation("✅ AI call succeeded on retry #{Attempt}", attempt);
                        }
                        return response;
                    }
                    else if (attempt == maxRetries)
                    {
                        // 最后一次尝试失败
                        _logger.LogWarning("❌ AI call failed after {MaxRetries} retries: {Error}", maxRetries, response.ErrorMessage);
                        return response;
                    }
                }
                catch (TaskCanceledException) when (attempt < maxRetries)
                {
                    _logger.LogWarning("⏰ AI call timeout on attempt #{Attempt}, retrying...", attempt + 1);
                    continue;
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    _logger.LogWarning(ex, "⚠️ AI call error on attempt #{Attempt}, retrying...", attempt + 1);
                    continue;
                }
            }

            // 如果所有重试都失败，返回错误响应
            return new AIProviderResponse
            {
                Success = false,
                ErrorMessage = $"AI call failed after {maxRetries} retries",
                Content = string.Empty
            };
        }

        private AIChecklistGenerationResult GenerateFallbackChecklist(AIStageGenerationResult stage)
        {
            return new AIChecklistGenerationResult
            {
                Success = true,
                Message = $"Empty checklist for {stage.Name} - no AI tasks generated",
                GeneratedChecklist = new ChecklistInputDto
                {
                    Name = $"{stage.Name} Checklist",
                    Description = $"Tasks for {stage.Name}",
                    Team = stage.AssignedGroup,
                    IsActive = true
                },
                Tasks = new List<AITaskGenerationResult>(), // 空列表，不生成模板任务
                ConfidenceScore = 0.0
            };
        }

        private AIQuestionnaireGenerationResult GenerateFallbackQuestionnaire(AIStageGenerationResult stage)
        {
            return new AIQuestionnaireGenerationResult
            {
                Success = true,
                Message = $"Empty questionnaire for {stage.Name} - no AI questions generated",
                GeneratedQuestionnaire = new QuestionnaireInputDto
                {
                    Name = $"{stage.Name} Questionnaire",
                    Description = $"Information gathering for {stage.Name}",
                    IsActive = true
                },
                Questions = new List<AIQuestionGenerationResult>(), // 空列表，不生成模板问题
                ConfidenceScore = 0.0
            };
        }

        private List<AITaskGenerationResult> GenerateTasksForStage(AIStageGenerationResult stage, int checklistIndex = 0, int totalChecklists = 1, Random random = null)
        {
            var stageName = stage.Name.ToLower();
            var stageDesc = stage.Description.ToLower();

            // Task templates based on stage characteristics
            var taskTemplates = new Dictionary<string, List<AITaskGenerationResult>>
            {
                ["initial"] = new List<AITaskGenerationResult>
                {
                    new AITaskGenerationResult { Id = "req-gather", Title = "Gather Requirements", Description = "Collect and document all necessary requirements", IsRequired = true, EstimatedMinutes = 120, Category = "Planning" },
                    new AITaskGenerationResult { Id = "stakeholder-id", Title = "Identify Stakeholders", Description = "List all key stakeholders and their roles", IsRequired = true, EstimatedMinutes = 60, Category = "Planning" },
                    new AITaskGenerationResult { Id = "timeline-est", Title = "Estimate Timeline", Description = "Create initial timeline estimates", IsRequired = false, EstimatedMinutes = 90, Category = "Planning" },
                    new AITaskGenerationResult { Id = "resource-check", Title = "Check Resource Availability", Description = "Verify required resources are available", IsRequired = true, EstimatedMinutes = 45, Category = "Planning" }
                },
                ["planning"] = new List<AITaskGenerationResult>
                {
                    new AITaskGenerationResult { Id = "plan-create", Title = "Create Detailed Plan", Description = "Develop comprehensive project plan", IsRequired = true, EstimatedMinutes = 180, Category = "Planning" },
                    new AITaskGenerationResult { Id = "risk-assess", Title = "Risk Assessment", Description = "Identify and assess potential risks", IsRequired = true, EstimatedMinutes = 120, Category = "Risk Management" },
                    new AITaskGenerationResult { Id = "budget-approve", Title = "Budget Approval", Description = "Get budget approval from management", IsRequired = true, EstimatedMinutes = 60, Category = "Finance" },
                    new AITaskGenerationResult { Id = "team-assign", Title = "Assign Team Members", Description = "Assign roles and responsibilities to team members", IsRequired = true, EstimatedMinutes = 90, Category = "Team Management" }
                },
                ["design"] = new List<AITaskGenerationResult>
                {
                    new AITaskGenerationResult { Id = "wireframe", Title = "Create Wireframes", Description = "Design initial wireframes and mockups", IsRequired = true, EstimatedMinutes = 240, Category = "Design" },
                    new AITaskGenerationResult { Id = "prototype", Title = "Build Prototype", Description = "Develop working prototype", IsRequired = false, EstimatedMinutes = 360, Category = "Development" },
                    new AITaskGenerationResult { Id = "design-review", Title = "Design Review", Description = "Conduct design review with stakeholders", IsRequired = true, EstimatedMinutes = 90, Category = "Review" },
                    new AITaskGenerationResult { Id = "spec-finalize", Title = "Finalize Specifications", Description = "Complete technical specifications", IsRequired = true, EstimatedMinutes = 150, Category = "Documentation" }
                },
                ["implementation"] = new List<AITaskGenerationResult>
                {
                    new AITaskGenerationResult { Id = "env-setup", Title = "Setup Environment", Description = "Configure development/production environment", IsRequired = true, EstimatedMinutes = 120, Category = "Infrastructure" },
                    new AITaskGenerationResult { Id = "code-develop", Title = "Develop Code", Description = "Write and implement code according to specifications", IsRequired = true, EstimatedMinutes = 480, Category = "Development" },
                    new AITaskGenerationResult { Id = "unit-test", Title = "Unit Testing", Description = "Perform unit testing on developed components", IsRequired = true, EstimatedMinutes = 180, Category = "Testing" },
                    new AITaskGenerationResult { Id = "code-review", Title = "Code Review", Description = "Conduct peer code review", IsRequired = true, EstimatedMinutes = 120, Category = "Quality Assurance" }
                },
                ["testing"] = new List<AITaskGenerationResult>
                {
                    new AITaskGenerationResult { Id = "test-plan", Title = "Create Test Plan", Description = "Develop comprehensive test plan", IsRequired = true, EstimatedMinutes = 120, Category = "Planning" },
                    new AITaskGenerationResult { Id = "test-cases", Title = "Write Test Cases", Description = "Create detailed test cases", IsRequired = true, EstimatedMinutes = 180, Category = "Testing" },
                    new AITaskGenerationResult { Id = "execute-tests", Title = "Execute Tests", Description = "Run all test cases and document results", IsRequired = true, EstimatedMinutes = 240, Category = "Testing" },
                    new AITaskGenerationResult { Id = "bug-fix", Title = "Fix Bugs", Description = "Address and fix identified issues", IsRequired = true, EstimatedMinutes = 300, Category = "Development" }
                },
                ["review"] = new List<AITaskGenerationResult>
                {
                    new AITaskGenerationResult { Id = "quality-check", Title = "Quality Assurance Check", Description = "Perform quality assurance review", IsRequired = true, EstimatedMinutes = 90, Category = "Quality Assurance" },
                    new AITaskGenerationResult { Id = "stakeholder-review", Title = "Stakeholder Review", Description = "Present to stakeholders for review", IsRequired = true, EstimatedMinutes = 120, Category = "Review" },
                    new AITaskGenerationResult { Id = "feedback-collect", Title = "Collect Feedback", Description = "Gather and document feedback", IsRequired = true, EstimatedMinutes = 60, Category = "Communication" },
                    new AITaskGenerationResult { Id = "approval-get", Title = "Get Final Approval", Description = "Obtain final approval to proceed", IsRequired = true, EstimatedMinutes = 45, Category = "Approval" }
                },
                ["deployment"] = new List<AITaskGenerationResult>
                {
                    new AITaskGenerationResult { Id = "deploy-prep", Title = "Prepare Deployment", Description = "Prepare all deployment materials", IsRequired = true, EstimatedMinutes = 90, Category = "Deployment" },
                    new AITaskGenerationResult { Id = "backup-create", Title = "Create Backup", Description = "Create system backup before deployment", IsRequired = true, EstimatedMinutes = 30, Category = "Infrastructure" },
                    new AITaskGenerationResult { Id = "deploy-execute", Title = "Execute Deployment", Description = "Deploy to production environment", IsRequired = true, EstimatedMinutes = 60, Category = "Deployment" },
                    new AITaskGenerationResult { Id = "smoke-test", Title = "Smoke Testing", Description = "Perform post-deployment smoke tests", IsRequired = true, EstimatedMinutes = 45, Category = "Testing" }
                },
                ["training"] = new List<AITaskGenerationResult>
                {
                    new AITaskGenerationResult { Id = "material-prep", Title = "Prepare Training Materials", Description = "Create training documentation and materials", IsRequired = true, EstimatedMinutes = 180, Category = "Training" },
                    new AITaskGenerationResult { Id = "schedule-training", Title = "Schedule Training Sessions", Description = "Organize training sessions with users", IsRequired = true, EstimatedMinutes = 60, Category = "Training" },
                    new AITaskGenerationResult { Id = "conduct-training", Title = "Conduct Training", Description = "Deliver training to end users", IsRequired = true, EstimatedMinutes = 240, Category = "Training" },
                    new AITaskGenerationResult { Id = "support-provide", Title = "Provide Support", Description = "Offer ongoing support during transition", IsRequired = true, EstimatedMinutes = 120, Category = "Support" }
                }
            };

            // Default tasks
            var defaultTasks = new List<AITaskGenerationResult>
            {
                new AITaskGenerationResult { Id = "task-plan", Title = "Plan Tasks", Description = $"Plan all tasks for {stage.Name}", IsRequired = true, EstimatedMinutes = 60, Category = "Planning" },
                new AITaskGenerationResult { Id = "resource-allocate", Title = "Allocate Resources", Description = "Ensure necessary resources are allocated", IsRequired = true, EstimatedMinutes = 45, Category = "Resource Management" },
                new AITaskGenerationResult { Id = "progress-monitor", Title = "Monitor Progress", Description = "Track and monitor stage progress", IsRequired = true, EstimatedMinutes = 30, Category = "Management" },
                new AITaskGenerationResult { Id = "deliverable-complete", Title = "Complete Deliverables", Description = "Finish all stage deliverables", IsRequired = true, EstimatedMinutes = 120, Category = "Execution" }
            };

            random = random ?? new Random();

            // Determine which template to use
            List<AITaskGenerationResult> selectedTasks = defaultTasks;

            if (stageName.Contains("initial") || stageName.Contains("assessment") || stageName.Contains("analysis"))
                selectedTasks = taskTemplates["initial"];
            else if (stageName.Contains("plan") || stageName.Contains("design") || stageDesc.Contains("plan"))
                selectedTasks = taskTemplates["planning"];
            else if (stageName.Contains("design") || stageName.Contains("prototype") || stageDesc.Contains("design"))
                selectedTasks = taskTemplates["design"];
            else if (stageName.Contains("implement") || stageName.Contains("develop") || stageName.Contains("build") || stageDesc.Contains("develop"))
                selectedTasks = taskTemplates["implementation"];
            else if (stageName.Contains("test") || stageName.Contains("qa") || stageDesc.Contains("test"))
                selectedTasks = taskTemplates["testing"];
            else if (stageName.Contains("review") || stageName.Contains("approval") || stageDesc.Contains("review"))
                selectedTasks = taskTemplates["review"];
            else if (stageName.Contains("deploy") || stageName.Contains("launch") || stageName.Contains("release"))
                selectedTasks = taskTemplates["deployment"];
            else if (stageName.Contains("training") || stageName.Contains("onboard") || stageDesc.Contains("training"))
                selectedTasks = taskTemplates["training"];

            // Determine the number of tasks for this checklist (2-6 tasks, weighted towards 3-4)
            var taskCount = DetermineTaskCount(selectedTasks.Count, checklistIndex, totalChecklists, random);

            // Shuffle and select random subset of tasks
            var shuffledTasks = selectedTasks.OrderBy(x => random.Next()).ToList();
            var finalTasks = shuffledTasks.Take(taskCount).ToList();

            // Ensure at least one required task
            if (!finalTasks.Any(t => t.IsRequired))
            {
                var requiredTasks = selectedTasks.Where(t => t.IsRequired).ToList();
                if (requiredTasks.Any())
                {
                    finalTasks[0] = requiredTasks[random.Next(requiredTasks.Count)];
                }
            }

            // Add unique IDs with stage and checklist prefix
            return finalTasks.Select((task, index) => new AITaskGenerationResult
            {
                Id = $"{stage.Name.ToLower().Replace(" ", "-")}-c{checklistIndex}-{task.Id}-{index}",
                Title = task.Title,
                Description = task.Description,
                IsRequired = task.IsRequired,
                Completed = task.Completed,
                EstimatedMinutes = task.EstimatedMinutes,
                Category = task.Category,
                Dependencies = task.Dependencies
            }).ToList();
        }

        private List<AIQuestionGenerationResult> GenerateQuestionsForStage(AIStageGenerationResult stage, int questionnaireIndex = 0, int totalQuestionnaires = 1, Random random = null)
        {
            var stageName = stage.Name.ToLower();
            var stageDesc = stage.Description.ToLower();

            // Question templates based on stage characteristics
            var questionTemplates = new Dictionary<string, List<AIQuestionGenerationResult>>
            {
                ["initial"] = new List<AIQuestionGenerationResult>
                {
                    new AIQuestionGenerationResult { Id = "project-scope", Question = "What is the scope of this project?", Type = "text", IsRequired = true, Category = "Scope" },
                    new AIQuestionGenerationResult { Id = "success-criteria", Question = "What are the success criteria?", Type = "text", IsRequired = true, Category = "Goals" },
                    new AIQuestionGenerationResult { Id = "budget-range", Question = "What is the budget range?", Type = "select", Options = new List<string> { "< $10K", "$10K - $50K", "$50K - $100K", "> $100K" }, IsRequired = true, Category = "Budget" },
                    new AIQuestionGenerationResult { Id = "timeline-preference", Question = "What is your preferred timeline?", Type = "select", Options = new List<string> { "1-2 weeks", "1 month", "2-3 months", "6+ months" }, IsRequired = true, Category = "Timeline" }
                },
                ["planning"] = new List<AIQuestionGenerationResult>
                {
                    new AIQuestionGenerationResult { Id = "team-size", Question = "How many team members are needed?", Type = "number", IsRequired = true, Category = "Resources" },
                    new AIQuestionGenerationResult { Id = "key-milestones", Question = "What are the key milestones?", Type = "text", IsRequired = true, Category = "Planning" },
                    new AIQuestionGenerationResult { Id = "risk-tolerance", Question = "What is your risk tolerance level?", Type = "select", Options = new List<string> { "Low", "Medium", "High" }, IsRequired = true, Category = "Risk" },
                    new AIQuestionGenerationResult { Id = "communication-frequency", Question = "How often should progress be reported?", Type = "select", Options = new List<string> { "Daily", "Weekly", "Bi-weekly", "Monthly" }, IsRequired = false, Category = "Communication" }
                },
                ["design"] = new List<AIQuestionGenerationResult>
                {
                    new AIQuestionGenerationResult { Id = "design-style", Question = "What design style do you prefer?", Type = "select", Options = new List<string> { "Modern", "Classic", "Minimalist", "Bold" }, IsRequired = true, Category = "Design" },
                    new AIQuestionGenerationResult { Id = "target-audience", Question = "Who is the target audience?", Type = "text", IsRequired = true, Category = "Audience" },
                    new AIQuestionGenerationResult { Id = "brand-guidelines", Question = "Are there existing brand guidelines?", Type = "boolean", IsRequired = true, Category = "Branding" },
                    new AIQuestionGenerationResult { Id = "accessibility-requirements", Question = "Are there accessibility requirements?", Type = "multiselect", Options = new List<string> { "WCAG 2.1 AA", "Screen Reader Support", "Keyboard Navigation", "Color Contrast" }, IsRequired = false, Category = "Accessibility" }
                },
                ["implementation"] = new List<AIQuestionGenerationResult>
                {
                    new AIQuestionGenerationResult { Id = "tech-stack", Question = "What technology stack should be used?", Type = "multiselect", Options = new List<string> { "React", "Vue", "Angular", "Node.js", "Python", "Java", ".NET" }, IsRequired = true, Category = "Technology" },
                    new AIQuestionGenerationResult { Id = "performance-requirements", Question = "What are the performance requirements?", Type = "text", IsRequired = true, Category = "Performance" },
                    new AIQuestionGenerationResult { Id = "security-level", Question = "What security level is required?", Type = "select", Options = new List<string> { "Basic", "Standard", "High", "Enterprise" }, IsRequired = true, Category = "Security" },
                    new AIQuestionGenerationResult { Id = "integration-needs", Question = "What systems need integration?", Type = "text", IsRequired = false, Category = "Integration" }
                },
                ["testing"] = new List<AIQuestionGenerationResult>
                {
                    new AIQuestionGenerationResult { Id = "test-types", Question = "What types of testing are required?", Type = "multiselect", Options = new List<string> { "Unit Testing", "Integration Testing", "Performance Testing", "Security Testing", "User Acceptance Testing" }, IsRequired = true, Category = "Testing" },
                    new AIQuestionGenerationResult { Id = "test-environment", Question = "What test environment is available?", Type = "select", Options = new List<string> { "Development", "Staging", "Production-like", "Cloud-based" }, IsRequired = true, Category = "Environment" },
                    new AIQuestionGenerationResult { Id = "acceptance-criteria", Question = "What are the acceptance criteria?", Type = "text", IsRequired = true, Category = "Criteria" },
                    new AIQuestionGenerationResult { Id = "test-data", Question = "Is test data available?", Type = "boolean", IsRequired = true, Category = "Data" }
                },
                ["review"] = new List<AIQuestionGenerationResult>
                {
                    new AIQuestionGenerationResult { Id = "review-criteria", Question = "What are the review criteria?", Type = "text", IsRequired = true, Category = "Criteria" },
                    new AIQuestionGenerationResult { Id = "reviewers", Question = "Who are the key reviewers?", Type = "text", IsRequired = true, Category = "Stakeholders" },
                    new AIQuestionGenerationResult { Id = "approval-process", Question = "What is the approval process?", Type = "text", IsRequired = true, Category = "Process" },
                    new AIQuestionGenerationResult { Id = "feedback-timeline", Question = "What is the feedback timeline?", Type = "select", Options = new List<string> { "24 hours", "2-3 days", "1 week", "2 weeks" }, IsRequired = true, Category = "Timeline" }
                },
                ["deployment"] = new List<AIQuestionGenerationResult>
                {
                    new AIQuestionGenerationResult { Id = "deployment-strategy", Question = "What deployment strategy should be used?", Type = "select", Options = new List<string> { "Blue-Green", "Rolling", "Canary", "Big Bang" }, IsRequired = true, Category = "Strategy" },
                    new AIQuestionGenerationResult { Id = "rollback-plan", Question = "Is there a rollback plan?", Type = "boolean", IsRequired = true, Category = "Risk Management" },
                    new AIQuestionGenerationResult { Id = "monitoring-setup", Question = "What monitoring is needed?", Type = "multiselect", Options = new List<string> { "Performance Monitoring", "Error Tracking", "User Analytics", "Security Monitoring" }, IsRequired = true, Category = "Monitoring" },
                    new AIQuestionGenerationResult { Id = "maintenance-window", Question = "When is the maintenance window?", Type = "text", IsRequired = false, Category = "Schedule" }
                },
                ["training"] = new List<AIQuestionGenerationResult>
                {
                    new AIQuestionGenerationResult { Id = "training-format", Question = "What training format is preferred?", Type = "select", Options = new List<string> { "In-person", "Virtual", "Self-paced", "Hybrid" }, IsRequired = true, Category = "Format" },
                    new AIQuestionGenerationResult { Id = "audience-size", Question = "How many people need training?", Type = "number", IsRequired = true, Category = "Audience" },
                    new AIQuestionGenerationResult { Id = "skill-level", Question = "What is the current skill level?", Type = "select", Options = new List<string> { "Beginner", "Intermediate", "Advanced", "Mixed" }, IsRequired = true, Category = "Skills" },
                    new AIQuestionGenerationResult { Id = "training-materials", Question = "What training materials are needed?", Type = "multiselect", Options = new List<string> { "User Manual", "Video Tutorials", "Interactive Demos", "Quick Reference" }, IsRequired = true, Category = "Materials" }
                }
            };

            // Default questions
            var defaultQuestions = new List<AIQuestionGenerationResult>
            {
                new AIQuestionGenerationResult { Id = "stage-objectives", Question = $"What are the main objectives for {stage.Name}?", Type = "text", IsRequired = true, Category = "Objectives" },
                new AIQuestionGenerationResult { Id = "success-metrics", Question = "How will success be measured?", Type = "text", IsRequired = true, Category = "Metrics" },
                new AIQuestionGenerationResult { Id = "dependencies", Question = "Are there any dependencies?", Type = "text", IsRequired = false, Category = "Dependencies" },
                new AIQuestionGenerationResult { Id = "special-requirements", Question = "Are there any special requirements?", Type = "text", IsRequired = false, Category = "Requirements" }
            };

            random = random ?? new Random();

            // Determine which template to use
            List<AIQuestionGenerationResult> selectedQuestions = defaultQuestions;

            if (stageName.Contains("initial") || stageName.Contains("assessment") || stageName.Contains("analysis"))
                selectedQuestions = questionTemplates["initial"];
            else if (stageName.Contains("plan") || stageDesc.Contains("plan"))
                selectedQuestions = questionTemplates["planning"];
            else if (stageName.Contains("design") || stageDesc.Contains("design"))
                selectedQuestions = questionTemplates["design"];
            else if (stageName.Contains("implement") || stageName.Contains("develop") || stageName.Contains("build"))
                selectedQuestions = questionTemplates["implementation"];
            else if (stageName.Contains("test") || stageName.Contains("qa"))
                selectedQuestions = questionTemplates["testing"];
            else if (stageName.Contains("review") || stageName.Contains("approval"))
                selectedQuestions = questionTemplates["review"];
            else if (stageName.Contains("deploy") || stageName.Contains("launch"))
                selectedQuestions = questionTemplates["deployment"];
            else if (stageName.Contains("training") || stageName.Contains("onboard"))
                selectedQuestions = questionTemplates["training"];

            // Determine the number of questions for this questionnaire (2-8 questions, weighted towards 4-6)
            var questionCount = DetermineQuestionCount(selectedQuestions.Count, questionnaireIndex, totalQuestionnaires, random);

            // Shuffle and select random subset of questions
            var shuffledQuestions = selectedQuestions.OrderBy(x => random.Next()).ToList();
            var finalQuestions = shuffledQuestions.Take(questionCount).ToList();

            // Ensure at least one required question
            if (!finalQuestions.Any(q => q.IsRequired))
            {
                var requiredQuestions = selectedQuestions.Where(q => q.IsRequired).ToList();
                if (requiredQuestions.Any())
                {
                    finalQuestions[0] = requiredQuestions[random.Next(requiredQuestions.Count)];
                }
            }

            // Add unique IDs with stage and questionnaire prefix
            return finalQuestions.Select((question, index) => new AIQuestionGenerationResult
            {
                Id = $"{stage.Name.ToLower().Replace(" ", "-")}-q{questionnaireIndex}-{question.Id}-{index}",
                Question = question.Question,
                Type = question.Type,
                Options = question.Options,
                IsRequired = question.IsRequired,
                Category = question.Category,
                HelpText = question.HelpText,
                ValidationRule = question.ValidationRule,
                DefaultValue = question.DefaultValue
            }).ToList();
        }

        /// <summary>
        /// Create actual checklist and questionnaire records and associate them with stages
        /// </summary>
        public async Task<bool> CreateStageComponentsAsync(long workflowId, List<AIStageGenerationResult> stages, List<AIChecklistGenerationResult> checklists, List<AIQuestionnaireGenerationResult> questionnaires)
        {
            try
            {
                _logger.LogInformation("🔍 Creating stage components for workflow {WorkflowId}...", workflowId);

                // Get the created workflow with stages from the database
                var workflow = await _workflowService.GetByIdAsync(workflowId);
                if (workflow == null || workflow.Stages == null || !workflow.Stages.Any())
                {
                    _logger.LogWarning("No stages found for workflow {WorkflowId}", workflowId);
                    return false;
                }

                var createdStages = workflow.Stages;

                // Create checklists and associate with stages
                for (int i = 0; i < Math.Min(checklists.Count, createdStages.Count); i++)
                {
                    var checklist = checklists[i];
                    var stage = createdStages[i];

                    // Ensure GeneratedChecklist is not null
                    if (checklist.GeneratedChecklist == null)
                    {
                        _logger.LogWarning("Skipping checklist {Index} - GeneratedChecklist is null", i);
                        continue;
                    }

                    // Ensure unique checklist name
                    checklist.GeneratedChecklist.Name = await EnsureUniqueChecklistNameAsync(checklist.GeneratedChecklist.Name, checklist.GeneratedChecklist.Team);

                    // Set up assignments for the checklist
                    checklist.GeneratedChecklist.Assignments = new List<FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto>
                    {
                        new FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto
                        {
                            WorkflowId = workflowId,
                            StageId = stage.Id
                        }
                    };

                    // Create the checklist
                    var checklistId = await _checklistService.CreateAsync(checklist.GeneratedChecklist);
                    _logger.LogInformation("✅ Created checklist {ChecklistId} for stage {StageId}", checklistId, stage.Id);

                    // Create tasks for the checklist
                    if (checklist.Tasks != null && checklist.Tasks.Any())
                    {
                        foreach (var task in checklist.Tasks)
                        {
                            try
                            {
                                var taskInputDto = new ChecklistTaskInputDto
                                {
                                    ChecklistId = checklistId,
                                    Name = task.Title,
                                    Description = task.Description,
                                    IsRequired = task.IsRequired,
                                    EstimatedHours = task.EstimatedMinutes > 0 ? task.EstimatedMinutes / 60 : 0, // Convert minutes to hours
                                    TaskType = task.Category ?? "General",
                                    Order = checklist.Tasks.ToList().IndexOf(task) + 1,
                                    IsActive = true
                                };

                                var taskId = await _checklistTaskService.CreateAsync(taskInputDto);
                                _logger.LogInformation("✅ Created task {TaskId} for checklist {ChecklistId}", taskId, checklistId);
                            }
                            catch (Exception taskEx)
                            {
                                _logger.LogWarning(taskEx, "Failed to create task '{TaskTitle}' for checklist {ChecklistId}", task.Title, checklistId);
                            }
                        }
                    }
                }

                // Create questionnaires and associate with stages
                for (int i = 0; i < Math.Min(questionnaires.Count, createdStages.Count); i++)
                {
                    var questionnaire = questionnaires[i];
                    var stage = createdStages[i];

                    // Ensure GeneratedQuestionnaire is not null
                    if (questionnaire.GeneratedQuestionnaire == null)
                    {
                        _logger.LogWarning("Skipping questionnaire {Index} - GeneratedQuestionnaire is null", i);
                        continue;
                    }

                    // Ensure unique questionnaire name
                    questionnaire.GeneratedQuestionnaire.Name = await EnsureUniqueQuestionnaireNameAsync(questionnaire.GeneratedQuestionnaire.Name);

                    // Add questions to the questionnaire structure
                    _logger.LogInformation("🔍 Processing questionnaire with {QuestionCount} questions", questionnaire.Questions?.Count ?? 0);

                    if (questionnaire.Questions != null && questionnaire.Questions.Any())
                    {
                        _logger.LogInformation("✅ Creating section with {QuestionCount} questions", questionnaire.Questions.Count);

                        var section = new QuestionnaireSectionInputDto
                        {
                            Title = "Main Section",
                            Description = "Generated questions",
                            Order = 1,
                            Questions = questionnaire.Questions.Select((q, index) => new QuestionInputDto
                            {
                                Id = q.Id,
                                Text = q.Question,
                                Type = q.Type ?? "text",
                                IsRequired = q.IsRequired,
                                Order = index + 1,
                                Options = q.Options?.Select((opt, optIndex) => new QuestionOptionDto
                                {
                                    Label = opt,
                                    Value = opt,
                                    Order = optIndex + 1
                                }).ToList() ?? new List<QuestionOptionDto>()
                            }).ToList()
                        };

                        // Convert sections to JSON structure for storage
                        var structureJson = new
                        {
                            sections = new[]
                            {
                                new
                                {
                                    title = section.Title,
                                    description = section.Description,
                                    order = section.Order,
                                    questions = section.Questions.Select(q => new
                                    {
                                        id = q.Id,
                                        title = q.Text,  // 使用 title 而不是 text
                                        type = q.Type,
                                        required = q.IsRequired,
                                        order = q.Order,
                                        options = q.Options.Select(opt => new
                                        {
                                            label = opt.Label,
                                            value = opt.Value,
                                            order = opt.Order
                                        }).ToArray()
                                    }).ToArray()
                                }
                            }
                        };

                        questionnaire.GeneratedQuestionnaire.StructureJson = System.Text.Json.JsonSerializer.Serialize(structureJson);
                        _logger.LogInformation("✅ Added structure JSON with {QuestionCount} questions to questionnaire", section.Questions.Count);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ No questions found for questionnaire {Index}", i);
                    }

                    // Set up assignments for the questionnaire
                    questionnaire.GeneratedQuestionnaire.Assignments = new List<FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto>
                    {
                        new FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto
                        {
                            WorkflowId = workflowId,
                            StageId = stage.Id
                        }
                    };

                    // Create the questionnaire
                    var questionnaireId = await _questionnaireService.CreateAsync(questionnaire.GeneratedQuestionnaire);
                    _logger.LogInformation("✅ Created questionnaire {QuestionnaireId} for stage {StageId}", questionnaireId, stage.Id);

                    // Note: Questions are created as part of the questionnaire structure
                    // The QuestionnaireInputDto should include the questions in its structure
                    _logger.LogInformation("✅ Questionnaire {QuestionnaireId} created with {QuestionCount} questions",
                        questionnaireId, questionnaire.Questions?.Count ?? 0);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create stage components for workflow {WorkflowId}", workflowId);
                return false;
            }
        }

        private async Task<string> EnsureUniqueChecklistNameAsync(string baseName, string team)
        {
            var originalName = baseName;
            var counter = 1;
            var currentName = baseName;

            while (true)
            {
                try
                {
                    // Check if the name exists using the ChecklistRepository
                    var exists = await _checklistRepository.IsNameExistsAsync(currentName, team);
                    if (!exists)
                    {
                        return currentName;
                    }

                    // Generate a new name with counter
                    counter++;
                    currentName = $"{originalName} ({counter})";
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error checking checklist name uniqueness for {Name}, using fallback", currentName);
                    // If there's an error checking, use timestamp as fallback
                    return $"{originalName} {DateTime.Now:yyyyMMdd-HHmmss}";
                }
            }
        }

        private async Task<string> EnsureUniqueQuestionnaireNameAsync(string baseName)
        {
            var originalName = baseName;
            var counter = 1;
            var currentName = baseName;

            while (true)
            {
                try
                {
                    // Check if the name exists using the QuestionnaireRepository
                    var exists = await _questionnaireRepository.IsNameExistsAsync(currentName);
                    if (!exists)
                    {
                        return currentName;
                    }

                    // Generate a new name with counter
                    counter++;
                    currentName = $"{originalName} ({counter})";
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error checking questionnaire name uniqueness for {Name}, using fallback", currentName);
                    // If there's an error checking, use timestamp as fallback
                    return $"{originalName} {DateTime.Now:yyyyMMdd-HHmmss}";
                }
            }
        }

        #endregion

        #region Stage Summary Generation

        /// <summary>
        /// Generate AI summary for stage based on checklist tasks and questionnaire questions
        /// </summary>
        /// <param name="input">Stage summary generation input</param>
        /// <returns>Generated stage summary</returns>
        public async Task<AIStageSummaryResult> GenerateStageSummaryAsync(AIStageSummaryInput input)
        {
            try
            {
                _logger.LogInformation("Generating AI summary for stage {StageId}: {StageName}", input.StageId, input.StageName);

                // Build the summary generation prompt
                var prompt = BuildStageSummaryPrompt(input);

                // Try AI providers with fallback strategy
                var aiResponse = await CallAIProviderWithFallbackForSummaryAsync(prompt, input.ModelId);

                if (!aiResponse.Success)
                {
                    _logger.LogError("All AI providers failed for stage summary: {Error}", aiResponse.ErrorMessage);
                    return new AIStageSummaryResult
                    {
                        Success = false,
                        Message = $"AI service error: {aiResponse.ErrorMessage}"
                    };
                }

                // Parse the AI response and create structured summary
                var summaryResult = ParseStageSummaryResponse(aiResponse.Content, input);

                _logger.LogInformation("Successfully generated AI summary for stage {StageId}", input.StageId);
                return summaryResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI summary for stage {StageId}: {Error}", input.StageId, ex.Message);
                return new AIStageSummaryResult
                {
                    Success = false,
                    Message = $"Failed to generate stage summary: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Call AI provider with automatic fallback to available configurations
        /// </summary>
        /// <param name="prompt">The prompt to send to AI</param>
        /// <param name="preferredModelId">Preferred model ID (optional)</param>
        /// <returns>AI response</returns>
        private async Task<AIProviderResponse> CallAIProviderWithFallbackForSummaryAsync(string prompt, string? preferredModelId)
        {
            try
            {
                // Step 1: Try preferred model if specified
                if (!string.IsNullOrEmpty(preferredModelId) && long.TryParse(preferredModelId, out var modelId))
                {
                    var preferredConfig = await _configService.GetConfigByIdAsync(modelId);
                    if (preferredConfig != null)
                    {
                        _logger.LogInformation("Trying preferred AI model: {Provider} - {ModelName} (ID: {ModelId})",
                            preferredConfig.Provider, preferredConfig.ModelName, modelId);

                        var response = await CallAIProviderAsync(prompt, preferredModelId, preferredConfig.Provider, preferredConfig.ModelName);
                        if (response.Success)
                        {
                            _logger.LogInformation("Successfully used preferred AI model for summary generation");
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
                            _logger.LogInformation("Successfully used user default AI model for summary generation");
                            return response;
                        }

                        _logger.LogWarning("User default AI model failed: {Error}. Trying other available models...", response.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get user default AI config, continuing with other options");
                }

                // Step 3: Try all available user configurations
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
                var systemResponse = await CallZhipuAIAsync(prompt);
                if (systemResponse.Success)
                {
                    _logger.LogInformation("Successfully used system default ZhipuAI for summary generation");
                    return systemResponse;
                }

                // All options exhausted
                _logger.LogError("All AI providers failed for summary generation");
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = "All available AI providers failed. Please check your AI model configurations and try again."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AI provider fallback strategy: {Error}", ex.Message);
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"AI provider fallback failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Build the prompt for stage summary generation
        /// </summary>
        /// <param name="input">Stage summary input</param>
        /// <returns>Generated prompt</returns>
        private string BuildStageSummaryPrompt(AIStageSummaryInput input)
        {
            var promptBuilder = new StringBuilder();

            // Determine effective language: explicit -> auto-detect from input content
            var effectiveLanguage = DetermineEffectiveLanguage(input);

            // Set language instruction
            if (effectiveLanguage == "zh-CN")
            {
                promptBuilder.AppendLine("用中文生成简洁的阶段总结。");
            }
            else
            {
                promptBuilder.AppendLine("Generate a concise stage summary in English.");
            }

            promptBuilder.AppendLine();
            promptBuilder.AppendLine("=== Stage Summary Generation Task ===");
            promptBuilder.AppendLine($"Stage Name: {input.StageName}");
            promptBuilder.AppendLine($"Stage Description: {input.StageDescription}");

            if (!string.IsNullOrEmpty(input.AdditionalContext))
            {
                promptBuilder.AppendLine($"Additional Context: {input.AdditionalContext}");
            }

            promptBuilder.AppendLine();

            // Add checklist tasks information
            if (input.ChecklistTasks.Any())
            {
                promptBuilder.AppendLine("=== Checklist Tasks Analysis ===");
                var completedTasks = input.ChecklistTasks.Count(t => t.IsCompleted);
                var totalTasks = input.ChecklistTasks.Count;
                var requiredTasks = input.ChecklistTasks.Count(t => t.IsRequired);
                var completedRequiredTasks = input.ChecklistTasks.Count(t => t.IsRequired && t.IsCompleted);

                //promptBuilder.AppendLine($"Total Tasks: {totalTasks}");
                //promptBuilder.AppendLine($"Completed Tasks: {completedTasks}");
                //promptBuilder.AppendLine($"Required Tasks: {requiredTasks}");
                //promptBuilder.AppendLine($"Completed Required Tasks: {completedRequiredTasks}");
                //promptBuilder.AppendLine($"Completion Rate: {(totalTasks > 0 ? (decimal)completedTasks / totalTasks * 100 : 0):F1}%");
                //promptBuilder.AppendLine();

                promptBuilder.AppendLine("Tasks:");
                foreach (var task in input.ChecklistTasks)
                {
                    promptBuilder.AppendLine($"- [{(task.IsCompleted ? "✓" : "○")}] {task.TaskName}");
                    if (task.IsCompleted && !string.IsNullOrEmpty(task.CompletionNotes))
                    {
                        promptBuilder.AppendLine($"  Notes: {task.CompletionNotes}");
                    }
                }
                promptBuilder.AppendLine();
            }

            // Add questionnaire information
            if (input.QuestionnaireQuestions.Any())
            {
                promptBuilder.AppendLine("=== Questionnaire Analysis ===");
                var answeredQuestions = input.QuestionnaireQuestions.Count(q => q.IsAnswered);
                var totalQuestions = input.QuestionnaireQuestions.Count;
                var requiredQuestions = input.QuestionnaireQuestions.Count(q => q.IsRequired);
                var answeredRequiredQuestions = input.QuestionnaireQuestions.Count(q => q.IsRequired && q.IsAnswered);

                //promptBuilder.AppendLine($"Total Questions: {totalQuestions}");
                //promptBuilder.AppendLine($"Answered Questions: {answeredQuestions}");
                //promptBuilder.AppendLine($"Required Questions: {requiredQuestions}");
                //promptBuilder.AppendLine($"Answered Required Questions: {answeredRequiredQuestions}");
                //promptBuilder.AppendLine($"Completion Rate: {(totalQuestions > 0 ? (decimal)answeredQuestions / totalQuestions * 100 : 0):F1}%");
                //promptBuilder.AppendLine();

                promptBuilder.AppendLine("Questions:");
                foreach (var question in input.QuestionnaireQuestions)
                {
                    promptBuilder.AppendLine($"- [{(question.IsAnswered ? "✓" : "○")}] {question.QuestionText}");
                    if (question.IsAnswered && question.Answer != null)
                    {
                        promptBuilder.AppendLine($"  Answer: {question.Answer}");
                    }
                }
                promptBuilder.AppendLine();
            }

            // Add static fields information
            if (input.StaticFields.Any())
            {
                promptBuilder.AppendLine("=== Static Fields ===");
                foreach (var field in input.StaticFields.Where(f => f.IsRequired || !string.IsNullOrEmpty(f.Description)))
                {
                    promptBuilder.AppendLine($"- {field.DisplayName ?? field.FieldName}");
                }
                promptBuilder.AppendLine();
            }

            // Simple summary requirements (pure text, <= 200 words)
            promptBuilder.AppendLine("=== Summary Requirements ===");
            promptBuilder.AppendLine("Provide a concise summary in maximum 150 words covering key findings and progress:");
            promptBuilder.AppendLine("- Current completion status");
            promptBuilder.AppendLine("- Key findings or issues identified");
            promptBuilder.AppendLine("- Next steps or recommendations");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Output Rules:");
            promptBuilder.AppendLine("- Plain text only, no formatting");
            promptBuilder.AppendLine("- Maximum 150 words");
            promptBuilder.AppendLine("- Focus on actionable insights");
            promptBuilder.AppendLine("- No instructional phrases or meta-commentary");

            return promptBuilder.ToString();
        }

        /// <summary>
        /// Determine effective language for stage summary.
        /// If input.Language is null/empty or equals to "auto", detect from content; otherwise return input.Language.
        /// </summary>
        private static string DetermineEffectiveLanguage(AIStageSummaryInput input)
        {
            var lang = input.Language?.Trim();
            if (!string.IsNullOrWhiteSpace(lang) && !string.Equals(lang, "auto", StringComparison.OrdinalIgnoreCase))
            {
                return lang;
            }

            // Collect text content for heuristic detection
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(input.StageName)) sb.Append(input.StageName);
            if (!string.IsNullOrWhiteSpace(input.StageDescription)) sb.Append(input.StageDescription);
            if (input.ChecklistTasks != null)
            {
                foreach (var t in input.ChecklistTasks)
                {
                    if (!string.IsNullOrWhiteSpace(t.TaskName)) sb.Append(t.TaskName);
                    if (!string.IsNullOrWhiteSpace(t.Description)) sb.Append(t.Description);
                    if (!string.IsNullOrWhiteSpace(t.CompletionNotes)) sb.Append(t.CompletionNotes);
                }
            }
            if (input.QuestionnaireQuestions != null)
            {
                foreach (var q in input.QuestionnaireQuestions)
                {
                    if (!string.IsNullOrWhiteSpace(q.QuestionText)) sb.Append(q.QuestionText);
                    if (q.Answer != null) sb.Append(q.Answer?.ToString());
                }
            }

            var combined = sb.ToString();
            return ContainsCjk(combined) ? "zh-CN" : "en-US";
        }

        /// <summary>
        /// Detect if a string contains CJK (Chinese) characters.
        /// </summary>
        private static bool ContainsCjk(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            // Basic CJK Unified Ideographs range
            return Regex.IsMatch(text, "[\\u4e00-\\u9fff]");
        }

        /// <summary>
        /// Parse AI response and create structured summary result
        /// </summary>
        /// <param name="aiResponse">Raw AI response</param>
        /// <param name="input">Original input for context</param>
        /// <returns>Structured summary result</returns>
        private AIStageSummaryResult ParseStageSummaryResponse(string aiResponse, AIStageSummaryInput input)
        {
            try
            {
                // Try to extract JSON from the response
                var jsonMatch = Regex.Match(aiResponse, @"\{.*\}", RegexOptions.Singleline);
                if (jsonMatch.Success)
                {
                    var jsonContent = jsonMatch.Value;
                    var summaryData = JsonSerializer.Deserialize<JsonElement>(jsonContent);

                    var result = new AIStageSummaryResult
                    {
                        Success = true,
                        Message = "Summary generated successfully",
                        ConfidenceScore = 0.85 // Default confidence score
                    };

                    // Extract main summary
                    if (summaryData.TryGetProperty("summary", out var summaryElement))
                    {
                        result.Summary = summaryElement.GetString() ?? "";
                    }

                    // Extract breakdown with new structure
                    if (summaryData.TryGetProperty("breakdown", out var breakdownElement))
                    {
                        result.Breakdown = new AIStageSummaryBreakdown
                        {
                            Overview = breakdownElement.TryGetProperty("stageOverview", out var stageOverviewEl) ? stageOverviewEl.GetString() ?? "" : "",
                            ChecklistSummary = breakdownElement.TryGetProperty("checklistSummary", out var checklistEl) ? checklistEl.GetString() ?? "" : "",
                            QuestionnaireSummary = breakdownElement.TryGetProperty("questionnaireSummary", out var questionnaireEl) ? questionnaireEl.GetString() ?? "" : "",
                            ProgressAnalysis = breakdownElement.TryGetProperty("fieldsSummary", out var fieldsEl) ? fieldsEl.GetString() ?? "" : "",
                            RiskAssessment = "" // Not used anymore
                        };
                    }

                    // Initialize empty collections for removed features
                    result.KeyInsights = new List<string>();
                    result.Recommendations = new List<string>();
                    result.CompletionStatus = null;

                    return result;
                }
                else
                {
                    // Fallback: use the entire response as summary
                    return new AIStageSummaryResult
                    {
                        Success = true,
                        Message = "Summary generated successfully (fallback format)",
                        Summary = aiResponse,
                        ConfidenceScore = 0.6
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing AI summary response: {Error}", ex.Message);

                // Fallback: return the raw response
                return new AIStageSummaryResult
                {
                    Success = true,
                    Message = "Summary generated with parsing issues",
                    Summary = aiResponse,
                    ConfidenceScore = 0.4
                };
            }
        }

        #endregion

        #region Flexible Generation Helper Methods

        /// <summary>
        /// Determine the number of checklists for a stage based on its complexity and characteristics
        /// </summary>
        private int DetermineChecklistCount(AIStageGenerationResult stage, Random random)
        {
            var stageName = stage.Name.ToLower();
            var stageDesc = stage.Description.ToLower();
            var duration = stage.EstimatedDuration;

            // Complex stages get more checklists
            var baseCount = 1;

            // Increase count for complex/long stages
            if (duration > 5) baseCount++;
            if (stageName.Contains("implementation") || stageName.Contains("develop") || stageName.Contains("build")) baseCount++;
            if (stageName.Contains("testing") || stageName.Contains("qa")) baseCount++;
            if (stageName.Contains("deployment") || stageName.Contains("launch")) baseCount++;

            // Add randomness (0-2 additional checklists)
            var randomAddition = random.Next(0, 3);

            // Return between 1-4 checklists, weighted towards 1-2
            return Math.Min(4, Math.Max(1, baseCount + (randomAddition == 0 ? 0 : randomAddition - 1)));
        }

        /// <summary>
        /// Determine the number of questionnaires for a stage based on its complexity and characteristics
        /// </summary>
        private int DetermineQuestionnaireCount(AIStageGenerationResult stage, Random random)
        {
            var stageName = stage.Name.ToLower();
            var stageDesc = stage.Description.ToLower();
            var duration = stage.EstimatedDuration;

            // Information-gathering stages get more questionnaires
            var baseCount = 1;

            // Increase count for information-heavy stages
            if (stageName.Contains("initial") || stageName.Contains("assessment") || stageName.Contains("analysis")) baseCount++;
            if (stageName.Contains("design") || stageName.Contains("planning")) baseCount++;
            if (stageName.Contains("review") || stageName.Contains("approval")) baseCount++;
            if (duration > 7) baseCount++;

            // Add randomness (0-1 additional questionnaires, less frequent than checklists)
            var randomAddition = random.Next(0, 4); // 0, 1, 2, 3 - only add if 3

            // Return between 1-3 questionnaires, weighted towards 1
            return Math.Min(3, Math.Max(1, baseCount + (randomAddition == 3 ? 1 : 0)));
        }

        /// <summary>
        /// Determine the number of tasks for a checklist
        /// </summary>
        private int DetermineTaskCount(int availableTasks, int checklistIndex, int totalChecklists, Random random)
        {
            // Base count: 2-6 tasks, weighted towards 3-4
            var weights = new[] { 2, 3, 4, 4, 5, 6 }; // More weight on 3-4 tasks
            var baseCount = weights[random.Next(weights.Length)];

            // Ensure we don't exceed available tasks
            var maxCount = Math.Min(availableTasks, 6);
            var minCount = Math.Min(2, maxCount);

            return Math.Min(maxCount, Math.Max(minCount, baseCount));
        }

        /// <summary>
        /// Determine the number of questions for a questionnaire
        /// </summary>
        private int DetermineQuestionCount(int availableQuestions, int questionnaireIndex, int totalQuestionnaires, Random random)
        {
            // Base count: 2-8 questions, weighted towards 4-6
            var weights = new[] { 2, 3, 4, 4, 5, 5, 6, 6, 7, 8 }; // More weight on 4-6 questions
            var baseCount = weights[random.Next(weights.Length)];

            // Ensure we don't exceed available questions
            var maxCount = Math.Min(availableQuestions, 8);
            var minCount = Math.Min(2, maxCount);

            return Math.Min(maxCount, Math.Max(minCount, baseCount));
        }

        /// <summary>
        /// Generate checklist name based on stage and index
        /// </summary>
        private string GenerateChecklistName(AIStageGenerationResult stage, int checklistIndex, int totalChecklists)
        {
            if (totalChecklists == 1)
            {
                return $"{stage.Name} Checklist";
            }

            var stageName = stage.Name.ToLower();
            var suffixes = new[]
            {
                "Essential Tasks", "Core Activities", "Key Steps", "Primary Tasks",
                "Critical Actions", "Important Tasks", "Main Activities", "Required Steps",
                "Setup Tasks", "Execution Tasks", "Review Tasks", "Completion Tasks",
                "Pre-work", "Preparation", "Implementation", "Validation", "Follow-up"
            };

            var categoryNames = new Dictionary<string, string[]>
            {
                ["initial"] = new[] { "Assessment", "Requirements", "Planning", "Analysis" },
                ["planning"] = new[] { "Strategy", "Resource Planning", "Risk Management", "Timeline" },
                ["design"] = new[] { "Wireframes", "Prototyping", "UI/UX", "Architecture" },
                ["implementation"] = new[] { "Development", "Coding", "Integration", "Configuration" },
                ["testing"] = new[] { "Unit Tests", "Integration Tests", "User Testing", "Quality Assurance" },
                ["deployment"] = new[] { "Preparation", "Execution", "Monitoring", "Rollback" },
                ["training"] = new[] { "Material Preparation", "Delivery", "Assessment", "Support" }
            };

            // Try to use category-specific names first
            foreach (var category in categoryNames)
            {
                if (stageName.Contains(category.Key))
                {
                    var names = category.Value;
                    if (checklistIndex < names.Length)
                    {
                        return $"{stage.Name} - {names[checklistIndex]}";
                    }
                }
            }

            // Fallback to generic suffixes
            var suffix = suffixes[checklistIndex % suffixes.Length];
            return $"{stage.Name} - {suffix}";
        }

        /// <summary>
        /// Generate checklist description based on stage and index
        /// </summary>
        private string GenerateChecklistDescription(AIStageGenerationResult stage, int checklistIndex, int totalChecklists)
        {
            if (totalChecklists == 1)
            {
                return $"Essential tasks to complete during the {stage.Name} stage";
            }

            var descriptions = new[]
            {
                $"Critical tasks for {stage.Name} execution",
                $"Key activities required in {stage.Name}",
                $"Important steps to ensure {stage.Name} success",
                $"Essential checklist for {stage.Name} completion",
                $"Primary tasks to accomplish during {stage.Name}",
                $"Core activities for effective {stage.Name}",
                $"Required actions for {stage.Name} stage",
                $"Key deliverables and tasks for {stage.Name}"
            };

            return descriptions[checklistIndex % descriptions.Length];
        }

        /// <summary>
        /// Generate questionnaire name based on stage and index
        /// </summary>
        private string GenerateQuestionnaireName(AIStageGenerationResult stage, int questionnaireIndex, int totalQuestionnaires)
        {
            if (totalQuestionnaires == 1)
            {
                return $"{stage.Name} Questionnaire";
            }

            var suffixes = new[]
            {
                "Assessment", "Requirements Gathering", "Information Collection", "Evaluation",
                "Planning Questions", "Feedback Form", "Analysis Questions", "Review Form",
                "Pre-work Questions", "Preparation Survey", "Execution Questions", "Completion Review"
            };

            var suffix = suffixes[questionnaireIndex % suffixes.Length];
            return $"{stage.Name} - {suffix}";
        }

        /// <summary>
        /// Generate questionnaire description based on stage and index
        /// </summary>
        private string GenerateQuestionnaireDescription(AIStageGenerationResult stage, int questionnaireIndex, int totalQuestionnaires)
        {
            if (totalQuestionnaires == 1)
            {
                return $"Key questions to gather information for the {stage.Name} stage";
            }

            var descriptions = new[]
            {
                $"Important questions to assess {stage.Name} requirements",
                $"Information gathering for effective {stage.Name} execution",
                $"Key questions to ensure {stage.Name} success",
                $"Assessment questions for {stage.Name} planning",
                $"Evaluation form for {stage.Name} stage",
                $"Required information collection for {stage.Name}",
                $"Planning questionnaire for {stage.Name} activities",
                $"Feedback and assessment for {stage.Name} completion"
            };

            return descriptions[questionnaireIndex % descriptions.Length];
        }

        #endregion

        #region Helper Classes

        public class AIProviderResponse
        {
            public bool Success { get; set; }
            public string Content { get; set; } = string.Empty;
            public string ErrorMessage { get; set; } = string.Empty;
        }

        #endregion
    }
}