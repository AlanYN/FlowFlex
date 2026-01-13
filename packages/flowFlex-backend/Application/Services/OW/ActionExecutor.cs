using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.StageCondition;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SqlSugar;

namespace FlowFlex.Application.Service.OW
{
    /// <summary>
    /// Condition action executor implementation for Stage Condition feature
    /// </summary>
    public class ConditionActionExecutor : IConditionActionExecutor, IScopedService
    {
        private readonly ISqlSugarClient _db;
        private readonly IStageRepository _stageRepository;
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IEmailService _emailService;
        private readonly IUserService _userService;
        private readonly UserContext _userContext;
        private readonly ILogger<ConditionActionExecutor> _logger;

        public ConditionActionExecutor(
            ISqlSugarClient db,
            IStageRepository stageRepository,
            IOnboardingRepository onboardingRepository,
            IEmailService emailService,
            IUserService userService,
            UserContext userContext,
            ILogger<ConditionActionExecutor> logger)
        {
            _db = db;
            _stageRepository = stageRepository;
            _onboardingRepository = onboardingRepository;
            _emailService = emailService;
            _userService = userService;
            _userContext = userContext;
            _logger = logger;
        }

        /// <summary>
        /// Execute all actions for a condition
        /// </summary>
        public async Task<ActionExecutionResult> ExecuteActionsAsync(string actionsJson, ActionExecutionContext context)
        {
            var result = new ActionExecutionResult { Success = true };

            try
            {
                _logger.LogDebug("Parsing actionsJson: {ActionsJson}", actionsJson);
                
                // Parse actions JSON with settings to handle various formats
                var settings = new JsonSerializerSettings
                {
                    Error = (sender, args) =>
                    {
                        _logger.LogWarning("JSON deserialization error at path {Path}: {Error}", 
                            args.ErrorContext.Path, args.ErrorContext.Error.Message);
                        args.ErrorContext.Handled = true;
                    }
                };
                
                var actions = JsonConvert.DeserializeObject<List<ConditionAction>>(actionsJson, settings);
                
                if (actions == null || actions.Count == 0)
                {
                    _logger.LogDebug("No actions to execute for condition {ConditionId}", context.ConditionId);
                    return result;
                }

                // Sort by order and execute sequentially
                foreach (var action in actions.OrderBy(a => a.Order))
                {
                    var actionResult = await ExecuteActionAsync(action, context);
                    result.Details.Add(actionResult);

                    if (!actionResult.Success)
                    {
                        _logger.LogWarning("Action {ActionType} failed for condition {ConditionId}: {ErrorMessage}",
                            action.Type, context.ConditionId, actionResult.ErrorMessage);
                        // Continue with next action even if one fails
                    }
                }

                // Overall success if at least one action succeeded
                result.Success = result.Details.Any(d => d.Success);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse ActionsJson for condition {ConditionId}", context.ConditionId);
                result.Success = false;
                result.Details.Add(new ActionExecutionDetail
                {
                    ActionType = "ParseError",
                    Success = false,
                    ErrorMessage = $"Invalid ActionsJson format: {ex.Message}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing actions for condition {ConditionId}", context.ConditionId);
                result.Success = false;
                result.Details.Add(new ActionExecutionDetail
                {
                    ActionType = "ExecutionError",
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }

            return result;
        }

        #region Private Methods

        /// <summary>
        /// Execute a single action
        /// </summary>
        private async Task<ActionExecutionDetail> ExecuteActionAsync(ConditionAction action, ActionExecutionContext context)
        {
            try
            {
                return action.Type.ToLower() switch
                {
                    "gotostage" => await ExecuteGoToStageAsync(action, context),
                    "skipstage" => await ExecuteSkipStageAsync(action, context),
                    "endworkflow" => await ExecuteEndWorkflowAsync(action, context),
                    "sendnotification" => await ExecuteSendNotificationAsync(action, context),
                    "updatefield" => await ExecuteUpdateFieldAsync(action, context),
                    "triggeraction" => await ExecuteTriggerActionAsync(action, context),
                    "assignuser" => await ExecuteAssignUserAsync(action, context),
                    _ => new ActionExecutionDetail
                    {
                        ActionType = action.Type,
                        Order = action.Order,
                        Success = false,
                        ErrorMessage = $"Unsupported action type: {action.Type}"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing action {ActionType}", action.Type);
                return new ActionExecutionDetail
                {
                    ActionType = action.Type,
                    Order = action.Order,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }


        /// <summary>
        /// Execute GoToStage action - jump to a specific stage
        /// </summary>
        private async Task<ActionExecutionDetail> ExecuteGoToStageAsync(ConditionAction action, ActionExecutionContext context)
        {
            var result = new ActionExecutionDetail
            {
                ActionType = "GoToStage",
                Order = action.Order,
                ResultData = new Dictionary<string, object>()
            };

            if (!action.TargetStageId.HasValue)
            {
                result.Success = false;
                result.ErrorMessage = "TargetStageId is required for GoToStage action";
                return result;
            }

            try
            {
                // Validate target stage exists
                var targetStage = await _stageRepository.GetByIdAsync(action.TargetStageId.Value);
                if (targetStage == null || !targetStage.IsActive)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Target stage {action.TargetStageId} not found or inactive";
                    return result;
                }

                // Get onboarding - use GetByIdWithoutTenantFilterAsync to avoid tenant filter issues in background tasks
                var onboarding = await _onboardingRepository.GetByIdWithoutTenantFilterAsync(context.OnboardingId);
                if (onboarding == null)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Onboarding {context.OnboardingId} not found";
                    return result;
                }

                // Get current stage for comparison
                var currentStage = await _stageRepository.GetByIdAsync(context.StageId);

                // Mark skipped stages if jumping forward
                if (currentStage != null && targetStage.Order > currentStage.Order)
                {
                    await MarkSkippedStagesAsync(onboarding, currentStage.Order, targetStage.Order);
                }

                // Update onboarding current stage
                await _db.Updateable<Onboarding>()
                    .SetColumns(o => new Onboarding
                    {
                        CurrentStageId = action.TargetStageId.Value,
                        CurrentStageOrder = targetStage.Order,
                        ModifyDate = DateTimeOffset.UtcNow,
                        ModifyBy = _userContext.UserName ?? "SYSTEM"
                    })
                    .Where(o => o.Id == context.OnboardingId)
                    .ExecuteCommandAsync();

                result.Success = true;
                result.ResultData["targetStageId"] = action.TargetStageId.Value;
                result.ResultData["targetStageName"] = targetStage.Name;

                _logger.LogInformation("GoToStage executed: Onboarding {OnboardingId} moved to stage {StageId} ({StageName})",
                    context.OnboardingId, action.TargetStageId.Value, targetStage.Name);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Execute SkipStage action - skip the next N stages
        /// </summary>
        private async Task<ActionExecutionDetail> ExecuteSkipStageAsync(ConditionAction action, ActionExecutionContext context)
        {
            var result = new ActionExecutionDetail
            {
                ActionType = "SkipStage",
                Order = action.Order,
                ResultData = new Dictionary<string, object>()
            };

            try
            {
                // Get current stage
                var currentStage = await _stageRepository.GetByIdAsync(context.StageId);
                if (currentStage == null)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Current stage {context.StageId} not found";
                    return result;
                }

                // Get stages to skip
                var skipCount = action.SkipCount > 0 ? action.SkipCount : 1;
                var nextStages = await _db.Queryable<Stage>()
                    .Where(s => s.WorkflowId == currentStage.WorkflowId && s.IsValid && s.IsActive)
                    .Where(s => s.TenantId == context.TenantId)
                    .Where(s => s.Order > currentStage.Order)
                    .OrderBy(s => s.Order)
                    .Take(skipCount + 1)
                    .ToListAsync();

                if (nextStages.Count <= skipCount)
                {
                    // Not enough stages to skip, go to last stage or end workflow
                    var lastStage = nextStages.LastOrDefault();
                    if (lastStage != null)
                    {
                        action.TargetStageId = lastStage.Id;
                        return await ExecuteGoToStageAsync(action, context);
                    }
                    else
                    {
                        // No more stages, end workflow
                        return await ExecuteEndWorkflowAsync(action, context);
                    }
                }

                // Target stage is the one after skipped stages
                var targetStage = nextStages[skipCount];
                action.TargetStageId = targetStage.Id;

                // Mark skipped stages - use GetByIdWithoutTenantFilterAsync to avoid tenant filter issues in background tasks
                var onboarding = await _onboardingRepository.GetByIdWithoutTenantFilterAsync(context.OnboardingId);
                if (onboarding != null)
                {
                    await MarkSkippedStagesAsync(onboarding, currentStage.Order, targetStage.Order);
                }

                // Use GoToStage to complete the action
                var goToResult = await ExecuteGoToStageAsync(action, context);
                result.Success = goToResult.Success;
                result.ErrorMessage = goToResult.ErrorMessage;
                result.ResultData["skippedCount"] = skipCount;
                result.ResultData["targetStageId"] = targetStage.Id;
                result.ResultData["targetStageName"] = targetStage.Name;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Execute EndWorkflow action - end the workflow
        /// </summary>
        private async Task<ActionExecutionDetail> ExecuteEndWorkflowAsync(ConditionAction action, ActionExecutionContext context)
        {
            var result = new ActionExecutionDetail
            {
                ActionType = "EndWorkflow",
                Order = action.Order,
                ResultData = new Dictionary<string, object>()
            };

            try
            {
                var endStatus = action.EndStatus ?? "Completed";

                // Update onboarding status
                await _db.Updateable<Onboarding>()
                    .SetColumns(o => new Onboarding
                    {
                        Status = endStatus,
                        ModifyDate = DateTimeOffset.UtcNow,
                        ModifyBy = _userContext.UserName ?? "SYSTEM"
                    })
                    .Where(o => o.Id == context.OnboardingId)
                    .ExecuteCommandAsync();

                result.Success = true;
                result.ResultData["endStatus"] = endStatus;

                _logger.LogInformation("EndWorkflow executed: Onboarding {OnboardingId} ended with status {Status}",
                    context.OnboardingId, endStatus);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }


        /// <summary>
        /// Execute SendNotification action - send notification to user
        /// </summary>
        private async Task<ActionExecutionDetail> ExecuteSendNotificationAsync(ConditionAction action, ActionExecutionContext context)
        {
            var result = new ActionExecutionDetail
            {
                ActionType = "SendNotification",
                Order = action.Order,
                ResultData = new Dictionary<string, object>()
            };

            try
            {
                // Get recipient info from action or parameters
                var recipientType = action.RecipientType;
                var recipientId = action.RecipientId;
                var templateId = action.TemplateId;

                // Try to get from parameters if not set at top level
                if (action.Parameters != null)
                {
                    if (string.IsNullOrEmpty(recipientType) && action.Parameters.TryGetValue("recipientType", out var typeObj))
                    {
                        recipientType = typeObj?.ToString();
                    }
                    if (string.IsNullOrEmpty(recipientId) && action.Parameters.TryGetValue("recipientId", out var idObj))
                    {
                        recipientId = idObj?.ToString();
                    }
                    if (string.IsNullOrEmpty(templateId) && action.Parameters.TryGetValue("templateId", out var templateObj))
                    {
                        templateId = templateObj?.ToString();
                    }
                }

                if (string.IsNullOrEmpty(recipientId))
                {
                    result.Success = false;
                    result.ErrorMessage = "RecipientId is required for SendNotification action";
                    return result;
                }

                // Get recipient email based on recipientType
                string recipientEmail = null;
                string recipientName = null;

                if (recipientType?.ToLower() == "user")
                {
                    // Get user email using UserService (supports IDM integration)
                    if (long.TryParse(recipientId, out var userId))
                    {
                        var users = await _userService.GetUsersByIdsAsync(new List<long> { userId }, context.TenantId);
                        var user = users?.FirstOrDefault();

                        if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                        {
                            recipientEmail = user.Email;
                            recipientName = user.Username ?? user.Email;
                            _logger.LogDebug("Found user {UserId} with email {Email} via UserService", userId, recipientEmail);
                        }
                        else
                        {
                            _logger.LogWarning("User {UserId} not found or has no email via UserService", userId);
                        }
                    }
                }
                else if (recipientType?.ToLower() == "email")
                {
                    // recipientId is the email address directly
                    recipientEmail = recipientId;
                    recipientName = recipientId;
                }

                if (string.IsNullOrEmpty(recipientEmail))
                {
                    result.Success = false;
                    result.ErrorMessage = $"Could not find email for recipient: {recipientType}={recipientId}";
                    return result;
                }

                // Get onboarding info for email content
                var onboarding = await _onboardingRepository.GetByIdWithoutTenantFilterAsync(context.OnboardingId);
                var stage = await _stageRepository.GetByIdAsync(context.StageId);

                var caseName = onboarding?.CaseName ?? $"Case #{context.OnboardingId}";
                var stageName = stage?.Name ?? "Current Stage";
                
                // Get next stage name - try to find the stage after current one
                var nextStageName = "Next Stage";
                if (stage != null && onboarding != null)
                {
                    // First check if onboarding has a current stage (might have been updated by GoToStage action)
                    if (onboarding.CurrentStageId.HasValue && onboarding.CurrentStageId.Value != context.StageId)
                    {
                        var currentStage = await _stageRepository.GetByIdAsync(onboarding.CurrentStageId.Value);
                        if (currentStage != null)
                        {
                            nextStageName = currentStage.Name;
                        }
                    }
                    else
                    {
                        // Find the next stage by order
                        var nextStage = await _db.Queryable<Stage>()
                            .Where(s => s.WorkflowId == stage.WorkflowId && s.IsValid && s.IsActive)
                            .Where(s => s.TenantId == onboarding.TenantId)
                            .Where(s => s.Order > stage.Order)
                            .OrderBy(s => s.Order)
                            .FirstAsync();
                        
                        if (nextStage != null)
                        {
                            nextStageName = nextStage.Name;
                        }
                    }
                }
                
                var completedBy = _userContext.UserName ?? "System";
                var completionTime = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                var caseUrl = $"/onboarding/{context.OnboardingId}";

                // Send stage completed notification email
                var emailSent = await _emailService.SendStageCompletedNotificationAsync(
                    recipientEmail,
                    context.OnboardingId.ToString(),
                    caseName,
                    stageName,
                    nextStageName,
                    completedBy,
                    completionTime,
                    caseUrl);

                result.Success = emailSent;
                result.ResultData["recipientType"] = recipientType ?? string.Empty;
                result.ResultData["recipientId"] = recipientId ?? string.Empty;
                result.ResultData["recipientEmail"] = recipientEmail;
                result.ResultData["recipientName"] = recipientName ?? string.Empty;
                result.ResultData["templateId"] = templateId ?? string.Empty;
                result.ResultData["status"] = emailSent ? "Sent" : "Failed";

                if (emailSent)
                {
                    _logger.LogInformation("SendNotification executed: Email sent to {RecipientEmail} for onboarding {OnboardingId}",
                        recipientEmail, context.OnboardingId);
                }
                else
                {
                    result.ErrorMessage = "Failed to send email";
                    _logger.LogWarning("SendNotification failed: Could not send email to {RecipientEmail} for onboarding {OnboardingId}",
                        recipientEmail, context.OnboardingId);
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error executing SendNotification action");
            }

            return result;
        }

        /// <summary>
        /// Execute UpdateField action - update a field value in onboarding
        /// </summary>
        private async Task<ActionExecutionDetail> ExecuteUpdateFieldAsync(ConditionAction action, ActionExecutionContext context)
        {
            var result = new ActionExecutionDetail
            {
                ActionType = "UpdateField",
                Order = action.Order,
                ResultData = new Dictionary<string, object>()
            };

            // Support stageId, fieldId, fieldName, and parameters
            var stageId = action.StageId;
            var fieldId = action.FieldId;
            var fieldName = action.FieldName;
            var fieldValue = action.FieldValue;

            // If top-level fields are empty, try to get from parameters
            if (action.Parameters != null)
            {
                if (!stageId.HasValue && action.Parameters.TryGetValue("stageId", out var stageIdObj))
                {
                    if (long.TryParse(stageIdObj?.ToString(), out var parsedStageId))
                    {
                        stageId = parsedStageId;
                    }
                }
                if (string.IsNullOrEmpty(fieldId) && action.Parameters.TryGetValue("fieldId", out var fieldIdObj))
                {
                    fieldId = fieldIdObj?.ToString();
                }
                if (string.IsNullOrEmpty(fieldName))
                {
                    if (action.Parameters.TryGetValue("fieldPath", out var fieldPathObj))
                    {
                        fieldName = fieldPathObj?.ToString();
                    }
                    else if (action.Parameters.TryGetValue("fieldName", out var fieldNameObj))
                    {
                        fieldName = fieldNameObj?.ToString();
                    }
                }

                // Get value from parameters if not set at top level
                if (fieldValue == null)
                {
                    if (action.Parameters.TryGetValue("newValue", out var newValueObj))
                    {
                        fieldValue = newValueObj;
                    }
                    else if (action.Parameters.TryGetValue("fieldValue", out var fieldValueObj))
                    {
                        fieldValue = fieldValueObj;
                    }
                }
            }

            // Use fieldId as key if provided, otherwise use fieldName
            var fieldKey = !string.IsNullOrEmpty(fieldId) ? fieldId : fieldName;

            if (string.IsNullOrEmpty(fieldKey))
            {
                result.Success = false;
                result.ErrorMessage = "FieldId, FieldName or parameters.fieldPath is required for UpdateField action";
                return result;
            }

            try
            {
                // Get onboarding
                var onboarding = await _onboardingRepository.GetByIdAsync(context.OnboardingId);
                if (onboarding == null)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Onboarding {context.OnboardingId} not found";
                    return result;
                }

                // All fields are stored in CustomFieldsJson
                // For stage-specific fields, use key format: stage_{stageId}_{fieldKey}
                var customFields = string.IsNullOrEmpty(onboarding.CustomFieldsJson)
                    ? new Dictionary<string, object>()
                    : JsonConvert.DeserializeObject<Dictionary<string, object>>(onboarding.CustomFieldsJson) ?? new Dictionary<string, object>();

                string storageKey;
                if (stageId.HasValue)
                {
                    // Stage-specific field: use prefixed key
                    storageKey = $"stage_{stageId.Value}_{fieldKey}";
                }
                else
                {
                    // Onboarding-level field: use fieldKey directly
                    storageKey = fieldKey;
                }

                var oldValue = customFields.ContainsKey(storageKey) ? customFields[storageKey] : null;
                customFields[storageKey] = fieldValue!;

                await _db.Updateable<Onboarding>()
                    .SetColumns(o => new Onboarding
                    {
                        CustomFieldsJson = JsonConvert.SerializeObject(customFields),
                        ModifyDate = DateTimeOffset.UtcNow,
                        ModifyBy = _userContext.UserName ?? "SYSTEM"
                    })
                    .Where(o => o.Id == context.OnboardingId)
                    .ExecuteCommandAsync();

                result.Success = true;
                if (stageId.HasValue)
                {
                    result.ResultData["stageId"] = stageId.Value;
                }
                result.ResultData["fieldId"] = fieldId ?? string.Empty;
                result.ResultData["fieldName"] = fieldName ?? string.Empty;
                result.ResultData["fieldKey"] = fieldKey;
                result.ResultData["storageKey"] = storageKey;
                result.ResultData["oldValue"] = oldValue!;
                result.ResultData["newValue"] = fieldValue!;

                _logger.LogInformation("UpdateField executed: Onboarding {OnboardingId}, StorageKey {StorageKey}, Field {FieldKey} updated from {OldValue} to {NewValue}",
                    context.OnboardingId, storageKey, fieldKey, oldValue, fieldValue);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Execute TriggerAction action - trigger a predefined action
        /// </summary>
        private async Task<ActionExecutionDetail> ExecuteTriggerActionAsync(ConditionAction action, ActionExecutionContext context)
        {
            var result = new ActionExecutionDetail
            {
                ActionType = "TriggerAction",
                Order = action.Order,
                ResultData = new Dictionary<string, object>()
            };

            if (!action.ActionDefinitionId.HasValue)
            {
                result.Success = false;
                result.ErrorMessage = "ActionDefinitionId is required for TriggerAction action";
                return result;
            }

            try
            {
                // Validate action definition exists
                var actionDefinition = await _db.Queryable<Domain.Entities.Action.ActionDefinition>()
                    .Where(a => a.Id == action.ActionDefinitionId.Value && a.IsValid)
                    .Where(a => a.TenantId == context.TenantId)
                    .FirstAsync();

                if (actionDefinition == null || !actionDefinition.IsEnabled)
                {
                    result.Success = false;
                    result.ErrorMessage = $"ActionDefinition {action.ActionDefinitionId} not found or disabled";
                    return result;
                }

                // TODO: Integrate with ActionService to execute the action
                // For now, just log the trigger request
                _logger.LogInformation("TriggerAction requested: ActionDefinitionId={ActionDefinitionId}, ActionName={ActionName}",
                    action.ActionDefinitionId, actionDefinition.ActionName);

                result.Success = true;
                result.ResultData["actionDefinitionId"] = action.ActionDefinitionId.Value;
                result.ResultData["actionName"] = actionDefinition.ActionName;
                result.ResultData["status"] = "Triggered";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Execute AssignUser action - assign user or team to onboarding
        /// </summary>
        private async Task<ActionExecutionDetail> ExecuteAssignUserAsync(ConditionAction action, ActionExecutionContext context)
        {
            var result = new ActionExecutionDetail
            {
                ActionType = "AssignUser",
                Order = action.Order,
                ResultData = new Dictionary<string, object>()
            };

            // Get assigneeType and assigneeIds from parameters
            string? assigneeType = null;
            List<string> assigneeIds = new List<string>();

            if (action.Parameters != null)
            {
                if (action.Parameters.TryGetValue("assigneeType", out var typeObj))
                {
                    assigneeType = typeObj?.ToString()?.ToLower();
                }

                if (action.Parameters.TryGetValue("assigneeIds", out var idsObj))
                {
                    _logger.LogDebug("AssignUser: assigneeIds raw value type={Type}, value={Value}", 
                        idsObj?.GetType().FullName ?? "null", idsObj?.ToString() ?? "null");
                    
                    if (idsObj is Newtonsoft.Json.Linq.JArray jArray)
                    {
                        assigneeIds = jArray.Select(x => x.ToString()).ToList();
                        _logger.LogDebug("AssignUser: Parsed as JArray, count={Count}", assigneeIds.Count);
                    }
                    else if (idsObj is Newtonsoft.Json.Linq.JToken jToken)
                    {
                        // Handle JToken (could be JArray or JValue)
                        if (jToken.Type == Newtonsoft.Json.Linq.JTokenType.Array)
                        {
                            assigneeIds = jToken.ToObject<List<string>>() ?? new List<string>();
                            _logger.LogDebug("AssignUser: Parsed JToken as array, count={Count}", assigneeIds.Count);
                        }
                        else
                        {
                            var val = jToken.ToString();
                            if (!string.IsNullOrEmpty(val))
                            {
                                assigneeIds.Add(val);
                            }
                        }
                    }
                    else if (idsObj is List<object> objList)
                    {
                        assigneeIds = objList.Select(x => x?.ToString() ?? "").Where(x => !string.IsNullOrEmpty(x)).ToList();
                        _logger.LogDebug("AssignUser: Parsed as List<object>, count={Count}", assigneeIds.Count);
                    }
                    else if (idsObj is IEnumerable<object> enumerable && !(idsObj is string))
                    {
                        assigneeIds = enumerable.Select(x => x?.ToString() ?? "").Where(x => !string.IsNullOrEmpty(x)).ToList();
                        _logger.LogDebug("AssignUser: Parsed as IEnumerable, count={Count}", assigneeIds.Count);
                    }
                    else if (idsObj is string strValue)
                    {
                        _logger.LogDebug("AssignUser: assigneeIds is string: {Value}", strValue);
                        // Handle case where assigneeIds is a JSON string array
                        if (strValue.TrimStart().StartsWith("["))
                        {
                            try
                            {
                                var parsed = JsonConvert.DeserializeObject<List<string>>(strValue);
                                if (parsed != null)
                                {
                                    assigneeIds = parsed;
                                    _logger.LogDebug("AssignUser: Parsed JSON string as List, count={Count}", assigneeIds.Count);
                                }
                            }
                            catch
                            {
                                assigneeIds.Add(strValue);
                            }
                        }
                        else if (!string.IsNullOrEmpty(strValue))
                        {
                            assigneeIds.Add(strValue);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("AssignUser: Unhandled assigneeIds type: {Type}", idsObj?.GetType().FullName ?? "null");
                        // Try to convert to string and add
                        var strVal = idsObj?.ToString();
                        if (!string.IsNullOrEmpty(strVal))
                        {
                            assigneeIds.Add(strVal);
                        }
                    }
                }
            }

            // Fallback to legacy top-level properties
            if (string.IsNullOrEmpty(assigneeType))
            {
                if (action.UserId.HasValue)
                {
                    assigneeType = "user";
                    assigneeIds.Add(action.UserId.Value.ToString());
                }
                else if (!string.IsNullOrEmpty(action.TeamId))
                {
                    assigneeType = "team";
                    assigneeIds.Add(action.TeamId);
                }
            }

            if (string.IsNullOrEmpty(assigneeType) || !assigneeIds.Any())
            {
                result.Success = false;
                result.ErrorMessage = "AssignUser action requires assigneeType and assigneeIds in parameters";
                return result;
            }

            try
            {
                // Get onboarding
                var onboarding = await _onboardingRepository.GetByIdAsync(context.OnboardingId);
                if (onboarding == null)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Onboarding {context.OnboardingId} not found";
                    return result;
                }

                if (assigneeType == "user")
                {
                    // Parse current ViewUsers list (JSONB array)
                    // Handle both normal JSON array and double-encoded string
                    var currentUsers = ParseJsonStringArray(onboarding.ViewUsers);

                    // Add new users (avoid duplicates)
                    foreach (var id in assigneeIds)
                    {
                        if (!currentUsers.Contains(id))
                        {
                            currentUsers.Add(id);
                        }
                    }

                    var newViewUsers = JsonConvert.SerializeObject(currentUsers);

                    // Also update OperateUsers
                    var currentOperateUsers = ParseJsonStringArray(onboarding.OperateUsers);

                    foreach (var id in assigneeIds)
                    {
                        if (!currentOperateUsers.Contains(id))
                        {
                            currentOperateUsers.Add(id);
                        }
                    }

                    var newOperateUsers = JsonConvert.SerializeObject(currentOperateUsers);

                    await _db.Updateable<Onboarding>()
                        .SetColumns(o => new Onboarding
                        {
                            ViewUsers = newViewUsers,
                            OperateUsers = newOperateUsers,
                            ModifyDate = DateTimeOffset.UtcNow,
                            ModifyBy = _userContext.UserName ?? "SYSTEM"
                        })
                        .Where(o => o.Id == context.OnboardingId)
                        .ExecuteCommandAsync();

                    result.ResultData["assigneeType"] = "user";
                    result.ResultData["assigneeIds"] = assigneeIds;
                    result.ResultData["newViewUsers"] = currentUsers;
                    result.ResultData["newOperateUsers"] = currentOperateUsers;
                }
                else if (assigneeType == "team")
                {
                    // Parse current ViewTeams list (JSONB array)
                    var currentTeams = string.IsNullOrEmpty(onboarding.ViewTeams)
                        ? new List<string>()
                        : JsonConvert.DeserializeObject<List<string>>(onboarding.ViewTeams) ?? new List<string>();

                    // Add new teams (avoid duplicates)
                    foreach (var id in assigneeIds)
                    {
                        if (!currentTeams.Contains(id))
                        {
                            currentTeams.Add(id);
                        }
                    }

                    var newViewTeams = JsonConvert.SerializeObject(currentTeams);

                    // Also update OperateTeams
                    var currentOperateTeams = string.IsNullOrEmpty(onboarding.OperateTeams)
                        ? new List<string>()
                        : JsonConvert.DeserializeObject<List<string>>(onboarding.OperateTeams) ?? new List<string>();

                    foreach (var id in assigneeIds)
                    {
                        if (!currentOperateTeams.Contains(id))
                        {
                            currentOperateTeams.Add(id);
                        }
                    }

                    var newOperateTeams = JsonConvert.SerializeObject(currentOperateTeams);

                    await _db.Updateable<Onboarding>()
                        .SetColumns(o => new Onboarding
                        {
                            ViewTeams = newViewTeams,
                            OperateTeams = newOperateTeams,
                            ModifyDate = DateTimeOffset.UtcNow,
                            ModifyBy = _userContext.UserName ?? "SYSTEM"
                        })
                        .Where(o => o.Id == context.OnboardingId)
                        .ExecuteCommandAsync();

                    result.ResultData["assigneeType"] = "team";
                    result.ResultData["assigneeIds"] = assigneeIds;
                    result.ResultData["newViewTeams"] = currentTeams;
                    result.ResultData["newOperateTeams"] = currentOperateTeams;
                }

                result.Success = true;

                _logger.LogInformation("AssignUser executed: Onboarding {OnboardingId}, Type={AssigneeType}, Ids={AssigneeIds}",
                    context.OnboardingId, assigneeType, string.Join(",", assigneeIds));
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Parse StagesProgressJson handling double-escaped JSON strings
        /// </summary>
        private List<OnboardingStageProgress> ParseStagesProgressJson(string json, long onboardingId)
        {
            if (string.IsNullOrEmpty(json))
            {
                return new List<OnboardingStageProgress>();
            }

            try
            {
                // First, try direct parsing as List<OnboardingStageProgress>
                return JsonConvert.DeserializeObject<List<OnboardingStageProgress>>(json) 
                    ?? new List<OnboardingStageProgress>();
            }
            catch (JsonException)
            {
                // If direct parsing fails, try to unescape first (handle double-escaped JSON)
                try
                {
                    // The JSON might be stored as a double-escaped string like: "\"[{\\\"stageId\\\":1}]\""
                    // First deserialize to get the inner string
                    var unescapedJson = JsonConvert.DeserializeObject<string>(json);
                    if (!string.IsNullOrEmpty(unescapedJson))
                    {
                        return JsonConvert.DeserializeObject<List<OnboardingStageProgress>>(unescapedJson) 
                            ?? new List<OnboardingStageProgress>();
                    }
                }
                catch (JsonException)
                {
                    // If still fails, the format is unexpected
                }

                _logger.LogWarning("Failed to parse StagesProgressJson for onboarding {OnboardingId}, starting with empty list. Raw value: {RawJson}", 
                    onboardingId, json.Length > 200 ? json.Substring(0, 200) + "..." : json);
                return new List<OnboardingStageProgress>();
            }
        }

        /// <summary>
        /// Mark stages as skipped in StagesProgressJson
        /// </summary>
        private async Task MarkSkippedStagesAsync(Onboarding onboarding, int fromOrder, int toOrder)
        {
            try
            {
                // Get stages to mark as skipped using onboarding's TenantId
                var stagesToSkip = await _db.Queryable<Stage>()
                    .Where(s => s.WorkflowId == onboarding.WorkflowId && s.IsValid && s.IsActive)
                    .Where(s => s.TenantId == onboarding.TenantId)
                    .Where(s => s.Order > fromOrder && s.Order < toOrder)
                    .ToListAsync();

                if (!stagesToSkip.Any())
                {
                    return;
                }

                // Parse StagesProgressJson - handle both string and already-parsed scenarios
                List<OnboardingStageProgress> stagesProgress;
                
                if (onboarding.StagesProgress != null && onboarding.StagesProgress.Any())
                {
                    // Use already-parsed StagesProgress if available
                    stagesProgress = onboarding.StagesProgress;
                }
                else if (!string.IsNullOrEmpty(onboarding.StagesProgressJson))
                {
                    stagesProgress = ParseStagesProgressJson(onboarding.StagesProgressJson, onboarding.Id);
                }
                else
                {
                    stagesProgress = new List<OnboardingStageProgress>();
                }

                foreach (var stage in stagesToSkip)
                {
                    var existingProgress = stagesProgress.FirstOrDefault(p => p.StageId == stage.Id);
                    if (existingProgress != null)
                    {
                        existingProgress.Status = "Skipped";
                        existingProgress.IsCompleted = false;
                    }
                    else
                    {
                        stagesProgress.Add(new OnboardingStageProgress
                        {
                            StageId = stage.Id,
                            StageName = stage.Name,
                            StageOrder = stage.Order,
                            Status = "Skipped",
                            IsCompleted = false
                        });
                    }
                }

                // Update onboarding
                await _db.Updateable<Onboarding>()
                    .SetColumns(o => new Onboarding
                    {
                        StagesProgressJson = JsonConvert.SerializeObject(stagesProgress),
                        ModifyDate = DateTimeOffset.UtcNow
                    })
                    .Where(o => o.Id == onboarding.Id)
                    .ExecuteCommandAsync();

                _logger.LogInformation("Marked {Count} stages as skipped for onboarding {OnboardingId}",
                    stagesToSkip.Count, onboarding.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking skipped stages for onboarding {OnboardingId}", onboarding.Id);
            }
        }

        /// <summary>
        /// Parse JSON string array, handling both normal and double-encoded formats
        /// </summary>
        private List<string> ParseJsonStringArray(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return new List<string>();
            }

            try
            {
                // First try normal JSON array deserialization
                var result = JsonConvert.DeserializeObject<List<string>>(json);
                if (result != null)
                {
                    return result;
                }
            }
            catch (JsonException)
            {
                // If normal parsing fails, try to handle double-encoded string
                try
                {
                    // Check if it's a double-encoded JSON string (e.g., "\"[\\\"123\\\"]\"")
                    var unescaped = JsonConvert.DeserializeObject<string>(json);
                    if (!string.IsNullOrEmpty(unescaped))
                    {
                        var result = JsonConvert.DeserializeObject<List<string>>(unescaped);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                }
                catch
                {
                    // Ignore nested parsing errors
                }
            }

            _logger.LogWarning("Failed to parse JSON string array: {Json}", 
                json.Length > 100 ? json.Substring(0, 100) + "..." : json);
            return new List<string>();
        }

        #endregion
    }
}
