using System;
using System.Collections.Generic;
using System.Linq;
using FlowFlex.Application.Contracts.Dtos.OW.StageCondition;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Application.Helpers
{
    /// <summary>
    /// Helper class for building condition evaluation log messages
    /// </summary>
    public static class ConditionLogHelper
    {
        /// <summary>
        /// Build operation title for condition evaluation log
        /// Format: "Condition Met: t1 | Rules: Rule_3 ✓ | Actions: GoToStage→Stage4 ✓"
        /// </summary>
        public static string BuildOperationTitle(
            string conditionName,
            bool isConditionMet,
            List<string> successfulRules,
            List<string> failedRules,
            List<ActionExecutionDetail> actionResults)
        {
            var statusText = isConditionMet ? "Met" : "Not Met";

            // Build rules summary
            var rulesSummary = "";
            if (isConditionMet && successfulRules.Any())
            {
                rulesSummary = string.Join(", ", successfulRules.Select(r => $"{r} ✓"));
            }
            else if (!isConditionMet && failedRules.Any())
            {
                rulesSummary = string.Join(", ", failedRules.Select(r => $"{r} ✗"));
            }

            // Build actions summary
            var actionsSummary = "";
            var actionsLabel = isConditionMet ? "Actions" : "Fallback";
            if (actionResults != null && actionResults.Any())
            {
                var actionParts = actionResults
                    .OrderBy(a => a.Order)
                    .Select(a => BuildActionTitlePart(a));
                actionsSummary = string.Join(", ", actionParts);
            }

            // Build title parts
            var titleParts = new List<string> { $"Condition {statusText}: {conditionName}" };
            if (!string.IsNullOrEmpty(rulesSummary))
            {
                titleParts.Add($"Rules: {rulesSummary}");
            }
            if (!string.IsNullOrEmpty(actionsSummary))
            {
                titleParts.Add($"{actionsLabel}: {actionsSummary}");
            }

            return string.Join(" | ", titleParts);
        }

        /// <summary>
        /// Build operation description for condition evaluation log
        /// </summary>
        public static string BuildOperationDescription(
            string conditionName,
            bool isConditionMet,
            List<string> successfulRules,
            List<string> failedRules,
            List<ActionExecutionDetail> successfulActions,
            List<ActionExecutionDetail> failedActions)
        {
            var statusText = isConditionMet ? "Met" : "Not Met";
            var descParts = new List<string>();
            descParts.Add($"Condition '{conditionName}' evaluated: {statusText}");

            if (successfulRules.Any())
            {
                descParts.Add($"Passed rules: {string.Join(", ", successfulRules)}");
            }
            if (failedRules.Any())
            {
                descParts.Add($"Failed rules: {string.Join(", ", failedRules)}");
            }
            if (successfulActions.Any())
            {
                var actionDetails = successfulActions.Select(a => BuildActionDescriptionPart(a));
                descParts.Add($"Executed actions: {string.Join("; ", actionDetails)}");
            }
            if (failedActions.Any())
            {
                var failedDetails = failedActions.Select(a => $"{a.ActionType}({a.ErrorMessage ?? "unknown error"})");
                descParts.Add($"Failed actions: {string.Join("; ", failedDetails)}");
            }

            return string.Join(". ", descParts);
        }

        /// <summary>
        /// Build extended data object for condition evaluation log
        /// </summary>
        public static object BuildExtendedData(
            StageCondition condition,
            ConditionEvaluationResult result)
        {
            return new
            {
                conditionId = condition.Id,
                conditionName = condition.Name,
                result = result.IsConditionMet,
                ruleEvaluations = result.RuleResults?.Select(r => new
                {
                    ruleName = r.RuleName,
                    isSuccess = r.IsSuccess,
                    expression = r.Expression,
                    errorMessage = r.ErrorMessage
                }),
                nextStageId = result.NextStageId,
                actionCount = result.ActionResults?.Count ?? 0,
                actionExecutions = result.ActionResults?.Select(a => new
                {
                    actionType = a.ActionType,
                    order = a.Order,
                    success = a.Success,
                    errorMessage = a.ErrorMessage,
                    resultSummary = BuildActionResultSummary(a)
                })
            };
        }

        /// <summary>
        /// Build action title part with result details
        /// Format: "GoToStage→Stage4 ✓" or "SendNotification→admin@test.com ✓"
        /// </summary>
        public static string BuildActionTitlePart(ActionExecutionDetail action)
        {
            var statusIcon = action.Success ? "✓" : "✗";
            var resultDetail = GetActionResultDetail(action);

            if (!string.IsNullOrEmpty(resultDetail))
            {
                return $"{action.ActionType}→{resultDetail} {statusIcon}";
            }
            return $"{action.ActionType} {statusIcon}";
        }

        /// <summary>
        /// Build action description part with full details
        /// </summary>
        public static string BuildActionDescriptionPart(ActionExecutionDetail action)
        {
            var resultDetail = GetActionResultDetail(action);
            if (!string.IsNullOrEmpty(resultDetail))
            {
                return $"{action.ActionType}({resultDetail})";
            }
            return action.ActionType;
        }

        /// <summary>
        /// Get action result detail based on action type
        /// </summary>
        public static string GetActionResultDetail(ActionExecutionDetail action)
        {
            if (action.ResultData == null || !action.ResultData.Any())
                return string.Empty;

            return action.ActionType.ToLower() switch
            {
                "gotostage" => GetResultDataString(action.ResultData, "targetStageName") ?? "",
                "skipstage" => GetSkipStageDetail(action.ResultData),
                "endworkflow" => GetEndWorkflowDetail(action.ResultData),
                "sendnotification" => GetSendNotificationDetail(action.ResultData),
                "updatefield" => GetUpdateFieldDetail(action.ResultData),
                "triggeraction" => GetResultDataString(action.ResultData, "actionName") ?? "",
                "assignuser" => GetAssignUserDetail(action.ResultData),
                _ => ""
            };
        }

        /// <summary>
        /// Build action result summary for extendedData
        /// </summary>
        public static string BuildActionResultSummary(ActionExecutionDetail action)
        {
            if (!action.Success)
            {
                return $"Failed: {action.ErrorMessage ?? "unknown error"}";
            }

            var detail = GetActionResultDetail(action);
            return string.IsNullOrEmpty(detail) ? "Success" : $"Success: {detail}";
        }

        #region Private Helper Methods

        private static string GetSkipStageDetail(Dictionary<string, object> resultData)
        {
            var targetStageName = GetResultDataString(resultData, "targetStageName") ?? "";
            var skippedCount = GetResultDataString(resultData, "skippedCount") ?? "";

            if (!string.IsNullOrEmpty(targetStageName))
            {
                if (!string.IsNullOrEmpty(skippedCount) && skippedCount != "1")
                {
                    return $"{targetStageName}(skip{skippedCount})";
                }
                return targetStageName;
            }

            return !string.IsNullOrEmpty(skippedCount) ? $"skip{skippedCount}" : "";
        }

        private static string GetEndWorkflowDetail(Dictionary<string, object> resultData)
        {
            var endStatus = GetResultDataString(resultData, "endStatus") ?? "";
            var previousStatus = GetResultDataString(resultData, "previousStatus") ?? "";

            if (string.IsNullOrEmpty(endStatus))
            {
                return "";
            }

            if (!string.IsNullOrEmpty(previousStatus) && previousStatus != endStatus)
            {
                return $"{endStatus}(was:{previousStatus})";
            }
            return endStatus;
        }

        private static string GetSendNotificationDetail(Dictionary<string, object> resultData)
        {
            var recipientName = GetResultDataString(resultData, "recipientName");
            var recipientEmail = GetResultDataString(resultData, "recipientEmail");

            if (string.IsNullOrEmpty(recipientEmail))
            {
                return "";
            }

            var truncatedEmail = TruncateString(recipientEmail, 20);

            if (!string.IsNullOrEmpty(recipientName) && recipientName != recipientEmail)
            {
                var truncatedName = TruncateString(recipientName, 10);
                return $"{truncatedName}<{truncatedEmail}>";
            }

            return truncatedEmail;
        }

        private static string GetUpdateFieldDetail(Dictionary<string, object> resultData)
        {
            var fieldDisplayName = GetResultDataString(resultData, "fieldName")
                ?? GetResultDataString(resultData, "fieldKey")
                ?? GetResultDataString(resultData, "fieldId")
                ?? "";

            if (string.IsNullOrEmpty(fieldDisplayName))
            {
                return "";
            }

            var fieldValue = GetResultDataString(resultData, "displayValue")
                ?? GetResultDataString(resultData, "newValue");
            if (!string.IsNullOrEmpty(fieldValue))
            {
                return $"{fieldDisplayName}={fieldValue}";
            }
            return fieldDisplayName;
        }

        private static string GetAssignUserDetail(Dictionary<string, object> resultData)
        {
            var assigneeType = GetResultDataString(resultData, "assigneeType");
            if (string.IsNullOrEmpty(assigneeType))
            {
                return "";
            }

            var assigneeNames = GetResultDataStringList(resultData, "assigneeNames");
            if (assigneeNames.Count > 0)
            {
                return $"{assigneeType}:{string.Join(",", assigneeNames)}";
            }

            var assigneeCount = GetResultDataString(resultData, "assigneeCount") ?? "0";
            return $"{assigneeType}×{assigneeCount}";
        }

        private static string? GetResultDataString(Dictionary<string, object> resultData, string key)
        {
            return resultData.TryGetValue(key, out var value) ? value?.ToString() : null;
        }

        private static List<string> GetResultDataStringList(Dictionary<string, object> resultData, string key)
        {
            if (!resultData.TryGetValue(key, out var value) || value == null)
            {
                return new List<string>();
            }

            if (value is IEnumerable<object> enumerable)
            {
                return enumerable.Select(x => x?.ToString() ?? "").Where(x => !string.IsNullOrEmpty(x)).ToList();
            }
            if (value is Newtonsoft.Json.Linq.JArray jArray)
            {
                return jArray.Select(x => x.ToString()).Where(x => !string.IsNullOrEmpty(x)).ToList();
            }

            return new List<string>();
        }

        private static string TruncateString(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Length <= maxLength) return value;
            return value.Substring(0, maxLength - 3) + "...";
        }

        #endregion
    }
}
