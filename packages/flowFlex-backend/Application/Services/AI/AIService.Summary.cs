using FlowFlex.Application.Contracts.IServices;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FlowFlex.Application.Services.AI
{
    public partial class AIService
    {
        #region Stage Summary Generation

        /// <summary>
        /// Generate AI summary for stage based on checklist tasks and questionnaire questions
        /// </summary>
        /// <param name="input">Stage summary generation input</param>
        /// <returns>Generated stage summary</returns>
        public async Task<AIStageSummaryResult> GenerateStageSummaryAsync(AIStageSummaryInput input)
        {
            var startTime = DateTime.UtcNow;
            string prompt = null;
            AIProviderResponse aiResponse = null;

            try
            {
                _logger.LogInformation("Generating AI summary for stage {StageId}: {StageName}", input.StageId, input.StageName);

                // Build the summary generation prompt
                prompt = BuildStageSummaryPrompt(input);

                // Try AI providers with fallback strategy
                aiResponse = await CallAIProviderWithFallbackForSummaryAsync(prompt, input.ModelId);

                // Save prompt history to database (fire-and-forget)
                // Background task queued
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        await SavePromptHistoryAsync("StageSummary", "Stage", input.StageId, input.OnboardingId,
                            prompt, aiResponse, startTime, input.ModelProvider, input.ModelName, input.ModelId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to save prompt history for stage {StageId}", input.StageId);
                    }
                });

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
                                await SavePromptHistoryAsync("StageSummary", "Stage", input.StageId, input.OnboardingId,
                                    prompt, failedResponse, startTime, input.ModelProvider, input.ModelName, input.ModelId);
                            }
                            catch (Exception saveEx)
                            {
                                _logger.LogWarning(saveEx, "Failed to save failed prompt history for stage {StageId}", input.StageId);
                            }
                        });
                }

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
            promptBuilder.AppendLine("Generate a concise stage summary in English.");

            promptBuilder.AppendLine();
            promptBuilder.AppendLine("=== Stage Summary Generation Task ===");
            promptBuilder.AppendLine($"Stage Name: {input.StageName}");
            promptBuilder.AppendLine($"Stage Description: {input.StageDescription}");

            if (!string.IsNullOrEmpty(input.AdditionalContext))
            {
                promptBuilder.AppendLine($"Additional Context: {input.AdditionalContext}");
            }

            promptBuilder.AppendLine();

            // Check if we have any actual data to analyze
            bool hasTaskData = input.ChecklistTasks.Any();
            bool hasQuestionData = input.QuestionnaireQuestions.Any();
            bool hasFieldData = input.StaticFields.Any();

            if (!hasTaskData && !hasQuestionData && !hasFieldData)
            {
                promptBuilder.AppendLine("=== Data Status ===");
                promptBuilder.AppendLine("No checklist tasks, questionnaire responses, or field data available for this stage.");
                promptBuilder.AppendLine("This appears to be a stage without configured components or data collection.");
                promptBuilder.AppendLine();
            }

            // Add checklist tasks information
            if (input.ChecklistTasks.Any())
            {
                promptBuilder.AppendLine("=== Checklist Tasks Analysis ===");
                var completedTasks = input.ChecklistTasks.Count(t => t.IsCompleted);
                var totalTasks = input.ChecklistTasks.Count;
                var requiredTasks = input.ChecklistTasks.Count(t => t.IsRequired);
                var completedRequiredTasks = input.ChecklistTasks.Count(t => t.IsRequired && t.IsCompleted);

                // Provide completion statistics for accurate assessment
                promptBuilder.AppendLine($"Completion Status: {completedTasks}/{totalTasks} tasks completed ({(totalTasks > 0 ? (decimal)completedTasks / totalTasks * 100 : 0):F0}%)");
                if (requiredTasks > 0)
                {
                    promptBuilder.AppendLine($"Required Tasks: {completedRequiredTasks}/{requiredTasks} completed");
                }
                promptBuilder.AppendLine();

                promptBuilder.AppendLine("Tasks:");
                foreach (var task in input.ChecklistTasks)
                {
                    var priority = task.IsRequired ? " [Required]" : "";
                    promptBuilder.AppendLine($"- [{(task.IsCompleted ? "✓" : "○")}] {task.TaskName}{priority}");
                    if (task.IsCompleted && !string.IsNullOrEmpty(task.CompletionNotes))
                    {
                        promptBuilder.AppendLine($"  Notes: {task.CompletionNotes}");
                    }
                    else if (!task.IsCompleted && task.IsRequired)
                    {
                        promptBuilder.AppendLine($"  Status: Pending (Required)");
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

                // Provide completion statistics for accurate assessment
                promptBuilder.AppendLine($"Response Status: {answeredQuestions}/{totalQuestions} questions answered ({(totalQuestions > 0 ? (decimal)answeredQuestions / totalQuestions * 100 : 0):F0}%)");
                if (requiredQuestions > 0)
                {
                    promptBuilder.AppendLine($"Required Questions: {answeredRequiredQuestions}/{requiredQuestions} answered");
                }
                promptBuilder.AppendLine();

                promptBuilder.AppendLine("Questions:");
                foreach (var question in input.QuestionnaireQuestions)
                {
                    var priority = question.IsRequired ? " [Required]" : "";
                    promptBuilder.AppendLine($"- [{(question.IsAnswered ? "✓" : "○")}] {question.QuestionText}{priority}");
                    if (question.IsAnswered && question.Answer != null)
                    {
                        promptBuilder.AppendLine($"  Answer: {question.Answer}");
                    }
                    else if (!question.IsAnswered && question.IsRequired)
                    {
                        promptBuilder.AppendLine($"  Status: Pending (Required)");
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

            // Enhanced summary requirements with data-driven guidance
            promptBuilder.AppendLine("=== Summary Requirements ===");
            if (hasTaskData || hasQuestionData || hasFieldData)
            {
                promptBuilder.AppendLine("Provide a comprehensive stage summary in maximum 150 words with two paragraphs:");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("First Paragraph - Stage Function:");
                promptBuilder.AppendLine("   - Describe the main purpose and core activities of this stage");
                promptBuilder.AppendLine("   - Explain what this stage aims to accomplish");
                promptBuilder.AppendLine("   - Outline the key areas of focus and primary deliverables");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("Second Paragraph - Progress Status:");
                promptBuilder.AppendLine("   - Report actual completion rates (use specific percentages and counts shown above)");
                promptBuilder.AppendLine("   - Highlight completed achievements and pending requirements");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("Output Rules:");
                promptBuilder.AppendLine("- Write as two natural paragraphs without section titles or labels");
                promptBuilder.AppendLine("- Start with stage function/content, then progress status");
                promptBuilder.AppendLine("- Base summary ONLY on the provided data above");
                promptBuilder.AppendLine("- Use specific numbers and completion rates shown");
                promptBuilder.AppendLine("- Plain text only, no formatting or headings");
                promptBuilder.AppendLine("- Maximum 150 words total");
                promptBuilder.AppendLine("- Do NOT invent information not present in the data");
            }
            else
            {
                promptBuilder.AppendLine("Provide a stage summary in maximum 150 words with two paragraphs:");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("First Paragraph - Stage Function:");
                promptBuilder.AppendLine("   - Describe the intended purpose and activities of this stage");
                promptBuilder.AppendLine("   - Explain what this stage is designed to accomplish");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("Second Paragraph - Current Status:");
                promptBuilder.AppendLine("   - Note that no specific tasks or questions are currently configured");
                promptBuilder.AppendLine("   - Suggest what components might be needed to make this stage actionable");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("Output Rules:");
                promptBuilder.AppendLine("- Write as two natural paragraphs without section titles or labels");
                promptBuilder.AppendLine("- Start with stage function/content, then current status");
                promptBuilder.AppendLine("- Do NOT invent completion percentages or fake data");
                promptBuilder.AppendLine("- Plain text only, no formatting or headings");
                promptBuilder.AppendLine("- Maximum 150 words");
                promptBuilder.AppendLine("- Be honest about the lack of configured activities");
            }

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
    }
}
