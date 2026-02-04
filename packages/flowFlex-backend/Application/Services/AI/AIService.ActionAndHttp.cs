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
        #region Action Analysis and Creation

        /// <summary>
        /// Analyze conversation to extract action insights
        /// </summary>
        /// <param name="input">Action analysis input</param>
        /// <returns>Action analysis result</returns>
        public async Task<AIActionAnalysisResult> AnalyzeActionAsync(AIActionAnalysisInput input)
        {
            var startTime = DateTime.UtcNow;
            string prompt = string.Empty;
            AIProviderResponse aiResponse;

            try
            {
                _logger.LogInformation("Analyzing conversation for action insights, SessionId: {SessionId}", input.SessionId);

                // Build conversation analysis prompt
                prompt = BuildActionAnalysisPrompt(input);

                // Call AI provider with automatic model switching and fallback
                aiResponse = await CallAIProviderWithFallbackForActionAsync(prompt, input.ModelId, input.ModelProvider, input.ModelName);

                // Save prompt history to database (fire-and-forget)
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        var metadata = JsonSerializer.Serialize(new
                        {
                            sessionId = input.SessionId,
                            conversationLength = input.ConversationHistory?.Count ?? 0,
                            focusAreasCount = input.FocusAreas?.Count ?? 0,
                            hasContext = !string.IsNullOrEmpty(input.Context)
                        });
                        await SavePromptHistoryAsync("ActionAnalysis", "Action", null, null,
                            prompt, aiResponse, startTime, aiResponse.Provider, aiResponse.ModelName, aiResponse.ModelId, metadata);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to save action analysis prompt history");
                    }
                });

                if (!aiResponse.Success)
                {
                    return new AIActionAnalysisResult
                    {
                        Success = false,
                        Message = aiResponse.ErrorMessage
                    };
                }

                // Parse the AI response
                var result = ParseActionAnalysisResponse(aiResponse.Content, input);
                result.Success = true;
                result.Message = "Action analysis completed successfully";

                _logger.LogInformation("Action analysis completed successfully for SessionId: {SessionId}", input.SessionId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing conversation for actions, SessionId: {SessionId}", input.SessionId);
                return new AIActionAnalysisResult
                {
                    Success = false,
                    Message = $"Failed to analyze conversation: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Create action based on conversation analysis
        /// </summary>
        /// <param name="input">Action creation input</param>
        /// <returns>Action creation result</returns>
        public async Task<AIActionCreationResult> CreateActionAsync(AIActionCreationInput input)
        {
            var startTime = DateTime.UtcNow;
            string prompt = string.Empty;
            AIProviderResponse aiResponse;

            try
            {
                _logger.LogInformation("Creating action plan based on analysis or description");

                // Build action creation prompt
                prompt = BuildActionCreationPrompt(input);

                // Call AI provider with automatic model switching and fallback
                aiResponse = await CallAIProviderWithFallbackForActionAsync(prompt, input.ModelId, input.ModelProvider, input.ModelName);

                // Save prompt history to database (fire-and-forget)
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        var metadata = JsonSerializer.Serialize(new
                        {
                            hasAnalysisResult = input.AnalysisResult != null,
                            actionDescriptionLength = input.ActionDescription?.Length ?? 0,
                            stakeholdersCount = input.Stakeholders?.Count ?? 0,
                            priority = input.Priority,
                            hasDueDate = input.DueDate.HasValue
                        });
                        await SavePromptHistoryAsync("ActionCreation", "Action", null, null,
                            prompt, aiResponse, startTime, aiResponse.Provider, aiResponse.ModelName, aiResponse.ModelId, metadata);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to save action creation prompt history");
                    }
                });

                if (!aiResponse.Success)
                {
                    return new AIActionCreationResult
                    {
                        Success = false,
                        Message = aiResponse.ErrorMessage
                    };
                }

                // Parse the AI response
                var result = ParseActionCreationResponse(aiResponse.Content, input);
                result.Success = true;
                result.Message = "Action plan created successfully";

                _logger.LogInformation("Action plan created successfully");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating action plan: {Error}", ex.Message);
                return new AIActionCreationResult
                {
                    Success = false,
                    Message = $"Failed to create action plan: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Build prompt for action analysis
        /// </summary>
        private string BuildActionAnalysisPrompt(AIActionAnalysisInput input)
        {
            // Optimize conversation text - limit to last 10 messages and truncate long messages
            var recentMessages = input.ConversationHistory.TakeLast(10).ToList();
            var conversationText = string.Join("\n", recentMessages.Select(m =>
            {
                var content = m.Content.Length > 500 ? m.Content.Substring(0, 500) + "..." : m.Content;
                return $"{m.Role}: {content}";
            }));

            var focusAreasText = input.FocusAreas.Any() ? string.Join(", ", input.FocusAreas) : "general action items";
            var contextText = !string.IsNullOrEmpty(input.Context) && input.Context.Length > 200
                ? input.Context.Substring(0, 200) + "..."
                : input.Context ?? "No additional context provided";

            return $@"Analyze this conversation and extract actionable insights (be concise):

CONVERSATION:
{conversationText}

CONTEXT: {contextText}
FOCUS: {focusAreasText}

Please analyze and extract:
1. Action Items: Specific tasks or actions that need to be taken
2. Key Insights: Important observations or conclusions from the conversation
3. Next Steps: Recommended follow-up actions
4. Stakeholders: People or roles involved or affected
5. Priority Level: Overall urgency (Low, Medium, High, Critical)

For each action item, include:
- Title: Brief descriptive title
- Description: Detailed description of what needs to be done
- Category: Type of action (task, decision, communication, etc.)
- Priority: Individual priority level
- Assigned To: Suggested person or role (if mentioned)
- Dependencies: Other actions this depends on

Please return the results in JSON format with the following structure:
{{
    ""actionItems"": [
        {{
            ""id"": ""unique_id"",
            ""title"": ""action title"",
            ""description"": ""detailed description"",
            ""category"": ""category"",
            ""priority"": ""priority level"",
            ""assignedTo"": ""person or role"",
            ""dependencies"": [""dependency1"", ""dependency2""],
            ""tags"": [""tag1"", ""tag2""]
        }}
    ],
    ""keyInsights"": [""insight1"", ""insight2""],
    ""nextSteps"": [""step1"", ""step2""],
    ""stakeholders"": [""stakeholder1"", ""stakeholder2""],
    ""priority"": ""overall priority"",
    ""confidenceScore"": 0.85
}}";
        }

        /// <summary>
        /// Build prompt for action creation
        /// </summary>
        private string BuildActionCreationPrompt(AIActionCreationInput input)
        {
            var basePrompt = "";

            if (input.AnalysisResult != null)
            {
                // Optimize analysis result - only include essential fields
                var essentialAnalysis = new
                {
                    actionItems = input.AnalysisResult.ActionItems?.Take(5).Select(a => new
                    {
                        title = a.Title?.Length > 100 ? a.Title.Substring(0, 100) + "..." : a.Title,
                        description = a.Description?.Length > 200 ? a.Description.Substring(0, 200) + "..." : a.Description,
                        priority = a.Priority
                    }),
                    keyInsights = input.AnalysisResult.KeyInsights?.Take(3).Select(i =>
                        i.Length > 150 ? i.Substring(0, 150) + "..." : i),
                    nextSteps = input.AnalysisResult.NextSteps?.Take(5).Select(s =>
                        s.Length > 100 ? s.Substring(0, 100) + "..." : s)
                };

                var analysisJson = JsonSerializer.Serialize(essentialAnalysis, new JsonSerializerOptions { WriteIndented = false });
                var contextText = !string.IsNullOrEmpty(input.Context) && input.Context.Length > 300
                    ? input.Context.Substring(0, 300) + "..."
                    : input.Context ?? "";

                basePrompt = $"""
                    Create action plan from analysis (be concise):

                    ANALYSIS: {analysisJson}
                    CONTEXT: {contextText}
                    """;
            }
            else
            {
                var actionDesc = input.ActionDescription?.Length > 500
                    ? input.ActionDescription.Substring(0, 500) + "..."
                    : input.ActionDescription ?? "";
                var contextText = !string.IsNullOrEmpty(input.Context) && input.Context.Length > 300
                    ? input.Context.Substring(0, 300) + "..."
                    : input.Context ?? "";

                basePrompt = $"""
                    Create action plan (be concise):

                    DESCRIPTION: {actionDesc}
                    CONTEXT: {contextText}
                    """;
            }

            var stakeholdersText = input.Stakeholders.Any() ? string.Join(", ", input.Stakeholders) : "to be determined";
            var requirementsText = input.Requirements.Any() ? string.Join("\n", input.Requirements) : "none specified";

            return basePrompt + $@"

STAKEHOLDERS:
{stakeholdersText}

PRIORITY:
{input.Priority}

DUE DATE:
{(input.DueDate?.ToString("yyyy-MM-dd") ?? "not specified")}

REQUIREMENTS:
{requirementsText}

Please create a detailed action plan that includes:
1. Action Plan Overview: Title, description, objective
2. Individual Actions: Specific tasks with details
3. Implementation Steps: How to execute the plan
4. Risk Factors: Potential challenges or obstacles
5. Success Metrics: How to measure success

Please return the results in JSON format with the following structure:
{{
    ""actionPlan"": {{
        ""id"": ""unique_plan_id"",
        ""title"": ""action plan title"",
        ""description"": ""detailed description"",
        ""objective"": ""main objective"",
        ""actions"": [
            {{
                ""id"": ""action_id"",
                ""title"": ""action title"",
                ""description"": ""action description"",
                ""category"": ""category"",
                ""priority"": ""priority"",
                ""status"": ""Pending"",
                ""assignedTo"": ""person or role"",
                ""dependencies"": [""dependency1""],
                ""tags"": [""tag1"", ""tag2""]
            }}
        ],
        ""stakeholders"": [""stakeholder1"", ""stakeholder2""],
        ""priority"": ""{input.Priority}"",
        ""startDate"": ""{DateTime.UtcNow:yyyy-MM-dd}"",
        ""endDate"": ""{(input.DueDate?.ToString("yyyy-MM-dd") ?? "")}"",
        ""status"": ""Draft""
    }},
    ""implementationSteps"": [""step1"", ""step2"", ""step3""],
    ""riskFactors"": [""risk1"", ""risk2""],
    ""successMetrics"": [""metric1"", ""metric2""],
    ""confidenceScore"": 0.85
}}";
        }

        /// <summary>
        /// Parse action analysis response
        /// </summary>
        private AIActionAnalysisResult ParseActionAnalysisResponse(string aiResponse, AIActionAnalysisInput input)
        {
            try
            {
                // Try to parse JSON response
                var jsonStart = aiResponse.IndexOf('{');
                var jsonEnd = aiResponse.LastIndexOf('}');

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonContent = aiResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    var parsed = JsonSerializer.Deserialize<JsonElement>(jsonContent);

                    var result = new AIActionAnalysisResult();

                    // Parse action items
                    if (parsed.TryGetProperty("actionItems", out var actionItemsEl) && actionItemsEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var itemEl in actionItemsEl.EnumerateArray())
                        {
                            var actionItem = new AIActionItem
                            {
                                Id = itemEl.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString(),
                                Title = itemEl.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? "" : "",
                                Description = itemEl.TryGetProperty("description", out var descEl) ? descEl.GetString() ?? "" : "",
                                Category = itemEl.TryGetProperty("category", out var catEl) ? catEl.GetString() ?? "" : "",
                                Priority = itemEl.TryGetProperty("priority", out var priEl) ? priEl.GetString() ?? "Medium" : "Medium",
                                AssignedTo = itemEl.TryGetProperty("assignedTo", out var assignEl) ? assignEl.GetString() ?? "" : "",
                                Status = "Pending"
                            };

                            // Parse dependencies
                            if (itemEl.TryGetProperty("dependencies", out var depsEl) && depsEl.ValueKind == JsonValueKind.Array)
                            {
                                actionItem.Dependencies = depsEl.EnumerateArray().Select(d => d.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
                            }

                            // Parse tags
                            if (itemEl.TryGetProperty("tags", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.Array)
                            {
                                actionItem.Tags = tagsEl.EnumerateArray().Select(t => t.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
                            }

                            result.ActionItems.Add(actionItem);
                        }
                    }

                    // Parse other fields
                    if (parsed.TryGetProperty("keyInsights", out var insightsEl) && insightsEl.ValueKind == JsonValueKind.Array)
                    {
                        result.KeyInsights = insightsEl.EnumerateArray().Select(i => i.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
                    }

                    if (parsed.TryGetProperty("nextSteps", out var stepsEl) && stepsEl.ValueKind == JsonValueKind.Array)
                    {
                        result.NextSteps = stepsEl.EnumerateArray().Select(s => s.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
                    }

                    if (parsed.TryGetProperty("stakeholders", out var stakeholdersEl) && stakeholdersEl.ValueKind == JsonValueKind.Array)
                    {
                        result.Stakeholders = stakeholdersEl.EnumerateArray().Select(s => s.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
                    }

                    result.Priority = parsed.TryGetProperty("priority", out var priorityEl) ? priorityEl.GetString() ?? "Medium" : "Medium";
                    result.ConfidenceScore = parsed.TryGetProperty("confidenceScore", out var confEl) ? confEl.GetDouble() : 0.7;

                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse action analysis JSON response, falling back to text parsing");
            }

            // Fallback: Generate basic result from conversation
            return GenerateFallbackActionAnalysis(input);
        }

        /// <summary>
        /// Parse action creation response
        /// </summary>
        private AIActionCreationResult ParseActionCreationResponse(string aiResponse, AIActionCreationInput input)
        {
            try
            {
                // Try to parse JSON response
                var jsonStart = aiResponse.IndexOf('{');
                var jsonEnd = aiResponse.LastIndexOf('}');

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonContent = aiResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    var parsed = JsonSerializer.Deserialize<JsonElement>(jsonContent);

                    var result = new AIActionCreationResult();

                    // Parse action plan
                    if (parsed.TryGetProperty("actionPlan", out var planEl))
                    {
                        result.ActionPlan = new AIActionPlan
                        {
                            Id = planEl.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString(),
                            Title = planEl.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? "" : "",
                            Description = planEl.TryGetProperty("description", out var descEl) ? descEl.GetString() ?? "" : "",
                            Objective = planEl.TryGetProperty("objective", out var objEl) ? objEl.GetString() ?? "" : "",
                            Priority = planEl.TryGetProperty("priority", out var priEl) ? priEl.GetString() ?? input.Priority : input.Priority,
                            Status = planEl.TryGetProperty("status", out var statusEl) ? statusEl.GetString() ?? "Draft" : "Draft"
                        };

                        // Parse start and end dates
                        if (planEl.TryGetProperty("startDate", out var startEl) && DateTime.TryParse(startEl.GetString(), out var startDate))
                        {
                            result.ActionPlan.StartDate = startDate;
                        }

                        if (planEl.TryGetProperty("endDate", out var endEl) && DateTime.TryParse(endEl.GetString(), out var endDate))
                        {
                            result.ActionPlan.EndDate = endDate;
                        }

                        // Parse stakeholders
                        if (planEl.TryGetProperty("stakeholders", out var stakeholdersEl) && stakeholdersEl.ValueKind == JsonValueKind.Array)
                        {
                            result.ActionPlan.Stakeholders = stakeholdersEl.EnumerateArray().Select(s => s.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
                        }

                        // Parse actions
                        if (planEl.TryGetProperty("actions", out var actionsEl) && actionsEl.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var actionEl in actionsEl.EnumerateArray())
                            {
                                var action = new AIActionItem
                                {
                                    Id = actionEl.TryGetProperty("id", out var actionIdEl) ? actionIdEl.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString(),
                                    Title = actionEl.TryGetProperty("title", out var actionTitleEl) ? actionTitleEl.GetString() ?? "" : "",
                                    Description = actionEl.TryGetProperty("description", out var actionDescEl) ? actionDescEl.GetString() ?? "" : "",
                                    Category = actionEl.TryGetProperty("category", out var catEl) ? catEl.GetString() ?? "" : "",
                                    Priority = actionEl.TryGetProperty("priority", out var actionPriEl) ? actionPriEl.GetString() ?? "Medium" : "Medium",
                                    Status = actionEl.TryGetProperty("status", out var actionStatusEl) ? actionStatusEl.GetString() ?? "Pending" : "Pending",
                                    AssignedTo = actionEl.TryGetProperty("assignedTo", out var assignEl) ? assignEl.GetString() ?? "" : ""
                                };

                                // Parse dependencies and tags
                                if (actionEl.TryGetProperty("dependencies", out var depsEl) && depsEl.ValueKind == JsonValueKind.Array)
                                {
                                    action.Dependencies = depsEl.EnumerateArray().Select(d => d.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
                                }

                                if (actionEl.TryGetProperty("tags", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.Array)
                                {
                                    action.Tags = tagsEl.EnumerateArray().Select(t => t.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
                                }

                                result.ActionPlan.Actions.Add(action);
                            }
                        }
                    }

                    // Parse other fields
                    if (parsed.TryGetProperty("implementationSteps", out var stepsEl) && stepsEl.ValueKind == JsonValueKind.Array)
                    {
                        result.ImplementationSteps = stepsEl.EnumerateArray().Select(s => s.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
                    }

                    if (parsed.TryGetProperty("riskFactors", out var risksEl) && risksEl.ValueKind == JsonValueKind.Array)
                    {
                        result.RiskFactors = risksEl.EnumerateArray().Select(r => r.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
                    }

                    if (parsed.TryGetProperty("successMetrics", out var metricsEl) && metricsEl.ValueKind == JsonValueKind.Array)
                    {
                        result.SuccessMetrics = metricsEl.EnumerateArray().Select(m => m.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
                    }

                    result.ConfidenceScore = parsed.TryGetProperty("confidenceScore", out var confEl) ? confEl.GetDouble() : 0.7;

                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse action creation JSON response, falling back to basic result");
            }

            // Fallback: Generate basic result
            return GenerateFallbackActionCreation(input);
        }

        /// <summary>
        /// Generate fallback action analysis when parsing fails
        /// </summary>
        private AIActionAnalysisResult GenerateFallbackActionAnalysis(AIActionAnalysisInput input)
        {
            var result = new AIActionAnalysisResult
            {
                Priority = "Medium",
                ConfidenceScore = 0.5
            };

            // Generate basic action items from conversation
            if (input.ConversationHistory.Any())
            {
                var userMessages = input.ConversationHistory.Where(m => m.Role == "user").ToList();
                for (int i = 0; i < Math.Min(userMessages.Count, 3); i++)
                {
                    var message = userMessages[i];
                    result.ActionItems.Add(new AIActionItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = $"Follow up on: {message.Content.Substring(0, Math.Min(50, message.Content.Length))}...",
                        Description = $"Address the request or question: {message.Content}",
                        Category = "Follow-up",
                        Priority = "Medium",
                        Status = "Pending"
                    });
                }

                result.KeyInsights.Add("Conversation contains multiple discussion points that require follow-up");
                result.NextSteps.Add("Review conversation details and prioritize action items");
                result.NextSteps.Add("Assign responsibilities to appropriate team members");
            }

            return result;
        }

        /// <summary>
        /// Generate fallback action creation when parsing fails
        /// </summary>
        private AIActionCreationResult GenerateFallbackActionCreation(AIActionCreationInput input)
        {
            var result = new AIActionCreationResult
            {
                ConfidenceScore = 0.5
            };

            result.ActionPlan = new AIActionPlan
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Action Plan",
                Description = !string.IsNullOrEmpty(input.ActionDescription) ? input.ActionDescription : "Generated action plan",
                Objective = "Complete the requested actions",
                Priority = input.Priority,
                Status = "Draft",
                StartDate = DateTime.UtcNow,
                EndDate = input.DueDate,
                Stakeholders = input.Stakeholders.ToList()
            };

            // Add basic action items
            if (input.AnalysisResult?.ActionItems.Any() == true)
            {
                result.ActionPlan.Actions = input.AnalysisResult.ActionItems.ToList();
            }
            else
            {
                result.ActionPlan.Actions.Add(new AIActionItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Primary Action",
                    Description = input.ActionDescription,
                    Category = "Task",
                    Priority = input.Priority,
                    Status = "Pending"
                });
            }

            result.ImplementationSteps.Add("Review action plan details");
            result.ImplementationSteps.Add("Assign responsibilities");
            result.ImplementationSteps.Add("Execute actions according to priority");
            result.ImplementationSteps.Add("Monitor progress and adjust as needed");

            return result;
        }

        /// <summary>
        /// Optimized AI provider call for Action operations with shorter timeouts
        /// </summary>
        private async Task<AIProviderResponse> CallAIProviderForActionAsync(string prompt, string? modelId = null, string? provider = null, string? modelName = null)
        {
            try
            {
                // Use shorter timeout for Action operations to improve responsiveness
                var originalTimeout = _aiOptions.ConnectionTest.TimeoutSeconds;

                // Temporarily reduce timeout for Action operations
                // DeepSeek: 15 seconds, others: 20 seconds
                var actionTimeout = provider?.ToLower() switch
                {
                    "deepseek" => 15,
                    "zhipuai" => 18,
                    "openai" => 20,
                    "claude" => 20,
                    _ => 18
                };

                _logger.LogInformation("Using optimized timeout for Action API: {Provider} - {Timeout}s", provider ?? "default", actionTimeout);

                // Call the appropriate provider method with optimized settings
                if (!string.IsNullOrEmpty(provider) && !string.IsNullOrEmpty(modelName))
                {
                    switch (provider.ToLower())
                    {
                        case "deepseek":
                            if (long.TryParse(modelId, out var deepSeekModelId))
                            {
                                var deepSeekConfig = await _configService.GetConfigByIdAsync(deepSeekModelId);
                                if (deepSeekConfig != null)
                                {
                                    return await CallDeepSeekWithOptimizedTimeoutAsync(prompt, deepSeekConfig, actionTimeout);
                                }
                            }
                            break;
                        case "zhipuai":
                            return await CallZhipuAIWithOptimizedTimeoutAsync(prompt, actionTimeout);
                        case "openai":
                            if (long.TryParse(modelId, out var openAIModelId))
                            {
                                var openAIConfig = await _configService.GetConfigByIdAsync(openAIModelId);
                                if (openAIConfig != null)
                                {
                                    return await CallOpenAIWithOptimizedTimeoutAsync(prompt, openAIConfig, actionTimeout);
                                }
                            }
                            break;
                        case "claude":
                            if (long.TryParse(modelId, out var claudeModelId))
                            {
                                var claudeConfig = await _configService.GetConfigByIdAsync(claudeModelId);
                                if (claudeConfig != null)
                                {
                                    return await CallClaudeWithOptimizedTimeoutAsync(prompt, claudeConfig, actionTimeout);
                                }
                            }
                            break;
                        case "gemini":
                            if (long.TryParse(modelId, out var geminiModelId))
                            {
                                var geminiConfig = await _configService.GetConfigByIdAsync(geminiModelId);
                                if (geminiConfig != null)
                                {
                                    return await CallGeminiWithOptimizedTimeoutAsync(prompt, geminiConfig, actionTimeout);
                                }
                            }
                            break;
                    }
                }

                // Fallback to regular method if specific provider method not available
                return await CallAIProviderAsync(prompt, modelId, provider, modelName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in optimized AI provider call for Action");
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"AI provider call failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Call DeepSeek with optimized timeout for Action operations
        /// </summary>
        private async Task<AIProviderResponse> CallDeepSeekWithOptimizedTimeoutAsync(string prompt, AIModelConfig config, int timeoutSeconds)
        {
            try
            {
                // For native DeepSeek API, strip provider prefix from model name
                var modelName = GetNativeModelName(config.ModelName, config.Provider);

                var requestBody = new
                {
                    model = modelName,
                    messages = new[]
                    {
                        new { role = "system", content = "You are a professional action analysis and creation expert. Generate structured responses based on user requirements. CRITICAL: Always provide complete and full JSON responses. NEVER truncate data or use '...' patterns. Include ALL data fields completely without any omissions. IMPORTANT: When extracting HTTP headers, especially 'authorization' values (Bearer tokens, JWT, API keys), you MUST copy them EXACTLY as provided - do NOT modify, shorten, or use placeholders. Complete JSON is mandatory." },
                        new { role = "user", content = prompt }
                    },
                    temperature = Math.Min(config.Temperature > 0 ? config.Temperature : 0.7, 0.8), // Lower temperature for more consistent results
                    max_tokens = Math.Min(config.MaxTokens > 0 ? config.MaxTokens : 3000, 4000), // Increased limit for complete JSON responses
                    stream = false
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var baseUrl = config.BaseUrl.TrimEnd('/');
                string apiUrl = baseUrl.Contains("/chat/completions") ? baseUrl :
                               baseUrl.Contains("/v1") ? $"{baseUrl}/chat/completions" : $"{baseUrl}/v1/chat/completions";

                _logger.LogInformation("DeepSeek Action API Request - Model: {Model}, Timeout: {Timeout}s", config.ModelName, timeoutSeconds);

                using var httpClient = _httpClientFactory.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
                {
                    Version = new Version(1, 1),
                    Content = content,
                };
                request.Headers.Add("Authorization", $"Bearer {config.ApiKey}");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                var response = await httpClient.SendAsync(request, cts.Token);
                var responseContent = await response.Content.ReadAsStringAsync(cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("DeepSeek Action API call failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    return new AIProviderResponse
                    {
                        Success = false,
                        ErrorMessage = $"DeepSeek API error: {response.StatusCode}",
                        Provider = "DeepSeek",
                        ModelName = config.ModelName,
                        ModelId = config.Id.ToString()
                    };
                }

                var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var choices = responseData.GetProperty("choices");
                if (choices.GetArrayLength() == 0)
                {
                    return new AIProviderResponse
                    {
                        Success = false,
                        ErrorMessage = "DeepSeek API returned empty choices array",
                        Provider = "DeepSeek",
                        ModelName = config.ModelName,
                        ModelId = config.Id.ToString()
                    };
                }
                var messageContent = choices[0].GetProperty("message").GetProperty("content").GetString();

                return new AIProviderResponse
                {
                    Success = true,
                    Content = messageContent ?? string.Empty,
                    Provider = "DeepSeek",
                    ModelName = config.ModelName,
                    ModelId = config.Id.ToString()
                };
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("DeepSeek Action API call timed out after {Timeout}s", timeoutSeconds);
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"DeepSeek API timeout after {timeoutSeconds} seconds",
                    Provider = "DeepSeek",
                    ModelName = config.ModelName,
                    ModelId = config.Id.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeepSeek Action API call failed");
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"DeepSeek API error: {ex.Message}",
                    Provider = "DeepSeek",
                    ModelName = config.ModelName,
                    ModelId = config.Id.ToString()
                };
            }
        }

        /// <summary>
        /// Call ZhipuAI with optimized timeout for Action operations
        /// </summary>
        private async Task<AIProviderResponse> CallZhipuAIWithOptimizedTimeoutAsync(string prompt, int timeoutSeconds)
        {
            try
            {
                var requestBody = new
                {
                    model = _aiOptions.ZhipuAI.Model,
                    messages = new[]
                    {
                        new { role = "system", content = "You are a professional action analysis and creation expert. Generate structured responses based on user requirements. Be concise and precise." },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.7,
                    max_tokens = 1500
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var apiUrl = $"{_aiOptions.ZhipuAI.BaseUrl}/chat/completions";
                _logger.LogInformation("ZhipuAI Action API Request - Timeout: {Timeout}s", timeoutSeconds);

                using var httpClient = _httpClientFactory.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
                {
                    Version = new Version(1, 1),
                    Content = content,
                };
                request.Headers.Add("Authorization", $"Bearer {_aiOptions.ZhipuAI.ApiKey}");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                var response = await httpClient.SendAsync(request, cts.Token);
                var responseContent = await response.Content.ReadAsStringAsync(cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("ZhipuAI Action API call failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    return new AIProviderResponse
                    {
                        Success = false,
                        ErrorMessage = $"ZhipuAI API error: {response.StatusCode}",
                        Provider = "ZhipuAI",
                        ModelName = _aiOptions.ZhipuAI.Model
                    };
                }

                var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var choices = responseData.GetProperty("choices");
                if (choices.GetArrayLength() == 0)
                {
                    return new AIProviderResponse
                    {
                        Success = false,
                        ErrorMessage = "ZhipuAI API returned empty choices array",
                        Provider = "ZhipuAI",
                        ModelName = _aiOptions.ZhipuAI.Model
                    };
                }
                var messageContent = choices[0].GetProperty("message").GetProperty("content").GetString();

                return new AIProviderResponse
                {
                    Success = true,
                    Content = messageContent ?? string.Empty,
                    Provider = "ZhipuAI",
                    ModelName = _aiOptions.ZhipuAI.Model
                };
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("ZhipuAI Action API call timed out after {Timeout}s", timeoutSeconds);
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"ZhipuAI API timeout after {timeoutSeconds} seconds",
                    Provider = "ZhipuAI",
                    ModelName = _aiOptions.ZhipuAI.Model
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZhipuAI Action API call failed");
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"ZhipuAI API error: {ex.Message}",
                    Provider = "ZhipuAI",
                    ModelName = _aiOptions.ZhipuAI.Model
                };
            }
        }

        /// <summary>
        /// Call OpenAI with optimized timeout for Action operations
        /// </summary>
        private async Task<AIProviderResponse> CallOpenAIWithOptimizedTimeoutAsync(string prompt, AIModelConfig config, int timeoutSeconds)
        {
            try
            {
                // For native OpenAI API, strip provider prefix from model name
                var modelName = GetNativeModelName(config.ModelName, config.Provider);

                var requestBody = new
                {
                    model = modelName,
                    messages = new[]
                    {
                        new { role = "system", content = "You are a professional action analysis and creation expert. Generate structured responses based on user requirements. Be concise and precise." },
                        new { role = "user", content = prompt }
                    },
                    temperature = Math.Min(config.Temperature > 0 ? config.Temperature : 0.7, 0.8),
                    max_tokens = Math.Min(config.MaxTokens > 0 ? config.MaxTokens : 1500, 2000)
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var baseUrl = config.BaseUrl.TrimEnd('/');
                var apiUrl = baseUrl.EndsWith("/chat/completions") ? baseUrl : $"{baseUrl}/v1/chat/completions";

                _logger.LogInformation("OpenAI Action API Request - Model: {Model}, Timeout: {Timeout}s", config.ModelName, timeoutSeconds);

                using var httpClient = _httpClientFactory.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
                {
                    Version = new Version(1, 1),
                    Content = content,
                };
                request.Headers.Add("Authorization", $"Bearer {config.ApiKey}");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                var response = await httpClient.SendAsync(request, cts.Token);
                var responseContent = await response.Content.ReadAsStringAsync(cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("OpenAI Action API call failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    return new AIProviderResponse
                    {
                        Success = false,
                        ErrorMessage = $"OpenAI API error: {response.StatusCode}",
                        Provider = "OpenAI",
                        ModelName = config.ModelName,
                        ModelId = config.Id.ToString()
                    };
                }

                var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var choices = responseData.GetProperty("choices");
                if (choices.GetArrayLength() == 0)
                {
                    return new AIProviderResponse
                    {
                        Success = false,
                        ErrorMessage = "OpenAI API returned empty choices array",
                        Provider = "OpenAI",
                        ModelName = config.ModelName,
                        ModelId = config.Id.ToString()
                    };
                }
                var messageContent = choices[0].GetProperty("message").GetProperty("content").GetString();

                return new AIProviderResponse
                {
                    Success = true,
                    Content = messageContent ?? string.Empty,
                    Provider = "OpenAI",
                    ModelName = config.ModelName,
                    ModelId = config.Id.ToString()
                };
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("OpenAI Action API call timed out after {Timeout}s", timeoutSeconds);
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"OpenAI API timeout after {timeoutSeconds} seconds",
                    Provider = "OpenAI",
                    ModelName = config.ModelName,
                    ModelId = config.Id.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI Action API call failed");
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"OpenAI API error: {ex.Message}",
                    Provider = "OpenAI",
                    ModelName = config.ModelName,
                    ModelId = config.Id.ToString()
                };
            }
        }

        /// <summary>
        /// Call Claude with optimized timeout for Action operations
        /// </summary>
        private async Task<AIProviderResponse> CallClaudeWithOptimizedTimeoutAsync(string prompt, AIModelConfig config, int timeoutSeconds)
        {
            try
            {
                var requestBody = new
                {
                    model = config.ModelName,
                    max_tokens = Math.Min(config.MaxTokens > 0 ? config.MaxTokens : 1500, 2000),
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    system = "You are a professional action analysis and creation expert. Generate structured responses based on user requirements. Be concise and precise."
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var baseUrl = config.BaseUrl.TrimEnd('/');
                var apiUrl = baseUrl.EndsWith("/messages") ? baseUrl : $"{baseUrl}/v1/messages";

                _logger.LogInformation("Claude Action API Request - Model: {Model}, Timeout: {Timeout}s", config.ModelName, timeoutSeconds);

                using var httpClient = _httpClientFactory.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
                {
                    Version = new Version(1, 1),
                    Content = content,
                };
                request.Headers.Add("x-api-key", config.ApiKey);
                request.Headers.Add("anthropic-version", "2023-06-01");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                var response = await httpClient.SendAsync(request, cts.Token);
                var responseContent = await response.Content.ReadAsStringAsync(cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Claude Action API call failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    return new AIProviderResponse
                    {
                        Success = false,
                        ErrorMessage = $"Claude API error: {response.StatusCode}",
                        Provider = "Claude",
                        ModelName = config.ModelName,
                        ModelId = config.Id.ToString()
                    };
                }

                var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var contentArray = responseData.GetProperty("content");
                if (contentArray.GetArrayLength() == 0)
                {
                    return new AIProviderResponse
                    {
                        Success = false,
                        ErrorMessage = "Claude API returned empty content array",
                        Provider = "Claude",
                        ModelName = config.ModelName,
                        ModelId = config.Id.ToString()
                    };
                }
                var messageContent = contentArray[0].GetProperty("text").GetString();

                return new AIProviderResponse
                {
                    Success = true,
                    Content = messageContent ?? string.Empty,
                    Provider = "Claude",
                    ModelName = config.ModelName,
                    ModelId = config.Id.ToString()
                };
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Claude Action API call timed out after {Timeout}s", timeoutSeconds);
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"Claude API timeout after {timeoutSeconds} seconds",
                    Provider = "Claude",
                    ModelName = config.ModelName,
                    ModelId = config.Id.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Claude Action API call failed");
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"Claude API error: {ex.Message}",
                    Provider = "Claude",
                    ModelName = config.ModelName,
                    ModelId = config.Id.ToString()
                };
            }
        }

        /// <summary>
        /// Call Gemini with optimized timeout for Action operations via Item Gateway
        /// </summary>
        private async Task<AIProviderResponse> CallGeminiWithOptimizedTimeoutAsync(string prompt, AIModelConfig config, int timeoutSeconds)
        {
            try
            {
                // Check if using Item Gateway
                var isItemGateway = !string.IsNullOrEmpty(config.BaseUrl) &&
                                  config.BaseUrl.Contains("aiop-gateway.item.com", StringComparison.OrdinalIgnoreCase);

                var requestBody = new
                {
                    model = config.ModelName, // e.g., "gemini/gemini-2.5-flash"
                    messages = new[]
                    {
                        new { role = "system", content = "You are a professional action analysis and creation expert. Generate structured responses based on user requirements. CRITICAL: Always provide complete and full JSON responses. NEVER truncate data or use '...' patterns. Include ALL data fields completely without any omissions. IMPORTANT: When extracting HTTP headers, especially 'authorization' values (Bearer tokens, JWT, API keys), you MUST copy them EXACTLY as provided - do NOT modify, shorten, or use placeholders. Complete JSON is mandatory." },
                        new { role = "user", content = prompt }
                    },
                    temperature = Math.Min(config.Temperature > 0 ? config.Temperature : 0.7, 0.8),
                    max_tokens = Math.Min(config.MaxTokens > 0 ? config.MaxTokens : 3000, 4000), // Increased limit for complete JSON
                    stream = false
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                string apiUrl;
                string authToken;

                if (isItemGateway)
                {
                    // Use Item Gateway path
                    var baseUrl = string.IsNullOrEmpty(config.BaseUrl)
                        ? "https://aiop-gateway.item.com"
                        : config.BaseUrl.TrimEnd('/');
                    apiUrl = $"{baseUrl}/openai/v1/chat/completions";

                    // Get JWT token for Item Gateway
                    authToken = await GetLLMGatewayJwtTokenAsync(config);
                }
                else
                {
                    // Standard OpenAI-compatible path
                    var baseUrl = config.BaseUrl?.TrimEnd('/') ?? "";
                    apiUrl = baseUrl.EndsWith("/chat/completions") ? baseUrl : $"{baseUrl}/v1/chat/completions";
                    authToken = config.ApiKey;
                }

                _logger.LogInformation("Gemini Action API Request - Model: {Model}, Timeout: {Timeout}s", config.ModelName, timeoutSeconds);

                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                var response = await httpClient.PostAsync(apiUrl, content, cts.Token);
                var responseContent = await response.Content.ReadAsStringAsync(cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Gemini Action API call failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    return new AIProviderResponse
                    {
                        Success = false,
                        ErrorMessage = $"Gemini API error: {response.StatusCode}",
                        Provider = "Gemini",
                        ModelName = config.ModelName,
                        ModelId = config.Id.ToString()
                    };
                }

                // Parse OpenAI-compatible response format
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var choices = responseData.GetProperty("choices");
                if (choices.GetArrayLength() == 0)
                {
                    return new AIProviderResponse
                    {
                        Success = false,
                        ErrorMessage = "Gemini API returned empty choices array",
                        Provider = "Gemini",
                        ModelName = config.ModelName,
                        ModelId = config.Id.ToString()
                    };
                }
                var messageContent = choices[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return new AIProviderResponse
                {
                    Success = true,
                    Content = messageContent ?? string.Empty,
                    Provider = "Gemini",
                    ModelName = config.ModelName,
                    ModelId = config.Id.ToString()
                };
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Gemini Action API call timed out after {Timeout}s", timeoutSeconds);
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"Gemini API timeout after {timeoutSeconds} seconds",
                    Provider = "Gemini",
                    ModelName = config.ModelName,
                    ModelId = config.Id.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini Action API call failed");
                return new AIProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"Gemini API error: {ex.Message}",
                    Provider = "Gemini",
                    ModelName = config.ModelName,
                    ModelId = config.Id.ToString()
                };
            }
        }

        /// <summary>
        /// Stream analyze conversation to extract action insights
        /// </summary>
        public async IAsyncEnumerable<AIActionStreamResult> StreamAnalyzeActionAsync(AIActionAnalysisInput input)
        {
            var startTime = DateTime.UtcNow;
            var sessionId = input.SessionId ?? Guid.NewGuid().ToString();

            _logger.LogInformation("Starting stream action analysis for SessionId: {SessionId}", sessionId);

            // Send initial progress
            yield return new AIActionStreamResult
            {
                Type = "progress",
                Content = "Initializing action analysis...",
                IsComplete = false,
                SessionId = sessionId,
                Progress = 10
            };

            // Build optimized prompt
            var prompt = BuildActionAnalysisPrompt(input);

            yield return new AIActionStreamResult
            {
                Type = "progress",
                Content = "Analyzing conversation with AI...",
                IsComplete = false,
                SessionId = sessionId,
                Progress = 30
            };

            // Call streaming analysis method
            await foreach (var result in StreamAnalyzeActionInternalAsync(input, prompt, sessionId, startTime))
            {
                yield return result;
            }
        }

        /// <summary>
        /// Internal streaming analysis method without try-catch to avoid yield issues
        /// </summary>
        private async IAsyncEnumerable<AIActionStreamResult> StreamAnalyzeActionInternalAsync(
            AIActionAnalysisInput input, string prompt, string sessionId, DateTime startTime)
        {
            var streamingContent = new StringBuilder();
            var hasReceivedContent = false;
            Exception? processingException = null;

            // Call AI provider with streaming support
            await foreach (var chunk in CallAIProviderStreamForActionAsync(prompt, input.ModelId, input.ModelProvider, input.ModelName))
            {
                if (!string.IsNullOrEmpty(chunk))
                {
                    streamingContent.Append(chunk);
                    hasReceivedContent = true;

                    // Send incremental content
                    yield return new AIActionStreamResult
                    {
                        Type = "analysis",
                        Content = chunk,
                        IsComplete = false,
                        SessionId = sessionId,
                        Progress = Math.Min(90, 30 + (streamingContent.Length / 10))
                    };
                }
            }

            if (!hasReceivedContent)
            {
                yield return new AIActionStreamResult
                {
                    Type = "error",
                    Content = "No response received from AI service",
                    IsComplete = true,
                    SessionId = sessionId,
                    Progress = 0
                };
                yield break;
            }

            // Parse the complete response
            yield return new AIActionStreamResult
            {
                Type = "progress",
                Content = "Processing analysis results...",
                IsComplete = false,
                SessionId = sessionId,
                Progress = 95
            };

            AIActionAnalysisResult? analysisResult = null;
            try
            {
                analysisResult = ParseActionAnalysisResponse(streamingContent.ToString(), input);
            }
            catch (Exception ex)
            {
                processingException = ex;
            }

            if (processingException != null)
            {
                _logger.LogError(processingException, "Error parsing analysis response for SessionId: {SessionId}", sessionId);
                yield return new AIActionStreamResult
                {
                    Type = "error",
                    Content = $"Analysis parsing failed: {processingException.Message}",
                    IsComplete = true,
                    SessionId = sessionId,
                    Progress = 0
                };
                yield break;
            }

            // Send final result
            yield return new AIActionStreamResult
            {
                Type = "complete",
                Content = "Action analysis completed successfully",
                IsComplete = true,
                SessionId = sessionId,
                ActionData = analysisResult,
                Progress = 100
            };

            // Save prompt history (fire-and-forget)
            _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
            {
                try
                {
                    var metadata = JsonSerializer.Serialize(new
                    {
                        sessionId = sessionId,
                        conversationLength = input.ConversationHistory?.Count ?? 0,
                        focusAreasCount = input.FocusAreas?.Count ?? 0,
                        hasContext = !string.IsNullOrEmpty(input.Context),
                        streamingMode = true
                    });

                    var aiResponse = new AIProviderResponse
                    {
                        Success = true,
                        Content = streamingContent.ToString(),
                        Provider = input.ModelProvider ?? "Unknown",
                        ModelName = input.ModelName ?? "Unknown",
                        ModelId = input.ModelId ?? "Unknown"
                    };

                    await SavePromptHistoryAsync("StreamActionAnalysis", "Action", null, null,
                        prompt, aiResponse, startTime, aiResponse.Provider, aiResponse.ModelName, aiResponse.ModelId, metadata);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to save stream action analysis prompt history");
                }
            });

            _logger.LogInformation("Stream action analysis completed for SessionId: {SessionId}", sessionId);
        }

        /// <summary>
        /// Stream create action plan based on analysis or description
        /// </summary>
        public async IAsyncEnumerable<AIActionStreamResult> StreamCreateActionAsync(AIActionCreationInput input)
        {
            var startTime = DateTime.UtcNow;
            var sessionId = Guid.NewGuid().ToString();

            _logger.LogInformation("Starting stream action creation");

            // Send initial progress
            yield return new AIActionStreamResult
            {
                Type = "progress",
                Content = "Initializing action plan creation...",
                IsComplete = false,
                SessionId = sessionId,
                Progress = 10
            };

            // Build optimized prompt
            var prompt = BuildActionCreationPrompt(input);

            yield return new AIActionStreamResult
            {
                Type = "progress",
                Content = "Creating action plan with AI...",
                IsComplete = false,
                SessionId = sessionId,
                Progress = 30
            };

            // Call streaming creation method
            await foreach (var result in StreamCreateActionInternalAsync(input, prompt, sessionId, startTime))
            {
                yield return result;
            }
        }

        /// <summary>
        /// Internal streaming creation method without try-catch to avoid yield issues
        /// </summary>
        private async IAsyncEnumerable<AIActionStreamResult> StreamCreateActionInternalAsync(
            AIActionCreationInput input, string prompt, string sessionId, DateTime startTime)
        {
            var streamingContent = new StringBuilder();
            var hasReceivedContent = false;
            Exception? processingException = null;

            // Call AI provider with streaming support
            await foreach (var chunk in CallAIProviderStreamForActionAsync(prompt, input.ModelId, input.ModelProvider, input.ModelName))
            {
                if (!string.IsNullOrEmpty(chunk))
                {
                    streamingContent.Append(chunk);
                    hasReceivedContent = true;

                    // Send incremental content
                    yield return new AIActionStreamResult
                    {
                        Type = "creation",
                        Content = chunk,
                        IsComplete = false,
                        SessionId = sessionId,
                        Progress = Math.Min(90, 30 + (streamingContent.Length / 10))
                    };
                }
            }

            if (!hasReceivedContent)
            {
                yield return new AIActionStreamResult
                {
                    Type = "error",
                    Content = "No response received from AI service",
                    IsComplete = true,
                    SessionId = sessionId,
                    Progress = 0
                };
                yield break;
            }

            // Parse the complete response
            yield return new AIActionStreamResult
            {
                Type = "progress",
                Content = "Processing action plan results...",
                IsComplete = false,
                SessionId = sessionId,
                Progress = 95
            };

            AIActionCreationResult? creationResult = null;
            try
            {
                creationResult = ParseActionCreationResponse(streamingContent.ToString(), input);
            }
            catch (Exception ex)
            {
                processingException = ex;
            }

            if (processingException != null)
            {
                _logger.LogError(processingException, "Error parsing creation response");
                yield return new AIActionStreamResult
                {
                    Type = "error",
                    Content = $"Action creation parsing failed: {processingException.Message}",
                    IsComplete = true,
                    SessionId = sessionId,
                    Progress = 0
                };
                yield break;
            }

            // Send final result
            yield return new AIActionStreamResult
            {
                Type = "complete",
                Content = "Action plan created successfully",
                IsComplete = true,
                SessionId = sessionId,
                ActionData = creationResult,
                Progress = 100
            };

            // Save prompt history (fire-and-forget)
            _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
            {
                try
                {
                    var metadata = JsonSerializer.Serialize(new
                    {
                        hasAnalysisResult = input.AnalysisResult != null,
                        actionDescriptionLength = input.ActionDescription?.Length ?? 0,
                        stakeholdersCount = input.Stakeholders?.Count ?? 0,
                        priority = input.Priority,
                        hasDueDate = input.DueDate.HasValue,
                        streamingMode = true
                    });

                    var aiResponse = new AIProviderResponse
                    {
                        Success = true,
                        Content = streamingContent.ToString(),
                        Provider = input.ModelProvider ?? "Unknown",
                        ModelName = input.ModelName ?? "Unknown",
                        ModelId = input.ModelId ?? "Unknown"
                    };

                    await SavePromptHistoryAsync("StreamActionCreation", "Action", null, null,
                        prompt, aiResponse, startTime, aiResponse.Provider, aiResponse.ModelName, aiResponse.ModelId, metadata);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to save stream action creation prompt history");
                }
            });

            _logger.LogInformation("Stream action creation completed");
        }

        /// <summary>
        /// Call AI provider with streaming support for Action operations
        /// </summary>
        private async IAsyncEnumerable<string> CallAIProviderStreamForActionAsync(string prompt, string? modelId = null, string? provider = null, string? modelName = null)
        {
            // Use optimized timeout for Action operations
            var actionTimeout = provider?.ToLower() switch
            {
                "deepseek" => 15,
                "zhipuai" => 18,
                "openai" => 20,
                "claude" => 20,
                _ => 18
            };

            _logger.LogInformation("Using streaming AI provider for Action: {Provider} - {Timeout}s timeout", provider ?? "default", actionTimeout);

            // Call the appropriate provider method with streaming
            if (!string.IsNullOrEmpty(provider) && !string.IsNullOrEmpty(modelName))
            {
                switch (provider.ToLower())
                {
                    case "deepseek":
                        if (long.TryParse(modelId, out var deepSeekModelId))
                        {
                            var deepSeekConfig = await _configService.GetConfigByIdAsync(deepSeekModelId);
                            if (deepSeekConfig != null)
                            {
                                await foreach (var chunk in CallDeepSeekStreamForActionAsync(prompt, deepSeekConfig))
                                {
                                    yield return chunk;
                                }
                                yield break;
                            }
                        }
                        break;
                    case "openai":
                        if (long.TryParse(modelId, out var openAIModelId))
                        {
                            var openAIConfig = await _configService.GetConfigByIdAsync(openAIModelId);
                            if (openAIConfig != null)
                            {
                                await foreach (var chunk in CallOpenAIStreamForActionAsync(prompt, openAIConfig))
                                {
                                    yield return chunk;
                                }
                                yield break;
                            }
                        }
                        break;
                    case "gemini":
                        if (long.TryParse(modelId, out var geminiModelId))
                        {
                            var geminiConfig = await _configService.GetConfigByIdAsync(geminiModelId);
                            if (geminiConfig != null)
                            {
                                await foreach (var chunk in CallGeminiStreamForActionAsync(prompt, geminiConfig))
                                {
                                    yield return chunk;
                                }
                                yield break;
                            }
                        }
                        break;
                }
            }

            // Fallback to non-streaming with chunked response
            await foreach (var chunk in CallAIProviderStreamFallbackAsync(prompt, modelId, provider, modelName))
            {
                yield return chunk;
            }
        }

        /// <summary>
        /// Fallback streaming method for AI providers
        /// </summary>
        private async IAsyncEnumerable<string> CallAIProviderStreamFallbackAsync(string prompt, string? modelId, string? provider, string? modelName)
        {
            AIProviderResponse? response = null;
            Exception? callException = null;

            try
            {
                response = await CallAIProviderForActionAsync(prompt, modelId, provider, modelName);
            }
            catch (Exception ex)
            {
                callException = ex;
            }

            if (callException != null)
            {
                _logger.LogError(callException, "Error in streaming AI provider call for Action");
                yield return $"Error: {callException.Message}";
                yield break;
            }

            if (response?.Success == true && !string.IsNullOrEmpty(response.Content))
            {
                // Simulate streaming by chunking the response
                var content = response.Content;
                var chunkSize = Math.Max(10, content.Length / 20);

                for (int i = 0; i < content.Length; i += chunkSize)
                {
                    var chunk = content.Substring(i, Math.Min(chunkSize, content.Length - i));
                    yield return chunk;

                    // Small delay to simulate streaming
                    await Task.Delay(50);
                }
            }
            else
            {
                yield return "Failed to get response from AI service.";
            }
        }

        /// <summary>
        /// Call DeepSeek with streaming for Action operations
        /// </summary>
        private async IAsyncEnumerable<string> CallDeepSeekStreamForActionAsync(string prompt, AIModelConfig config)
        {
            // For native DeepSeek API, strip provider prefix from model name
            var modelName = GetNativeModelName(config.ModelName, config.Provider);

            var requestBody = new
            {
                model = modelName,
                messages = new[]
                {
                    new { role = "system", content = "You are a professional action analysis and creation expert. Generate structured responses based on user requirements. CRITICAL: Always provide complete and full JSON responses. NEVER truncate data or use '...' patterns. Include ALL data fields completely without any omissions. IMPORTANT: When extracting HTTP headers, especially 'authorization' values (Bearer tokens, JWT, API keys), you MUST copy them EXACTLY as provided - do NOT modify, shorten, or use placeholders. Complete JSON is mandatory." },
                    new { role = "user", content = prompt }
                },
                temperature = Math.Min(config.Temperature > 0 ? config.Temperature : 0.7, 0.8),
                max_tokens = Math.Min(config.MaxTokens > 0 ? config.MaxTokens : 3000, 4000),
                stream = true
            };

            var json = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var baseUrl = config.BaseUrl.TrimEnd('/');
            string apiUrl = baseUrl.Contains("/chat/completions") ? baseUrl :
                           baseUrl.Contains("/v1") ? $"{baseUrl}/chat/completions" : $"{baseUrl}/v1/chat/completions";

            _logger.LogInformation("DeepSeek Action Stream API: {Url} - Model: {Model}", apiUrl, config.ModelName);

            // Call the streaming helper method
            await foreach (var chunk in CallDeepSeekStreamInternalAsync(apiUrl, httpContent, config))
            {
                yield return chunk;
            }
        }

        /// <summary>
        /// Internal DeepSeek streaming method
        /// </summary>
        private async IAsyncEnumerable<string> CallDeepSeekStreamInternalAsync(string apiUrl, HttpContent httpContent, AIModelConfig config)
        {
            HttpResponseMessage? response = null;
            Exception? requestException = null;

            try
            {
                using var httpClient = _httpClientFactory.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
                {
                    Content = httpContent
                };
                request.Headers.Add("Authorization", $"Bearer {config.ApiKey}");
                request.Headers.Add("Accept", "text/event-stream");

                response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            }
            catch (Exception ex)
            {
                requestException = ex;
            }

            if (requestException != null)
            {
                _logger.LogError(requestException, "Error in DeepSeek Action streaming request");
                yield return $"Error: {requestException.Message}";
                yield break;
            }

            if (response == null || !response.IsSuccessStatusCode)
            {
                var errorContent = response != null ? await response.Content.ReadAsStringAsync() : "No response";
                _logger.LogWarning("DeepSeek Action Stream API failed: {StatusCode} - {Content}", response?.StatusCode, errorContent);
                yield return "I'm having trouble connecting to the AI service. Please try again.";
                response?.Dispose();
                yield break;
            }

            try
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);

                string? line;
                var lineCount = 0;
                var emptyContentCount = 0;
                var yieldedCount = 0;

                _logger.LogInformation("🔄 DeepSeek: Starting stream processing");

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lineCount++;

                    // Log empty lines
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    if (line.StartsWith("data: "))
                    {
                        var data = line.Substring(6);

                        if (data == "[DONE]")
                        {
                            _logger.LogInformation("🏁 DeepSeek: Received [DONE]. Lines: {LineCount}, Yielded: {YieldedCount}, Empty: {EmptyCount}",
                                lineCount, yieldedCount, emptyContentCount);
                            break;
                        }

                        // Log raw AI response data for debugging
                        _logger.LogDebug("🔍 DeepSeek chunk #{LineCount}: {Data}", lineCount,
                            data.Length > 200 ? data.Substring(0, 200) + "..." : data);

                        var contentText = ParseStreamingJsonContent(data);
                        if (!string.IsNullOrEmpty(contentText))
                        {
                            yieldedCount++;
                            _logger.LogDebug("📤 DeepSeek yield #{YieldedCount}: {Content}", yieldedCount,
                                contentText.Length > 100 ? contentText.Substring(0, 100) + "..." : contentText);
                            yield return contentText;
                        }
                        else
                        {
                            emptyContentCount++;
                            if (emptyContentCount <= 5) // Only log first 5 empty chunks
                            {
                                _logger.LogDebug("⚠️ DeepSeek empty content #{EmptyCount} at line {LineCount}: {Data}",
                                    emptyContentCount, lineCount, data.Length > 300 ? data.Substring(0, 300) + "..." : data);
                            }
                        }
                    }
                }

                _logger.LogInformation("✅ DeepSeek stream completed. Lines: {LineCount}, Yielded: {YieldedCount}, Empty: {EmptyCount}",
                    lineCount, yieldedCount, emptyContentCount);
            }
            finally
            {
                response?.Dispose();
            }
        }

        /// <summary>
        /// Call OpenAI with streaming for Action operations
        /// </summary>
        private async IAsyncEnumerable<string> CallOpenAIStreamForActionAsync(string prompt, AIModelConfig config)
        {
            // Check if using Item Gateway
            var isItemGateway = !string.IsNullOrEmpty(config.BaseUrl) &&
                              config.BaseUrl.Contains("aiop-gateway.item.com", StringComparison.OrdinalIgnoreCase);

            // For native OpenAI API, strip provider prefix from model name
            var modelName = isItemGateway ? config.ModelName : GetNativeModelName(config.ModelName, config.Provider);

            var requestBody = new
            {
                model = modelName,
                messages = new[]
                {
                    new { role = "system", content = "You are a professional action analysis and creation expert. Generate structured responses based on user requirements. CRITICAL: Always provide complete and full JSON responses. NEVER truncate data or use '...' patterns. Include ALL data fields completely without any omissions. IMPORTANT: When extracting HTTP headers, especially 'authorization' values (Bearer tokens, JWT, API keys), you MUST copy them EXACTLY as provided - do NOT modify, shorten, or use placeholders. Complete JSON is mandatory." },
                    new { role = "user", content = prompt }
                },
                temperature = Math.Min(config.Temperature > 0 ? config.Temperature : 0.7, 0.8),
                max_tokens = Math.Min(config.MaxTokens > 0 ? config.MaxTokens : 3000, 4000),
                stream = true
            };

            var json = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            string apiUrl;
            string? jwtToken = null;

            if (isItemGateway)
            {
                // Use Item Gateway path: /openai/v1/chat/completions
                var baseUrl = string.IsNullOrEmpty(config.BaseUrl)
                    ? "https://aiop-gateway.item.com"
                    : config.BaseUrl.TrimEnd('/');
                apiUrl = $"{baseUrl}/openai/v1/chat/completions";

                // Get JWT token for Item Gateway
                string? errorMessage = null;
                try
                {
                    jwtToken = await GetLLMGatewayJwtTokenAsync(config);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to obtain JWT token for Item Gateway streaming");
                    errorMessage = "Failed to authenticate with Item Gateway. Please check your API key.";
                }

                // Check for authentication error and yield outside of catch block
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    yield return errorMessage;
                    yield break;
                }
            }
            else
            {
                // Standard OpenAI API path
                var baseUrl = config.BaseUrl.TrimEnd('/');
                apiUrl = baseUrl.EndsWith("/chat/completions") ? baseUrl : $"{baseUrl}/v1/chat/completions";
            }

            _logger.LogInformation("OpenAI Action Stream API: {Url} - Model: {Model}", apiUrl, config.ModelName);

            // Call the streaming helper method
            await foreach (var chunk in CallOpenAIStreamInternalAsync(apiUrl, httpContent, config, jwtToken))
            {
                yield return chunk;
            }
        }

        /// <summary>
        /// Call Gemini with streaming for Action operations via Item Gateway
        /// Since Item Gateway uses OpenAI-compatible format, we reuse the OpenAI streaming logic
        /// </summary>
        private async IAsyncEnumerable<string> CallGeminiStreamForActionAsync(string prompt, AIModelConfig config)
        {
            // Gemini via Item Gateway uses OpenAI-compatible API format
            var isItemGateway = !string.IsNullOrEmpty(config.BaseUrl) &&
                              config.BaseUrl.Contains("aiop-gateway.item.com", StringComparison.OrdinalIgnoreCase);

            var requestBody = new
            {
                model = config.ModelName, // e.g., "gemini/gemini-2.5-flash" or "gemini/gemini-2.5-pro"
                messages = new[]
                {
                    new { role = "system", content = "You are a professional action analysis and creation expert. Generate structured responses based on user requirements. CRITICAL: Always provide complete and full JSON responses. NEVER truncate data or use '...' patterns. Include ALL data fields completely without any omissions. IMPORTANT: When extracting HTTP headers, especially 'authorization' values (Bearer tokens, JWT, API keys), you MUST copy them EXACTLY as provided - do NOT modify, shorten, or use placeholders. Complete JSON is mandatory." },
                    new { role = "user", content = prompt }
                },
                temperature = Math.Min(config.Temperature > 0 ? config.Temperature : 0.7, 0.8),
                max_tokens = Math.Min(config.MaxTokens > 0 ? config.MaxTokens : 3000, 4000), // Increased limit for complete JSON responses
                stream = true
            };

            var json = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            string apiUrl;
            string? jwtToken = null;

            if (isItemGateway)
            {
                // Use Item Gateway path: /openai/v1/chat/completions (same format as OpenAI)
                var baseUrl = string.IsNullOrEmpty(config.BaseUrl)
                    ? "https://aiop-gateway.item.com"
                    : config.BaseUrl.TrimEnd('/');
                apiUrl = $"{baseUrl}/openai/v1/chat/completions";

                // Get JWT token for Item Gateway
                string? errorMessage = null;
                try
                {
                    jwtToken = await GetLLMGatewayJwtTokenAsync(config);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to obtain JWT token for Item Gateway (Gemini) streaming");
                    errorMessage = "Failed to authenticate with Item Gateway. Please check your API key.";
                }

                // Check for authentication error and yield outside of catch block
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    yield return errorMessage;
                    yield break;
                }
            }
            else
            {
                // For non-Item Gateway Gemini, use OpenAI-compatible path
                var baseUrl = config.BaseUrl?.TrimEnd('/') ?? "";
                apiUrl = baseUrl.EndsWith("/chat/completions") ? baseUrl : $"{baseUrl}/v1/chat/completions";
            }

            _logger.LogInformation("Gemini Action Stream API: {Url} - Model: {Model}", apiUrl, config.ModelName);

            // Reuse the OpenAI streaming internal method since Item Gateway returns OpenAI-compatible format
            await foreach (var chunk in CallOpenAIStreamInternalAsync(apiUrl, httpContent, config, jwtToken))
            {
                yield return chunk;
            }
        }

        /// <summary>
        /// Internal OpenAI streaming method
        /// </summary>
        private async IAsyncEnumerable<string> CallOpenAIStreamInternalAsync(string apiUrl, HttpContent httpContent, AIModelConfig config, string? jwtToken = null)
        {
            HttpResponseMessage? response = null;
            Exception? requestException = null;

            try
            {
                using var httpClient = _httpClientFactory.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
                {
                    Content = httpContent
                };
                // Use JWT token if provided (for Item Gateway), otherwise use API key
                var authToken = jwtToken ?? config.ApiKey;
                request.Headers.Add("Authorization", $"Bearer {authToken}");
                request.Headers.Add("Accept", "text/event-stream");

                response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            }
            catch (Exception ex)
            {
                requestException = ex;
            }

            if (requestException != null)
            {
                _logger.LogError(requestException, "Error in OpenAI Action streaming request");
                yield return $"Error: {requestException.Message}";
                yield break;
            }

            if (response == null || !response.IsSuccessStatusCode)
            {
                var errorContent = response != null ? await response.Content.ReadAsStringAsync() : "No response";
                _logger.LogWarning("OpenAI Action Stream API failed: {StatusCode} - {Content}", response?.StatusCode, errorContent);
                yield return "I'm having trouble connecting to the AI service. Please try again.";
                response?.Dispose();
                yield break;
            }

            try
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);

                string? line;
                var lineCount = 0;
                var emptyContentCount = 0;
                var yieldedCount = 0;

                _logger.LogInformation("🔄 OpenAI: Starting stream processing");

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lineCount++;

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    if (line.StartsWith("data: "))
                    {
                        var data = line.Substring(6);

                        if (data == "[DONE]")
                        {
                            _logger.LogInformation("🏁 OpenAI: Received [DONE]. Lines: {LineCount}, Yielded: {YieldedCount}, Empty: {EmptyCount}",
                                lineCount, yieldedCount, emptyContentCount);
                            break;
                        }

                        // Log raw AI response data for debugging
                        _logger.LogDebug("🔍 OpenAI chunk #{LineCount}: {Data}", lineCount,
                            data.Length > 200 ? data.Substring(0, 200) + "..." : data);

                        var contentText = ParseStreamingJsonContent(data);
                        if (!string.IsNullOrEmpty(contentText))
                        {
                            yieldedCount++;
                            _logger.LogDebug("📤 OpenAI yield #{YieldedCount}: {Content}", yieldedCount,
                                contentText.Length > 100 ? contentText.Substring(0, 100) + "..." : contentText);
                            yield return contentText;
                        }
                        else
                        {
                            emptyContentCount++;
                            if (emptyContentCount <= 5)
                            {
                                _logger.LogDebug("⚠️ OpenAI empty content #{EmptyCount} at line {LineCount}: {Data}",
                                    emptyContentCount, lineCount, data.Length > 300 ? data.Substring(0, 300) + "..." : data);
                            }
                        }
                    }
                }

                _logger.LogInformation("✅ OpenAI stream completed. Lines: {LineCount}, Yielded: {YieldedCount}, Empty: {EmptyCount}",
                    lineCount, yieldedCount, emptyContentCount);
            }
            finally
            {
                response?.Dispose();
            }
        }

        /// <summary>
        /// Parse streaming JSON content and extract text content
        /// </summary>
        private string? ParseStreamingJsonContent(string jsonData)
        {
            try
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    MaxDepth = 64,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var parsedData = JsonSerializer.Deserialize<JsonElement>(jsonData, jsonOptions);

                // Standard format: choices[0].delta.content (OpenAI and DeepSeek)
                if (parsedData.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var choice = choices[0];
                    if (choice.TryGetProperty("delta", out var delta))
                    {
                        if (delta.TryGetProperty("content", out var content))
                        {
                            var contentText = content.GetString();
                            if (!string.IsNullOrEmpty(contentText))
                            {
                                return contentText;
                            }
                        }

                        // DeepSeek alternative: check for 'text' field in delta
                        if (delta.TryGetProperty("text", out var text))
                        {
                            var textContent = text.GetString();
                            if (!string.IsNullOrEmpty(textContent))
                            {
                                _logger.LogDebug("📝 Using 'text' field from delta: {Text}", textContent);
                                return textContent;
                            }
                        }
                    }

                    // Alternative format: choices[0].message.content (some providers)
                    if (choice.TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var messageContent))
                    {
                        var msgText = messageContent.GetString();
                        if (!string.IsNullOrEmpty(msgText))
                        {
                            _logger.LogDebug("📝 Using 'message.content' field: {Text}", msgText);
                            return msgText;
                        }
                    }
                }

                // DeepSeek direct format: content at root level
                if (parsedData.TryGetProperty("content", out var rootContent))
                {
                    var rootText = rootContent.GetString();
                    if (!string.IsNullOrEmpty(rootText))
                    {
                        _logger.LogDebug("📝 Using root 'content' field: {Text}", rootText);
                        return rootText;
                    }
                }
            }
            catch (JsonException ex)
            {
                // Log the error for debugging instead of silently skipping
                _logger.LogWarning(ex, "Failed to parse streaming JSON content: {JsonData}",
                    jsonData.Length > 200 ? jsonData.Substring(0, 200) + "..." : jsonData);
            }
            return null;
        }

        /// <summary>
        /// Generate HTTP configuration directly from user input (optimized single-step process)
        /// </summary>
        public async IAsyncEnumerable<AIActionStreamResult> StreamGenerateHttpConfigAsync(AIHttpConfigGenerationInput input)
        {
            var startTime = DateTime.UtcNow;
            var sessionId = input.SessionId ?? Guid.NewGuid().ToString();

            _logger.LogInformation("Starting optimized HTTP config generation for SessionId: {SessionId}", sessionId);

            // Send initial progress
            yield return new AIActionStreamResult
            {
                Type = "progress",
                Content = "Generating HTTP configuration...",
                IsComplete = false,
                SessionId = sessionId,
                Progress = 20
            };

            // Build optimized prompt for direct HTTP config generation with token protection
            var (prompt, tokenMap) = BuildHttpConfigGenerationPromptWithTokenProtection(input);

            // Call streaming generation method with token map for restoration
            await foreach (var result in StreamGenerateHttpConfigInternalAsync(input, prompt, sessionId, startTime, tokenMap))
            {
                yield return result;
            }
        }

        /// <summary>
        /// Internal streaming HTTP config generation method
        /// </summary>
        private async IAsyncEnumerable<AIActionStreamResult> StreamGenerateHttpConfigInternalAsync(
            AIHttpConfigGenerationInput input, string prompt, string sessionId, DateTime startTime, Dictionary<string, string>? tokenMap = null)
        {
            // Log input data for debugging
            _logger.LogInformation("🔍 Input UserInput length: {Length}, FileContent length: {FileLength}",
                input.UserInput?.Length ?? 0, input.FileContent?.Length ?? 0);
            if (!string.IsNullOrEmpty(input.UserInput))
            {
                _logger.LogInformation("📋 UserInput preview: {Preview}",
                    input.UserInput.Length > 300 ? input.UserInput.Substring(0, 300) + "..." : input.UserInput);
            }

            var streamingContent = new StringBuilder();
            var hasReceivedContent = false;
            Exception? processingException = null;

            // Call AI provider with streaming support
            await foreach (var chunk in CallAIProviderStreamForActionAsync(prompt, input.ModelId, input.ModelProvider, input.ModelName))
            {
                if (!string.IsNullOrEmpty(chunk))
                {
                    hasReceivedContent = true;
                    streamingContent.Append(chunk);

                    // Send minimal streaming update
                    yield return new AIActionStreamResult
                    {
                        Type = "generation",
                        Content = "Generating...",
                        IsComplete = false,
                        SessionId = sessionId,
                        Progress = Math.Min(90, 30 + (streamingContent.Length / 100))
                    };
                }
            }

            if (hasReceivedContent)
            {
                var fullContent = streamingContent.ToString();
                _logger.LogInformation("📋 DeepSeek full response content length: {Length} chars", fullContent.Length);

                // Log a portion of the response for debugging (focus on the end where truncation occurs)
                if (fullContent.Length > 1000)
                {
                    var endContent = fullContent.Substring(fullContent.Length - 500);
                    _logger.LogInformation("📋 DeepSeek response ending: ...{EndContent}", endContent);
                }
                else
                {
                    _logger.LogInformation("📋 DeepSeek response full: {Content}", fullContent);
                }

                object? httpConfigResult = null;
                string resultMessage = "HTTP configuration generated successfully!";

                // Restore protected authorization tokens before parsing
                if (tokenMap != null && tokenMap.Count > 0)
                {
                    fullContent = RestoreAuthorizationTokens(fullContent, tokenMap);
                    _logger.LogInformation("🔓 Restored {Count} protected authorization tokens", tokenMap.Count);
                }

                try
                {
                    // Parse the HTTP configuration directly
                    httpConfigResult = ParseHttpConfigGenerationResponse(fullContent, input);
                }
                catch (Exception ex)
                {
                    processingException = ex;
                    _logger.LogError(ex, "Error processing HTTP config generation response");

                    // Generate fallback config
                    httpConfigResult = GenerateFallbackHttpConfig(input);
                    if (httpConfigResult == null)
                    {
                        resultMessage = "Unable to generate HTTP configuration. Please provide more specific API details including URL and method.";
                    }
                    else
                    {
                        resultMessage = "HTTP configuration generated (fallback mode)!";
                    }
                }

                // Send completion result
                yield return new AIActionStreamResult
                {
                    Type = "complete",
                    Content = resultMessage,
                    IsComplete = true,
                    SessionId = sessionId,
                    Progress = 100,
                    ActionData = httpConfigResult
                };

                // Save prompt history (only if parsing was successful)
                if (processingException == null)
                {
                    try
                    {
                        var aiResponse = new AIProviderResponse
                        {
                            Success = true,
                            Content = fullContent,
                            Provider = input.ModelProvider ?? "default",
                            ModelName = input.ModelName ?? "default",
                            ModelId = input.ModelId ?? "default"
                        };

                        await SavePromptHistoryAsync("HTTP_CONFIG_GENERATION", "HTTP_CONFIG", null, null,
                            prompt, aiResponse, startTime, input.ModelProvider, input.ModelName, input.ModelId,
                            JsonSerializer.Serialize(new { SessionId = sessionId, OutputFormat = input.OutputFormat }));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to save prompt history for HTTP config generation");
                        // Don't fail the entire operation for logging issues
                    }
                }
            }

            if (processingException == null && !hasReceivedContent)
            {
                // Generate fallback config if no content received
                var fallbackConfig = GenerateFallbackHttpConfig(input);
                var message = fallbackConfig == null
                    ? "Unable to generate HTTP configuration. Please provide more specific API details including URL and method."
                    : "HTTP configuration generated (fallback mode)!";

                yield return new AIActionStreamResult
                {
                    Type = "complete",
                    Content = message,
                    IsComplete = true,
                    SessionId = sessionId,
                    Progress = 100,
                    ActionData = fallbackConfig
                };
            }
        }

        /// <summary>
        /// Build optimized prompt for HTTP configuration generation
        /// Extracts and replaces authorization tokens with placeholders to prevent AI hallucination
        /// </summary>
        private (string prompt, Dictionary<string, string> tokenMap) BuildHttpConfigGenerationPromptWithTokenProtection(AIHttpConfigGenerationInput input)
        {
            var tokenMap = new Dictionary<string, string>();
            var originalInputLength = input.UserInput?.Length ?? 0;
            var userInput = input.UserInput?.Length > 10000
                ? input.UserInput.Substring(0, 10000) + "..."
                : input.UserInput ?? "";

            // Extract and replace authorization tokens to prevent AI modification
            userInput = ProtectAuthorizationTokens(userInput, tokenMap);

            _logger.LogInformation("📝 User input length: {OriginalLength}, truncated: {IsTruncated}, protected tokens: {TokenCount}",
                originalInputLength, originalInputLength > 10000, tokenMap.Count);

            var contextText = !string.IsNullOrEmpty(input.Context) && input.Context.Length > 2000
                ? input.Context.Substring(0, 2000) + "..."
                : input.Context ?? "";

            var fileContentText = "";
            if (!string.IsNullOrEmpty(input.FileContent))
            {
                // Increase limit for file content to avoid truncating large JSON bodies
                // 10000 characters should be enough for most API request bodies
                var content = input.FileContent.Length > 10000
                    ? input.FileContent.Substring(0, 10000) + "...[truncated]"
                    : input.FileContent;
                fileContentText = $"\n\nFILE CONTENT ({input.FileName ?? "uploaded file"}):\n{content}";

                _logger.LogInformation("📄 File content included, length: {Length} chars, truncated: {IsTruncated}",
                    input.FileContent.Length, input.FileContent.Length > 10000);
            }

            var promptText = $@"CRITICAL: Extract HTTP configuration from the following input and return ONLY valid JSON. DO NOT TRUNCATE OR USE '...' IN JSON FIELDS. Provide COMPLETE data content.

HEADER PARSING REQUIREMENTS:
- Parse ALL headers from the input, not just Content-Type and Accept
- Include headers like: authorization, cache-control, origin, referer, user-agent, accept-language, pragma, priority, sec-*, time-zone, etc.
- Each -H flag in cURL represents a header that MUST be included
- Headers object should contain ALL found headers with their exact values
- IMPORTANT: If you see placeholders like __AUTH_TOKEN_0__ in the authorization header, keep them EXACTLY as-is without modification
- Copy header values character-by-character without any changes

QUERY PARAMETERS PARSING REQUIREMENTS:
- Parse ALL query parameters from the URL (after the ? symbol)
- Extract each parameter as a separate key-value pair in the params object
- URL should NOT contain query parameters - extract them to params instead
- Example: URL ""https://api.example.com/users?page=1&size=10"" should become:
  - url: ""https://api.example.com/users""
  - params: {{ ""page"": ""1"", ""size"": ""10"" }}
- Windows cURL may use ^& to escape ampersands - handle this correctly

BODY PARSING REQUIREMENTS:
- Extract the COMPLETE body content without any truncation
- Include ALL JSON properties and nested objects in their entirety
- Pay special attention to complex objects like 'components', 'defaultAssignee', 'estimatedDuration', etc.
- Windows cURL uses ^"" for escaping quotes - handle this correctly
- Body can be found in multiple places:
  * cURL: -d flag, --data flag, --data-raw flag, --data-binary flag
  * FILE CONTENT section: If a file is provided, it likely contains the request body
  * Direct JSON: JSON content in the input that is not part of URL or headers
- If FILE CONTENT is provided and contains JSON, treat it as the request body
- If body content exists, set bodyType to ""raw"" (not ""none"")

INPUT: {userInput}{fileContentText}

CRITICAL REQUIREMENTS:
1. Extract the ACTUAL URL from the input - DO NOT use placeholder URLs
2. Extract the ACTUAL HTTP method from the input
3. Extract ALL query parameters from the URL and put them in params object
4. If no valid URL is found in input, return null instead of generating fake URLs

REQUIRED OUTPUT FORMAT (JSON only, no markdown, no explanation):
{{
    ""actionPlan"": {{
        ""actions"": [{{
            ""httpConfig"": {{
                ""method"": ""[EXTRACTED_METHOD]"",
                ""url"": ""[EXTRACTED_BASE_URL_WITHOUT_QUERY_PARAMS]"",
                ""headers"": {{
                    ""Content-Type"": ""application/json"",
                    ""Accept"": ""application/json""
                    // INCLUDE ALL HEADERS found in the input (authorization, cache-control, origin, referer, user-agent, etc.)
                }},
                ""params"": {{
                    // INCLUDE ALL QUERY PARAMETERS extracted from URL (e.g., ""pageIndex"": ""1"", ""pageSize"": ""15"", ""search"": ""cyn"")
                }},
                ""bodyType"": ""none"",
                ""body"": """",
                ""rawFormat"": ""json"",
                ""actionName"": ""[GENERATED_ACTION_NAME]"",
                ""timeout"": 30,
                ""followRedirects"": true
            }}
        }}]
    }}
}}

EXTRACTION RULES:
1. method: Extract from curl -X, HTTP verb, or default to GET
2. url: Must be complete valid URL with protocol (https:// or http://) - EXTRACT FROM INPUT ONLY, REMOVE QUERY PARAMETERS
3. headers: Extract ALL headers from -H flags with EXACT values - especially 'authorization' must be copied verbatim without any modification
4. params: Extract ALL query parameters from URL (everything after ?) as key-value pairs
5. body: Extract from -d/--data/--data-raw flags, FILE CONTENT, or direct JSON - EXTRACT AND INCLUDE THE COMPLETE BODY CONTENT, DO NOT TRUNCATE JSON DATA
6. bodyType: MUST be ""raw"" if body content exists (from -d flag, FILE CONTENT, or any request payload), ""none"" ONLY if truly no body
7. actionName: Generate from URL path (e.g., get_users, post_data, put_onboarding)
8. timeout: Default to 30 seconds
9. followRedirects: Default to true

IMPORTANT BODY DETECTION:
- If you see -d, --data, --data-raw, or --data-binary flags → bodyType = ""raw""
- If FILE CONTENT section contains JSON or any data → bodyType = ""raw"", body = FILE CONTENT
- If method is POST/PUT/PATCH and any payload exists → bodyType = ""raw""
- ONLY use bodyType = ""none"" when there is absolutely no body content anywhere

VALIDATION REQUIREMENTS:
- url MUST be extracted from input - NO PLACEHOLDER URLS, NO QUERY PARAMETERS IN URL
- method MUST be one of: GET, POST, PUT, DELETE, PATCH
- headers MUST be object with string values
- params MUST be object with string values (extracted from URL query string)
- body MUST be string (empty if no body)
- actionName MUST be valid identifier (alphanumeric + underscore)

CRITICAL: Extract and include the COMPLETE body content from the input, do not truncate JSON data. Ensure all JSON content in the body field is complete and valid.

FORBIDDEN PATTERNS: 
- Do NOT use ""..."" or ""...anything"" to indicate omitted content
- Do NOT truncate any part of the JSON data
- Do NOT end JSON fields with ""Ord..."", ""ExtendProperty..."" or similar patterns
- Do NOT limit headers to only Content-Type and Accept
- Do NOT include query parameters in the url field - they must be in params
- Include ALL properties and values completely
- Include ALL headers found in the cURL command
- If authorization header contains __AUTH_TOKEN_X__ placeholder, keep it EXACTLY as-is
- Do NOT modify any token placeholders or header values
- Include ALL query parameters in the params object

EXAMPLE OF WHAT NOT TO DO:
""ExtendProperty"": {{ ""Order_Property"": """", ""Ord...""}}
""url"": ""https://api.example.com/users?page=1&size=10""
""bodyType"": ""none"" when FILE CONTENT has JSON data

CORRECT APPROACH:
""ExtendProperty"": {{ ""Order_Property"": """", ""Order_Status"": ""pending"", ""Order_Notes"": ""sample""}}
""url"": ""https://api.example.com/users"",
""params"": {{ ""page"": ""1"", ""size"": ""10"" }}

EXAMPLE - When FILE CONTENT is provided:
If you see: FILE CONTENT (data.json): {{ ""name"": ""John"", ""age"": 30 }}
Then output:
""bodyType"": ""raw"",
""body"": ""{{ \""name\"": \""John\"", \""age\"": 30 }}"",
""rawFormat"": ""json""

EXAMPLE - When cURL has --data-raw:
If you see: curl -X POST ... --data-raw '{{ ""key"": ""value"" }}'
Then output:
""bodyType"": ""raw"",
""body"": ""{{ \""key\"": \""value\"" }}"",
""rawFormat"": ""json""

IMPORTANT: If no valid URL can be extracted from the input, return null instead of any placeholder URL.

Return ONLY the JSON object, no additional text or formatting.";

            return (promptText, tokenMap);
        }

        /// <summary>
        /// Legacy method for backward compatibility - builds prompt without token protection
        /// </summary>
        private string BuildHttpConfigGenerationPrompt(AIHttpConfigGenerationInput input)
        {
            var (prompt, _) = BuildHttpConfigGenerationPromptWithTokenProtection(input);
            return prompt;
        }

        /// <summary>
        /// Protect authorization tokens by replacing them with placeholders
        /// This prevents AI from hallucinating/modifying long tokens during processing
        /// </summary>
        private string ProtectAuthorizationTokens(string input, Dictionary<string, string> tokenMap)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var result = input;
            var tokenIndex = 0;

            // Pattern to match Bearer tokens in various formats
            // JWT tokens consist of three base64url-encoded parts separated by dots
            // Format: Bearer <header>.<payload>.<signature>
            var bearerPatterns = new[]
            {
                // cURL header format: -H 'authorization: Bearer ...' or -H "Authorization: Bearer ..."
                @"(-H\s+'[Aa]uthorization:\s*Bearer\s+)([A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+)(')",
                @"(-H\s+""[Aa]uthorization:\s*Bearer\s+)([A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+)("")",
                // Without quotes around the whole header
                @"(-H\s+[Aa]uthorization:\s*Bearer\s+)([A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+)(\s|$)",
            };

            // First pass: protect JWT tokens (three-part format)
            foreach (var pattern in bearerPatterns)
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(result, pattern);
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (match.Groups.Count >= 4)
                    {
                        var fullToken = match.Groups[2].Value;

                        // Only protect tokens longer than 50 characters
                        if (fullToken.Length > 50 && !tokenMap.ContainsValue(fullToken))
                        {
                            var placeholder = $"__AUTH_TOKEN_{tokenIndex}__";
                            tokenMap[placeholder] = fullToken;

                            var prefix = match.Groups[1].Value;
                            var suffix = match.Groups[3].Value;
                            var replacement = prefix + placeholder + suffix;
                            result = result.Replace(match.Value, replacement);

                            tokenIndex++;
                            _logger.LogInformation("🔐 Protected JWT token #{Index}, length: {Length} chars", tokenIndex, fullToken.Length);
                        }
                    }
                }
            }

            // Second pass: protect any remaining long Bearer tokens (non-JWT format)
            var simpleBearerPattern = @"(Bearer\s+)([A-Za-z0-9_\-\.=+/]{100,})";
            var simpleMatches = System.Text.RegularExpressions.Regex.Matches(result, simpleBearerPattern);
            foreach (System.Text.RegularExpressions.Match match in simpleMatches)
            {
                if (match.Groups.Count >= 3)
                {
                    var fullToken = match.Groups[2].Value;
                    if (!tokenMap.ContainsValue(fullToken))
                    {
                        var placeholder = $"__AUTH_TOKEN_{tokenIndex}__";
                        tokenMap[placeholder] = fullToken;

                        var prefix = match.Groups[1].Value;
                        result = result.Replace(match.Value, prefix + placeholder);

                        tokenIndex++;
                        _logger.LogInformation("🔐 Protected Bearer token #{Index}, length: {Length} chars", tokenIndex, fullToken.Length);
                    }
                }
            }

            if (tokenIndex > 0)
            {
                _logger.LogInformation("🔐 Total protected authorization tokens: {Count}", tokenIndex);
            }

            return result;
        }

        /// <summary>
        /// Restore protected authorization tokens in the response
        /// </summary>
        private string RestoreAuthorizationTokens(string response, Dictionary<string, string> tokenMap)
        {
            if (string.IsNullOrEmpty(response) || tokenMap == null || tokenMap.Count == 0)
            {
                return response;
            }

            var result = response;
            foreach (var kvp in tokenMap)
            {
                if (result.Contains(kvp.Key))
                {
                    result = result.Replace(kvp.Key, kvp.Value);
                    _logger.LogDebug("🔓 Restored authorization token: {Placeholder}", kvp.Key);
                }
            }

            return result;
        }

        /// <summary>
        /// Parse HTTP configuration generation response
        /// </summary>
        private object ParseHttpConfigGenerationResponse(string aiResponse, AIHttpConfigGenerationInput input)
        {
            try
            {
                // Log response for debugging
                _logger.LogInformation("🔍 Parsing HTTP config response, length: {Length}", aiResponse.Length);
                _logger.LogDebug("📋 Response start (500 chars): {Start}",
                    aiResponse.Length > 500 ? aiResponse.Substring(0, 500) : aiResponse);
                _logger.LogDebug("📋 Response end (500 chars): {End}",
                    aiResponse.Length > 500 ? aiResponse.Substring(aiResponse.Length - 500) : aiResponse);

                // Try to extract JSON from markdown code blocks
                var extractedJson = ExtractJsonFromMarkdown(aiResponse);
                if (extractedJson != null)
                {
                    _logger.LogInformation("✅ Extracted JSON from markdown code block");
                    aiResponse = extractedJson;
                }

                // Configure JSON serializer options to prevent truncation
                var jsonOptions = new JsonSerializerOptions
                {
                    MaxDepth = 64,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                // Try to parse as JSON
                JsonElement jsonResponse;
                try
                {
                    jsonResponse = JsonSerializer.Deserialize<JsonElement>(aiResponse, jsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "❌ Failed to parse response as JSON, trying flexible parsing");
                    return TryParseFlexibleJsonStructure(aiResponse, input, jsonOptions);
                }

                // Try multiple field names for action plan (case-insensitive)
                var possibleFieldNames = new[] { "actionPlan", "action_plan", "plan", "actionplan", "ActionPlan" };
                foreach (var fieldName in possibleFieldNames)
                {
                    if (TryGetPropertyCaseInsensitive(jsonResponse, fieldName, out var actionPlan))
                    {
                        _logger.LogInformation("✅ Found action plan using field: {FieldName}", fieldName);
                        return actionPlan;
                    }
                }

                // Try to extract from top-level structure with various field names
                var possibleActionsFields = new[] { "actions", "action", "items", "steps" };
                foreach (var fieldName in possibleActionsFields)
                {
                    if (TryGetPropertyCaseInsensitive(jsonResponse, fieldName, out var actions) &&
                        actions.ValueKind == JsonValueKind.Array)
                    {
                        _logger.LogInformation("✅ Found actions array using field: {FieldName}", fieldName);
                        return new
                        {
                            id = "http_config_001",
                            title = "HTTP API Configuration",
                            description = "Generated HTTP configuration",
                            actions = JsonSerializer.Deserialize<object[]>(actions.GetRawText(), jsonOptions)
                        };
                    }
                }

                // Try to find any array at root level
                foreach (var property in jsonResponse.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        _logger.LogInformation("✅ Found array at property: {PropertyName}", property.Name);
                        return new
                        {
                            id = "http_config_001",
                            title = "HTTP API Configuration",
                            description = "Generated HTTP configuration",
                            actions = JsonSerializer.Deserialize<object[]>(property.Value.GetRawText(), jsonOptions)
                        };
                    }
                }

                // If no specific structure found, try to deserialize the entire response
                var deserializedResult = JsonSerializer.Deserialize<object>(aiResponse, jsonOptions);
                if (deserializedResult != null)
                {
                    _logger.LogInformation("✅ Using entire deserialized response");
                    return deserializedResult;
                }

                _logger.LogWarning("⚠️ No valid structure found in response, using fallback");
                return GenerateFallbackHttpConfig(input);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "❌ Failed to parse HTTP config JSON response");
                _logger.LogDebug("📋 Failed response content: {Content}",
                    aiResponse.Length > 1000 ? aiResponse.Substring(0, 1000) + "..." : aiResponse);

                var fallbackResult = GenerateFallbackHttpConfig(input);
                if (fallbackResult == null)
                {
                    _logger.LogWarning("⚠️ Fallback config generation also failed - no valid URL found in input");
                }
                return fallbackResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Unexpected error parsing HTTP config response");
                return GenerateFallbackHttpConfig(input);
            }
        }

        /// <summary>
        /// Extract JSON from markdown code blocks
        /// </summary>
        private string? ExtractJsonFromMarkdown(string content)
        {
            // Try to find JSON in markdown code blocks: ```json ... ``` or ``` ... ```
            var jsonBlockPattern = @"```(?:json)?\s*(\{[\s\S]*?\})\s*```";
            var match = System.Text.RegularExpressions.Regex.Match(content, jsonBlockPattern);

            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value.Trim();
            }

            // Try to find JSON without markdown (look for first { to last })
            var firstBrace = content.IndexOf('{');
            var lastBrace = content.LastIndexOf('}');

            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                var potentialJson = content.Substring(firstBrace, lastBrace - firstBrace + 1);
                // Only return if it looks like valid JSON (simple check)
                if (potentialJson.Contains("\"") && potentialJson.Contains(":"))
                {
                    return potentialJson;
                }
            }

            return null;
        }

        /// <summary>
        /// Try to get property with case-insensitive name matching
        /// </summary>
        private bool TryGetPropertyCaseInsensitive(JsonElement element, string propertyName, out JsonElement value)
        {
            // Try exact match first
            if (element.TryGetProperty(propertyName, out value))
            {
                return true;
            }

            // Try case-insensitive match
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Try flexible JSON structure parsing for non-standard responses
        /// </summary>
        private object? TryParseFlexibleJsonStructure(string content, AIHttpConfigGenerationInput input, JsonSerializerOptions jsonOptions)
        {
            try
            {
                // Remove potential leading/trailing text
                var trimmedContent = content.Trim();

                // Try to find and extract the main JSON object/array
                var jsonMatch = System.Text.RegularExpressions.Regex.Match(trimmedContent, @"(\{[\s\S]*\}|\[[\s\S]*\])");
                if (jsonMatch.Success)
                {
                    var jsonPart = jsonMatch.Groups[1].Value;
                    _logger.LogDebug("🔧 Extracted JSON part: {JsonPart}",
                        jsonPart.Length > 500 ? jsonPart.Substring(0, 500) + "..." : jsonPart);

                    var parsed = JsonSerializer.Deserialize<JsonElement>(jsonPart, jsonOptions);
                    return JsonSerializer.Deserialize<object>(jsonPart, jsonOptions);
                }

                _logger.LogWarning("⚠️ Could not extract valid JSON structure");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Flexible JSON parsing failed");
            }

            return GenerateFallbackHttpConfig(input);
        }

        /// <summary>
        /// Generate fallback HTTP configuration
        /// </summary>
        private object GenerateFallbackHttpConfig(AIHttpConfigGenerationInput input)
        {
            var userInput = input.UserInput?.ToLowerInvariant() ?? "";
            var method = "GET";
            string url = null;
            var actionName = "api_request";
            var queryParams = new Dictionary<string, string>();

            // Enhanced method detection
            if (userInput.Contains("post") || userInput.Contains("create") || userInput.Contains("submit")) method = "POST";
            else if (userInput.Contains("put") || userInput.Contains("update")) method = "PUT";
            else if (userInput.Contains("delete") || userInput.Contains("remove")) method = "DELETE";
            else if (userInput.Contains("patch") || userInput.Contains("modify")) method = "PATCH";

            // Enhanced URL extraction - only use if found in input
            var urlMatch = System.Text.RegularExpressions.Regex.Match(input.UserInput ?? "", @"https?://[^\s'""<>\[\]{}|\\^`]+");
            if (urlMatch.Success)
            {
                url = urlMatch.Value;
                try
                {
                    var uri = new Uri(url);
                    var pathSegments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (pathSegments.Length > 0)
                    {
                        actionName = $"{method.ToLowerInvariant()}_{pathSegments.Last().Replace("-", "_")}";
                    }

                    // Extract query parameters from URL
                    if (!string.IsNullOrEmpty(uri.Query))
                    {
                        var query = uri.Query.TrimStart('?');
                        var queryParts = query.Split('&');
                        foreach (var part in queryParts)
                        {
                            var keyValue = part.Split('=');
                            if (keyValue.Length == 2)
                            {
                                var key = System.Web.HttpUtility.UrlDecode(keyValue[0]);
                                var value = System.Web.HttpUtility.UrlDecode(keyValue[1]);
                                queryParams[key] = value;
                            }
                        }

                        // Remove query parameters from URL
                        url = uri.GetLeftPart(UriPartial.Path);
                    }
                }
                catch
                {
                    // Ignore URI parsing errors
                }
            }
            else
            {
                // Try to find relative paths
                var pathMatch = System.Text.RegularExpressions.Regex.Match(input.UserInput ?? "", @"/[a-zA-Z0-9\-_/]+");
                if (pathMatch.Success)
                {
                    // Found a path but no domain - this is still not a complete URL
                    // According to user requirements, don't provide default domain
                    return null;
                }
                else
                {
                    // No URL found at all - return null instead of default
                    return null;
                }
            }

            // Only proceed if we have a valid URL
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            return new
            {
                actionPlan = new
                {
                    actions = new[] {
                    new {
                        httpConfig = new {
                            method = method,
                            url = url,
                                headers = new Dictionary<string, string>
                                {
                                    ["Content-Type"] = "application/json",
                                    ["Accept"] = "application/json"
                                },
                                @params = queryParams,
                                bodyType = method == "GET" || method == "DELETE" ? "none" : "raw",
                            body = "",
                            rawFormat = "json",
                                actionName = actionName,
                                timeout = 30,
                                followRedirects = true
                            }
                        }
                    }
                }
            };
        }

        #endregion
    }
}
