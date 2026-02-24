using FlowFlex.Application.Contracts.Dtos.OW.Checklist;
using FlowFlex.Application.Contracts.Dtos.OW.Questionnaire;
using FlowFlex.Application.Contracts.Dtos.OW.Workflow;
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
using System.Threading.Channels;

namespace FlowFlex.Application.Services.AI.Workflow
{
    /// <summary>
    /// AI workflow generation service implementation.
    /// Responsible for workflow generation (sync + streaming), enhancement, validation,
    /// and stage component creation.
    /// Migrated from AIService.Generation.cs
    /// </summary>
    public class AIWorkflowGenerationService : AIServiceBase, IAIWorkflowGenerationService, IScopedService
    {
        private readonly IAIProviderAdapter _providerAdapter;
        private readonly IAIPromptBuilder _promptBuilder;
        private readonly IAIResponseParser _responseParser;
        private readonly IWorkflowService _workflowService;
        private readonly IStageComponentService _stageComponentService;
        private readonly IStageRepository _stageRepository;
        private readonly IAIModelConfigService _configService;

        public AIWorkflowGenerationService(
            IAIProviderAdapter providerAdapter,
            IAIPromptBuilder promptBuilder,
            IAIResponseParser responseParser,
            IWorkflowService workflowService,
            IStageComponentService stageComponentService,
            IStageRepository stageRepository,
            IAIModelConfigService configService,
            // Base class dependencies
            ILogger<AIWorkflowGenerationService> logger,
            IAIPromptHistoryRepository promptHistoryRepository,
            IOperatorContextService operatorContextService,
            IHttpContextAccessor httpContextAccessor,
            IBackgroundTaskQueue backgroundTaskQueue)
            : base(logger, promptHistoryRepository, operatorContextService, httpContextAccessor, backgroundTaskQueue)
        {
            _providerAdapter = providerAdapter;
            _promptBuilder = promptBuilder;
            _responseParser = responseParser;
            _workflowService = workflowService;
            _stageComponentService = stageComponentService;
            _stageRepository = stageRepository;
            _configService = configService;
        }

        #region GenerateWorkflowAsync

        /// <summary>
        /// Generate workflow from natural language description
        /// </summary>
        public async Task<AIWorkflowGenerationResult> GenerateWorkflowAsync(AIWorkflowGenerationInput input)
        {
            var startTime = DateTime.UtcNow;
            string prompt = null;
            AIProviderResponse aiResponse = null;

            try
            {
                Logger.LogInformation("Generating workflow from natural language with enhanced context");
                Logger.LogInformation("Description length: {DescriptionLength} characters", input.Description?.Length ?? 0);
                Logger.LogInformation("AI Model: {Provider} {Model} (ID: {ModelId})",
                    input.ModelProvider, input.ModelName, input.ModelId);
                Logger.LogInformation("Session ID: {SessionId}", input.SessionId);
                Logger.LogInformation("Conversation History: {MessageCount} messages",
                    input.ConversationHistory?.Count ?? 0);

                prompt = _promptBuilder.BuildWorkflowGenerationPrompt(input);
                // Use larger max_tokens for workflow generation to prevent truncation
                const int workflowGenerationMaxTokens = 16000;
                aiResponse = await _providerAdapter.CallAsync(new AIProviderRequest
                {
                    Prompt = prompt,
                    ModelId = input.ModelId,
                    Provider = input.ModelProvider,
                    ModelName = input.ModelName,
                    MaxTokensOverride = workflowGenerationMaxTokens
                });

                // Save prompt history to database using background task queue
                QueuePromptHistorySave(
                    "WorkflowGeneration", "Workflow", prompt, aiResponse, startTime,
                    input.ModelProvider, input.ModelName, input.ModelId,
                    () => new
                    {
                        sessionId = input.SessionId,
                        conversationHistoryCount = input.ConversationHistory?.Count ?? 0
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

                var result = _responseParser.ParseWorkflowResponse(aiResponse.Content);
                result.ConfidenceScore = _responseParser.CalculateConfidenceScore(result.GeneratedWorkflow);

                // Ensure at least some stages exist
                if (result.Stages == null || !result.Stages.Any())
                {
                    Logger.LogWarning("AI response did not contain valid stages, using fallback stages");
                    result = _responseParser.GenerateFallbackWorkflow(aiResponse.Content);
                }

                Logger.LogInformation("Successfully generated workflow with {StageCount} stages", result.Stages.Count);
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error generating workflow from description: {Description}", input.Description);

                // Save failed prompt history (fire-and-forget)
                if (!string.IsNullOrEmpty(prompt))
                {
                    QueueFailedPromptHistorySave(
                        "WorkflowGeneration", "Workflow", prompt, ex, startTime,
                        input.ModelProvider, input.ModelName, input.ModelId,
                        () => new
                        {
                            sessionId = input.SessionId,
                            error = ex.Message
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

        #endregion

        #region StreamGenerateWorkflowAsync

        /// <summary>
        /// Stream generate workflow with real-time updates
        /// </summary>
        public async IAsyncEnumerable<AIWorkflowStreamResult> StreamGenerateWorkflowAsync(AIWorkflowGenerationInput input)
        {
            var startTime = DateTime.UtcNow;

            Logger.LogInformation("Starting streaming workflow generation: {Description}", input.Description);

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

            // Use Channel producer-consumer pattern to avoid yield in try/catch
            var channel = Channel.CreateUnbounded<AIWorkflowStreamResult>();
            BackgroundTaskQueue.QueueBackgroundWorkItem(async token =>
            {
                await ProduceWorkflowStreamAsync(input, startTime, channel.Writer);
            });

            await foreach (var result in channel.Reader.ReadAllAsync())
            {
                yield return result;
            }
        }

        /// <summary>
        /// Producer method for streaming workflow generation via Channel
        /// </summary>
        private async Task ProduceWorkflowStreamAsync(
            AIWorkflowGenerationInput input,
            DateTime startTime,
            ChannelWriter<AIWorkflowStreamResult> writer)
        {
            var streamingContent = new StringBuilder();
            string prompt = null;
            try
            {
                // Try to use real streaming AI call
                prompt = _promptBuilder.BuildWorkflowGenerationPrompt(input);
                var hasReceivedContent = false;

                // Build chat message format
                var messages = new List<object>
                {
                    new { role = "system", content = "You are an AI workflow assistant that generates detailed business workflows." },
                    new { role = "user", content = prompt }
                };

                // Get user configuration
                AIModelConfig userConfig = null;
                if (!string.IsNullOrEmpty(input.ModelId) && long.TryParse(input.ModelId, out var modelId))
                {
                    userConfig = await _configService.GetConfigByIdAsync(modelId);
                }

                if (userConfig != null)
                {
                    var streamStartTime = DateTime.UtcNow;

                    // Send initial progress message
                    await writer.WriteAsync(new AIWorkflowStreamResult
                    {
                        Type = "progress",
                        Message = "Generating workflow structure...",
                        IsComplete = false
                    });
                    Logger.LogInformation("Initial progress message sent");

                    // Use unified streaming via IAIProviderAdapter
                    var lastProgressLength = 0;
                    var lastProgressTime = DateTime.UtcNow;
                    var providerName = userConfig.Provider?.ToLower() ?? "unknown";

                    Logger.LogInformation("Using {Provider} streaming - real-time progress updates", providerName);

                    var chatRequest = new AIChatProviderRequest
                    {
                        Messages = messages,
                        Config = userConfig,
                        Provider = userConfig.Provider
                    };

                    await foreach (var chunk in _providerAdapter.StreamChatAsync(chatRequest))
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

                                Logger.LogInformation("Progress update: {Length} chars, {Duration}ms since last",
                                    streamingContent.Length, timeSinceLastProgress);
                            }
                        }
                    }

                    var totalDuration = (DateTime.UtcNow - streamStartTime).TotalMilliseconds;
                    Logger.LogInformation("{Provider} stream completed: {Length} chars in {Duration}ms",
                        providerName, streamingContent.Length, totalDuration);

                    // After streaming completes, start parsing
                    if (hasReceivedContent)
                    {
                        await ProcessStreamingResultAsync(input, startTime, prompt, streamingContent, writer);
                        return;
                    }
                }
                else
                {
                    // Fallback to non-streaming API
                    var aiResponse = await _providerAdapter.CallAsync(new AIProviderRequest
                    {
                        Prompt = prompt,
                        ModelId = input.ModelId,
                        Provider = input.ModelProvider,
                        ModelName = input.ModelName
                    });

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

                // Parse AI response (non-streaming path)
                var result = _responseParser.ParseWorkflowResponse(streamingContent.ToString());

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
                Logger.LogError(ex, "Error in streaming workflow generation: {Description}", input.Description);

                // Save failed prompt history (fire-and-forget)
                if (!string.IsNullOrEmpty(prompt))
                {
                    QueueFailedPromptHistorySave(
                        "WorkflowGenerationStream", "Workflow", prompt, ex, startTime,
                        input.ModelProvider, input.ModelName, input.ModelId,
                        () => new
                        {
                            description = input.Description,
                            streamingMode = true,
                            error = ex.Message
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

        /// <summary>
        /// Process streaming result after content has been received
        /// </summary>
        private async Task ProcessStreamingResultAsync(
            AIWorkflowGenerationInput input,
            DateTime startTime,
            string prompt,
            StringBuilder streamingContent,
            ChannelWriter<AIWorkflowStreamResult> writer)
        {
            await writer.WriteAsync(new AIWorkflowStreamResult
            {
                Type = "progress",
                Message = "Parsing workflow structure...",
                IsComplete = false
            });

            Logger.LogInformation("Starting to parse AI response, content length: {Length}", streamingContent.Length);
            var parseStartTime = DateTime.UtcNow;
            var streamResult = _responseParser.ParseWorkflowResponse(streamingContent.ToString());
            var parseEndTime = DateTime.UtcNow;
            Logger.LogInformation("Parsing completed in {Duration}ms", (parseEndTime - parseStartTime).TotalMilliseconds);

            if (streamResult?.GeneratedWorkflow != null)
            {
                Logger.LogInformation("About to yield workflow and {Count} stages", streamResult.Stages?.Count ?? 0);

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
                        Logger.LogInformation("Processed {Count} stages so far...", stageCount);
                    }
                }
                var stageEndTime = DateTime.UtcNow;
                Logger.LogInformation("All {Count} stages yielded in {Duration}ms", stageCount, (stageEndTime - stageStartTime).TotalMilliseconds);

                // AI generation quality analysis
                var checklistCount = streamResult.Checklists?.Count ?? 0;
                var questionnaireCount = streamResult.Questionnaires?.Count ?? 0;
                stageCount = streamResult.Stages.Count;

                var effectiveChecklists = streamResult.Checklists?.Count(c => c?.Tasks?.Count > 0) ?? 0;
                var effectiveQuestionnaires = streamResult.Questionnaires?.Count(q => q?.Questions?.Count > 0) ?? 0;
                var qualityScore = stageCount > 0 ? ((effectiveChecklists + effectiveQuestionnaires) * 50.0) / stageCount : 0;

                Logger.LogInformation("AI generation quality: {QualityScore:F1}% - Effective components {EffectiveChecklists}/{ChecklistCount} checklists, {EffectiveQuestionnaires}/{QuestionnaireCount} questionnaires",
                    qualityScore, effectiveChecklists, checklistCount, effectiveQuestionnaires, questionnaireCount);

                // Ensure each stage has complete components
                streamResult.Checklists ??= new List<AIChecklistGenerationResult>();
                streamResult.Questionnaires ??= new List<AIQuestionnaireGenerationResult>();

                SupplementMissingComponents(streamResult);

                await writer.WriteAsync(new AIWorkflowStreamResult
                {
                    Type = "complete",
                    Data = streamResult,
                    Message = "Workflow generation completed with AI-powered components",
                    IsComplete = true
                });

                // Save successful streaming prompt history (fire-and-forget)
                QueuePromptHistorySave(
                    "WorkflowGenerationStream", "Workflow", prompt,
                    new AIProviderResponse { Success = true, Content = streamingContent.ToString() },
                    startTime, input.ModelProvider, input.ModelName, input.ModelId,
                    () => new
                    {
                        sessionId = input.SessionId,
                        conversationHistoryCount = input.ConversationHistory?.Count ?? 0,
                        streamingMode = true,
                        contentLength = streamingContent.Length
                    });

                Logger.LogInformation("StreamGenerateWorkflowAsync about to exit successfully");
            }
            else
            {
                Logger.LogWarning("AI response parsing failed, attempting JSON repair and fallback generation");

                var fallbackStartTime = DateTime.UtcNow;
                var fallbackResult = _responseParser.TryRepairAndParseWorkflow(streamingContent.ToString());

                if (fallbackResult == null)
                {
                    fallbackResult = _responseParser.GenerateFallbackWorkflow(streamingContent.ToString());
                }

                if (fallbackResult?.Stages?.Any() == true)
                {
                    Logger.LogInformation("Fallback workflow generated with {Count} stages", fallbackResult.Stages.Count);

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
            }
        }

        /// <summary>
        /// Supplement missing checklist and questionnaire components for stages
        /// </summary>
        private void SupplementMissingComponents(AIWorkflowGenerationResult streamResult)
        {
            var supplementStartTime = DateTime.UtcNow;
            var supplementCount = 0;

            var stageCount = streamResult.Stages.Count;
            var checklistCount = streamResult.Checklists?.Count ?? 0;
            var questionnaireCount = streamResult.Questionnaires?.Count ?? 0;

            var hasCompleteGeneration = checklistCount >= stageCount && questionnaireCount >= stageCount &&
                streamResult.Checklists.Take(stageCount).All(c => c?.Tasks?.Count > 0) &&
                streamResult.Questionnaires.Take(stageCount).All(q => q?.Questions?.Count > 0);

            if (hasCompleteGeneration)
            {
                Logger.LogInformation("AI generated all components completely, skipping supplement logic - {ChecklistCount} checklists, {QuestionnaireCount} questionnaires",
                    checklistCount, questionnaireCount);
            }
            else
            {
                Logger.LogInformation("Detected missing components, starting intelligent supplement - Existing: {ChecklistCount}/{StageCount} checklists, {QuestionnaireCount}/{StageCount} questionnaires",
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
                            Logger.LogDebug("Added empty checklist for stage {StageIndex}-{StageName}", i, stage.Name);
                        }
                    }

                    if (i >= questionnaireCount || streamResult.Questionnaires[i]?.Questions?.Count == 0)
                    {
                        if (i >= questionnaireCount)
                        {
                            streamResult.Questionnaires.Add(GenerateFallbackQuestionnaire(stage));
                            supplementCount++;
                            Logger.LogDebug("Added empty questionnaire for stage {StageIndex}-{StageName}", i, stage.Name);
                        }
                    }
                }
            }

            if (supplementCount > 0)
            {
                Logger.LogWarning("AI generation incomplete, supplemented {SupplementCount} empty structures ({Duration:F1}ms) - Final {ChecklistCount} checklists + {QuestionnaireCount} questionnaires",
                    supplementCount,
                    (DateTime.UtcNow - supplementStartTime).TotalMilliseconds,
                    streamResult.Checklists.Count,
                    streamResult.Questionnaires.Count);
            }
            else
            {
                Logger.LogInformation("AI generation perfect: no supplement needed");
            }
        }

        #endregion

        #region EnhanceWorkflowAsync

        /// <summary>
        /// Enhance existing workflow with AI suggestions
        /// </summary>
        public async Task<AIWorkflowEnhancementResult> EnhanceWorkflowAsync(long workflowId, string enhancement)
        {
            try
            {
                Logger.LogInformation("Enhancing workflow {WorkflowId} with: {Enhancement}", workflowId, enhancement);

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

                var aiResponse = await _providerAdapter.CallAsync(new AIProviderRequest
                {
                    Prompt = prompt
                });

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
                Logger.LogError(ex, "Error enhancing workflow {WorkflowId}", workflowId);
                return new AIWorkflowEnhancementResult
                {
                    Success = false,
                    Message = $"Failed to enhance workflow: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Enhance existing workflow using modification input
        /// </summary>
        public async Task<AIWorkflowGenerationResult> EnhanceWorkflowAsync(AIWorkflowModificationInput input)
        {
            var result = new AIWorkflowGenerationResult();

            try
            {
                Logger.LogInformation("Modifying workflow {WorkflowId}: {Description}",
                    input.WorkflowId, input.Description);

                // Get detailed information of existing workflow
                Logger.LogInformation("Fetching existing workflow with ID: {WorkflowId}", input.WorkflowId);
                var existingWorkflowInfo = await GetExistingWorkflowAsync(input.WorkflowId);
                Logger.LogInformation("Retrieved workflow: Name={Name}, Description={Description}, StageCount={StageCount}",
                    existingWorkflowInfo.Name, existingWorkflowInfo.Description, existingWorkflowInfo.Stages.Count);

                // Record detailed information of existing stages
                for (int i = 0; i < existingWorkflowInfo.Stages.Count; i++)
                {
                    var stage = existingWorkflowInfo.Stages[i];
                    Logger.LogInformation("Stage {Index}: Name='{Name}', Description='{Description}', Duration={Duration}, Team='{Team}'",
                        i + 1, stage.Name, stage.Description, stage.EstimatedDuration, stage.AssignedGroup);
                }

                // Build modification prompt
                var prompt = await _promptBuilder.BuildWorkflowModificationPromptAsync(input, existingWorkflowInfo);

                // Debug log: output complete prompt
                Logger.LogInformation("AI Modification Prompt: {Prompt}", prompt);

                // Call AI for workflow modification
                var aiResponse = await _providerAdapter.CallAsync(new AIProviderRequest
                {
                    Prompt = prompt
                });

                // Debug log: output AI response
                Logger.LogInformation("AI Modification Response: Success={Success}, Content={Content}",
                    aiResponse.Success, aiResponse.Content);

                if (!aiResponse.Success)
                {
                    result.Success = false;
                    result.Message = aiResponse.ErrorMessage;
                    return _responseParser.GenerateFallbackWorkflow($"Error modifying workflow {input.WorkflowId}");
                }

                // Parse AI response
                var modificationResult = _responseParser.ParseWorkflowResponse(aiResponse.Content);

                if (modificationResult.Stages == null || !modificationResult.Stages.Any())
                {
                    modificationResult = _responseParser.GenerateFallbackWorkflow($"Modified workflow for ID: {input.WorkflowId}");
                }

                // Force ensure workflow name is correct (prevent AI from not following instructions)
                Logger.LogInformation("Checking workflow name correction: AI returned '{AIName}', expected '{ExpectedName}'",
                    modificationResult.GeneratedWorkflow?.Name ?? "NULL", existingWorkflowInfo.Name);

                if (modificationResult.GeneratedWorkflow != null &&
                    modificationResult.GeneratedWorkflow.Name != existingWorkflowInfo.Name)
                {
                    Logger.LogWarning("AI returned incorrect workflow name '{AIName}', forcing to correct name '{CorrectName}'",
                        modificationResult.GeneratedWorkflow.Name, existingWorkflowInfo.Name);

                    var originalName = modificationResult.GeneratedWorkflow.Name;
                    modificationResult.GeneratedWorkflow.Name = existingWorkflowInfo.Name;
                    modificationResult.GeneratedWorkflow.Description = existingWorkflowInfo.Description + " - Modified based on user requirements";

                    Logger.LogInformation("Name correction applied: '{OriginalName}' -> '{CorrectedName}'",
                        originalName, modificationResult.GeneratedWorkflow.Name);
                }
                else
                {
                    Logger.LogInformation("Workflow name is correct: '{Name}'", modificationResult.GeneratedWorkflow?.Name);
                }

                result = modificationResult;
                result.Message = "Workflow modified successfully";

                Logger.LogInformation("Workflow modification completed successfully for ID: {WorkflowId}", input.WorkflowId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to modify workflow {WorkflowId}", input.WorkflowId);
                result.Success = false;
                result.Message = "Workflow modification failed";
                result = _responseParser.GenerateFallbackWorkflow($"Error modifying workflow {input.WorkflowId}");
            }

            return result;
        }

        #endregion

        #region ValidateWorkflowAsync

        /// <summary>
        /// Validate and suggest improvements for workflow
        /// </summary>
        public async Task<AIValidationResult> ValidateWorkflowAsync(WorkflowInputDto workflow)
        {
            try
            {
                Logger.LogInformation("Validating workflow: {WorkflowName}", workflow.Name);

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

                var qualityScore = _responseParser.CalculateWorkflowQualityScore(workflow, issues);

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
                Logger.LogError(ex, "Error validating workflow");
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

        #endregion

        #region CreateStageComponentsAsync

        /// <summary>
        /// Create actual checklist and questionnaire records and associate them with stages.
        /// Delegates to IStageComponentService.
        /// </summary>
        public async Task<bool> CreateStageComponentsAsync(
            long workflowId,
            List<AIStageGenerationResult> stages,
            List<AIChecklistGenerationResult> checklists,
            List<AIQuestionnaireGenerationResult> questionnaires)
        {
            return await _stageComponentService.CreateStageComponentsAsync(workflowId, stages, checklists, questionnaires);
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Get existing workflow information for modification
        /// </summary>
        private async Task<WorkflowInfo> GetExistingWorkflowAsync(long workflowId)
        {
            try
            {
                Logger.LogInformation("Attempting to fetch workflow with ID: {WorkflowId}", workflowId);
                var workflow = await _workflowService.GetByIdAsync(workflowId);

                if (workflow != null)
                {
                    Logger.LogInformation("Successfully retrieved workflow: Name={Name}, Description={Description}, StageCount={StageCount}",
                        workflow.Name, workflow.Description, workflow.Stages?.Count ?? 0);

                    var workflowInfo = new WorkflowInfo
                    {
                        Name = workflow.Name,
                        Description = workflow.Description,
                        Stages = workflow.Stages?.Select(s => new WorkflowStageInfo
                        {
                            Name = s.Name,
                            Description = s.Description,
                            EstimatedDuration = (int)(s.EstimatedDuration ?? 1),
                            AssignedGroup = s.DefaultAssignedGroup ?? "Default Team"
                        }).ToList() ?? new List<WorkflowStageInfo>()
                    };

                    Logger.LogInformation("Converted to WorkflowInfo: Name={Name}, StageCount={StageCount}",
                        workflowInfo.Name, workflowInfo.Stages.Count);

                    return workflowInfo;
                }
                else
                {
                    Logger.LogWarning("Workflow with ID {WorkflowId} not found", workflowId);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error fetching workflow {WorkflowId}", workflowId);
            }

            // If retrieval fails, return default data
            Logger.LogWarning("Returning default workflow data for ID {WorkflowId}", workflowId);
            return new WorkflowInfo
            {
                Name = "Default Workflow",
                Description = "Default workflow description",
                Stages = new List<WorkflowStageInfo>
                {
                    new WorkflowStageInfo { Name = "Default Stage", Description = "Default stage description", EstimatedDuration = 1, AssignedGroup = "Default Team" }
                }
            };
        }

        /// <summary>
        /// Call AI provider with retry logic for transient failures
        /// </summary>
        private async Task<AIProviderResponse> CallAIProviderWithRetryAsync(string prompt, int maxRetries = 2)
        {
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        // Quick retry: 2s, 5s - suitable for network instability
                        var delay = TimeSpan.FromSeconds(attempt == 1 ? 2 : 5);
                        Logger.LogWarning("AI call retry #{Attempt} after {Delay}ms delay", attempt, delay.TotalMilliseconds);
                        await Task.Delay(delay);
                    }

                    var response = await _providerAdapter.CallAsync(new AIProviderRequest
                    {
                        Prompt = prompt
                    });

                    if (response.Success)
                    {
                        if (attempt > 0)
                        {
                            Logger.LogInformation("AI call succeeded on retry #{Attempt}", attempt);
                        }
                        return response;
                    }
                    else if (attempt == maxRetries)
                    {
                        // Last attempt failed
                        Logger.LogWarning("AI call failed after {MaxRetries} retries: {Error}", maxRetries, response.ErrorMessage);
                        return response;
                    }
                }
                catch (TaskCanceledException) when (attempt < maxRetries)
                {
                    Logger.LogWarning("AI call timeout on attempt #{Attempt}, retrying...", attempt + 1);
                    continue;
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    Logger.LogWarning(ex, "AI call error on attempt #{Attempt}, retrying...", attempt + 1);
                    continue;
                }
            }

            // If all retries fail, return error response
            return new AIProviderResponse
            {
                Success = false,
                ErrorMessage = $"AI call failed after {maxRetries} retries",
                Content = string.Empty
            };
        }

        /// <summary>
        /// Generate a fallback checklist for a stage when AI generation is incomplete
        /// </summary>
        private static AIChecklistGenerationResult GenerateFallbackChecklist(AIStageGenerationResult stage)
        {
            return new AIChecklistGenerationResult
            {
                Success = true,
                GeneratedChecklist = new ChecklistInputDto
                {
                    Name = $"{stage.Name} Checklist",
                    Description = $"Checklist for {stage.Name}"
                },
                Tasks = new List<AITaskGenerationResult>(),
                ConfidenceScore = 0.3
            };
        }

        /// <summary>
        /// Generate a fallback questionnaire for a stage when AI generation is incomplete
        /// </summary>
        private static AIQuestionnaireGenerationResult GenerateFallbackQuestionnaire(AIStageGenerationResult stage)
        {
            return new AIQuestionnaireGenerationResult
            {
                Success = true,
                GeneratedQuestionnaire = new QuestionnaireInputDto
                {
                    Name = $"{stage.Name} Questionnaire",
                    Description = $"Questionnaire for {stage.Name}"
                },
                Questions = new List<AIQuestionGenerationResult>(),
                ConfidenceScore = 0.3
            };
        }

        #endregion
    }
}
