using System.Text.Json;
using Microsoft.Extensions.Logging;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Services.OW.ChangeLog
{
    /// <summary>
    /// Questionnaire answer parser for generating human-readable change descriptions
    /// Reference: customer-overview.vue answer formatting logic
    /// </summary>
    public class QuestionnaireAnswerParser : IScopedService
    {
        private readonly ILogger<QuestionnaireAnswerParser> _logger;

        public QuestionnaireAnswerParser(ILogger<QuestionnaireAnswerParser> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Parse questionnaire answer changes and generate human-readable descriptions
        /// </summary>
        public List<string> ParseQuestionnaireAnswerChanges(
            string beforeData,
            string afterData,
            object questionnaireConfig = null)
        {
            try
            {
                if (string.IsNullOrEmpty(afterData))
                    return new List<string>();

                var changesList = new List<string>();

                // Parse JSON data
                var after = ParseJsonData(afterData);
                var before = ParseJsonData(beforeData);

                // Safely check if after is null (handles JsonElement case)
                if (ReferenceEquals(after, null))
                {
                    _logger.LogError("Failed to parse afterData JSON");
                    return new List<string> { "Questionnaire updated (JSON parse error)" };
                }

                // Process answer submission (only afterData)
                // Safely check if before is null and after has responses
                bool beforeIsNull = ReferenceEquals(before, null);
                bool afterHasResponsesForSubmit = false;
                try
                {
                    afterHasResponsesForSubmit = after?.responses != null;
                }
                catch
                {
                    afterHasResponsesForSubmit = false;
                }

                if (beforeIsNull && afterHasResponsesForSubmit)
                {
                    foreach (var response in after.responses)
                    {
                        if (HasValidAnswer(response?.answer) || HasValidAnswer(response?.responseText))
                        {
                            var formattedAnswer = FormatAnswerWithConfig(response, questionnaireConfig);
                            var questionTitle = response?.question ?? response?.questionId?.ToString() ?? "Unknown Question";
                            changesList.Add($"{questionTitle}: {formattedAnswer}");
                        }
                    }
                    return changesList;
                }

                // Process answer updates (both beforeData and afterData)
                // Safely check if both before and after have responses
                bool beforeHasResponses = false;
                bool afterHasResponsesForUpdate = false;
                try
                {
                    beforeHasResponses = before?.responses != null;
                }
                catch
                {
                    beforeHasResponses = false;
                }
                try
                {
                    afterHasResponsesForUpdate = after?.responses != null;
                }
                catch
                {
                    afterHasResponsesForUpdate = false;
                }

                if (beforeHasResponses && afterHasResponsesForUpdate)
                {
                    // Debug: Processing answer updates

                    // Use questionId + question as key for grid type questions to handle multiple sub-questions
                    var beforeMap = new Dictionary<string, dynamic>();
                    foreach (var resp in before.responses)
                    {
                        if (resp?.questionId != null)
                        {
                            var questionId = resp.questionId.ToString();
                            var question = resp?.question?.ToString() ?? string.Empty;
                            // For grid type questions, use questionId + question as key to distinguish sub-questions
                            var key = string.IsNullOrEmpty(question) ? questionId : $"{questionId}_{question}";
                            beforeMap[key] = resp;
                        }
                    }

                    foreach (var afterResp in after.responses)
                    {
                        var questionId = afterResp?.questionId?.ToString();
                        var questionTitle = afterResp?.question ?? questionId ?? "Unknown Question";

                        if (string.IsNullOrEmpty(questionId))
                            continue;

                        // Use questionId + question as key to match with beforeMap
                        var question = afterResp?.question?.ToString() ?? string.Empty;
                        var key = string.IsNullOrEmpty(question) ? questionId : $"{questionId}_{question}";

                        if (!beforeMap.TryGetValue(key, out dynamic beforeResp))
                        {
                            // New answer - but skip file upload questions
                            if (IsFileUploadQuestion(afterResp))
                            {
                                // Skip file upload questions to reduce log noise
                                continue;
                            }

                            var formattedAnswer = FormatAnswerWithConfig(afterResp, questionnaireConfig);
                            changesList.Add($"{questionTitle}: {formattedAnswer}");
                            // Debug: Added new answer
                        }
                        else
                        {
                            // Check if answer or responseText has changed
                            // responseText contains custom values for "Other" options
                            var answerChanged = !AreAnswersEqual(beforeResp?.answer, afterResp?.answer);
                            var responseTextChanged = !AreResponseTextsEqual(beforeResp?.responseText?.ToString(), afterResp?.responseText?.ToString());

                            if (answerChanged || responseTextChanged)
                            {
                                // Modified answer - but skip file upload questions
                                if (IsFileUploadQuestion(afterResp) || IsFileUploadQuestion(beforeResp))
                                {
                                    // Skip file upload questions to reduce log noise
                                    continue;
                                }

                                var beforeAnswer = FormatAnswerWithConfig(beforeResp, questionnaireConfig);
                                var afterAnswer = FormatAnswerWithConfig(afterResp, questionnaireConfig);
                                changesList.Add($"{questionTitle}: {beforeAnswer} → {afterAnswer}");
                                // Debug: Added modified answer
                            }
                            else
                            {
                                // Debug: No change detected
                            }
                        }
                    }
                }
                else
                {
                    // Debug: Skipping answer comparison
                }

                return changesList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing questionnaire answer changes. BeforeData length: {BeforeLength}, AfterData length: {AfterLength}",
                    beforeData?.Length ?? 0, afterData?.Length ?? 0);
                return new List<string> { "Questionnaire updated (parsing error)" };
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Parse JSON data safely
        /// </summary>
        private dynamic ParseJsonData(string jsonData)
        {
            if (string.IsNullOrEmpty(jsonData))
                return null;

            try
            {
                // Parse as JsonElement first to validate structure
                using var document = JsonDocument.Parse(jsonData);
                var root = document.RootElement;

                // Check if it has responses property
                if (root.TryGetProperty("responses", out var responsesElement))
                {
                    // Convert to a simple object structure for easier access
                    var result = new
                    {
                        responses = responsesElement.EnumerateArray().Select(r => new
                        {
                            questionId = GetJsonElementString(r, "questionId"),
                            question = GetJsonElementString(r, "question"),
                            answer = GetJsonElementValue(r, "answer"),
                            type = GetJsonElementString(r, "type"),
                            responseText = GetJsonElementString(r, "responseText")
                        }).ToList()
                    };
                    return result;
                }

                return JsonSerializer.Deserialize<object>(jsonData);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse JSON data: {JsonData}", jsonData?.Substring(0, Math.Min(200, jsonData.Length)));
                return null;
            }
        }

        /// <summary>
        /// Get string value from JsonElement safely
        /// </summary>
        private string GetJsonElementString(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                return prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.ToString();
            }
            return null;
        }

        /// <summary>
        /// Get value from JsonElement safely (handles arrays and strings)
        /// </summary>
        private object GetJsonElementValue(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                return prop.ValueKind switch
                {
                    JsonValueKind.String => prop.GetString(),
                    JsonValueKind.Array => prop.EnumerateArray().Select(e => e.ToString()).ToArray(),
                    JsonValueKind.Number => prop.ToString(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => prop.ToString()
                };
            }
            return null;
        }

        /// <summary>
        /// Format answer with question configuration (reference: customer-overview.vue)
        /// </summary>
        private string FormatAnswerWithConfig(dynamic response, object questionnaireConfig)
        {
            if (response == null)
                return "No answer";

            var type = response.type?.ToString();
            var questionId = response.questionId?.ToString();

            // Find question configuration
            dynamic questionConfig = null;
            if (questionnaireConfig != null)
            {
                questionConfig = FindQuestionConfig(questionnaireConfig, questionId);
            }

            // For short_answer_grid, answer is stored in responseText, not answer field
            if (type == "short_answer_grid")
            {
                return GetShortAnswerGridSummary(response, questionConfig);
            }

            var answer = response.answer ?? response.responseText;
            if (!HasValidAnswer(answer))
                return "No answer";

            switch (type)
            {
                case "multiple_choice":
                    return GetMultipleChoiceLabel(answer, questionConfig, response.responseText?.ToString(), questionId);

                case "dropdown":
                    return GetDropdownLabel(answer, questionConfig);

                case "checkboxes":
                    return GetCheckboxLabels(answer, questionConfig, response.responseText?.ToString(), questionId);

                case "multiple_choice_grid":
                case "checkbox_grid":
                    return GetGridAnswerLabels(answer, questionConfig, response.responseText?.ToString(), questionId);

                case "date":
                    return FormatAnswerDate(answer, "date");

                case "time":
                    return FormatAnswerDate(answer, "time");

                case "file":
                case "file_upload":
                    return FormatFileAnswer(answer);

                case "rating":
                    return FormatRatingAnswer(answer, questionConfig);

                case "linear_scale":
                    return FormatLinearScaleAnswer(answer, questionConfig);

                default:
                    return answer?.ToString() ?? "No answer";
            }
        }

        /// <summary>
        /// Find question configuration by questionId
        /// </summary>
        private dynamic FindQuestionConfig(object questionnaireConfig, string questionId)
        {
            try
            {
                var config = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(questionnaireConfig));

                if (config.TryGetProperty("sections", out JsonElement sections) && sections.ValueKind == JsonValueKind.Array)
                {
                    foreach (var section in sections.EnumerateArray())
                    {
                        if (section.TryGetProperty("questions", out JsonElement questions) && questions.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var question in questions.EnumerateArray())
                            {
                                // Direct match
                                if (question.TryGetProperty("id", out JsonElement id) && id.GetString() == questionId)
                                {
                                    return JsonSerializer.Deserialize<dynamic>(question.GetRawText());
                                }
                                if (question.TryGetProperty("questionId", out JsonElement qId) && qId.GetString() == questionId)
                                {
                                    return JsonSerializer.Deserialize<dynamic>(question.GetRawText());
                                }

                                // For grid types, try to match base question ID (before underscore)
                                var baseQuestionId = questionId.Contains('_') ? questionId.Split('_')[0] : questionId;
                                if (baseQuestionId != questionId)
                                {
                                    if (question.TryGetProperty("id", out JsonElement baseId) && baseId.GetString() == baseQuestionId)
                                    {
                                        return JsonSerializer.Deserialize<dynamic>(question.GetRawText());
                                    }
                                    if (question.TryGetProperty("questionId", out JsonElement baseQId) && baseQId.GetString() == baseQuestionId)
                                    {
                                        return JsonSerializer.Deserialize<dynamic>(question.GetRawText());
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to find question config for questionId: {QuestionId}", questionId);
            }
            return null;
        }

        /// <summary>
        /// Get multiple choice label (aligned with frontend getMultipleChoiceLabel logic)
        /// </summary>
        private string GetMultipleChoiceLabel(dynamic answer, dynamic questionConfig, string responseText = null, string questionId = null)
        {
            if (answer == null)
                return "No answer";

            // Use a more robust null check that avoids dynamic comparison
            bool configIsNull = false;
            try
            {
                configIsNull = ReferenceEquals(questionConfig, null) || IsNullOrEmpty(questionConfig);
            }
            catch
            {
                configIsNull = true;
            }

            var answerStr = answer.ToString();
            var answerLower = answerStr?.ToLower() ?? "";

            // Find the option with matching value
            dynamic matchedOption = null;
            if (!configIsNull)
            {
                try
                {
                    var options = GetOptionsFromConfig(questionConfig);
                    foreach (var option in options)
                    {
                        string value = null;
                        try
                        {
                            var optionObj = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(option));
                            if (optionObj.TryGetProperty("value", out JsonElement valueElement))
                            {
                                value = valueElement.GetString();
                            }
                        }
                        catch
                        {
                            try
                            {
                                value = option.value?.ToString();
                            }
                            catch
                            {
                                continue;
                            }
                        }

                        if (value == answerStr)
                        {
                            matchedOption = option;
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    // Failed to find matching option, will use default behavior
                }
            }

            // If answer is "other" or option is Other type, try to extract custom value from responseText
            bool isOtherAnswer = answerLower == "other" || answerLower.Contains("other");
            bool isOtherOption = false;
            string optionLabel = null;
            bool hasMatchedOption = false;

            // Safe null check for dynamic type
            try
            {
                hasMatchedOption = !ReferenceEquals(matchedOption, null);
            }
            catch
            {
                hasMatchedOption = false;
            }

            if (hasMatchedOption)
            {
                try
                {
                    var optionObj = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(matchedOption));
                    optionLabel = optionObj.TryGetProperty("label", out JsonElement labelElement)
                        ? labelElement.GetString()
                        : null;

                    if (string.IsNullOrEmpty(optionLabel))
                    {
                        try
                        {
                            optionLabel = matchedOption.label?.ToString();
                        }
                        catch (Exception)
                        {
                            // Failed to get option label from dynamic object
                        }
                    }

                    isOtherOption = IsOtherOption(matchedOption);
                }
                catch
                {
                    try
                    {
                        optionLabel = matchedOption.label?.ToString();
                        isOtherOption = IsOtherOption(matchedOption);
                    }
                    catch (Exception)
                    {
                        // Failed to extract option label from matched option
                    }
                }
            }

            if ((isOtherAnswer || isOtherOption) && !string.IsNullOrEmpty(responseText) && !string.IsNullOrEmpty(questionId))
            {
                var otherValues = ExtractOtherValues(responseText, questionId);
                // Try multiple ways to find custom value
                string customValue = null;

                if (otherValues.TryGetValue(answerStr, out customValue) ||
                    otherValues.TryGetValue("other", out customValue) ||
                    (hasMatchedOption && otherValues.TryGetValue(answerStr, out customValue)))
                {
                    // Found
                }
                // Find key containing questionId and option value
                else if (hasMatchedOption)
                {
                    var optionValue = answerStr;
                    var matchingKey = otherValues.Keys.FirstOrDefault(k =>
                        k.Contains(questionId) && k.Contains(optionValue));
                    if (matchingKey != null)
                    {
                        otherValues.TryGetValue(matchingKey, out customValue);
                    }
                }
                // Find any key containing "other"
                if (customValue == null)
                {
                    var otherEntry = otherValues.FirstOrDefault(kvp =>
                        kvp.Key.ToLower().Contains("other"));
                    customValue = otherEntry.Value;
                }
                // Finally use first value
                if (customValue == null && otherValues.Count > 0)
                {
                    customValue = otherValues.Values.First();
                }

                if (!string.IsNullOrEmpty(customValue))
                {
                    return $"Other: {customValue}";
                }
            }

            // Return option label if found, otherwise return answer
            if (!string.IsNullOrEmpty(optionLabel))
            {
                return optionLabel;
            }

            return answerStr ?? "No answer";
        }

        /// <summary>
        /// Get dropdown label
        /// </summary>
        private string GetDropdownLabel(dynamic answer, dynamic questionConfig)
        {
            return GetMultipleChoiceLabel(answer, questionConfig);
        }

        /// <summary>
        /// Get checkbox labels
        /// </summary>
        private string GetCheckboxLabels(dynamic answer, dynamic questionConfig, string responseText, string questionId)
        {
            if (answer == null)
                return "No answer";

            var answerValues = GetCheckboxAnswers(answer);
            if (answerValues.Count == 0)
                return "No answer";

            // Use a more robust null check that avoids dynamic comparison
            bool configIsNull = false;
            try
            {
                configIsNull = ReferenceEquals(questionConfig, null) || IsNullOrEmpty(questionConfig);
            }
            catch
            {
                configIsNull = true;
            }

            if (configIsNull)
                return string.Join(", ", answerValues);

            try
            {
                var options = GetOptionsFromConfig(questionConfig);
                var labels = new List<string>();
                var optionMap = new Dictionary<string, string>();
                var otherOptionIds = new HashSet<string>();

                foreach (var option in options)
                {
                    string value = null;
                    string label = null;

                    try
                    {
                        // Safely extract value and label from dynamic object
                        var optionObj = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(option));

                        if (optionObj.TryGetProperty("value", out JsonElement valueElement))
                        {
                            value = valueElement.GetString();
                        }

                        if (optionObj.TryGetProperty("label", out JsonElement labelElement))
                        {
                            label = labelElement.GetString();
                        }
                    }
                    catch
                    {
                        // Fallback to dynamic access if JSON approach fails
                        try
                        {
                            value = option.value?.ToString();
                            label = option.label?.ToString();
                        }
                        catch
                        {
                            continue; // Skip this option if we can't extract value/label
                        }
                    }

                    if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(label))
                    {
                        optionMap[value] = label;

                        // Check if it's an "other" option
                        if (IsOtherOption(option))
                        {
                            otherOptionIds.Add(value);
                        }
                    }
                }

                // Extract other values from responseText
                var otherValues = ExtractOtherValues(responseText, questionId);

                foreach (var value in answerValues)
                {
                    if (optionMap.TryGetValue(value, out string label))
                    {
                        if (otherOptionIds.Contains(value))
                        {
                            // Try multiple ways to find custom value
                            string customValue = null;

                            // Direct match with value (option ID or "other")
                            if (otherValues.TryGetValue(value, out customValue) ||
                                otherValues.TryGetValue("other", out customValue))
                            {
                                // Found
                            }
                            // Find key containing questionId and option value
                            else if (!string.IsNullOrEmpty(questionId))
                            {
                                var matchingKey = otherValues.Keys.FirstOrDefault(k =>
                                    k.Contains(questionId) && k.Contains(value));
                                if (matchingKey != null)
                                {
                                    otherValues.TryGetValue(matchingKey, out customValue);
                                }
                            }
                            // Find any key containing "other"
                            if (customValue == null)
                            {
                                var otherEntry = otherValues.FirstOrDefault(kvp =>
                                    kvp.Key.ToLower().Contains("other"));
                                customValue = otherEntry.Value;
                            }
                            // Finally use first value
                            if (customValue == null && otherValues.Count > 0)
                            {
                                customValue = otherValues.Values.First();
                            }

                            if (!string.IsNullOrEmpty(customValue))
                            {
                                labels.Add($"Other: {customValue}");
                            }
                            else
                            {
                                labels.Add($"Other: {label}");
                            }
                        }
                        else
                        {
                            labels.Add(label);
                        }
                    }
                    else
                    {
                        // Value not found in optionMap, might be "other" directly
                        var valueLower = value?.ToLower() ?? "";
                        if (valueLower == "other" || valueLower.Contains("other"))
                        {
                            // Try multiple ways to find custom value
                            string customValue = null;

                            // Direct match with value or "other"
                            if (otherValues.TryGetValue(value, out customValue) ||
                                otherValues.TryGetValue("other", out customValue))
                            {
                                // Found
                            }
                            // Find key containing questionId (for format questionId_optionId)
                            else if (!string.IsNullOrEmpty(questionId))
                            {
                                // Try to find key that starts with questionId or baseQuestionId
                                var baseQuestionId = questionId.Contains("_row-")
                                    ? questionId.Split(new[] { "_row-" }, StringSplitOptions.None)[0].Split('_')[0]
                                    : questionId.Split('_')[0];

                                var matchingKey = otherValues.Keys.FirstOrDefault(k =>
                                    k.StartsWith(questionId + "_") ||
                                    k.StartsWith(baseQuestionId + "_") ||
                                    k.Contains(questionId) ||
                                    k.Contains(baseQuestionId));
                                if (matchingKey != null)
                                {
                                    otherValues.TryGetValue(matchingKey, out customValue);
                                }
                            }
                            // Find any key containing "other"
                            if (customValue == null)
                            {
                                var otherEntry = otherValues.FirstOrDefault(kvp =>
                                    kvp.Key.ToLower().Contains("other"));
                                customValue = otherEntry.Value;
                            }
                            // Finally use first value (as last resort)
                            if (customValue == null && otherValues.Count > 0)
                            {
                                customValue = otherValues.Values.First();
                            }

                            labels.Add(!string.IsNullOrEmpty(customValue)
                                ? $"Other: {customValue}"
                                : "Other");
                        }
                        else
                        {
                            labels.Add(value);
                        }
                    }
                }

                return string.Join(", ", labels);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to format checkbox labels");
                return string.Join(", ", answerValues);
            }
        }

        /// <summary>
        /// Get grid answer labels
        /// </summary>
        private string GetGridAnswerLabels(dynamic answer, dynamic questionConfig, string responseText, string questionId)
        {
            if (answer == null)
                return "No answer";

            var answerValues = GetCheckboxAnswers(answer);
            if (answerValues.Count == 0)
                return "No answer";

            // Use a more robust null check that avoids dynamic comparison
            bool configIsNull = false;
            try
            {
                configIsNull = ReferenceEquals(questionConfig, null) || IsNullOrEmpty(questionConfig);
            }
            catch
            {
                configIsNull = true;
            }

            if (configIsNull)
            {
                // Log debug info when no config is found
                // _logger.LogDebug("No question config found for questionId: {QuestionId}, returning raw values: {Values}", questionId, string.Join(", ", answerValues));
                return string.Join(", ", answerValues);
            }

            try
            {
                var columns = GetColumnsFromConfig(questionConfig);
                var columnMap = new Dictionary<string, string>();
                var otherColumnIds = new HashSet<string>();

                // Build column mapping
                foreach (var column in columns)
                {
                    string id = null;
                    string label = null;

                    try
                    {
                        // Safely extract id and label from dynamic object
                        var columnObj = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(column));

                        if (columnObj.TryGetProperty("id", out JsonElement idElement))
                        {
                            id = idElement.GetString();
                        }

                        if (columnObj.TryGetProperty("label", out JsonElement labelElement))
                        {
                            label = labelElement.GetString();
                        }
                    }
                    catch
                    {
                        // Fallback to dynamic access if JSON approach fails
                        try
                        {
                            id = column.id?.ToString();
                            label = column.label?.ToString();
                        }
                        catch
                        {
                            continue; // Skip this column if we can't extract id/label
                        }
                    }

                    if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(label))
                    {
                        columnMap[id] = label;

                        if (IsOtherOption(column))
                        {
                            otherColumnIds.Add(id);
                        }
                    }
                }

                // Log debug info about column mapping
                // _logger.LogDebug("Found {ColumnCount} columns for questionId: {QuestionId}, mapping: {Mapping}", 
                //     columnMap.Count, questionId, string.Join(", ", columnMap.Select(kvp => $"{kvp.Key}={kvp.Value}")));

                var otherValues = ExtractOtherValues(responseText, questionId);
                var labels = new List<string>();

                foreach (var id in answerValues)
                {
                    var idLower = id?.ToLower() ?? "";
                    if (columnMap.TryGetValue(id, out string label))
                    {
                        // If it's an Other type column, prioritize showing user input value
                        if (otherColumnIds.Contains(id) || idLower.Contains("other") ||
                            (label != null && label.ToLower().Contains("other")))
                        {
                            // Try multiple matching ways to find custom value
                            string customValue = null;

                            // Direct match column ID in various formats
                            if (otherValues.TryGetValue(id, out customValue) ||
                                otherValues.TryGetValue($"column-{id}", out customValue) ||
                                otherValues.TryGetValue($"column-other-{id}", out customValue) ||
                                otherValues.TryGetValue(id.Replace("column-", "column-other-"), out customValue) ||
                                otherValues.TryGetValue(id.Replace("column-other-", ""), out customValue))
                            {
                                // Found custom value
                            }
                            // Try matching full key format: questionId_rowId_columnId
                            else if (!string.IsNullOrEmpty(questionId))
                            {
                                var matchingKey = otherValues.Keys.FirstOrDefault(k =>
                                    k.EndsWith($"_{id}") || k == $"{questionId}_{id}");
                                if (matchingKey != null)
                                {
                                    otherValues.TryGetValue(matchingKey, out customValue);
                                }
                            }
                            // Format conversion matching
                            if (customValue == null)
                            {
                                if (idLower.StartsWith("column-other-"))
                                {
                                    var extractedId = idLower.Replace("column-other-", "");
                                    otherValues.TryGetValue(extractedId, out customValue);
                                }
                            }
                            // Find key containing the column id
                            if (customValue == null)
                            {
                                var matchingEntry = otherValues.FirstOrDefault(kvp =>
                                {
                                    var keyLower = kvp.Key.ToLower();
                                    return keyLower.Contains(idLower) ||
                                           keyLower.Contains($"column-other-{idLower}") ||
                                           keyLower.Contains($"_{idLower}_") ||
                                           keyLower.EndsWith($"_{idLower}") ||
                                           keyLower.EndsWith(idLower);
                                });
                                customValue = matchingEntry.Value;
                            }
                            // If still not found, try finding any value containing "other" as fallback
                            if (customValue == null)
                            {
                                var otherEntry = otherValues.FirstOrDefault(kvp =>
                                    kvp.Key.ToLower().Contains("other"));
                                customValue = otherEntry.Value;
                            }
                            // Finally use first value as last resort
                            if (customValue == null && otherValues.Count > 0)
                            {
                                customValue = otherValues.Values.First();
                            }

                            if (!string.IsNullOrEmpty(customValue))
                            {
                                labels.Add($"Other: {customValue}");
                            }
                            else
                            {
                                labels.Add(label);
                            }
                        }
                        else
                        {
                            labels.Add(label);
                        }
                    }
                    else
                    {
                        // Not found in columnMap, might be directly "Other"
                        if (idLower == "other" || idLower.Contains("other"))
                        {
                            // Try multiple ways to find custom value
                            string customValue = null;

                            if (otherValues.TryGetValue(id, out customValue) ||
                                otherValues.TryGetValue($"column-other-{id}", out customValue) ||
                                otherValues.TryGetValue($"column-{id}", out customValue))
                            {
                                // Found
                            }
                            else
                            {
                                // Find key containing "other"
                                var otherEntry = otherValues.FirstOrDefault(kvp =>
                                    kvp.Key.ToLower().Contains("other") &&
                                    (kvp.Key.ToLower().Contains(idLower) ||
                                     kvp.Key.ToLower().EndsWith("other") ||
                                     kvp.Key.ToLower().Contains("column-other-")));
                                customValue = otherEntry.Value;
                            }

                            // If still not found, use first value or "Other"
                            if (string.IsNullOrEmpty(customValue) && otherValues.Count > 0)
                            {
                                customValue = otherValues.Values.First();
                            }

                            labels.Add(!string.IsNullOrEmpty(customValue)
                                ? $"Other: {customValue}"
                                : "Other");
                        }
                        else
                        {
                            // Fallback: case-insensitive match otherValues keys
                            var hasRelatedOther = otherValues.Keys.Any(k =>
                                k.ToLower().Contains(idLower) ||
                                k.ToLower().Contains($"column-other-{idLower}") ||
                                k.ToLower().Contains($"_{idLower}_") ||
                                k.ToLower().EndsWith($"_{idLower}"));

                            if (hasRelatedOther)
                            {
                                var matchingEntry = otherValues.FirstOrDefault(kvp =>
                                    kvp.Key.ToLower().Contains(idLower) ||
                                    kvp.Key.ToLower().Contains($"column-other-{idLower}") ||
                                    kvp.Key.ToLower().Contains($"_{idLower}_") ||
                                    kvp.Key.ToLower().EndsWith($"_{idLower}"));
                                var customValue = matchingEntry.Value;
                                labels.Add(!string.IsNullOrEmpty(customValue)
                                    ? $"Other: {customValue}"
                                    : id);
                            }
                            else
                            {
                                labels.Add(id);
                            }
                        }
                    }
                }

                var result = string.Join(", ", labels);
                // _logger.LogDebug("Grid answer labels result for questionId {QuestionId}: {Result}", questionId, result);
                return result;
            }
            catch (Exception ex)
            {
                // _logger.LogWarning(ex, "Failed to format grid labels for questionId: {QuestionId}", questionId);
                return string.Join(", ", answerValues);
            }
        }

        /// <summary>
        /// Get short answer grid summary
        /// </summary>
        private string GetShortAnswerGridSummary(dynamic response, dynamic questionConfig)
        {
            try
            {
                var responseText = response?.responseText?.ToString();
                if (string.IsNullOrEmpty(responseText) || responseText.Trim() == "{}")
                    return "No answer";

                var parsed = ParseResponseText(responseText);
                if (parsed == null || parsed.Count == 0)
                    return "No answer";

                var rowIdToLabel = new Dictionary<string, string>();
                if (questionConfig != null)
                {
                    try
                    {
                        // Use JsonElement directly for safer access
                        var configObj = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(questionConfig));
                        if (configObj.TryGetProperty("rows", out JsonElement rowsElement) &&
                            rowsElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var rowElement in rowsElement.EnumerateArray())
                            {
                                try
                                {
                                    string id = null;
                                    string label = null;

                                    // Extract id property
                                    if (rowElement.TryGetProperty("id", out JsonElement idElement))
                                    {
                                        id = idElement.ValueKind == JsonValueKind.String
                                            ? idElement.GetString()
                                            : idElement.ToString();
                                    }

                                    // Extract label property
                                    if (rowElement.TryGetProperty("label", out JsonElement labelElement))
                                    {
                                        label = labelElement.ValueKind == JsonValueKind.String
                                            ? labelElement.GetString()
                                            : labelElement.ToString();
                                    }

                                    if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(label))
                                    {
                                        rowIdToLabel[id] = label;
                                    }
                                }
                                catch
                                {
                                    // Skip this row if we can't extract its properties
                                    continue;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Exception exception = ex;
                        _logger.LogWarning(exception, "Failed to extract rows from question config");
                    }
                }

                // Group values by row ID
                var rowValues = new Dictionary<string, List<string>>();

                foreach (KeyValuePair<string, string> kvp in parsed)
                {
                    try
                    {
                        var parts = kvp.Key.Split('_');
                        string rowId = null;

                        // Handle format: questionId_rowId_columnId (3 parts)
                        if (parts.Length >= 3)
                        {
                            rowId = parts[1]; // Second part is rowId
                        }
                        // Handle format with "row-" prefix
                        else if (parts.Length > 0)
                        {
                            var rowPart = parts.FirstOrDefault((Func<string, bool>)(p => p.StartsWith("row-")));
                            if (!string.IsNullOrEmpty(rowPart))
                            {
                                rowId = rowPart;
                            }
                            else if (parts.Length == 2)
                            {
                                // Fallback: use second part as rowId
                                rowId = parts[1];
                            }
                        }

                        if (!string.IsNullOrEmpty(rowId))
                        {
                            var value = kvp.Value?.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(value))
                            {
                                if (!rowValues.ContainsKey(rowId))
                                {
                                    rowValues[rowId] = new List<string>();
                                }
                                rowValues[rowId].Add(value);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        string keyStr = kvp.Key?.ToString() ?? "unknown";
                        _logger.LogWarning(ex, "Failed to process key-value pair in short answer grid: {Key}", keyStr);
                        // Continue processing other entries
                    }
                }

                if (rowValues.Count == 0)
                    return "No answer";

                // Build results with row labels
                var results = new List<string>();
                foreach (var kvp in rowValues)
                {
                    var rowId = kvp.Key;
                    var values = kvp.Value;

                    // Get row label
                    string rowLabel = null;
                    if (rowIdToLabel.TryGetValue(rowId, out string label))
                    {
                        rowLabel = label;
                    }
                    else if (rowId.StartsWith("row-"))
                    {
                        rowLabel = rowId.Replace("row-", "");
                    }
                    else
                    {
                        // Use a more user-friendly label
                        // Try to extract from question title if available
                        var questionTitle = response?.question?.ToString();
                        if (!string.IsNullOrEmpty(questionTitle) && questionTitle.Contains(" - "))
                        {
                            var titleParts = questionTitle.Split(new[] { " - " }, StringSplitOptions.None);
                            if (titleParts.Length > 1)
                            {
                                rowLabel = titleParts[1]; // Use the part after " - "
                            }
                            else
                            {
                                rowLabel = $"Row {rowId}";
                            }
                        }
                        else
                        {
                            rowLabel = $"Row {rowId}";
                        }
                    }

                    // Join multiple values for the same row
                    var valuesStr = string.Join(", ", values);
                    results.Add($"{rowLabel}: {valuesStr}");
                }

                return results.Count > 0 ? string.Join("; ", results) : "No answer";
            }
            catch (Exception ex)
            {
                var responseTextPreview = string.Empty;
                try
                {
                    var responseTextStr = response?.responseText?.ToString();
                    if (!string.IsNullOrEmpty(responseTextStr))
                    {
                        var length = Math.Min(200, responseTextStr.Length);
                        responseTextPreview = responseTextStr.Substring(0, length);
                    }
                }
                catch
                {
                    // Ignore preview errors
                }
                Exception exception = ex; // Explicit type to avoid dynamic dispatch issue
                _logger.LogWarning(exception, "Failed to format short answer grid. ResponseText: {ResponseText}", responseTextPreview);

                // Try to return at least some information
                try
                {
                    var responseText = response?.responseText?.ToString();
                    if (!string.IsNullOrEmpty(responseText) && responseText.Trim() != "{}")
                    {
                        var parsed = ParseResponseText(responseText);
                        if (parsed != null && parsed.Count > 0)
                        {
                            var values = new List<string>();
                            foreach (var kvp in parsed)
                            {
                                var valueStr = kvp.Value?.ToString();
                                if (!string.IsNullOrWhiteSpace(valueStr))
                                {
                                    values.Add(valueStr);
                                }
                            }
                            if (values.Count > 0)
                            {
                                return string.Join(", ", values.Take(5));
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore fallback errors
                }
                return "Grid answer provided";
            }
        }

        /// <summary>
        /// Format file answer
        /// </summary>
        private string FormatFileAnswer(dynamic answer)
        {
            try
            {
                if (answer == null)
                    return "No file";

                // Handle different answer types
                if (answer is JsonElement jsonElement)
                {
                    return FormatFileAnswerFromJsonElement(jsonElement);
                }

                var answerStr = answer.ToString();

                // Handle common array type string representation
                if (answerStr == "System.String[]" || answerStr == "System.Object[]")
                {
                    try
                    {
                        // Try to convert to array and extract file information
                        if (answer is IEnumerable<object> enumerable)
                        {
                            var fileNames = new List<string>();
                            foreach (var item in enumerable)
                            {
                                var fileName = ExtractFileNameFromObject(item);
                                if (!string.IsNullOrEmpty(fileName))
                                {
                                    fileNames.Add(fileName);
                                }
                            }
                            return fileNames.Count > 0 ? $"Files: {string.Join(", ", fileNames)}" : "Files uploaded";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to extract file names from enumerable");
                    }
                    return "Files uploaded";
                }

                if (answerStr == "[object Object]")
                    return "File uploaded (name not available)";

                // Try to parse as JSON array/object
                try
                {
                    var files = JsonSerializer.Deserialize<JsonElement>(answerStr);
                    return FormatFileAnswerFromJsonElement(files);
                }
                catch (JsonException)
                {
                    // Not valid JSON, fall through to return raw string
                }

                return $"File: {answerStr}";
            }
            catch
            {
                return "File uploaded";
            }
        }

        /// <summary>
        /// Format file answer from JsonElement
        /// </summary>
        private string FormatFileAnswerFromJsonElement(JsonElement files)
        {
            if (files.ValueKind == JsonValueKind.Array)
            {
                var fileNames = new List<string>();
                foreach (var file in files.EnumerateArray())
                {
                    if (file.TryGetProperty("name", out JsonElement name) && name.ValueKind == JsonValueKind.String)
                    {
                        fileNames.Add(name.GetString());
                    }
                    else if (file.TryGetProperty("fileName", out JsonElement fileName) && fileName.ValueKind == JsonValueKind.String)
                    {
                        fileNames.Add(fileName.GetString());
                    }
                    else if (file.ValueKind == JsonValueKind.String)
                    {
                        fileNames.Add(file.GetString());
                    }
                }
                return fileNames.Count > 0 ? $"Files: {string.Join(", ", fileNames)}" : "Files uploaded";
            }
            else if (files.TryGetProperty("name", out JsonElement singleName) && singleName.ValueKind == JsonValueKind.String)
            {
                return $"File: {singleName.GetString()}";
            }
            else if (files.ValueKind == JsonValueKind.String)
            {
                return $"File: {files.GetString()}";
            }

            return "File uploaded";
        }

        /// <summary>
        /// Extract file name from dynamic object
        /// </summary>
        private string ExtractFileNameFromObject(object item)
        {
            if (item == null) return null;

            try
            {
                // Try to get name property via reflection
                var type = item.GetType();
                var nameProperty = type.GetProperty("name") ?? type.GetProperty("Name");
                if (nameProperty != null)
                {
                    var nameValue = nameProperty.GetValue(item);
                    return nameValue?.ToString();
                }

                // Try to get fileName property
                var fileNameProperty = type.GetProperty("fileName") ?? type.GetProperty("FileName");
                if (fileNameProperty != null)
                {
                    var fileNameValue = fileNameProperty.GetValue(item);
                    return fileNameValue?.ToString();
                }

                // Try to parse as JSON string
                var itemStr = item.ToString();
                if (!string.IsNullOrEmpty(itemStr) && itemStr.StartsWith("{"))
                {
                    try
                    {
                        var jsonElement = JsonSerializer.Deserialize<JsonElement>(itemStr);
                        if (jsonElement.TryGetProperty("name", out JsonElement name) && name.ValueKind == JsonValueKind.String)
                        {
                            return name.GetString();
                        }
                        if (jsonElement.TryGetProperty("fileName", out JsonElement fileName) && fileName.ValueKind == JsonValueKind.String)
                        {
                            return fileName.GetString();
                        }
                    }
                    catch (JsonException)
                    {
                        // Not valid JSON, fall through to return raw string
                    }
                }

                return itemStr;
            }
            catch (Exception)
            {
                return item.ToString();
            }
        }

        /// <summary>
        /// Format rating answer
        /// </summary>
        private string FormatRatingAnswer(dynamic answer, dynamic questionConfig)
        {
            if (answer == null)
                return "No rating";

            try
            {
                var rating = Convert.ToInt32(answer);
                var maxRating = GetRatingMax(questionConfig);
                return $"{rating}/{maxRating}";
            }
            catch
            {
                return answer.ToString();
            }
        }

        /// <summary>
        /// Format linear scale answer
        /// </summary>
        private string FormatLinearScaleAnswer(dynamic answer, dynamic questionConfig)
        {
            if (answer == null)
                return "No answer";

            try
            {
                var value = Convert.ToDouble(answer);
                var maxValue = GetLinearScaleMax(questionConfig);
                return $"{value}/{maxValue}";
            }
            catch
            {
                return answer.ToString();
            }
        }

        /// <summary>
        /// Format date answer
        /// </summary>
        private string FormatAnswerDate(dynamic dateValue, string questionType)
        {
            if (dateValue == null)
                return "No date";

            try
            {
                var date = DateTime.Parse(dateValue.ToString());

                if (questionType == "time")
                {
                    return date.ToString("HH:mm:ss");
                }
                else
                {
                    return date.ToString("MM/dd/yyyy");
                }
            }
            catch
            {
                return dateValue.ToString();
            }
        }

        /// <summary>
        /// Get checkbox answers as list
        /// </summary>
        private List<string> GetCheckboxAnswers(dynamic answer)
        {
            if (answer == null)
                return new List<string>();

            try
            {
                if (answer is IEnumerable<object> enumerable)
                {
                    var results = new List<string>();
                    foreach (var item in enumerable)
                    {
                        var itemStr = item?.ToString();
                        if (!string.IsNullOrEmpty(itemStr))
                        {
                            results.Add(itemStr);
                        }
                    }
                    return results;
                }

                var answerStr = answer.ToString();
                if (answerStr.StartsWith("[") && answerStr.EndsWith("]"))
                {
                    var array = JsonSerializer.Deserialize<string[]>(answerStr);
                    if (array != null)
                    {
                        var results = new List<string>();
                        foreach (var item in array)
                        {
                            if (!string.IsNullOrEmpty(item))
                            {
                                results.Add(item);
                            }
                        }
                        return results;
                    }
                    return new List<string>();
                }

                // Comma-separated string
                var parts = answerStr.Split(',');
                var trimmedResults = new List<string>();
                foreach (var part in parts)
                {
                    var trimmed = part.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        trimmedResults.Add(trimmed);
                    }
                }
                return trimmedResults;
            }
            catch
            {
                return new List<string> { answer.ToString() };
            }
        }

        /// <summary>
        /// Parse response text for other values
        /// </summary>
        private Dictionary<string, string> ParseResponseText(string responseText)
        {
            if (string.IsNullOrEmpty(responseText) || responseText.Trim() == "{}")
                return new Dictionary<string, string>();

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(responseText) ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Extract other values from responseText (aligned with frontend extractOtherValues logic)
        /// </summary>
        private Dictionary<string, string> ExtractOtherValues(string responseText, string questionId)
        {
            var otherValues = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(responseText) || string.IsNullOrEmpty(questionId))
                return otherValues;

            try
            {
                var parsed = ParseResponseText(responseText);

                // Extract base questionId (remove row info, e.g., question-xxx_row-xxx -> question-xxx)
                var baseQuestionId = questionId.Contains("_row-")
                    ? questionId.Split(new[] { "_row-" }, StringSplitOptions.None)[0].Split('_')[0]
                    : questionId.Split('_')[0];

                foreach (var kvp in parsed)
                {
                    // Match questionId or baseQuestionId
                    if (!kvp.Key.Contains(questionId) && !kvp.Key.Contains(baseQuestionId))
                        continue;

                    var customValue = kvp.Value;
                    var parts = kvp.Key.Split('_');

                    // Store full key for exact matching
                    otherValues[kvp.Key] = customValue;

                    // Always store "other" as fallback if we have a matching key
                    if (!otherValues.ContainsKey("other"))
                    {
                        otherValues["other"] = customValue;
                    }

                    // Format 1: questionId_optionId (single/multiple choice)
                    // Example: "1988483416273850373_1988483416273850372"
                    // Also handle cases where questionId might be a substring match
                    if (parts.Length == 2 && (parts[0] == questionId || parts[0] == baseQuestionId || kvp.Key.StartsWith(questionId + "_") || kvp.Key.StartsWith(baseQuestionId + "_")))
                    {
                        var optionId = parts[1];
                        otherValues[optionId] = customValue;
                        if (!otherValues.ContainsKey("other"))
                        {
                            otherValues["other"] = customValue; // Generic fallback
                        }
                    }

                    // Format 2: questionId_rowId_columnId (grid type)
                    // Example: "1988483416273850381_1988483416273850376_1988483416273850380"
                    if (parts.Length == 3)
                    {
                        var columnId = parts[2]; // Last part is column ID
                        otherValues[columnId] = customValue;
                        otherValues[$"column-{columnId}"] = customValue;
                        otherValues[$"column-other-{columnId}"] = customValue;
                        // Also store full key format for matching
                        var fullKey = $"{questionId}_{parts[1]}_{columnId}";
                        otherValues[fullKey] = customValue;
                    }

                    // Format 3: Contains column-other- format
                    var columnPart = parts.FirstOrDefault(p => p.StartsWith("column-other-"));
                    if (!string.IsNullOrEmpty(columnPart))
                    {
                        var columnId = columnPart.Replace("column-other-", "");
                        otherValues[columnPart] = customValue;
                        otherValues[columnId] = customValue;
                        otherValues[$"column-{columnId}"] = customValue;
                    }

                    // Format 4: Contains option- format
                    var optionPart = parts.FirstOrDefault(p => p.StartsWith("option-"));
                    if (!string.IsNullOrEmpty(optionPart))
                    {
                        var optionId = optionPart.Replace("option-", "");
                        otherValues[optionPart] = customValue;
                        otherValues[optionId] = customValue;
                        var alternativeKey = optionPart.Replace("option-", "option_");
                        otherValues[alternativeKey] = customValue;
                    }

                    // Store last part (usually ID) for matching
                    if (parts.Length > 0)
                    {
                        var lastPart = parts[parts.Length - 1];
                        if (!string.IsNullOrEmpty(lastPart))
                        {
                            otherValues[lastPart] = customValue;
                        }
                    }

                    // Store generic "other" key as final fallback
                    if (!otherValues.ContainsKey("other"))
                    {
                        otherValues["other"] = customValue;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract other values from responseText");
            }

            return otherValues;
        }

        /// <summary>
        /// Check if has valid answer
        /// </summary>
        private bool HasValidAnswer(dynamic answer)
        {
            if (answer == null)
                return false;

            var answerStr = answer.ToString();
            if (string.IsNullOrWhiteSpace(answerStr))
                return false;

            var trimmed = answerStr.Trim();
            return trimmed != "{}" &&
                   trimmed != "[]" &&
                   trimmed != "null" &&
                   trimmed != "undefined" &&
                   trimmed != "No answer provided" &&
                   trimmed != "No selection made";
        }

        /// <summary>
        /// Safely check if a dynamic object is null or empty
        /// </summary>
        private bool IsNullOrEmpty(dynamic obj)
        {
            try
            {
                // Handle null reference
                if (ReferenceEquals(obj, null))
                    return true;

                // Handle JsonElement specifically
                if (obj is JsonElement element)
                {
                    return element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined;
                }

                // Try to serialize to check if it's a valid object
                var serialized = JsonSerializer.Serialize(obj);
                return string.IsNullOrEmpty(serialized) || serialized == "null" || serialized == "{}";
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Check if answers are equal
        /// </summary>
        private bool AreAnswersEqual(dynamic answer1, dynamic answer2)
        {
            try
            {
                // Handle null cases
                if (answer1 == null && answer2 == null) return true;
                if (answer1 == null || answer2 == null) return false;

                // For file upload answers, compare the meaningful content
                if (IsFileUploadAnswer(answer1) || IsFileUploadAnswer(answer2))
                {
                    return AreFileAnswersEqual(answer1, answer2);
                }

                // For array answers, use deep comparison
                if (IsArrayAnswer(answer1) || IsArrayAnswer(answer2))
                {
                    return AreArrayAnswersEqual(answer1, answer2);
                }

                // Default JSON serialization comparison
                var json1 = JsonSerializer.Serialize(answer1);
                var json2 = JsonSerializer.Serialize(answer2);
                return json1 == json2;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to compare answers using JSON serialization, falling back to string comparison");
                return answer1?.ToString() == answer2?.ToString();
            }
        }

        /// <summary>
        /// Check if responseTexts are equal (for detecting changes in "Other" custom values)
        /// </summary>
        private bool AreResponseTextsEqual(string responseText1, string responseText2)
        {
            try
            {
                // Handle null/empty cases
                if (string.IsNullOrEmpty(responseText1) && string.IsNullOrEmpty(responseText2)) return true;
                if (string.IsNullOrEmpty(responseText1) || string.IsNullOrEmpty(responseText2)) return false;

                // Normalize empty JSON objects
                var normalized1 = responseText1.Trim();
                var normalized2 = responseText2.Trim();
                if (normalized1 == "{}" && normalized2 == "{}") return true;
                if (normalized1 == "{}" || normalized2 == "{}") return false;

                // Parse and compare JSON dictionaries
                var dict1 = ParseResponseText(normalized1);
                var dict2 = ParseResponseText(normalized2);

                if (dict1.Count != dict2.Count) return false;

                foreach (var kvp in dict1)
                {
                    if (!dict2.TryGetValue(kvp.Key, out string value2) || kvp.Value != value2)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                // Try to normalize JSON by parsing and re-serializing before string comparison
                try
                {
                    var dict1 = ParseResponseText(responseText1?.Trim() ?? string.Empty);
                    var dict2 = ParseResponseText(responseText2?.Trim() ?? string.Empty);

                    if (dict1.Count != dict2.Count) return false;

                    foreach (var kvp in dict1)
                    {
                        if (!dict2.TryGetValue(kvp.Key, out string value2) || kvp.Value != value2)
                        {
                            return false;
                        }
                    }

                    return true;
                }
                catch
                {
                    _logger.LogWarning(ex, "Failed to compare responseTexts, falling back to string comparison");
                    return responseText1 == responseText2;
                }
            }
        }

        /// <summary>
        /// Check if question is a file upload question
        /// </summary>
        private bool IsFileUploadQuestion(dynamic response)
        {
            if (response == null) return false;

            try
            {
                // Check question type
                var type = response.type?.ToString();
                if (type == "file" || type == "file_upload")
                    return true;

                // Check if answer contains file upload indicators
                return IsFileUploadAnswer(response.answer);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if answer is a file upload answer
        /// </summary>
        private bool IsFileUploadAnswer(dynamic answer)
        {
            if (answer == null) return false;

            try
            {
                var answerStr = answer.ToString();

                // Check for common file upload indicators
                if (answerStr.Contains("\"name\"") && answerStr.Contains("\"size\""))
                    return true;

                if (answerStr.Contains("\"fileName\""))
                    return true;

                // Check if it's an array of file objects
                if (answer is IEnumerable<object> enumerable)
                {
                    foreach (var item in enumerable.Take(1)) // Check first item only
                    {
                        var itemStr = item?.ToString() ?? "";
                        if (itemStr.Contains("\"name\"") && itemStr.Contains("\"size\""))
                            return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if answer is an array answer
        /// </summary>
        private bool IsArrayAnswer(dynamic answer)
        {
            if (answer == null) return false;

            try
            {
                return answer is IEnumerable<object> && !(answer is string);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Compare file upload answers by meaningful content
        /// </summary>
        private bool AreFileAnswersEqual(dynamic answer1, dynamic answer2)
        {
            try
            {
                List<FileInfo> files1 = ExtractFileInfoList(answer1);
                List<FileInfo> files2 = ExtractFileInfoList(answer2);

                if (files1.Count != files2.Count)
                    return false;

                // Sort by name for consistent comparison
                files1.Sort(CompareFileInfoByName);
                files2.Sort(CompareFileInfoByName);

                for (int i = 0; i < files1.Count; i++)
                {
                    if (!files1[i].Equals(files2[i]))
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to compare file answers, falling back to JSON comparison");
                return JsonSerializer.Serialize(answer1) == JsonSerializer.Serialize(answer2);
            }
        }

        /// <summary>
        /// Compare array answers
        /// </summary>
        private bool AreArrayAnswersEqual(dynamic answer1, dynamic answer2)
        {
            try
            {
                if (!(answer1 is IEnumerable<object> enum1) || !(answer2 is IEnumerable<object> enum2))
                {
                    return JsonSerializer.Serialize(answer1) == JsonSerializer.Serialize(answer2);
                }

                var list1 = enum1.ToList();
                var list2 = enum2.ToList();

                if (list1.Count != list2.Count)
                    return false;

                for (int i = 0; i < list1.Count; i++)
                {
                    var item1Json = JsonSerializer.Serialize(list1[i]);
                    var item2Json = JsonSerializer.Serialize(list2[i]);
                    if (item1Json != item2Json)
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to compare array answers, falling back to JSON comparison");
                return JsonSerializer.Serialize(answer1) == JsonSerializer.Serialize(answer2);
            }
        }

        /// <summary>
        /// Extract file information from answer
        /// </summary>
        private List<FileInfo> ExtractFileInfoList(dynamic answer)
        {
            var fileInfos = new List<FileInfo>();

            if (answer == null)
                return fileInfos;

            try
            {
                if (answer is IEnumerable<object> enumerable)
                {
                    foreach (var item in enumerable)
                    {
                        var fileInfo = ExtractFileInfo(item);
                        if (fileInfo != null)
                            fileInfos.Add(fileInfo);
                    }
                }
                else
                {
                    var fileInfo = ExtractFileInfo(answer);
                    if (fileInfo != null)
                        fileInfos.Add(fileInfo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract file info list from answer");
            }

            return fileInfos;
        }

        /// <summary>
        /// Extract file information from a single file object
        /// </summary>
        private FileInfo ExtractFileInfo(dynamic fileObj)
        {
            if (fileObj == null)
                return null;

            try
            {
                var fileObjStr = fileObj.ToString();
                if (string.IsNullOrEmpty(fileObjStr))
                    return null;

                // Try to parse as JSON
                if (fileObjStr.StartsWith("{"))
                {
                    var jsonElement = JsonSerializer.Deserialize<JsonElement>(fileObjStr);

                    var name = "";
                    var size = 0L;
                    var uid = "";

                    if (jsonElement.TryGetProperty("name", out JsonElement nameElement))
                        name = nameElement.GetString() ?? "";

                    if (jsonElement.TryGetProperty("size", out JsonElement sizeElement))
                        size = sizeElement.GetInt64();

                    if (jsonElement.TryGetProperty("uid", out JsonElement uidElement))
                        uid = uidElement.GetString() ?? "";

                    return new FileInfo { Name = name, Size = size, Uid = uid };
                }

                // Fallback to string representation
                return new FileInfo { Name = fileObjStr, Size = 0, Uid = "" };
            }
            catch
            {
                return new FileInfo { Name = fileObj.ToString() ?? "", Size = 0, Uid = "" };
            }
        }

        /// <summary>
        /// Compare FileInfo objects by name
        /// </summary>
        private static int CompareFileInfoByName(FileInfo a, FileInfo b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Simple file info class for comparison
        /// </summary>
        private class FileInfo : IEquatable<FileInfo>
        {
            public string Name { get; set; } = "";
            public long Size { get; set; }
            public string Uid { get; set; } = "";

            public bool Equals(FileInfo other)
            {
                if (other == null) return false;
                return Name == other.Name && Size == other.Size && Uid == other.Uid;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as FileInfo);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Name, Size, Uid);
            }
        }

        /// <summary>
        /// Check if option is "other" type
        /// </summary>
        private bool IsOtherOption(dynamic option)
        {
            try
            {
                var optionObj = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(option));

                if (optionObj.TryGetProperty("isOther", out JsonElement isOther) && isOther.GetBoolean())
                    return true;

                if (optionObj.TryGetProperty("type", out JsonElement type) && type.GetString() == "other")
                    return true;

                if (optionObj.TryGetProperty("allowCustom", out JsonElement allowCustom) && allowCustom.GetBoolean())
                    return true;

                if (optionObj.TryGetProperty("hasInput", out JsonElement hasInput) && hasInput.GetBoolean())
                    return true;

                if (optionObj.TryGetProperty("label", out JsonElement label))
                {
                    var labelStr = label.GetString()?.ToLower() ?? "";
                    return labelStr.Contains("other") || labelStr.Contains("custom") || labelStr.Contains("specify");
                }
            }
            catch (JsonException)
            {
                // Not valid JSON structure, return false
            }

            return false;
        }

        /// <summary>
        /// Get options from question config
        /// </summary>
        private List<dynamic> GetOptionsFromConfig(dynamic questionConfig)
        {
            try
            {
                var configObj = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(questionConfig));
                if (configObj.TryGetProperty("options", out JsonElement options) && options.ValueKind == JsonValueKind.Array)
                {
                    return options.EnumerateArray().Select(x => JsonSerializer.Deserialize<dynamic>(x.GetRawText())).ToList();
                }
            }
            catch (JsonException)
            {
                // Not valid JSON structure, return empty list
            }
            return new List<dynamic>();
        }

        /// <summary>
        /// Get columns from question config
        /// </summary>
        private List<dynamic> GetColumnsFromConfig(dynamic questionConfig)
        {
            try
            {
                var configObj = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(questionConfig));
                if (configObj.TryGetProperty("columns", out JsonElement columns) && columns.ValueKind == JsonValueKind.Array)
                {
                    return columns.EnumerateArray().Select(x => JsonSerializer.Deserialize<dynamic>(x.GetRawText())).ToList();
                }
            }
            catch (JsonException)
            {
                // Not valid JSON structure, return empty list
            }
            return new List<dynamic>();
        }

        /// <summary>
        /// Get rows from question config
        /// </summary>
        private List<dynamic> GetRowsFromConfig(dynamic questionConfig)
        {
            try
            {
                var configObj = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(questionConfig));
                if (configObj.TryGetProperty("rows", out JsonElement rows) && rows.ValueKind == JsonValueKind.Array)
                {
                    return rows.EnumerateArray().Select(x => JsonSerializer.Deserialize<dynamic>(x.GetRawText())).ToList();
                }
            }
            catch (JsonException)
            {
                // Not valid JSON structure, return empty list
            }
            return new List<dynamic>();
        }

        /// <summary>
        /// Get rating max value
        /// </summary>
        private int GetRatingMax(dynamic questionConfig)
        {
            try
            {
                var configObj = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(questionConfig));

                if (configObj.TryGetProperty("max", out JsonElement max))
                    return max.GetInt32();

                if (configObj.TryGetProperty("maxValue", out JsonElement maxValue))
                    return maxValue.GetInt32();

                if (configObj.TryGetProperty("scale", out JsonElement scale))
                {
                    if (scale.TryGetProperty("max", out JsonElement scaleMax))
                        return scaleMax.GetInt32();
                }
            }
            catch (JsonException)
            {
                // Not valid JSON structure, return default
            }

            return 5; // Default rating max
        }

        /// <summary>
        /// Get linear scale max value
        /// </summary>
        private int GetLinearScaleMax(dynamic questionConfig)
        {
            try
            {
                var configObj = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(questionConfig));

                if (configObj.TryGetProperty("max", out JsonElement max))
                    return max.GetInt32();

                if (configObj.TryGetProperty("maxValue", out JsonElement maxValue))
                    return maxValue.GetInt32();

                if (configObj.TryGetProperty("scale", out JsonElement scale))
                {
                    if (scale.TryGetProperty("max", out JsonElement scaleMax))
                        return scaleMax.GetInt32();
                }
            }
            catch (JsonException)
            {
                // Not valid JSON structure, return default
            }

            return 10; // Default linear scale max
        }

        #endregion
    }
}