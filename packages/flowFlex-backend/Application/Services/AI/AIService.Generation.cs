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
    public partial class AIService
    {

        public async Task<AIWorkflowGenerationResult> GenerateWorkflowAsync(AIWorkflowGenerationInput input)
        {
            var startTime = DateTime.UtcNow;
            string prompt = null;
            AIProviderResponse aiResponse = null;

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

                prompt = BuildWorkflowGenerationPrompt(input);
                aiResponse = await CallAIProviderAsync(prompt, input.ModelId, input.ModelProvider, input.ModelName);

                // Save prompt history to database using background task queue
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        var metadata = JsonSerializer.Serialize(new
                        {
                            sessionId = input.SessionId,
                            conversationHistoryCount = input.ConversationHistory?.Count ?? 0,
                            contextId = contextId
                        });
                        await SavePromptHistoryAsync("WorkflowGeneration", "Workflow", null, null,
                            prompt, aiResponse, startTime, input.ModelProvider, input.ModelName, input.ModelId, metadata);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to save workflow generation prompt history");
                    }
                });

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
                                    error = ex.Message
                                });
                                await SavePromptHistoryAsync("WorkflowGeneration", "Workflow", null, null,
                                    prompt, failedResponse, startTime, input.ModelProvider, input.ModelName, input.ModelId, metadata);
                            }
                            catch (Exception saveEx)
                            {
                                _logger.LogWarning(saveEx, "Failed to save failed workflow generation prompt history");
                            }
                        });
                }

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
            var startTime = DateTime.UtcNow;
            string prompt = null;
            AIProviderResponse aiResponse = null;

            try
            {
                _logger.LogInformation("Generating questionnaire for purpose: {Purpose}", input.Purpose);

                prompt = BuildQuestionnaireGenerationPrompt(input);
                aiResponse = await CallAIProviderAsync(prompt);

                // Save prompt history to database (fire-and-forget)
                // Background task queued
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        var metadata = JsonSerializer.Serialize(new
                        {
                            purpose = input.Purpose,
                            targetAudience = input.TargetAudience,
                            estimatedQuestions = input.EstimatedQuestions
                        });
                        await SavePromptHistoryAsync("QuestionnaireGeneration", "Questionnaire", null, null,
                            prompt, aiResponse, startTime, null, null, null, metadata);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to save questionnaire generation prompt history");
                    }
                });

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
                                    purpose = input.Purpose,
                                    error = ex.Message
                                });
                                await SavePromptHistoryAsync("QuestionnaireGeneration", "Questionnaire", null, null,
                                    prompt, failedResponse, startTime, null, null, null, metadata);
                            }
                            catch (Exception saveEx)
                            {
                                _logger.LogWarning(saveEx, "Failed to save failed questionnaire generation prompt history");
                            }
                        });
                }

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
            var startTime = DateTime.UtcNow;
            string prompt = null;
            AIProviderResponse aiResponse = null;

            try
            {
                _logger.LogInformation("Generating checklist for process: {ProcessName}", input.ProcessName);

                prompt = BuildChecklistGenerationPrompt(input);
                aiResponse = await CallAIProviderAsync(prompt);

                // Save prompt history to database (fire-and-forget)
                // Background task queued
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        var metadata = JsonSerializer.Serialize(new
                        {
                            processName = input.ProcessName,
                            team = input.Team,
                            requiredStepsCount = input.RequiredSteps?.Count ?? 0
                        });
                        await SavePromptHistoryAsync("ChecklistGeneration", "Checklist", null, null,
                            prompt, aiResponse, startTime, null, null, null, metadata);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to save checklist generation prompt history");
                    }
                });

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
                                    processName = input.ProcessName,
                                    error = ex.Message
                                });
                                await SavePromptHistoryAsync("ChecklistGeneration", "Checklist", null, null,
                                    prompt, failedResponse, startTime, null, null, null, metadata);
                            }
                            catch (Exception saveEx)
                            {
                                _logger.LogWarning(saveEx, "Failed to save failed checklist generation prompt history");
                            }
                        });
                }

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
            var startTime = DateTime.UtcNow;
            string prompt = null;

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

            // 使用Channel生产者-消费者模式，避免在try/catch中使用yield
            var channel = System.Threading.Channels.Channel.CreateUnbounded<AIWorkflowStreamResult>();
            // Background task queued
            _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
        {
            await ProduceWorkflowStreamAsync(input, startTime, channel.Writer);
        });

            await foreach (var result in channel.Reader.ReadAllAsync())
            {
                yield return result;
            }
        }

        private async Task ProduceWorkflowStreamAsync(AIWorkflowGenerationInput input, DateTime startTime, System.Threading.Channels.ChannelWriter<AIWorkflowStreamResult> writer)
        {
            var streamingContent = new StringBuilder();
            string prompt = null;
            try
            {
                // 尝试使用真正的流式AI调用
                prompt = BuildWorkflowGenerationPrompt(input);
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
                    var streamStartTime = DateTime.UtcNow;

                    // 发送初始进度消息
                    await writer.WriteAsync(new AIWorkflowStreamResult
                    {
                        Type = "progress",
                        Message = "Generating workflow structure...",
                        IsComplete = false
                    });
                    _logger.LogInformation("✅ Initial progress message sent");

                    // 根据模型类型选择处理方式
                    if (userConfig.Provider?.ToLower() == "openai")
                    {
                        _logger.LogInformation("🚀 Using OpenAI TRUE streaming - real-time progress updates");

                        var lastProgressLength = 0;
                        var lastProgressTime = DateTime.UtcNow;

                        await foreach (var chunk in CallOpenAIStreamAsync(messages, userConfig))
                        {
                            if (!string.IsNullOrEmpty(chunk))
                            {
                                streamingContent.Append(chunk);
                                hasReceivedContent = true;

                                var now = DateTime.UtcNow;
                                var timeSinceLastProgress = (now - lastProgressTime).TotalMilliseconds;
                                var lengthDifference = streamingContent.Length - lastProgressLength;

                                if (lengthDifference >= 50 || timeSinceLastProgress >= 2000)
                                {
                                    await writer.WriteAsync(new AIWorkflowStreamResult
                                    {
                                        Type = "progress",
                                        Message = $"Generating workflow... ({streamingContent.Length} characters, {timeSinceLastProgress / 1000:F1}s)",
                                        IsComplete = false
                                    });

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

                        await foreach (var chunk in CallDeepSeekStreamAsync(messages, userConfig))
                        {
                            if (!string.IsNullOrEmpty(chunk))
                            {
                                streamingContent.Append(chunk);
                                hasReceivedContent = true;

                                var now = DateTime.UtcNow;
                                var timeSinceLastProgress = (now - lastProgressTime).TotalMilliseconds;
                                var lengthDifference = streamingContent.Length - lastProgressLength;

                                if (lengthDifference >= 50 || timeSinceLastProgress >= 2000)
                                {
                                    await writer.WriteAsync(new AIWorkflowStreamResult
                                    {
                                        Type = "progress",
                                        Message = $"Generating workflow... ({streamingContent.Length} characters, {timeSinceLastProgress / 1000:F1}s)",
                                        IsComplete = false
                                    });

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
                        _logger.LogInformation("🚀 Using {Provider} TRUE streaming - real-time progress updates", userConfig.Provider);

                        var lastProgressLength = 0;
                        var lastProgressTime = DateTime.UtcNow;

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

                                if (lengthDifference >= 50 || timeSinceLastProgress >= 2000)
                                {
                                    await writer.WriteAsync(new AIWorkflowStreamResult
                                    {
                                        Type = "progress",
                                        Message = $"Generating workflow... ({streamingContent.Length} characters, {timeSinceLastProgress / 1000:F1}s)",
                                        IsComplete = false
                                    });

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
                        await writer.WriteAsync(new AIWorkflowStreamResult
                        {
                            Type = "progress",
                            Message = "Parsing workflow structure...",
                            IsComplete = false
                        });

                        _logger.LogInformation("🔍 Starting to parse AI response, content length: {Length}", streamingContent.Length);
                        var parseStartTime = DateTime.UtcNow;
                        var streamResult = ParseWorkflowGenerationResponse(streamingContent.ToString());
                        var parseEndTime = DateTime.UtcNow;
                        _logger.LogInformation("✅ Parsing completed in {Duration}ms", (parseEndTime - parseStartTime).TotalMilliseconds);

                        if (streamResult?.GeneratedWorkflow != null)
                        {
                            _logger.LogInformation("🎯 About to yield workflow and {Count} stages", streamResult.Stages?.Count ?? 0);

                            await writer.WriteAsync(new AIWorkflowStreamResult
                            {
                                Type = "workflow",
                                Data = streamResult.GeneratedWorkflow,
                                Message = "Workflow basic information generated",
                                IsComplete = false
                            });

                            var stageStartTime = DateTime.UtcNow;
                            var stageCount = 0;
                            foreach (var stage in streamResult.Stages)
                            {
                                await writer.WriteAsync(new AIWorkflowStreamResult
                                {
                                    Type = "stage",
                                    Data = stage,
                                    Message = $"Stage '{stage.Name}' generated",
                                    IsComplete = false
                                });
                                stageCount++;

                                if (stageCount % 10 == 0)
                                {
                                    _logger.LogInformation("📊 Processed {Count} stages so far...", stageCount);
                                }
                            }
                            var stageEndTime = DateTime.UtcNow;
                            _logger.LogInformation("✅ All {Count} stages yielded in {Duration}ms", stageCount, (stageEndTime - stageStartTime).TotalMilliseconds);

                            // AI生成质量分析
                            var checklistCount = streamResult.Checklists?.Count ?? 0;
                            var questionnaireCount = streamResult.Questionnaires?.Count ?? 0;
                            stageCount = streamResult.Stages.Count;

                            var effectiveChecklists = streamResult.Checklists?.Count(c => c?.Tasks?.Count > 0) ?? 0;
                            var effectiveQuestionnaires = streamResult.Questionnaires?.Count(q => q?.Questions?.Count > 0) ?? 0;
                            var qualityScore = stageCount > 0 ? ((effectiveChecklists + effectiveQuestionnaires) * 50.0) / stageCount : 0;

                            _logger.LogInformation("📈 AI生成质量: {QualityScore:F1}% - 有效组件 {EffectiveChecklists}/{ChecklistCount} checklists, {EffectiveQuestionnaires}/{QuestionnaireCount} questionnaires",
                                qualityScore, effectiveChecklists, checklistCount, effectiveQuestionnaires, questionnaireCount);

                            // 确保每个stage都有完整的组件
                            streamResult.Checklists ??= new List<AIChecklistGenerationResult>();
                            streamResult.Questionnaires ??= new List<AIQuestionnaireGenerationResult>();

                            var supplementStartTime = DateTime.UtcNow;
                            var supplementCount = 0;

                            stageCount = streamResult.Stages.Count;
                            checklistCount = streamResult.Checklists?.Count ?? 0;
                            questionnaireCount = streamResult.Questionnaires?.Count ?? 0;

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
                                _logger.LogInformation("🔧 检测到组件缺失，开始智能补充 - 现有: {ChecklistCount}/{StageCount} checklists, {QuestionnaireCount}/{StageCount} questionnaires",
                                    checklistCount, stageCount, questionnaireCount, stageCount);

                                for (int i = 0; i < stageCount; i++)
                                {
                                    var stage = streamResult.Stages[i];

                                    if (i >= checklistCount || streamResult.Checklists[i]?.Tasks?.Count == 0)
                                    {
                                        if (i >= checklistCount)
                                        {
                                            streamResult.Checklists.Add(GenerateFallbackChecklist(stage));
                                            supplementCount++;
                                            _logger.LogDebug("➕ 为stage {StageIndex}-{StageName} 添加空checklist", i, stage.Name);
                                        }
                                    }

                                    if (i >= questionnaireCount || streamResult.Questionnaires[i]?.Questions?.Count == 0)
                                    {
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

                            await writer.WriteAsync(new AIWorkflowStreamResult
                            {
                                Type = "complete",
                                Data = streamResult,
                                Message = "Workflow generation completed with AI-powered components",
                                IsComplete = true
                            });

                            // Save successful streaming prompt history (fire-and-forget)
                            // Background task queued
                            _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                                        {
                                            try
                                            {
                                                var response = new AIProviderResponse
                                                {
                                                    Success = true,
                                                    Content = streamingContent.ToString()
                                                };
                                                var metadata = JsonSerializer.Serialize(new
                                                {
                                                    sessionId = input.SessionId,
                                                    conversationHistoryCount = input.ConversationHistory?.Count ?? 0,
                                                    streamingMode = true,
                                                    contentLength = streamingContent.Length
                                                });
                                                await SavePromptHistoryAsync("WorkflowGenerationStream", "Workflow", null, null,
                                                    prompt, response, startTime, input.ModelProvider, input.ModelName, input.ModelId, metadata);
                                            }
                                            catch (Exception ex)
                                            {
                                                _logger.LogWarning(ex, "Failed to save streaming workflow generation prompt history");
                                            }
                                        });

                            _logger.LogInformation("🎉 StreamGenerateWorkflowAsync about to exit successfully");
                            return;
                        }
                        else
                        {
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

                                await writer.WriteAsync(new AIWorkflowStreamResult
                                {
                                    Type = "workflow",
                                    Data = fallbackResult.GeneratedWorkflow,
                                    Message = "Workflow generated using optimized fallback",
                                    IsComplete = false
                                });

                                foreach (var stage in fallbackResult.Stages)
                                {
                                    await writer.WriteAsync(new AIWorkflowStreamResult
                                    {
                                        Type = "stage",
                                        Data = stage,
                                        Message = $"Stage '{stage.Name}' generated",
                                        IsComplete = false
                                    });
                                }

                                await writer.WriteAsync(new AIWorkflowStreamResult
                                {
                                    Type = "complete",
                                    Data = fallbackResult,
                                    Message = $"Workflow generation completed in {(DateTime.UtcNow - fallbackStartTime).TotalMilliseconds}ms",
                                    IsComplete = true
                                });
                                return;
                            }

                            await writer.WriteAsync(new AIWorkflowStreamResult
                            {
                                Type = "error",
                                Message = "AI generation failed and fallback unsuccessful",
                                IsComplete = true
                            });
                            return;
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
                        await writer.WriteAsync(new AIWorkflowStreamResult
                        {
                            Type = "error",
                            Message = aiResponse.ErrorMessage ?? "AI service call failed",
                            IsComplete = true
                        });
                        return;
                    }
                }

                if (!hasReceivedContent)
                {
                    await writer.WriteAsync(new AIWorkflowStreamResult
                    {
                        Type = "error",
                        Message = "No content received from AI service",
                        IsComplete = true
                    });
                    return;
                }

                await writer.WriteAsync(new AIWorkflowStreamResult
                {
                    Type = "progress",
                    Message = "Parsing workflow structure...",
                    IsComplete = false
                });

                // 解析AI响应（非流式路径）
                var result = ParseWorkflowGenerationResponse(streamingContent.ToString());

                if (result?.GeneratedWorkflow != null)
                {
                    await writer.WriteAsync(new AIWorkflowStreamResult
                    {
                        Type = "workflow",
                        Data = result.GeneratedWorkflow,
                        Message = "Workflow basic information generated",
                        IsComplete = false
                    });

                    foreach (var stage in result.Stages)
                    {
                        await writer.WriteAsync(new AIWorkflowStreamResult
                        {
                            Type = "stage",
                            Data = stage,
                            Message = $"Stage '{stage.Name}' generated",
                            IsComplete = false
                        });
                    }

                    await writer.WriteAsync(new AIWorkflowStreamResult
                    {
                        Type = "complete",
                        Data = result,
                        Message = "Workflow generation completed",
                        IsComplete = true
                    });
                }
                else
                {
                    await writer.WriteAsync(new AIWorkflowStreamResult
                    {
                        Type = "error",
                        Message = "Unable to parse AI-generated workflow structure",
                        IsComplete = true
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in streaming workflow generation: {Description}", input.Description);

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
                                    description = input.Description,
                                    streamingMode = true,
                                    error = ex.Message
                                });
                                await SavePromptHistoryAsync("WorkflowGenerationStream", "Workflow", null, null,
                                    prompt, failedResponse, startTime, input.ModelProvider, input.ModelName, input.ModelId, metadata);
                            }
                            catch (Exception saveEx)
                            {
                                _logger.LogWarning(saveEx, "Failed to save failed streaming workflow generation prompt history");
                            }
                        });
                }

                await writer.WriteAsync(new AIWorkflowStreamResult
                {
                    Type = "error",
                    Message = $"Workflow generation failed: {ex.Message}",
                    IsComplete = true
                });
            }
            finally
            {
                writer.TryComplete();
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

        #region AI Chat Implementation       

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

                // First try to get stages from database, but fallback to provided stages if database query fails
                var workflow = await _workflowService.GetByIdAsync(workflowId);
                List<StageOutputDto> createdStages = null;

                if (workflow?.Stages != null && workflow.Stages.Any())
                {
                    _logger.LogInformation("✅ Using stages from database: {StageCount} stages found", workflow.Stages.Count);
                    createdStages = workflow.Stages;
                }
                else
                {
                    _logger.LogWarning("⚠️ No stages found in database for workflow {WorkflowId}, using provided stages data", workflowId);

                    // Use provided stages data and try to find corresponding database records by name
                    if (stages == null || !stages.Any())
                    {
                        _logger.LogError("❌ No stages provided in request and none found in database for workflow {WorkflowId}", workflowId);
                        return false;
                    }

                    // Try to find stages in database by name and order
                    createdStages = new List<StageOutputDto>();
                    foreach (var providedStage in stages.OrderBy(s => s.Order))
                    {
                        try
                        {
                            // Query stages by workflow ID and name
                            var dbStages = await _stageRepository.GetByWorkflowIdAsync(workflowId);
                            var matchingStage = dbStages?.FirstOrDefault(s =>
                                s.Name.Equals(providedStage.Name, StringComparison.OrdinalIgnoreCase) &&
                                s.Order == providedStage.Order);

                            if (matchingStage != null)
                            {
                                // Convert to StageOutputDto
                                var stageDto = new StageOutputDto
                                {
                                    Id = matchingStage.Id,
                                    Name = matchingStage.Name,
                                    Description = matchingStage.Description,
                                    Order = matchingStage.Order,
                                    DefaultAssignedGroup = matchingStage.DefaultAssignedGroup,
                                    EstimatedDuration = matchingStage.EstimatedDuration,
                                    IsActive = matchingStage.IsActive
                                };
                                createdStages.Add(stageDto);
                                _logger.LogInformation("✅ Found matching stage in database: {StageName} (ID: {StageId})", matchingStage.Name, matchingStage.Id);
                            }
                            else
                            {
                                _logger.LogWarning("⚠️ Could not find stage '{StageName}' in database for workflow {WorkflowId}", providedStage.Name, workflowId);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "⚠️ Error finding stage '{StageName}' in database", providedStage.Name);
                        }
                    }

                    if (!createdStages.Any())
                    {
                        _logger.LogError("❌ Could not find any matching stages in database for workflow {WorkflowId}", workflowId);
                        return false;
                    }

                    _logger.LogInformation("✅ Using {StageCount} stages found by name matching", createdStages.Count);
                }

                // Create checklists and associate with stages using StageName and StageOrder
                foreach (var checklist in checklists)
                {
                    // Find the corresponding stage using StageName and StageOrder from the checklist
                    StageOutputDto stage = null;

                    // First try to match by StageName if provided
                    if (!string.IsNullOrEmpty(checklist.StageName))
                    {
                        stage = createdStages.FirstOrDefault(s =>
                            s.Name.Equals(checklist.StageName, StringComparison.OrdinalIgnoreCase));
                        _logger.LogInformation("🔍 Looking for stage by name '{StageName}' for checklist '{ChecklistName}'",
                            checklist.StageName, checklist.GeneratedChecklist?.Name);
                    }

                    // If not found by name, try to match by StageOrder
                    if (stage == null && checklist.StageOrder > 0)
                    {
                        stage = createdStages.FirstOrDefault(s => s.Order == checklist.StageOrder);
                        _logger.LogInformation("🔍 Looking for stage by order {StageOrder} for checklist '{ChecklistName}'",
                            checklist.StageOrder, checklist.GeneratedChecklist?.Name);
                    }

                    // If still not found, skip this checklist
                    if (stage == null)
                    {
                        _logger.LogWarning("⚠️ Could not find matching stage for checklist '{ChecklistName}' (StageName: '{StageName}', StageOrder: {StageOrder})",
                            checklist.GeneratedChecklist?.Name, checklist.StageName, checklist.StageOrder);
                        continue;
                    }

                    _logger.LogInformation("✅ Found matching stage '{StageName}' (ID: {StageId}) for checklist '{ChecklistName}'",
                        stage.Name, stage.Id, checklist.GeneratedChecklist?.Name);

                    // Ensure GeneratedChecklist is not null
                    if (checklist.GeneratedChecklist == null)
                    {
                        _logger.LogWarning("Skipping checklist '{ChecklistName}' - GeneratedChecklist is null", checklist.GeneratedChecklist?.Name ?? "Unknown");
                        continue;
                    }

                    // Ensure unique checklist name
                    checklist.GeneratedChecklist.Name = await EnsureUniqueChecklistNameAsync(checklist.GeneratedChecklist.Name, checklist.GeneratedChecklist.Team);

                    // Set up assignments for the checklist (assignments are now managed through Stage Components)
                    // These will be populated when the checklist is queried through the service layer
                    checklist.GeneratedChecklist.Assignments = new List<FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto>();

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

                // Create questionnaires and associate with stages using StageName and StageOrder
                foreach (var questionnaire in questionnaires)
                {
                    // Find the corresponding stage using StageName and StageOrder from the questionnaire
                    StageOutputDto stage = null;

                    // First try to match by StageName if provided
                    if (!string.IsNullOrEmpty(questionnaire.StageName))
                    {
                        stage = createdStages.FirstOrDefault(s =>
                            s.Name.Equals(questionnaire.StageName, StringComparison.OrdinalIgnoreCase));
                        _logger.LogInformation("🔍 Looking for stage by name '{StageName}' for questionnaire '{QuestionnaireName}'",
                            questionnaire.StageName, questionnaire.GeneratedQuestionnaire?.Name);
                    }

                    // If not found by name, try to match by StageOrder
                    if (stage == null && questionnaire.StageOrder > 0)
                    {
                        stage = createdStages.FirstOrDefault(s => s.Order == questionnaire.StageOrder);
                        _logger.LogInformation("🔍 Looking for stage by order {StageOrder} for questionnaire '{QuestionnaireName}'",
                            questionnaire.StageOrder, questionnaire.GeneratedQuestionnaire?.Name);
                    }

                    // If still not found, skip this questionnaire
                    if (stage == null)
                    {
                        _logger.LogWarning("⚠️ Could not find matching stage for questionnaire '{QuestionnaireName}' (StageName: '{StageName}', StageOrder: {StageOrder})",
                            questionnaire.GeneratedQuestionnaire?.Name, questionnaire.StageName, questionnaire.StageOrder);
                        continue;
                    }

                    _logger.LogInformation("✅ Found matching stage '{StageName}' (ID: {StageId}) for questionnaire '{QuestionnaireName}'",
                        stage.Name, stage.Id, questionnaire.GeneratedQuestionnaire?.Name);

                    // Ensure GeneratedQuestionnaire is not null
                    if (questionnaire.GeneratedQuestionnaire == null)
                    {
                        _logger.LogWarning("Skipping questionnaire '{QuestionnaireName}' - GeneratedQuestionnaire is null", questionnaire.GeneratedQuestionnaire?.Name ?? "Unknown");
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
                        _logger.LogWarning("⚠️ No questions found for questionnaire '{QuestionnaireName}'", questionnaire.GeneratedQuestionnaire?.Name ?? "Unknown");
                    }

                    // Set up assignments for the questionnaire (assignments are now managed through Stage Components)
                    // These will be populated when the questionnaire is queried through the service layer
                    questionnaire.GeneratedQuestionnaire.Assignments = new List<FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto>();

                    // Create the questionnaire
                    var questionnaireId = await _questionnaireService.CreateAsync(questionnaire.GeneratedQuestionnaire);
                    _logger.LogInformation("✅ Created questionnaire {QuestionnaireId} for stage {StageId}", questionnaireId, stage.Id);

                    // Note: Questions are created as part of the questionnaire structure
                    // The QuestionnaireInputDto should include the questions in its structure
                    _logger.LogInformation("✅ Questionnaire {QuestionnaireId} created with {QuestionCount} questions",
                        questionnaireId, questionnaire.Questions?.Count ?? 0);
                }

                // Now update stage components to link the created checklists and questionnaires

                for (int i = 0; i < createdStages.Count; i++)
                {
                    var stage = createdStages[i];
                    var stageComponents = new List<dynamic>();

                    // Add checklist component if we have a checklist for this stage
                    if (i < checklists.Count && checklists[i].GeneratedChecklist != null)
                    {
                        // Get the created checklist ID (we need to find it by name since we don't store it above)
                        var checklistName = checklists[i].GeneratedChecklist.Name;
                        var checklistEntities = await _checklistRepository.GetByNameAsync(checklistName);
                        var checklistEntity = checklistEntities?.FirstOrDefault();

                        if (checklistEntity != null)
                        {
                            stageComponents.Add(new
                            {
                                Key = "checklist",
                                Order = 1,
                                IsEnabled = true,
                                Configuration = (object)null,
                                StaticFields = new List<object>(),
                                ChecklistIds = new List<long> { checklistEntity.Id },
                                QuestionnaireIds = new List<long>(),
                                ChecklistNames = new List<string> { checklistEntity.Name },
                                QuestionnaireNames = new List<string>()
                            });
                        }
                    }

                    // Add questionnaire component if we have a questionnaire for this stage
                    if (i < questionnaires.Count && questionnaires[i].GeneratedQuestionnaire != null)
                    {
                        // Get the created questionnaire ID
                        var questionnaireName = questionnaires[i].GeneratedQuestionnaire.Name;
                        // Use LINQ query to find questionnaire by name
                        var questionnaireEntity = await _questionnaireRepository.GetFirstAsync(q => q.Name == questionnaireName && q.IsValid);

                        if (questionnaireEntity != null)
                        {
                            // If we already have a checklist component, add questionnaire to a new component
                            // Otherwise, create a questionnaire-only component
                            stageComponents.Add(new
                            {
                                Key = "questionnaires",
                                Order = stageComponents.Count + 1,
                                IsEnabled = true,
                                Configuration = (object)null,
                                StaticFields = new List<object>(),
                                ChecklistIds = new List<long>(),
                                QuestionnaireIds = new List<long> { questionnaireEntity.Id },
                                ChecklistNames = new List<string>(),
                                QuestionnaireNames = new List<string> { questionnaireEntity.Name }
                            });
                        }
                    }

                    // Update stage components if we have any
                    if (stageComponents.Any())
                    {
                        try
                        {
                            var updateComponentsInput = new
                            {
                                Components = stageComponents
                            };

                            // We need to use StageService to update components, but it's not available in AI service
                            // Let's manually update the stage's ComponentsJson
                            var stageEntity = await _stageRepository.GetByIdAsync(stage.Id);
                            if (stageEntity != null)
                            {
                                stageEntity.ComponentsJson = JsonSerializer.Serialize(stageComponents);
                                await _stageRepository.UpdateAsync(stageEntity);

                                _logger.LogInformation("✅ Updated stage {StageId} components with {ComponentCount} components",
                                    stage.Id, stageComponents.Count);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to update components for stage {StageId}: {Error}", stage.Id, ex.Message);
                        }
                    }
                }

                // Finally, sync the component mappings for the entire workflow
                try
                {
                    await _componentMappingService.SyncWorkflowMappingsAsync(workflowId);
                    _logger.LogInformation("✅ Successfully synced component mappings for AI-generated workflow {WorkflowId}", workflowId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to sync component mappings for workflow {WorkflowId}: {Error}", workflowId, ex.Message);
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
    }
}
