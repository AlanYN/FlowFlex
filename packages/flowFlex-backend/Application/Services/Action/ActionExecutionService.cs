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
                    var lookupMappingsToken = actionDefinition.ActionConfig?["lookupMappings"];
                    if (lookupMappingsToken != null && lookupMappingsToken.Type == JTokenType.Array && lookupMappingsToken.Any())
                    {
                        try
                        {
                            var lookupMappings = lookupMappingsToken.ToObject<List<FieldMappingItem>>();
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
                        execution.ExecutionInput = JToken.FromObject(new { lookupResults });
                        await _actionExecutionRepository.UpdateAsync(execution);

                        // Resolve lookup values and inject into contextData
                        // For each successful lookup, match source field value against options,
                        // then inject the resolved value as the target param into contextData
                        contextData = EnrichContextWithLookupValues(contextData, lookupResults, triggerMappingId ?? 0);
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
        /// Enrich contextData with resolved lookup values.
        /// For each successful lookup field:
        /// 1. Get the source field value from contextData (the WFE field value, e.g. "张三")
        /// 2. Match it against the lookup options' display field
        /// 3. If matched, inject the corresponding value as the target param (apiField) into contextData
        /// This allows the executor's placeholder replacement to pick up the resolved values.
        /// </summary>
        private object EnrichContextWithLookupValues(object contextData, List<FieldLookupResult> lookupResults, long triggerMappingId)
        {
            try
            {
                // Convert contextData to a mutable dictionary
                Dictionary<string, object> contextDict;

                if (contextData is IDictionary<string, object> existingDict)
                {
                    contextDict = new Dictionary<string, object>(existingDict);
                }
                else if (contextData is Dictionary<string, object> dict)
                {
                    contextDict = new Dictionary<string, object>(dict);
                }
                else
                {
                    // Try to convert from JToken or other object
                    var json = contextData is JToken jt ? jt : JToken.FromObject(contextData);
                    contextDict = json.ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>();
                }

                foreach (var lookupResult in lookupResults)
                {
                    if (lookupResult.Status != "success" || !lookupResult.Options.Any())
                    {
                        continue;
                    }

                    // The ApiField is the target param name (e.g. "sales_rep_id")
                    // The WfeField (stored in a parallel structure) is the source field
                    // We need to find the source value from contextData using the source field name

                    // Get source field value from context
                    // The source field name corresponds to the WFE field that was used in the lookup endpoint
                    // For now, if there's only one option or the lookup returned a single match, use it directly
                    if (lookupResult.Options.Count == 1)
                    {
                        // Single result - use it directly as the resolved value
                        contextDict[lookupResult.ApiField] = lookupResult.Options[0].Value;
                        _logger.LogDebug("Lookup resolved: {TargetParam} = {Value} (single result)",
                            lookupResult.ApiField, lookupResult.Options[0].Value);
                    }
                    else
                    {
                        // Multiple results - try to match by source field value in display
                        // Look for the source field value in contextData to match against display
                        var matched = false;
                        foreach (var kvp in contextDict)
                        {
                            var sourceValue = kvp.Value?.ToString();
                            if (string.IsNullOrEmpty(sourceValue)) continue;

                            var matchedOption = lookupResult.Options.FirstOrDefault(
                                o => string.Equals(o.Display, sourceValue, StringComparison.OrdinalIgnoreCase));

                            if (matchedOption != null)
                            {
                                contextDict[lookupResult.ApiField] = matchedOption.Value;
                                _logger.LogDebug("Lookup resolved: {TargetParam} = {Value} (matched display '{Display}' from field '{SourceField}')",
                                    lookupResult.ApiField, matchedOption.Value, matchedOption.Display, kvp.Key);
                                matched = true;
                                break;
                            }
                        }

                        if (!matched)
                        {
                            // Fallback: use first option value
                            contextDict[lookupResult.ApiField] = lookupResult.Options[0].Value;
                            _logger.LogDebug("Lookup resolved: {TargetParam} = {Value} (fallback to first option, no display match found)",
                                lookupResult.ApiField, lookupResult.Options[0].Value);
                        }
                    }
                }

                return contextDict;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enrich context with lookup values for trigger mapping {MappingId}, returning original context",
                    triggerMappingId);
                return contextData;
            }
        }

        #endregion
    }
}