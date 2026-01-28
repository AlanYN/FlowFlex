using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.StageCondition;
using FlowFlex.Domain.Shared.Const;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FlowFlex.Application.Helpers
{
    /// <summary>
    /// Helper class for converting frontend rule format to RulesEngine format
    /// </summary>
    public class RuleConversionHelper
    {
        private readonly ILogger _logger;

        public RuleConversionHelper(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Convert frontend custom rule format to RulesEngine format if needed
        /// </summary>
        public async Task<string> ConvertToRulesEngineFormatIfNeededAsync(
            string rulesJson,
            Func<List<FrontendRule>, Task<Dictionary<string, string>>> buildComponentNameMapAsync)
        {
            try
            {
                var jsonObj = Newtonsoft.Json.Linq.JToken.Parse(rulesJson);

                // Check if it's frontend format (has "logic" property)
                if (jsonObj is Newtonsoft.Json.Linq.JObject jObject && jObject.ContainsKey("logic"))
                {
                    _logger.LogDebug("Converting frontend rule format to RulesEngine format");
                    return await ConvertFrontendRulesToRulesEngineFormatAsync(rulesJson, buildComponentNameMapAsync);
                }

                // Already in RulesEngine format
                return rulesJson;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to detect rules format, assuming RulesEngine format");
                return rulesJson;
            }
        }

        /// <summary>
        /// Convert frontend custom rule format to RulesEngine format
        /// Frontend format: {"logic":"AND","rules":[{"fieldPath":"...","operator":"==","value":"..."}]}
        /// RulesEngine format: [{"WorkflowName":"StageCondition","Rules":[{"RuleName":"Rule1","Expression":"..."}]}]
        /// </summary>
        public async Task<string> ConvertFrontendRulesToRulesEngineFormatAsync(
            string frontendRulesJson,
            Func<List<FrontendRule>, Task<Dictionary<string, string>>> buildComponentNameMapAsync)
        {
            try
            {
                var frontendRules = JsonConvert.DeserializeObject<FrontendRuleConfig>(frontendRulesJson);
                if (frontendRules == null || frontendRules.Rules == null || !frontendRules.Rules.Any())
                {
                    return "[]";
                }

                // Pre-fetch component names for all rules
                var componentNameMap = await buildComponentNameMapAsync(frontendRules.Rules);

                var expressions = new List<string>();
                var rulesEngineRules = new List<RulesEngine.Models.Rule>();
                int ruleIndex = 1;

                foreach (var rule in frontendRules.Rules)
                {
                    var expression = BuildExpressionFromFrontendRule(rule);
                    if (!string.IsNullOrEmpty(expression))
                    {
                        var ruleName = GetDescriptiveRuleName(rule, ruleIndex, componentNameMap);
                        rulesEngineRules.Add(new RulesEngine.Models.Rule
                        {
                            RuleName = ruleName,
                            Expression = expression,
                            SuccessEvent = "true"
                        });
                        expressions.Add(expression);
                        ruleIndex++;
                    }
                }

                var workflow = new RulesEngine.Models.Workflow
                {
                    WorkflowName = "StageCondition",
                    Rules = rulesEngineRules
                };

                return JsonConvert.SerializeObject(new[] { workflow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert frontend rules to RulesEngine format");
                throw;
            }
        }

        /// <summary>
        /// Build RulesEngine expression from frontend rule with security validation
        /// </summary>
        public string BuildExpressionFromFrontendRule(FrontendRule rule)
        {
            if (string.IsNullOrEmpty(rule.FieldPath))
            {
                return null;
            }

            // Validate field path for security
            var fieldPathValidation = ExpressionValidator.ValidateFieldPath(rule.FieldPath);
            if (!fieldPathValidation.IsValid)
            {
                _logger.LogWarning("Invalid field path rejected: {FieldPath}. Reason: {Reason}",
                    rule.FieldPath, fieldPathValidation.ErrorMessage);
                return null;
            }

            // Convert input.fields.xxx to input.fields["xxx"] for numeric field IDs
            var fieldPath = ConvertFieldPathToDictionaryAccess(rule.FieldPath);

            // Validate and sanitize the value
            var valueValidation = ExpressionValidator.ValidateValue(rule.Value);
            if (!valueValidation.IsValid)
            {
                _logger.LogWarning("Invalid value rejected for field {FieldPath}. Reason: {Reason}",
                    rule.FieldPath, valueValidation.ErrorMessage);
                return null;
            }
            var valueStr = valueValidation.SanitizedValue;

            // Map frontend operator to C# expression operator
            var operatorLower = rule.Operator?.ToLower();

            // Handle special checklist operators
            if (operatorLower == "completetask")
            {
                return $"{fieldPath} == true";
            }
            else if (operatorLower == "completestage")
            {
                if (!string.IsNullOrEmpty(fieldPath) && fieldPath.Contains(".isCompleted"))
                {
                    return $"{fieldPath} == true";
                }
                else
                {
                    return $"input.checklist.status == \"{StageConditionConstants.StatusCompleted}\"";
                }
            }

            // Build expression using safe operator mapping
            return BuildSafeExpression(fieldPath, operatorLower, valueStr);
        }

        /// <summary>
        /// Build a safe expression with validated inputs
        /// </summary>
        public string BuildSafeExpression(string fieldPath, string operatorLower, string valueStr)
        {
            var op = operatorLower switch
            {
                "==" or "equals" or "eq" => "==",
                "!=" or "notequals" or "ne" => "!=",
                ">" or "gt" => ">",
                "<" or "lt" => "<",
                ">=" or "gte" => ">=",
                "<=" or "lte" => "<=",
                "contains" => "Contains",
                "notcontains" => "NotContains",
                "startswith" => "StartsWith",
                "endswith" => "EndsWith",
                "isnull" => "== null",
                "isnotnull" => "!= null",
                "isempty" => "IsEmpty",
                "isnotempty" => "!IsEmpty",
                "inlist" => "InList",
                "notinlist" => "!InList",
                _ => "=="
            };

            // Build expression based on operator type with null safety
            if (op == "Contains")
            {
                return $"(np({fieldPath}) != null && np({fieldPath}).ToString().Contains({valueStr}))";
            }
            else if (op == "NotContains")
            {
                return $"(np({fieldPath}) == null || !np({fieldPath}).ToString().Contains({valueStr}))";
            }
            else if (op == "StartsWith")
            {
                return $"(np({fieldPath}) != null && np({fieldPath}).ToString().StartsWith({valueStr}))";
            }
            else if (op == "EndsWith")
            {
                return $"(np({fieldPath}) != null && np({fieldPath}).ToString().EndsWith({valueStr}))";
            }
            else if (op == "== null")
            {
                return $"np({fieldPath}) == null";
            }
            else if (op == "!= null")
            {
                return $"np({fieldPath}) != null";
            }
            else if (op == "IsEmpty")
            {
                return $"(np({fieldPath}) == null || string.IsNullOrWhiteSpace(np({fieldPath}).ToString()))";
            }
            else if (op == "!IsEmpty")
            {
                return $"(np({fieldPath}) != null && !string.IsNullOrWhiteSpace(np({fieldPath}).ToString()))";
            }
            else if (op == "InList" || op == "!InList")
            {
                var prefix = op.StartsWith("!") ? "!" : "";
                return $"(np({fieldPath}) != null && {prefix}{valueStr}.Contains(np({fieldPath}).ToString()))";
            }
            else if (op == ">" || op == "<" || op == ">=" || op == "<=")
            {
                return $"(np({fieldPath}) != null && double.Parse(np({fieldPath}).ToString()) {op} double.Parse({valueStr}))";
            }
            else if (op == "==")
            {
                return $"(np({fieldPath}) == null ? {valueStr} == \"\" : np({fieldPath}).ToString() == {valueStr})";
            }
            else if (op == "!=")
            {
                return $"(np({fieldPath}) == null ? {valueStr} != \"\" : np({fieldPath}).ToString() != {valueStr})";
            }
            else
            {
                return $"(np({fieldPath}) == null ? {valueStr} == \"\" : np({fieldPath}).ToString() == {valueStr})";
            }
        }

        /// <summary>
        /// Convert field path with numeric identifiers to dictionary access syntax
        /// e.g., input.fields.2006236814662307840 -> input.fields["2006236814662307840"]
        /// </summary>
        public string ConvertFieldPathToDictionaryAccess(string fieldPath)
        {
            if (string.IsNullOrEmpty(fieldPath))
            {
                return fieldPath;
            }

            var pattern = @"input\.fields\.(\d+)";
            var result = System.Text.RegularExpressions.Regex.Replace(
                fieldPath,
                pattern,
                "input.fields[\"$1\"]");

            return result;
        }

        /// <summary>
        /// Generate descriptive rule name based on field path, operator and actual component name
        /// </summary>
        public string GetDescriptiveRuleName(FrontendRule rule, int index, Dictionary<string, string> componentNameMap)
        {
            var componentType = DetectComponentType(rule);
            var op = GetOperatorDisplayName(rule.Operator);

            // Try to get actual component name
            string componentName = null;

            if (componentType == "checklist" && !string.IsNullOrEmpty(rule.FieldPath))
            {
                var taskId = ExtractTaskIdFromPath(rule.FieldPath);
                if (taskId > 0 && componentNameMap.TryGetValue($"task_{taskId}", out var taskName))
                {
                    componentName = taskName;
                }
            }
            else if (componentType == "questionnaire" && !string.IsNullOrEmpty(rule.FieldPath))
            {
                var (_, questionId) = ExtractQuestionnaireAndQuestionIdFromPath(rule.FieldPath);
                if (questionId > 0 && componentNameMap.TryGetValue($"question_{questionId}", out var questionTitle))
                {
                    componentName = questionTitle;
                }
            }
            else if ((componentType == "field" || componentType == "fields") && !string.IsNullOrEmpty(rule.FieldPath))
            {
                var fieldId = ExtractFieldIdFromFieldPath(rule.FieldPath);
                if (fieldId > 0 && componentNameMap.TryGetValue($"field_{fieldId}", out var fieldName))
                {
                    componentName = fieldName;
                }
            }

            // Build descriptive name with actual component name if available
            if (!string.IsNullOrEmpty(componentName))
            {
                if (componentName.Length > 50)
                {
                    componentName = componentName.Substring(0, 47) + "...";
                }

                var typePrefix = componentType switch
                {
                    "checklist" => "Task",
                    "questionnaire" => "Question",
                    "field" or "fields" => "Field",
                    "attachment" => "Attachment",
                    _ => "Rule"
                };

                return $"{typePrefix}: {componentName} - {op}";
            }

            // Fallback to index-based name
            var ruleNumber = index + 1;
            return componentType switch
            {
                "field" or "fields" => $"Field_{ruleNumber}_{op}",
                "questionnaire" => $"Question_{ruleNumber}_{op}",
                "checklist" => $"Task_{ruleNumber}_{op}",
                "attachment" => $"Attachment_{ruleNumber}_{op}",
                _ => $"Rule_{ruleNumber}_{op}"
            };
        }

        /// <summary>
        /// Detect component type from rule's componentType field or fieldPath pattern
        /// </summary>
        public string DetectComponentType(FrontendRule rule)
        {
            var componentType = rule.ComponentType?.ToLower()?.Trim() ?? "";

            if (!string.IsNullOrEmpty(componentType))
            {
                if (componentType == "checklist" || componentType == "task" || componentType == "tasks")
                    return "checklist";
                if (componentType == "questionnaire" || componentType == "question" || componentType == "questions")
                    return "questionnaire";
                if (componentType == "field" || componentType == "fields" || componentType == "dynamicfield")
                    return "field";
                if (componentType == "attachment" || componentType == "attachments")
                    return "attachment";
            }

            // Fallback: detect from fieldPath pattern
            var fieldPath = rule.FieldPath ?? "";

            if (fieldPath.Contains("input.checklist") || fieldPath.Contains(".tasks["))
                return "checklist";
            if (fieldPath.Contains("input.questionnaire") || fieldPath.Contains(".answers["))
                return "questionnaire";
            if (fieldPath.Contains("input.fields"))
                return "field";
            if (fieldPath.Contains("input.attachment") || fieldPath.Contains(".attachments"))
                return "attachment";

            return componentType;
        }

        /// <summary>
        /// Get human-readable operator display name
        /// </summary>
        public string GetOperatorDisplayName(string op)
        {
            if (string.IsNullOrEmpty(op)) return "check";

            return op.ToLower() switch
            {
                "==" or "eq" or "equals" => "equals",
                "!=" or "ne" or "notequals" => "notEquals",
                ">" or "gt" => "greaterThan",
                ">=" or "gte" => "greaterOrEqual",
                "<" or "lt" => "lessThan",
                "<=" or "lte" => "lessOrEqual",
                "contains" => "contains",
                "notcontains" => "notContains",
                "startswith" => "startsWith",
                "endswith" => "endsWith",
                "isempty" => "isEmpty",
                "isnotempty" => "isNotEmpty",
                "isnull" => "isNull",
                "isnotnull" => "isNotNull",
                "in" or "inlist" => "inList",
                "notin" or "notinlist" => "notInList",
                "completestage" => "Complete",
                "completetask" => "Complete",
                "iscompleted" or "completed" => "Complete",
                "isnotcompleted" or "notcompleted" => "NotComplete",
                "true" => "isTrue",
                "false" => "isFalse",
                _ => op.Length > 15 ? op.Substring(0, 12) + "..." : op
            };
        }

        /// <summary>
        /// Extract task ID from field path
        /// Path format: input.checklist.tasks["checklistId"]["taskId"].isCompleted
        /// </summary>
        public long ExtractTaskIdFromPath(string fieldPath)
        {
            try
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(fieldPath, @"\[""(\d+)""\]");
                if (matches.Count >= 2)
                {
                    if (long.TryParse(matches[1].Groups[1].Value, out var taskId))
                    {
                        return taskId;
                    }
                }
            }
            catch { }
            return 0;
        }

        /// <summary>
        /// Extract questionnaire ID and question ID from field path
        /// Path format: input.questionnaire.answers["questionnaireId"]["questionId"]
        /// </summary>
        public (long questionnaireId, long questionId) ExtractQuestionnaireAndQuestionIdFromPath(string fieldPath)
        {
            try
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(fieldPath, @"\[""(\d+)""\]");
                if (matches.Count >= 2)
                {
                    long.TryParse(matches[0].Groups[1].Value, out var questionnaireId);
                    long.TryParse(matches[1].Groups[1].Value, out var questionId);
                    return (questionnaireId, questionId);
                }
            }
            catch { }
            return (0, 0);
        }

        /// <summary>
        /// Extract field ID from field path
        /// Path format: input.fields["fieldId"] or input.fields.fieldId
        /// </summary>
        public long ExtractFieldIdFromFieldPath(string fieldPath)
        {
            try
            {
                var match = System.Text.RegularExpressions.Regex.Match(fieldPath, @"input\.fields\[""(\d+)""\]");
                if (match.Success && long.TryParse(match.Groups[1].Value, out var fieldId1))
                {
                    return fieldId1;
                }

                var dotMatch = System.Text.RegularExpressions.Regex.Match(fieldPath, @"input\.fields\.(\d+)");
                if (dotMatch.Success && long.TryParse(dotMatch.Groups[1].Value, out var fieldId2))
                {
                    return fieldId2;
                }
            }
            catch { }
            return 0;
        }

        /// <summary>
        /// Extract field/question/task ID from field path (for display)
        /// </summary>
        public string ExtractFieldIdFromPath(string fieldPath)
        {
            if (string.IsNullOrEmpty(fieldPath))
            {
                return "unknown";
            }

            var matches = System.Text.RegularExpressions.Regex.Matches(fieldPath, @"\[""(\d+)""\]");
            if (matches.Count > 0)
            {
                var lastMatch = matches[matches.Count - 1];
                var id = lastMatch.Groups[1].Value;
                return id.Length > 8 ? id.Substring(id.Length - 8) : id;
            }

            var dotMatch = System.Text.RegularExpressions.Regex.Match(fieldPath, @"\.(\d+)(?:\.|$)");
            if (dotMatch.Success)
            {
                var id = dotMatch.Groups[1].Value;
                return id.Length > 8 ? id.Substring(id.Length - 8) : id;
            }

            return "unknown";
        }
    }
}
