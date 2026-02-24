using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.IServices.AI;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FlowFlex.Application.Services.AI.Action
{
    /// <summary>
    /// AI action service implementation.
    /// Responsible for action analysis, creation (sync and streaming),
    /// and HTTP configuration generation.
    /// Migrated from AIService.ActionAndHttp.cs
    /// </summary>
    public class AIActionService : AIServiceBase, IAIActionService, IScopedService
    {
        private readonly IAIProviderAdapter _providerAdapter;
        private readonly IAIPromptBuilder _promptBuilder;

        public AIActionService(
            IAIProviderAdapter providerAdapter,
            IAIPromptBuilder promptBuilder,
            // Base class dependencies
            ILogger<AIActionService> logger,
            IAIPromptHistoryRepository promptHistoryRepository,
            IOperatorContextService operatorContextService,
            IHttpContextAccessor httpContextAccessor,
            IBackgroundTaskQueue backgroundTaskQueue)
            : base(logger, promptHistoryRepository, operatorContextService, httpContextAccessor, backgroundTaskQueue)
        {
            _providerAdapter = providerAdapter;
            _promptBuilder = promptBuilder;
        }

        #region AnalyzeActionAsync

        /// <inheritdoc />
        public async Task<AIActionAnalysisResult> AnalyzeActionAsync(AIActionAnalysisInput input)
        {
            var startTime = DateTime.UtcNow;
            string prompt = null;
            AIProviderResponse aiResponse = null;

            try
            {
                Logger.LogInformation("Analyzing action for session {SessionId}", input.SessionId);

                prompt = _promptBuilder.BuildActionAnalysisPrompt(input);

                aiResponse = await _providerAdapter.CallAsync(new AIProviderRequest
                {
                    Prompt = prompt,
                    ModelId = input.ModelId,
                    Provider = input.ModelProvider,
                    ModelName = input.ModelName
                });

                QueuePromptHistorySave("ActionAnalysis", "Action", prompt, aiResponse, startTime,
                    input.ModelProvider, input.ModelName, input.ModelId,
                    () => new { sessionId = input.SessionId, messageCount = input.ConversationMessages?.Count ?? 0 });

                if (!aiResponse.Success)
                {
                    return new AIActionAnalysisResult
                    {
                        Success = false,
                        Message = $"AI service error: {aiResponse.ErrorMessage}"
                    };
                }

                var result = ParseActionAnalysisResponse(aiResponse.Content);
                result.SessionId = input.SessionId;
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error analyzing action for session {SessionId}", input.SessionId);

                if (!string.IsNullOrEmpty(prompt))
                {
                    QueueFailedPromptHistorySave("ActionAnalysis", "Action", prompt, ex, startTime,
                        input.ModelProvider, input.ModelName, input.ModelId,
                        () => new { sessionId = input.SessionId });
                }

                return new AIActionAnalysisResult
                {
                    Success = false,
                    Message = $"Failed to analyze action: {ex.Message}"
                };
            }
        }

        #endregion

        #region CreateActionAsync

        /// <inheritdoc />
        public async Task<AIActionCreationResult> CreateActionAsync(AIActionCreationInput input)
        {
            var startTime = DateTime.UtcNow;
            string prompt = null;
            AIProviderResponse aiResponse = null;

            try
            {
                Logger.LogInformation("Creating action plan");

                prompt = _promptBuilder.BuildActionCreationPrompt(input);

                aiResponse = await _providerAdapter.CallAsync(new AIProviderRequest
                {
                    Prompt = prompt,
                    ModelId = input.ModelId,
                    Provider = input.ModelProvider,
                    ModelName = input.ModelName
                });

                QueuePromptHistorySave("ActionCreation", "Action", prompt, aiResponse, startTime,
                    input.ModelProvider, input.ModelName, input.ModelId,
                    () => new { hasAnalysis = input.AnalysisResult != null, priority = input.Priority });

                if (!aiResponse.Success)
                {
                    return new AIActionCreationResult
                    {
                        Success = false,
                        Message = $"AI service error: {aiResponse.ErrorMessage}"
                    };
                }

                return ParseActionCreationResponse(aiResponse.Content);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating action plan");

                if (!string.IsNullOrEmpty(prompt))
                {
                    QueueFailedPromptHistorySave("ActionCreation", "Action", prompt, ex, startTime,
                        input.ModelProvider, input.ModelName, input.ModelId,
                        () => new { hasAnalysis = input.AnalysisResult != null });
                }

                return new AIActionCreationResult
                {
                    Success = false,
                    Message = $"Failed to create action plan: {ex.Message}"
                };
            }
        }

        #endregion

        #region Streaming Methods

        /// <inheritdoc />
        public async IAsyncEnumerable<AIActionStreamResult> StreamAnalyzeActionAsync(
            AIActionAnalysisInput input)
        {
            yield return new AIActionStreamResult { Type = "delta", Content = "Analyzing conversation...", Progress = 10 };

            // Execute outside yield context to avoid yield-in-catch restriction
            AIActionAnalysisResult result = null;
            string errorMessage = null;
            try
            {
                result = await AnalyzeActionAsync(input);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }

            if (errorMessage != null)
            {
                yield return new AIActionStreamResult { Type = "error", Content = errorMessage, Message = "Analysis failed" };
                yield break;
            }

            if (!result.Success)
            {
                yield return new AIActionStreamResult { Type = "error", Content = result.Message, Message = "Analysis failed" };
                yield break;
            }

            yield return new AIActionStreamResult
            {
                Type = "analysis",
                Content = JsonSerializer.Serialize(result),
                Data = result,
                Progress = 100,
                Message = "Analysis complete"
            };
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<AIActionStreamResult> StreamCreateActionAsync(
            AIActionCreationInput input)
        {
            yield return new AIActionStreamResult { Type = "delta", Content = "Creating action plan...", Progress = 10 };

            AIActionCreationResult result = null;
            string errorMessage = null;
            try
            {
                result = await CreateActionAsync(input);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }

            if (errorMessage != null)
            {
                yield return new AIActionStreamResult { Type = "error", Content = errorMessage, Message = "Creation failed" };
                yield break;
            }

            if (!result.Success)
            {
                yield return new AIActionStreamResult { Type = "error", Content = result.Message, Message = "Creation failed" };
                yield break;
            }

            yield return new AIActionStreamResult
            {
                Type = "creation",
                Content = JsonSerializer.Serialize(result),
                Data = result,
                Progress = 100,
                Message = "Action plan created"
            };
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<AIActionStreamResult> StreamGenerateHttpConfigAsync(
            AIHttpConfigGenerationInput input)
        {
            yield return new AIActionStreamResult { Type = "delta", Content = "Generating HTTP configuration...", Progress = 10 };

            // Execute AI call outside yield context
            AIProviderResponse aiResponse = null;
            string errorMessage = null;
            var startTime = DateTime.UtcNow;
            string prompt = null;

            try
            {
                prompt = BuildHttpConfigPrompt(input);

                aiResponse = await _providerAdapter.CallAsync(new AIProviderRequest
                {
                    Prompt = prompt,
                    ModelId = input.ModelId,
                    Provider = input.ModelProvider,
                    ModelName = input.ModelName
                });

                QueuePromptHistorySave("HttpConfigGeneration", "HttpConfig", prompt, aiResponse, startTime,
                    input.ModelProvider, input.ModelName, input.ModelId,
                    () => new { sessionId = input.SessionId, outputFormat = input.OutputFormat });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error generating HTTP config for session {SessionId}", input.SessionId);
                errorMessage = ex.Message;

                if (!string.IsNullOrEmpty(prompt))
                {
                    QueueFailedPromptHistorySave("HttpConfigGeneration", "HttpConfig", prompt, ex, startTime,
                        input.ModelProvider, input.ModelName, input.ModelId,
                        () => new { sessionId = input.SessionId });
                }
            }

            if (errorMessage != null)
            {
                yield return new AIActionStreamResult { Type = "error", Content = errorMessage, Message = "HTTP config generation failed" };
                yield break;
            }

            if (!aiResponse.Success)
            {
                yield return new AIActionStreamResult { Type = "error", Content = aiResponse.ErrorMessage, Message = "HTTP config generation failed" };
                yield break;
            }

            yield return new AIActionStreamResult
            {
                Type = "complete",
                Content = aiResponse.Content,
                Progress = 100,
                Message = "HTTP configuration generated"
            };
        }

        #endregion

        #region Response Parsing

        /// <summary>
        /// Parse AI response into action analysis result
        /// </summary>
        private AIActionAnalysisResult ParseActionAnalysisResponse(string content)
        {
            try
            {
                var jsonContent = ExtractJsonFromResponse(content);
                using var doc = JsonDocument.Parse(jsonContent);
                var root = doc.RootElement;

                var result = new AIActionAnalysisResult
                {
                    Success = true,
                    Message = "Analysis completed successfully"
                };

                if (root.TryGetProperty("actionItems", out var actionItems) && actionItems.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in actionItems.EnumerateArray())
                    {
                        result.ActionItems.Add(new AIActionItem
                        {
                            Id = item.TryGetProperty("id", out var id) ? id.GetString() ?? "" : Guid.NewGuid().ToString(),
                            Title = item.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
                            Description = item.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                            Category = item.TryGetProperty("category", out var cat) ? cat.GetString() ?? "" : "",
                            Priority = item.TryGetProperty("priority", out var pri) ? pri.GetString() ?? "Medium" : "Medium",
                            AssignedTo = item.TryGetProperty("assignedTo", out var assigned) ? assigned.GetString() ?? "" : "",
                            Dependencies = item.TryGetProperty("dependencies", out var deps) && deps.ValueKind == JsonValueKind.Array
                                ? deps.EnumerateArray().Select(d => d.GetString() ?? "").ToList()
                                : new List<string>()
                        });
                    }
                }

                if (root.TryGetProperty("keyInsights", out var insights) && insights.ValueKind == JsonValueKind.Array)
                    result.KeyInsights = insights.EnumerateArray().Select(i => i.GetString() ?? "").ToList();

                if (root.TryGetProperty("nextSteps", out var steps) && steps.ValueKind == JsonValueKind.Array)
                    result.NextSteps = steps.EnumerateArray().Select(s => s.GetString() ?? "").ToList();

                if (root.TryGetProperty("stakeholders", out var stakeholders) && stakeholders.ValueKind == JsonValueKind.Array)
                    result.Stakeholders = stakeholders.EnumerateArray().Select(s => s.GetString() ?? "").ToList();

                if (root.TryGetProperty("priority", out var priority))
                    result.Priority = priority.GetString() ?? "Medium";

                if (root.TryGetProperty("summary", out var summary))
                    result.Summary = summary.GetString() ?? "";

                result.ConfidenceScore = result.ActionItems.Count > 0 ? 0.85 : 0.5;
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to parse action analysis response, generating fallback");
                return GenerateFallbackAnalysisResult(content);
            }
        }

        /// <summary>
        /// Parse AI response into action creation result
        /// </summary>
        private AIActionCreationResult ParseActionCreationResponse(string content)
        {
            try
            {
                var jsonContent = ExtractJsonFromResponse(content);
                using var doc = JsonDocument.Parse(jsonContent);
                var root = doc.RootElement;

                var result = new AIActionCreationResult
                {
                    Success = true,
                    Message = "Action plan created successfully"
                };

                if (root.TryGetProperty("actionPlan", out var planElement))
                {
                    result.ActionPlan = new AIActionPlan
                    {
                        Id = planElement.TryGetProperty("id", out var id) ? id.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString(),
                        Title = planElement.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
                        Description = planElement.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                        Objective = planElement.TryGetProperty("objective", out var obj) ? obj.GetString() ?? "" : "",
                        Priority = planElement.TryGetProperty("priority", out var pri) ? pri.GetString() ?? "Medium" : "Medium"
                    };

                    if (planElement.TryGetProperty("actions", out var actions) && actions.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var action in actions.EnumerateArray())
                        {
                            result.ActionPlan.Actions.Add(new AIActionItem
                            {
                                Id = action.TryGetProperty("id", out var aId) ? aId.GetString() ?? "" : Guid.NewGuid().ToString(),
                                Title = action.TryGetProperty("title", out var aTitle) ? aTitle.GetString() ?? "" : "",
                                Description = action.TryGetProperty("description", out var aDesc) ? aDesc.GetString() ?? "" : "",
                                Category = action.TryGetProperty("category", out var aCat) ? aCat.GetString() ?? "" : "",
                                Priority = action.TryGetProperty("priority", out var aPri) ? aPri.GetString() ?? "Medium" : "Medium"
                            });
                        }
                    }
                }

                if (root.TryGetProperty("implementationSteps", out var implSteps) && implSteps.ValueKind == JsonValueKind.Array)
                    result.ImplementationSteps = implSteps.EnumerateArray().Select(s => s.GetString() ?? "").ToList();

                if (root.TryGetProperty("riskFactors", out var risks) && risks.ValueKind == JsonValueKind.Array)
                    result.RiskFactors = risks.EnumerateArray().Select(r => r.GetString() ?? "").ToList();

                if (root.TryGetProperty("successMetrics", out var metrics) && metrics.ValueKind == JsonValueKind.Array)
                    result.SuccessMetrics = metrics.EnumerateArray().Select(m => m.GetString() ?? "").ToList();

                result.ConfidenceScore = result.ActionPlan?.Actions.Count > 0 ? 0.85 : 0.5;
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to parse action creation response, generating fallback");
                return GenerateFallbackCreationResult(content);
            }
        }

        #endregion

        #region Fallback Generators

        private AIActionAnalysisResult GenerateFallbackAnalysisResult(string rawContent)
        {
            return new AIActionAnalysisResult
            {
                Success = true,
                Message = "Analysis completed with fallback parsing",
                Summary = rawContent?.Length > 500 ? rawContent.Substring(0, 500) + "..." : rawContent ?? "",
                ConfidenceScore = 0.4
            };
        }

        private AIActionCreationResult GenerateFallbackCreationResult(string rawContent)
        {
            return new AIActionCreationResult
            {
                Success = true,
                Message = "Action plan created with fallback parsing",
                ActionPlan = new AIActionPlan
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Generated Action Plan",
                    Description = rawContent?.Length > 500 ? rawContent.Substring(0, 500) + "..." : rawContent ?? ""
                },
                ConfidenceScore = 0.4
            };
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Build HTTP configuration generation prompt
        /// </summary>
        private string BuildHttpConfigPrompt(AIHttpConfigGenerationInput input)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Generate HTTP API configuration based on the following requirements:");
            sb.AppendLine();
            sb.AppendLine($"USER INPUT: {input.UserInput}");

            if (!string.IsNullOrEmpty(input.Context))
                sb.AppendLine($"CONTEXT: {input.Context}");

            if (!string.IsNullOrEmpty(input.FileContent))
            {
                var truncatedContent = input.FileContent.Length > 2000
                    ? input.FileContent.Substring(0, 2000) + "..."
                    : input.FileContent;
                sb.AppendLine($"FILE ({input.FileName}): {truncatedContent}");
            }

            sb.AppendLine();
            sb.AppendLine("Please generate a complete HTTP configuration in JSON format including:");
            sb.AppendLine("- method (GET, POST, PUT, DELETE, PATCH)");
            sb.AppendLine("- url (full URL with path)");
            sb.AppendLine("- headers (key-value pairs)");
            sb.AppendLine("- body (request body if applicable)");
            sb.AppendLine("- authentication (type and details)");
            sb.AppendLine("- queryParameters (if applicable)");

            return sb.ToString();
        }

        /// <summary>
        /// Extract JSON content from AI response that may contain markdown or extra text
        /// </summary>
        private static string ExtractJsonFromResponse(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return "{}";

            // Try to find JSON block in markdown code fence
            var jsonMatch = Regex.Match(content, @"```(?:json)?\s*\n?([\s\S]*?)\n?```", RegexOptions.Singleline);
            if (jsonMatch.Success)
                return jsonMatch.Groups[1].Value.Trim();

            // Try to find JSON object directly
            var braceStart = content.IndexOf('{');
            var braceEnd = content.LastIndexOf('}');
            if (braceStart >= 0 && braceEnd > braceStart)
                return content.Substring(braceStart, braceEnd - braceStart + 1);

            return content.Trim();
        }

        #endregion
    }
}
