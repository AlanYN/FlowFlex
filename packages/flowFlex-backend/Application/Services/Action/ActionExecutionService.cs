using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Domain.Repository.Action;
using FlowFlex.Domain.Shared.Enums.Action;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using AutoMapper;
using FlowFlex.Application.Services.OW.Extensions;
using System.Collections.Generic;

namespace FlowFlex.Application.Services.Action
{
    /// <summary>
    /// Service for executing individual actions
    /// </summary>
    public class ActionExecutionService : IActionExecutionService
    {
        private readonly IActionDefinitionRepository _actionDefinitionRepository;
        private readonly IActionExecutionRepository _actionExecutionRepository;
        private readonly IActionExecutionFactory _actionExecutorFactory;
        private readonly IActionManagementService _actionManagementService;
        private readonly IFieldLookupService _fieldLookupService;
        private readonly IFieldMatchingAIService _fieldMatchingAIService;
        private readonly IActionTriggerMappingRepository _actionTriggerMappingRepository;
        private readonly ILogger<ActionExecutionService> _logger;
        private readonly IMapper _mapper;
        private readonly UserContext _userContext;

        public ActionExecutionService(
            IActionDefinitionRepository actionDefinitionRepository,
            IActionExecutionRepository actionExecutionRepository,
            IActionExecutionFactory actionExecutorFactory,
            IActionManagementService actionManagementService,
            IFieldLookupService fieldLookupService,
            IFieldMatchingAIService fieldMatchingAIService,
            IActionTriggerMappingRepository actionTriggerMappingRepository,
            IMapper mapper,
            UserContext userContext,
            ILogger<ActionExecutionService> logger)
        {
            _actionDefinitionRepository = actionDefinitionRepository;
            _actionExecutionRepository = actionExecutionRepository;
            _actionExecutorFactory = actionExecutorFactory;
            _actionManagementService = actionManagementService;
            _fieldLookupService = fieldLookupService;
            _fieldMatchingAIService = fieldMatchingAIService;
            _actionTriggerMappingRepository = actionTriggerMappingRepository;
            _logger = logger;
            _mapper = mapper;
            _userContext = userContext;
        }

        public async Task<JToken?> ExecuteActionAsync(
            long actionDefinitionId,
            object contextData = null,
            long? userId = null,
            long? triggerMappingId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Get action definition
                var actionDefinition = await _actionDefinitionRepository.GetByIdAsync(actionDefinitionId);
                if (actionDefinition == null)
                {
                    _logger.LogWarning("Action definition not found: {ActionId}", actionDefinitionId);
                    return null;
                }

                // Create execution record
                var execution = new Domain.Entities.Action.ActionExecution
                {
                    ActionDefinitionId = actionDefinitionId,
                    ActionName = actionDefinition.ActionName,
                    ActionType = actionDefinition.ActionType,
                    ExecutionStatus = ActionExecutionStatusEnum.Running.ToString(),
                    StartedAt = DateTime.UtcNow,
                    TriggerContext = contextData != null ? JToken.FromObject(contextData) : new JObject(),
                    ExecutionId = SnowFlakeSingle.Instance.NextId().ToString(),
                    ActionTriggerMappingId = triggerMappingId
                };

                // Initialize create information with proper tenant and app context
                execution.InitCreateInfo(_userContext);

                await _actionExecutionRepository.InsertAsync(execution, cancellationToken);

                try
                {
                    // Fetch lookup options from ActionConfig.lookupMappings
                    List<FieldLookupResult>? lookupResults = null;
                    List<FieldMappingItem>? lookupMappings = null;
                    var lookupMappingsToken = actionDefinition.ActionConfig?["lookupMappings"];
                    if (lookupMappingsToken != null && lookupMappingsToken.Type == JTokenType.Array && lookupMappingsToken.Any())
                    {
                        try
                        {
                            lookupMappings = lookupMappingsToken.ToObject<List<FieldMappingItem>>();
                            if (lookupMappings != null && lookupMappings.Any())
                            {
                                lookupResults = await _fieldLookupService.FetchLookupOptionsAsync(
                                    lookupMappings, contextData, cancellationToken);

                                _logger.LogDebug("Fetched lookup results for action {ActionId}: {Count} fields",
                                    actionDefinitionId, lookupResults.Count);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Lookup failure should not block action execution
                            _logger.LogWarning(ex, "Failed to fetch lookup options for action {ActionId}, continuing execution",
                                actionDefinitionId);
                        }
                    }

                    // Store lookup metadata in execution input if available
                    if (lookupResults != null && lookupResults.Any())
                    {
                        // Resolve lookup values with AI-enhanced matching
                        var (enrichedContext, aiMetadata) = await EnrichContextWithLookupValuesAsync(
                            contextData, lookupResults, lookupMappings ?? new List<FieldMappingItem>(), triggerMappingId ?? 0, cancellationToken);
                        contextData = enrichedContext;

                        // Store lookup results and AI match metadata in execution record
                        var executionInputData = new Dictionary<string, object>
                        {
                            ["lookupResults"] = lookupResults
                        };
                        if (aiMetadata.Any())
                        {
                            executionInputData["aiMatchMetadata"] = aiMetadata;
                        }
                        execution.ExecutionInput = JToken.FromObject(executionInputData);
                        await _actionExecutionRepository.UpdateAsync(execution);

                        // Apply default values — use aiMetadata to detect unmatched fields
                        if (lookupMappings != null && lookupMappings.Any())
                        {
                            contextData = ApplyDefaultValues(contextData, lookupMappings, aiMetadata);
                        }
                    }
                    else if (lookupMappings != null && lookupMappings.Any())
                    {
                        // No lookup was performed, but some fields may have defaultValue configured
                        contextData = ApplyDefaultValues(contextData, lookupMappings, null);
                    }

                    // Get executor and execute
                    var executor = _actionExecutorFactory.CreateExecutor((ActionTypeEnum)Enum.Parse(typeof(ActionTypeEnum), actionDefinition.ActionType));
                    var result = await executor.ExecuteAsync(JsonConvert.SerializeObject(actionDefinition.ActionConfig), contextData);

                    // Update execution record with success
                    execution.ExecutionStatus = ActionExecutionStatusEnum.Completed.ToString();
                    execution.CompletedAt = DateTime.UtcNow;
                    execution.ExecutionOutput = result != null ? SafeCreateJToken(result) : new JObject();
                    execution.InitUpdateInfo(_userContext);
                    await _actionExecutionRepository.UpdateAsync(execution);

                    _logger.LogInformation(
                        "Action executed successfully: ActionId={ActionId}, ExecutionId={ExecutionId}",
                        actionDefinitionId,
                        execution.Id);
                }
                catch (Exception ex)
                {
                    // Update execution record with failure
                    execution.ExecutionStatus = ActionExecutionStatusEnum.Failed.ToString();
                    execution.CompletedAt = DateTime.UtcNow;
                    execution.ErrorMessage = ex.Message;
                    execution.InitUpdateInfo(_userContext);
                    await _actionExecutionRepository.UpdateAsync(execution);

                    _logger.LogError(ex,
                        "Action execution failed: ActionId={ActionId}, ExecutionId={ExecutionId}",
                        actionDefinitionId,
                        execution.Id);
                    throw;
                }
                return execution.ExecutionOutput;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExecuteActionAsync: ActionId={ActionId}", actionDefinitionId);
                throw;
            }
        }

        public async Task<object> ExecuteActionDirectlyAsync(
            ActionTypeEnum actionType,
            string actionConfig,
            object contextData = null)
        {
            try
            {
                _logger.LogInformation("Executing action directly: ActionType={ActionType}", actionType);

                // Validate action config using ActionManagementService
                _actionManagementService.ValidateActionConfig(actionType, actionConfig);

                // Create executor and execute
                var executor = _actionExecutorFactory.CreateExecutor(actionType);
                var result = await executor.ExecuteAsync(actionConfig, contextData);

                _logger.LogInformation("Action executed successfully: ActionType={ActionType}", actionType);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExecuteActionDirectlyAsync: ActionType={ActionType}", actionType);
                throw;
            }
        }

        #region Execution History

        public async Task<PageModelDto<ActionExecutionWithActionInfoDto>> GetExecutionsByTriggerSourceIdAsync(
            long triggerSourceId,
            int pageIndex = 1,
            int pageSize = 10,
            List<JsonQueryCondition>? jsonConditions = null)
        {
            try
            {
                var (data, totalCount) = await _actionExecutionRepository.GetByTriggerSourceIdWithActionInfoAsync(
                    triggerSourceId, pageIndex, pageSize, jsonConditions);

                var dtoList = _mapper.Map<List<ActionExecutionWithActionInfoDto>>(data);

                return new PageModelDto<ActionExecutionWithActionInfoDto>(pageIndex, pageSize, dtoList, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetExecutionsByTriggerSourceIdAsync: TriggerSourceId={TriggerSourceId}", triggerSourceId);
                throw;
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Safely create JToken from object, handling potential Unicode escape sequence issues
        /// </summary>
        private JToken SafeCreateJToken(object obj)
        {
            try
            {
                // First attempt: direct conversion
                return JToken.FromObject(obj);
            }
            catch (Exception ex) when (ex.Message.Contains("Unicode") || ex.Message.Contains("escape"))
            {
                _logger.LogWarning(ex, "Failed to create JToken directly, attempting safe conversion");

                try
                {
                    // Second attempt: serialize to JSON string first, then sanitize
                    var jsonString = JsonConvert.SerializeObject(obj);
                    var sanitizedJson = SanitizeJsonString(jsonString);
                    return JToken.Parse(sanitizedJson);
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx, "Failed to create JToken with sanitization, creating error object");

                    // Fallback: create a safe error object
                    return JObject.FromObject(new
                    {
                        success = false,
                        error = "Failed to serialize execution output due to encoding issues",
                        originalError = ex.Message,
                        timestamp = DateTimeOffset.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating JToken, creating error object");

                return JObject.FromObject(new
                {
                    success = false,
                    error = "Failed to serialize execution output",
                    originalError = ex.Message,
                    timestamp = DateTimeOffset.UtcNow
                });
            }
        }

        /// <summary>
        /// Sanitize JSON string to remove problematic Unicode escape sequences
        /// </summary>
        private string SanitizeJsonString(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
                return jsonString;

            try
            {
                // Replace problematic Unicode escape sequences that might cause PostgreSQL issues
                var sanitized = jsonString;

                // Remove or replace null characters and other control characters that cause issues
                sanitized = System.Text.RegularExpressions.Regex.Replace(
                    sanitized,
                    @"\\u000[0-8]|\\u000[bB]|\\u000[eE-fF]|\\u001[0-9a-fA-F]",
                    "");

                // Ensure the result is still valid JSON
                JToken.Parse(sanitized); // This will throw if invalid

                return sanitized;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to sanitize JSON string, returning safe placeholder");

                // Return a safe JSON object if sanitization fails
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = "Content sanitized due to encoding issues",
                    originalLength = jsonString.Length,
                    timestamp = DateTimeOffset.UtcNow
                });
            }
        }

        /// <summary>
        /// Enrich contextData with resolved lookup values using exact match first, then AI matching as fallback.
        /// For each successful lookup field:
        /// 1. Get the source field value from contextData (the WFE field value, e.g. "张三")
        /// 2. Try exact match against the lookup options' display field
        /// 3. If exact match fails, collect unmatched fields for AI batch matching
        /// 4. Use AI results to resolve remaining fields
        /// 5. Store match metadata in execution record for audit
        /// </summary>
        private async Task<(object enrichedContext, Dictionary<string, AiMatchMetadata> metadata)> EnrichContextWithLookupValuesAsync(
            object contextData,
            List<FieldLookupResult> lookupResults,
            List<FieldMappingItem> lookupMappings,
            long triggerMappingId,
            CancellationToken cancellationToken)
        {
            var metadata = new Dictionary<string, AiMatchMetadata>();

            try
            {
                // Convert contextData to a mutable dictionary
                Dictionary<string, object> contextDict = ConvertToContextDictionary(contextData);
                var lookupMappingByApiField = lookupMappings
                    .Where(m => !string.IsNullOrWhiteSpace(m.ApiField) && m.Lookup != null)
                    .GroupBy(m => m.ApiField, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                var unmatchedFields = new List<FieldMatchContext>();

                // Phase 1: Exact matching
                foreach (var lookupResult in lookupResults)
                {
                    if (lookupResult.Status != "success" || !lookupResult.Options.Any())
                    {
                        // Record metadata so ApplyDefaultValues knows this field's lookup yielded nothing
                        metadata[lookupResult.ApiField] = new AiMatchMetadata
                        {
                            OriginalInput = string.Empty,
                            MatchedValue = null,
                            Confidence = 0,
                            Reasoning = lookupResult.Status != "success"
                                ? $"Lookup failed: {lookupResult.Error ?? "unknown error"}"
                                : "Lookup returned empty options list",
                            Source = "lookup_empty"
                        };
                        continue;
                    }

                    if (!lookupMappingByApiField.TryGetValue(lookupResult.ApiField, out var lookupMapping))
                    {
                        _logger.LogWarning("Lookup result for {ApiField} has no matching ActionConfig.lookupMappings entry, skipping value enrichment",
                            lookupResult.ApiField);
                        continue;
                    }

                    var sourceValue = ExtractSourceFieldValue(contextData, contextDict, lookupMapping.WfeField);

                    if (string.IsNullOrWhiteSpace(sourceValue))
                    {
                        metadata[lookupResult.ApiField] = new AiMatchMetadata
                        {
                            OriginalInput = string.Empty,
                            MatchedValue = null,
                            Confidence = 0,
                            Reasoning = $"Source field '{lookupMapping.WfeField}' was not found in context data",
                            Source = "source_missing"
                        };
                        _logger.LogDebug("Lookup unresolved: {TargetParam}, source field '{SourceField}' was not found in context",
                            lookupResult.ApiField, lookupMapping.WfeField);
                        continue;
                    }

                    // Multiple results - try exact match by configured source field value in display
                    var exactMatched = false;
                    var matchedOption = lookupResult.Options.FirstOrDefault(
                        o => string.Equals(o.Display, sourceValue, StringComparison.OrdinalIgnoreCase));

                    if (matchedOption != null)
                    {
                        contextDict[lookupResult.ApiField] = matchedOption.Value;
                        metadata[lookupResult.ApiField] = new AiMatchMetadata
                        {
                            OriginalInput = sourceValue,
                            MatchedValue = matchedOption.Value,
                            Confidence = 1.0,
                            Reasoning = $"Exact match for '{sourceValue}'",
                            Source = "exact_matched"
                        };
                        _logger.LogDebug("Lookup resolved: {TargetParam} = {Value} (exact match from '{SourceField}')",
                            lookupResult.ApiField, matchedOption.Value, lookupMapping.WfeField);
                        exactMatched = true;
                    }

                    if (!exactMatched)
                    {
                        // Collect for AI matching using the configured source field only.
                        unmatchedFields.Add(new FieldMatchContext
                        {
                            ApiField = lookupResult.ApiField,
                            RawValue = sourceValue,
                            Options = lookupResult.Options
                        });
                    }
                }

                // Phase 2: AI matching for unmatched fields
                if (unmatchedFields.Any())
                {
                    _logger.LogInformation("AI field matching: {Count} fields need AI resolution", unmatchedFields.Count);

                    try
                    {
                        var aiResults = await _fieldMatchingAIService.MatchFieldsAsync(unmatchedFields, cancellationToken);

                        // Build a lookup: apiField → valid option values, for hallucination guard
                        var validOptionsByField = unmatchedFields
                            .ToDictionary(f => f.ApiField, f => f.Options, StringComparer.OrdinalIgnoreCase);

                        foreach (var aiResult in aiResults)
                        {
                            // Guard: reject AI values that are not in the original options list
                            if (aiResult.MatchedValue != null)
                            {
                                var isValidValue = validOptionsByField.TryGetValue(aiResult.ApiField, out var validOptions)
                                    && validOptions.Any(o => string.Equals(o.Value, aiResult.MatchedValue, StringComparison.OrdinalIgnoreCase));

                                if (!isValidValue)
                                {
                                    _logger.LogWarning(
                                        "AI returned value '{Value}' for field '{Field}' is not in the valid options list, treating as unmatched",
                                        aiResult.MatchedValue, aiResult.ApiField);
                                    metadata[aiResult.ApiField] = new AiMatchMetadata
                                    {
                                        OriginalInput = aiResult.RawValue,
                                        MatchedValue = null,
                                        Confidence = 0,
                                        Reasoning = $"AI suggested '{aiResult.MatchedValue}' but it is not in the valid options list",
                                        Source = "ai_unmatched"
                                    };
                                    continue;
                                }

                                contextDict[aiResult.ApiField] = aiResult.MatchedValue;
                                _logger.LogDebug("AI matched: {TargetParam} = {Value} (confidence: {Confidence})",
                                    aiResult.ApiField, aiResult.MatchedValue, aiResult.Confidence);
                            }
                            else
                            {
                                _logger.LogDebug("AI unmatched: {TargetParam}, raw value '{RawValue}' has no match",
                                    aiResult.ApiField, aiResult.RawValue);
                            }

                            metadata[aiResult.ApiField] = new AiMatchMetadata
                            {
                                OriginalInput = aiResult.RawValue,
                                MatchedValue = aiResult.MatchedValue,
                                Confidence = aiResult.Confidence,
                                Reasoning = aiResult.Reasoning,
                                Source = aiResult.Source
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        // AI failure should not block execution - mark all as unmatched
                        _logger.LogWarning(ex, "AI field matching failed, continuing with unmatched fields");
                        foreach (var field in unmatchedFields)
                        {
                            metadata[field.ApiField] = new AiMatchMetadata
                            {
                                OriginalInput = field.RawValue,
                                MatchedValue = null,
                                Confidence = 0,
                                Reasoning = "AI matching unavailable",
                                Source = "ai_unmatched"
                            };
                        }
                    }
                }

                return (contextDict, metadata);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enrich context with lookup values for trigger mapping {MappingId}, returning original context",
                    triggerMappingId);
                return (contextData, metadata);
            }
        }

        /// <summary>
        /// Extract the configured source field value from context data.
        /// </summary>
        private string? ExtractSourceFieldValue(object contextData, Dictionary<string, object> contextDict, string wfeField)
        {
            var sourceField = NormalizeWfeField(wfeField);
            if (string.IsNullOrWhiteSpace(sourceField))
            {
                return null;
            }

            if (TryGetContextDictionaryValue(contextDict, sourceField, out var dictionaryValue))
            {
                return ConvertContextValueToString(dictionaryValue);
            }

            if (!string.Equals(sourceField, wfeField, StringComparison.Ordinal) &&
                TryGetContextDictionaryValue(contextDict, wfeField, out dictionaryValue))
            {
                return ConvertContextValueToString(dictionaryValue);
            }

            try
            {
                // Fallback: use JSONPath SelectToken to support nested paths (e.g. "user.name")
                // that cannot be expressed as flat dictionary keys. contextDict is checked first
                // so enriched values from previous iterations do not interfere here.
                var token = contextData is JToken jt ? jt : JToken.FromObject(contextData);
                var valueToken = token.SelectToken(sourceField);
                if (valueToken != null && valueToken.Type != JTokenType.Null)
                {
                    if (valueToken.Type == JTokenType.Array)
                    {
                        var items = valueToken.Select(t => t.ToString()).ToArray();
                        return string.Join(",", items);
                    }
                    return valueToken.Type == JTokenType.Object
                        ? valueToken.ToString(Formatting.None)
                        : valueToken.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to extract source field '{SourceField}' from context data", sourceField);
            }

            return null;
        }

        private static string NormalizeWfeField(string wfeField)
        {
            var value = (wfeField ?? string.Empty).Trim();
            if (value.StartsWith("{{", StringComparison.Ordinal) && value.EndsWith("}}", StringComparison.Ordinal) && value.Length > 4)
            {
                value = value[2..^2].Trim();
            }

            return value;
        }

        private static bool TryGetContextDictionaryValue(Dictionary<string, object> contextDict, string key, out object? value)
        {
            if (contextDict.TryGetValue(key, out value))
            {
                return true;
            }

            foreach (var kvp in contextDict)
            {
                if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    value = kvp.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        private static string? ConvertContextValueToString(object? value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is JToken token)
            {
                if (token.Type == JTokenType.Array)
                {
                    var items = token.Select(t => t.ToString()).ToArray();
                    return string.Join(",", items);
                }
                return token.Type == JTokenType.Object
                    ? token.ToString(Formatting.None)
                    : token.ToString();
            }

            var str = value.ToString();
            // Handle case where value is already a serialized JSON array string
            if (str != null && str.TrimStart().StartsWith("["))
            {
                try
                {
                    var arr = JArray.Parse(str);
                    var items = arr.Select(t => t.ToString()).ToArray();
                    return string.Join(",", items);
                }
                catch
                {
                    // Not valid JSON array, return as-is
                }
            }

            return str;
        }

        /// <summary>
        /// Convert contextData to a mutable dictionary
        /// </summary>
        private Dictionary<string, object> ConvertToContextDictionary(object contextData)
        {
            // Dictionary<string, object> is a subtype of IDictionary<string, object>,
            // so the first branch covers both — the second branch is not needed.
            if (contextData is IDictionary<string, object> existingDict)
            {
                return new Dictionary<string, object>(existingDict);
            }
            else
            {
                var json = contextData is JToken jt ? jt : JToken.FromObject(contextData);
                return json.ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Apply default values from lookupMappings for fields that are still empty/missing in contextData,
        /// or fields that went through lookup but failed to match (detected via aiMetadata).
        /// This runs after lookup + AI matching, as a final fallback to prevent JSON corruption from empty placeholders.
        /// </summary>
        private object ApplyDefaultValues(object contextData, List<FieldMappingItem> lookupMappings, Dictionary<string, AiMatchMetadata>? aiMetadata)
        {
            try
            {
                var contextDict = ConvertToContextDictionary(contextData);
                var applied = false;

                foreach (var mapping in lookupMappings)
                {
                    // DefaultValue is C# null when not configured (JToken? = null means key absent in JSON)
                    if (mapping.DefaultValue == null)
                        continue;

                    var apiField = mapping.ApiField;
                    if (string.IsNullOrWhiteSpace(apiField))
                        continue;

                    // Determine if this field needs a default value:
                    // 1. Field is empty/missing in contextData
                    // 2. Field went through lookup but failed to match (unmatched metadata)
                    var needsDefault = false;

                    if (!contextDict.TryGetValue(apiField, out var existing) || IsEmptyValue(existing))
                    {
                        needsDefault = true;
                    }
                    else if (aiMetadata != null && aiMetadata.TryGetValue(apiField, out var meta))
                    {
                        // Field has a value but lookup/AI failed to match — the current value is
                        // the raw source value (not a resolved lookup value). Apply default.
                        var isUnmatched = meta.MatchedValue == null ||
                                          meta.Source == "ai_unmatched" ||
                                          meta.Source == "source_missing" ||
                                          meta.Source == "lookup_empty";
                        if (isUnmatched)
                        {
                            needsDefault = true;
                        }
                    }

                    if (!needsDefault)
                        continue;

                    // Inject default value
                    object valueToInject;
                    if (mapping.DefaultValue.Type == JTokenType.Null)
                    {
                        // JSON null literal — inject string "null" so template renders as: null
                        valueToInject = "null";
                    }
                    else
                    {
                        valueToInject = mapping.DefaultValue.ToObject<object>()!;
                    }

                    contextDict[apiField] = valueToInject;
                    applied = true;

                    _logger.LogDebug("Applied default value for field {ApiField}: {DefaultValue} (reason: {Reason})",
                        apiField, mapping.DefaultValue.ToString(Newtonsoft.Json.Formatting.None),
                        needsDefault && aiMetadata?.ContainsKey(apiField) == true ? "lookup_unmatched" : "empty_value");
                }

                return applied ? contextDict : contextData;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply default values, returning original context");
                return contextData;
            }
        }

        /// <summary>
        /// Check if a value is considered "empty" for defaultValue injection purposes
        /// </summary>
        private static bool IsEmptyValue(object? value)
        {
            if (value == null) return true;
            if (value is string str) return string.IsNullOrEmpty(str);
            if (value is JToken jt) return jt.Type == JTokenType.Null || (jt.Type == JTokenType.String && string.IsNullOrEmpty(jt.ToString()));
            return false;
        }

        #endregion
    }
}
