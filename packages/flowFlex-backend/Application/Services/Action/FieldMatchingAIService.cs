using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Application.Contracts.IServices.AI;
using FlowFlex.Domain.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FlowFlex.Application.Services.Action
{
    /// <summary>
    /// AI-powered field matching service that uses LLM to resolve
    /// free-text input to the correct option value from lookup lists.
    /// Handles synonyms, abbreviations, typos, and cross-language equivalents.
    /// </summary>
    public class FieldMatchingAIService : IFieldMatchingAIService, IScopedService
    {
        private readonly IAIProviderAdapter _aiProviderAdapter;
        private readonly ILogger<FieldMatchingAIService> _logger;

        private const int MaxOptionsForAI = 20;
        private const int LargeListThreshold = 200;
        private const int AiTimeoutSeconds = 10;

        public FieldMatchingAIService(
            IAIProviderAdapter aiProviderAdapter,
            ILogger<FieldMatchingAIService> logger)
        {
            _aiProviderAdapter = aiProviderAdapter;
            _logger = logger;
        }

        /// <summary>
        /// Match multiple fields' raw values against their respective option lists using AI
        /// </summary>
        public async Task<List<FieldMatchResult>> MatchFieldsAsync(
            List<FieldMatchContext> fields,
            CancellationToken cancellationToken = default)
        {
            if (fields == null || !fields.Any())
            {
                return new List<FieldMatchResult>();
            }

            try
            {
                // Pre-filter large option lists to reduce token usage
                var processedFields = fields.Select(f => new FieldMatchContext
                {
                    ApiField = f.ApiField,
                    RawValue = f.RawValue,
                    Options = f.Options.Count > LargeListThreshold
                        ? PreFilterOptions(f.RawValue, f.Options, MaxOptionsForAI)
                        : f.Options
                }).ToList();

                // Build prompt
                var prompt = BuildPrompt(processedFields);

                _logger.LogDebug("AI field matching: sending {FieldCount} fields to LLM", processedFields.Count);

                // Call AI provider with timeout
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(AiTimeoutSeconds));

                var response = await _aiProviderAdapter.CallAsync(new AIProviderRequest
                {
                    Prompt = prompt
                });

                if (!response.Success || string.IsNullOrWhiteSpace(response.Content))
                {
                    _logger.LogWarning("AI field matching failed: {Error}", response.ErrorMessage);
                    return CreateUnmatchedResults(fields);
                }

                // Parse and validate response
                var results = ParseAndValidateResponse(response.Content, fields);

                _logger.LogInformation("AI field matching completed: {Matched}/{Total} fields matched",
                    results.Count(r => r.Source == "ai_matched"), results.Count);

                return results;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("AI field matching timed out after {Timeout}s", AiTimeoutSeconds);
                return CreateUnmatchedResults(fields);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI field matching failed unexpectedly");
                return CreateUnmatchedResults(fields);
            }
        }

        /// <summary>
        /// Build the LLM prompt for field matching
        /// </summary>
        private string BuildPrompt(List<FieldMatchContext> fields)
        {
            var sb = new StringBuilder();

            sb.AppendLine("You are a data matching assistant. Match the user's free-text input to the correct option value from the provided lists.");
            sb.AppendLine();
            sb.AppendLine("## Fields to match");
            sb.AppendLine();

            foreach (var field in fields)
            {
                sb.AppendLine($"### {field.ApiField}");
                sb.AppendLine($"User input: \"{field.RawValue}\"");
                sb.AppendLine("Available options:");
                foreach (var option in field.Options)
                {
                    sb.AppendLine($"  - \"{option.Display}\" → {option.Value}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("## Rules");
            sb.AppendLine("1. Consider exact matches, partial matches, synonyms, abbreviations, typos, and cross-language equivalents");
            sb.AppendLine("2. You MUST select from the given options only. Do not invent values.");
            sb.AppendLine("3. If no option matches, return null for value");
            sb.AppendLine("4. confidence is a float between 0 and 1");
            sb.AppendLine();
            sb.AppendLine("## Response format (strict JSON, no extra text)");
            sb.AppendLine("```json");
            sb.AppendLine("{");
            sb.AppendLine("  \"matches\": [");
            sb.AppendLine("    { \"field\": \"field_name\", \"value\": \"matched_value_or_null\", \"confidence\": 0.95, \"reasoning\": \"brief reason\" }");
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            sb.AppendLine("```");

            return sb.ToString();
        }

        /// <summary>
        /// Parse AI response and validate that returned values exist in the option lists
        /// </summary>
        private List<FieldMatchResult> ParseAndValidateResponse(string aiResponse, List<FieldMatchContext> fields)
        {
            try
            {
                // Extract JSON from potential markdown code block wrapper
                var jsonContent = ExtractJsonFromResponse(aiResponse);

                var parsed = JObject.Parse(jsonContent);
                var matchesArray = parsed["matches"] as JArray;

                if (matchesArray == null || !matchesArray.Any())
                {
                    _logger.LogWarning("AI response contains no matches array");
                    return CreateUnmatchedResults(fields);
                }

                var results = new List<FieldMatchResult>();

                foreach (var field in fields)
                {
                    var match = matchesArray.FirstOrDefault(m =>
                        string.Equals(m["field"]?.ToString(), field.ApiField, StringComparison.OrdinalIgnoreCase));

                    if (match == null)
                    {
                        results.Add(new FieldMatchResult
                        {
                            ApiField = field.ApiField,
                            RawValue = field.RawValue,
                            MatchedValue = null,
                            Confidence = 0,
                            Reasoning = "Field not found in AI response",
                            Source = "ai_unmatched"
                        });
                        continue;
                    }

                    var matchedValue = match["value"]?.Type == JTokenType.Null ? null : match["value"]?.ToString();
                    var confidence = match["confidence"]?.Value<double>() ?? 0;
                    var reasoning = match["reasoning"]?.ToString() ?? string.Empty;

                    // Validate: AI returned value must exist in the option list
                    if (matchedValue != null && !field.Options.Any(o =>
                        string.Equals(o.Value, matchedValue, StringComparison.OrdinalIgnoreCase)))
                    {
                        _logger.LogWarning("AI returned invalid value '{Value}' for field '{Field}' - not in option list",
                            matchedValue, field.ApiField);
                        matchedValue = null;
                        confidence = 0;
                        reasoning = "AI returned value not in option list (rejected)";
                    }

                    results.Add(new FieldMatchResult
                    {
                        ApiField = field.ApiField,
                        RawValue = field.RawValue,
                        MatchedValue = matchedValue,
                        Confidence = confidence,
                        Reasoning = reasoning,
                        Source = matchedValue != null ? "ai_matched" : "ai_unmatched"
                    });
                }

                return results;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse AI response JSON");
                return CreateUnmatchedResults(fields);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unexpected error parsing AI response");
                return CreateUnmatchedResults(fields);
            }
        }

        /// <summary>
        /// Extract JSON content from AI response, handling markdown code block wrappers
        /// </summary>
        private string ExtractJsonFromResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return "{}";

            var trimmed = response.Trim();

            // Try to extract from markdown code block: ```json ... ``` or ``` ... ```
            var codeBlockMatch = Regex.Match(trimmed, @"```(?:json)?\s*\n?(.*?)\n?\s*```", RegexOptions.Singleline);
            if (codeBlockMatch.Success)
            {
                return codeBlockMatch.Groups[1].Value.Trim();
            }

            // If it starts with { and ends with }, assume it's raw JSON
            if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
            {
                return trimmed;
            }

            // Try to find JSON object in the response
            var jsonMatch = Regex.Match(trimmed, @"\{.*\}", RegexOptions.Singleline);
            if (jsonMatch.Success)
            {
                return jsonMatch.Value;
            }

            return trimmed;
        }

        /// <summary>
        /// Pre-filter large option lists using fuzzy matching to reduce token usage
        /// </summary>
        private List<OptionItem> PreFilterOptions(string rawValue, List<OptionItem> options, int maxCount)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
                return options.Take(maxCount).ToList();

            var normalizedInput = rawValue.Trim().ToLowerInvariant();

            // Score each option by relevance
            var scored = options.Select(o => new
            {
                Option = o,
                Score = CalculateRelevanceScore(normalizedInput, o.Display?.ToLowerInvariant() ?? string.Empty)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(maxCount)
            .Select(x => x.Option)
            .ToList();

            // If we got too few results from scoring, pad with first N options
            if (scored.Count < 5)
            {
                var remaining = options
                    .Where(o => !scored.Contains(o))
                    .Take(maxCount - scored.Count);
                scored.AddRange(remaining);
            }

            return scored;
        }

        /// <summary>
        /// Calculate relevance score between input and option display text
        /// </summary>
        private double CalculateRelevanceScore(string input, string display)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(display))
                return 0;

            // Exact match
            if (input == display) return 1.0;

            // Contains match
            if (display.Contains(input) || input.Contains(display))
                return 0.8;

            // Starts with
            if (display.StartsWith(input) || input.StartsWith(display))
                return 0.7;

            // Character overlap ratio (simple Jaccard-like similarity)
            var inputChars = new HashSet<char>(input);
            var displayChars = new HashSet<char>(display);
            var intersection = inputChars.Intersect(displayChars).Count();
            var union = inputChars.Union(displayChars).Count();

            if (union == 0) return 0;

            var similarity = (double)intersection / union;
            return similarity > 0.3 ? similarity * 0.6 : 0;
        }

        /// <summary>
        /// Create unmatched results for all fields (used as fallback on AI failure)
        /// </summary>
        private List<FieldMatchResult> CreateUnmatchedResults(List<FieldMatchContext> fields)
        {
            return fields.Select(f => new FieldMatchResult
            {
                ApiField = f.ApiField,
                RawValue = f.RawValue,
                MatchedValue = null,
                Confidence = 0,
                Reasoning = "AI matching unavailable",
                Source = "ai_unmatched"
            }).ToList();
        }
    }
}
