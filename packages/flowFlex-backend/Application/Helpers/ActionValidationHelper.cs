using System;
using System.Collections.Generic;
using System.Linq;
using FlowFlex.Application.Contracts.Dtos.OW.StageCondition;
using FlowFlex.Domain.Shared.Const;
using Newtonsoft.Json;

namespace FlowFlex.Application.Helpers
{
    /// <summary>
    /// Helper class for validating stage condition actions
    /// Extracted from StageConditionService to improve maintainability
    /// </summary>
    public static class ActionValidationHelper
    {
        /// <summary>
        /// Validate ActionsJson format and content
        /// </summary>
        public static ConditionValidationResult ValidateActionsJson(string actionsJson)
        {
            var result = new ConditionValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(actionsJson))
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError { Code = StageConditionConstants.ErrorCodeActionsRequired, Message = "ActionsJson is required" });
                return result;
            }

            try
            {
                var actions = JsonConvert.DeserializeObject<List<ConditionAction>>(actionsJson);
                
                if (actions == null || actions.Count == 0)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError { Code = StageConditionConstants.ErrorCodeActionsEmpty, Message = "ActionsJson must contain at least one action" });
                    return result;
                }

                // Check for conflicting stage control actions
                ValidateStageControlActions(actions, result);

                // Validate each action
                foreach (var action in actions)
                {
                    ValidateSingleAction(action, result);
                }
            }
            catch (JsonException ex)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError { Code = StageConditionConstants.ErrorCodeInvalidJson, Message = $"Invalid JSON format: {ex.Message}" });
            }

            return result;
        }

        /// <summary>
        /// Validate stage control actions for conflicts
        /// </summary>
        private static void ValidateStageControlActions(List<ConditionAction> actions, ConditionValidationResult result)
        {
            var stageControlActions = actions.Where(a => 
                StageConditionConstants.StageControlActionTypes.Contains(a.Type?.ToLower() ?? string.Empty)).ToList();
            
            if (stageControlActions.Count > 1)
            {
                var actionDetails = stageControlActions.Select(a => 
                {
                    if (a.Type?.ToLower() == StageConditionConstants.ActionTypeGoToStage && a.TargetStageId.HasValue)
                        return $"{a.Type}(targetStageId={a.TargetStageId})";
                    else if (a.Type?.ToLower() == StageConditionConstants.ActionTypeSkipStage)
                        return $"{a.Type}(skipCount={a.SkipCount})";
                    else
                        return a.Type;
                });
                
                result.Warnings.Add(new ValidationWarning 
                { 
                    Code = StageConditionConstants.WarningCodeConflictingStageActions, 
                    Message = $"Multiple stage control actions detected: [{string.Join(", ", actionDetails)}]. Only the first action will take effect for stage navigation." 
                });
            }

            // Check for multiple GoToStage with different targets
            var goToStageActions = actions.Where(a => a.Type?.ToLower() == StageConditionConstants.ActionTypeGoToStage && a.TargetStageId.HasValue).ToList();
            if (goToStageActions.Count > 1)
            {
                var targetStageIds = goToStageActions.Select(a => a.TargetStageId!.Value).Distinct().ToList();
                if (targetStageIds.Count > 1)
                {
                    result.Warnings.Add(new ValidationWarning 
                    { 
                        Code = "MULTIPLE_GOTOSTAGE_TARGETS", 
                        Message = $"Multiple GoToStage actions with different targets: [{string.Join(", ", targetStageIds)}]. Only the first GoToStage action will be executed." 
                    });
                }
            }
        }

        /// <summary>
        /// Validate a single action
        /// </summary>
        private static void ValidateSingleAction(ConditionAction action, ConditionValidationResult result)
        {
            if (string.IsNullOrEmpty(action.Type))
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError { Code = StageConditionConstants.ErrorCodeActionTypeRequired, Message = "Action type is required" });
                return;
            }

            if (!StageConditionConstants.ValidActionTypes.Contains(action.Type.ToLower()))
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError { Code = StageConditionConstants.ErrorCodeInvalidActionType, Message = $"Invalid action type: {action.Type}" });
                return;
            }

            // Validate required parameters for each action type
            switch (action.Type.ToLower())
            {
                case StageConditionConstants.ActionTypeGoToStage:
                    ValidateGoToStageAction(action, result);
                    break;

                case StageConditionConstants.ActionTypeUpdateField:
                    ValidateUpdateFieldAction(action, result);
                    break;

                case StageConditionConstants.ActionTypeTriggerAction:
                    ValidateTriggerActionAction(action, result);
                    break;

                case StageConditionConstants.ActionTypeSendNotification:
                    ValidateSendNotificationAction(action, result);
                    break;

                case StageConditionConstants.ActionTypeAssignUser:
                    ValidateAssignUserAction(action, result);
                    break;
            }
        }

        private static void ValidateGoToStageAction(ConditionAction action, ConditionValidationResult result)
        {
            if (!action.TargetStageId.HasValue)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError { Code = "GOTOSTAGE_TARGET_REQUIRED", Message = "GoToStage action requires targetStageId" });
            }
        }

        private static void ValidateUpdateFieldAction(ConditionAction action, ConditionValidationResult result)
        {
            var hasFieldId = !string.IsNullOrEmpty(action.FieldId);
            var hasFieldName = !string.IsNullOrEmpty(action.FieldName);
            var hasFieldInParams = action.Parameters != null && 
                (action.Parameters.ContainsKey("fieldId") || action.Parameters.ContainsKey("fieldPath") || action.Parameters.ContainsKey("fieldName"));
            
            if (!hasFieldId && !hasFieldName && !hasFieldInParams)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError { Code = "UPDATEFIELD_NAME_REQUIRED", Message = "UpdateField action requires fieldId, fieldName or parameters.fieldPath" });
            }
        }

        private static void ValidateTriggerActionAction(ConditionAction action, ConditionValidationResult result)
        {
            if (!action.ActionDefinitionId.HasValue)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError { Code = "TRIGGERACTION_ID_REQUIRED", Message = "TriggerAction action requires actionDefinitionId" });
            }
        }

        private static void ValidateSendNotificationAction(ConditionAction action, ConditionValidationResult result)
        {
            if (action.Parameters != null)
            {
                var hasUsers = HasNonEmptyArray(action.Parameters, "users");
                var hasTeams = HasNonEmptyArray(action.Parameters, "teams");
                var hasLegacyRecipientId = action.Parameters.ContainsKey("recipientId") || 
                                          action.Parameters.ContainsKey("recipientEmail");
                
                if (!hasUsers && !hasTeams && !hasLegacyRecipientId && string.IsNullOrEmpty(action.RecipientId))
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError 
                    { 
                        Code = "SENDNOTIFICATION_RECIPIENT_REQUIRED", 
                        Message = "SendNotification action requires users[], teams[], recipientId, or recipientEmail" 
                    });
                }

                // Validate subject
                if (action.Parameters.TryGetValue("subject", out var subjectObj) && 
                    subjectObj != null && 
                    string.IsNullOrWhiteSpace(subjectObj.ToString()))
                {
                    result.Warnings.Add(new ValidationWarning 
                    { 
                        Code = "SENDNOTIFICATION_EMPTY_SUBJECT", 
                        Message = "SendNotification subject is empty, default subject will be used" 
                    });
                }

                // Validate emailBody
                if (action.Parameters.TryGetValue("emailBody", out var emailBodyObj) && 
                    emailBodyObj != null && 
                    string.IsNullOrWhiteSpace(emailBodyObj.ToString()))
                {
                    result.Warnings.Add(new ValidationWarning 
                    { 
                        Code = "SENDNOTIFICATION_EMPTY_BODY", 
                        Message = "SendNotification emailBody is empty, default template content will be used" 
                    });
                }
            }
            else if (string.IsNullOrEmpty(action.RecipientId) && string.IsNullOrEmpty(action.RecipientType))
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError 
                { 
                    Code = "SENDNOTIFICATION_RECIPIENT_REQUIRED", 
                    Message = "SendNotification action requires users[], teams[], recipientId, or recipientEmail in parameters" 
                });
            }
        }

        private static void ValidateAssignUserAction(ConditionAction action, ConditionValidationResult result)
        {
            if (action.Parameters != null)
            {
                // Check assigneeType
                if (action.Parameters.TryGetValue("assigneeType", out var assigneeType) && 
                    !string.IsNullOrEmpty(assigneeType?.ToString()))
                {
                    var assigneeTypeStr = assigneeType.ToString()?.ToLower();
                    if (assigneeTypeStr != StageConditionConstants.AssigneeTypeUser && 
                        assigneeTypeStr != StageConditionConstants.AssigneeTypeTeam)
                    {
                        result.IsValid = false;
                        result.Errors.Add(new ValidationError { Code = "ASSIGNUSER_TYPE_INVALID", Message = "AssignUser action assigneeType must be 'user' or 'team'" });
                    }
                }

                // Check assigneeIds array
                if (!HasNonEmptyArray(action.Parameters, "assigneeIds"))
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError { Code = "ASSIGNUSER_IDS_REQUIRED", Message = "AssignUser action requires assigneeIds (non-empty array) in parameters" });
                }
            }
            else
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError { Code = "ASSIGNUSER_PARAMS_REQUIRED", Message = "AssignUser action requires parameters with assigneeType and assigneeIds" });
            }
        }

        /// <summary>
        /// Check if a parameter contains a non-empty array
        /// </summary>
        public static bool HasNonEmptyArray(Dictionary<string, object> parameters, string key)
        {
            if (!parameters.TryGetValue(key, out var value) || value == null)
                return false;

            // Handle JsonElement
            if (value is System.Text.Json.JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    return jsonElement.GetArrayLength() > 0;
                }
                return false;
            }

            // Handle Newtonsoft JArray
            if (value is Newtonsoft.Json.Linq.JArray jArray)
            {
                return jArray.Count > 0;
            }

            // Handle IEnumerable
            if (value is System.Collections.IEnumerable enumerable && !(value is string))
            {
                return enumerable.Cast<object>().Any();
            }

            return false;
        }
    }
}
