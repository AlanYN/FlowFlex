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

                if (after == null)
                {
                    _logger.LogError("Failed to parse afterData JSON");
                    return new List<string> { "Questionnaire updated (JSON parse error)" };
                }

                // Process answer submission (only afterData)
                if (before == null && after?.responses != null)
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
                if (before?.responses != null && after?.responses != null)
                {
                    // Debug: Processing answer updates

                    var beforeMap = new Dictionary<string, dynamic>();
                    foreach (var resp in before.responses)
                    {
                        if (resp?.questionId != null)
                        {
                            beforeMap[resp.questionId.ToString()] = resp;
                        }
                    }

                    foreach (var afterResp in after.responses)
                    {
                        var questionId = afterResp?.questionId?.ToString();
                        var questionTitle = afterResp?.question ?? questionId ?? "Unknown Question";

                        if (string.IsNullOrEmpty(questionId))
                            continue;

                        if (!beforeMap.TryGetValue(questionId, out dynamic beforeResp))
                        {
                            // New answer
                            var formattedAnswer = FormatAnswerWithConfig(afterResp, questionnaireConfig);
                            changesList.Add($"{questionTitle}: {formattedAnswer}");
                            // Debug: Added new answer
                        }
                        else if (!AreAnswersEqual(beforeResp?.answer, afterResp?.answer))
                        {
                            // Modified answer
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

            var answer = response.answer ?? response.responseText;
            if (!HasValidAnswer(answer))
                return "No answer";

            var type = response.type?.ToString();
            var questionId = response.questionId?.ToString();

            // Find question configuration
            dynamic questionConfig = null;
            if (questionnaireConfig != null)
            {
                questionConfig = FindQuestionConfig(questionnaireConfig, questionId);
            }

            switch (type)
            {
                case "multiple_choice":
                    return GetMultipleChoiceLabel(answer, questionConfig);

                case "dropdown":
                    return GetDropdownLabel(answer, questionConfig);

                case "checkboxes":
                    return GetCheckboxLabels(answer, questionConfig, response.responseText?.ToString(), questionId);

                case "multiple_choice_grid":
                case "checkbox_grid":
                    return GetGridAnswerLabels(answer, questionConfig, response.responseText?.ToString(), questionId);

                case "short_answer_grid":
                    return GetShortAnswerGridSummary(response, questionConfig);

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
        /// Get multiple choice label
        /// </summary>
        private string GetMultipleChoiceLabel(dynamic answer, dynamic questionConfig)
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

            if (configIsNull)
                return answer?.ToString() ?? "No answer";

            try
            {
                var options = GetOptionsFromConfig(questionConfig);
                var answerStr = answer.ToString();

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

                    if (value == answerStr)
                    {
                        return label ?? answerStr;
                    }
                }
            }
            catch { }

            return answer.ToString();
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
                            if (otherValues.TryGetValue(value, out string customValue))
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
                        labels.Add(value);
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
                    if (columnMap.TryGetValue(id, out string label))
                    {
                        if (otherColumnIds.Contains(id))
                        {
                            if (otherValues.TryGetValue(id, out string customValue))
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
                        // If no exact match found, keep the original ID
                        // This is where IDs like "1963784695460270089" would remain as-is
                        // _logger.LogDebug("No label found for answer ID: {AnswerId} in questionId: {QuestionId}", id, questionId);
                        labels.Add(id);
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
                if (string.IsNullOrEmpty(responseText))
                    return "No answer";

                var parsed = ParseResponseText(responseText);
                if (parsed == null || parsed.Count == 0)
                    return "No answer";

                var rowIdToLabel = new Dictionary<string, string>();
                if (questionConfig != null)
                {
                    var rows = GetRowsFromConfig(questionConfig);
                    foreach (var row in rows)
                    {
                        var id = row.id?.ToString();
                        var label = row.label?.ToString();
                        if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(label))
                        {
                            rowIdToLabel[id] = label;
                        }
                    }
                }

                var results = new List<string>();
                foreach (var kvp in parsed)
                {
                    var parts = kvp.Key.Split('_');
                    var rowPart = parts.FirstOrDefault((Func<string, bool>)(p => p.StartsWith("row-")));
                    if (!string.IsNullOrEmpty(rowPart))
                    {
                        var label = rowIdToLabel.TryGetValue(rowPart, out string rowLabel)
                            ? rowLabel
                            : rowPart.Replace("row-", "");
                        var value = kvp.Value?.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(value))
                        {
                            results.Add($"{label}: {value}");
                        }
                    }
                }

                return results.Count > 0 ? string.Join("; ", results) : "No answer";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to format short answer grid");
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

                var answerStr = answer.ToString();
                if (answerStr == "[object Object]")
                    return "File uploaded (name not available)";

                // Try to parse as JSON array/object
                try
                {
                    var files = JsonSerializer.Deserialize<JsonElement>(answerStr);
                    if (files.ValueKind == JsonValueKind.Array)
                    {
                        var fileNames = new List<string>();
                        foreach (var file in files.EnumerateArray())
                        {
                            if (file.TryGetProperty("name", out JsonElement name))
                            {
                                fileNames.Add(name.GetString());
                            }
                            else if (file.TryGetProperty("fileName", out JsonElement fileName))
                            {
                                fileNames.Add(fileName.GetString());
                            }
                        }
                        return fileNames.Count > 0 ? $"Files: {string.Join(", ", fileNames)}" : "Files uploaded";
                    }
                    else if (files.TryGetProperty("name", out JsonElement singleName))
                    {
                        return $"File: {singleName.GetString()}";
                    }
                }
                catch { }

                return $"File: {answerStr}";
            }
            catch
            {
                return "File uploaded";
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
                    return enumerable.Select(x => x?.ToString()).Where(x => !string.IsNullOrEmpty(x)).ToList();
                }

                var answerStr = answer.ToString();
                if (answerStr.StartsWith("[") && answerStr.EndsWith("]"))
                {
                    var array = JsonSerializer.Deserialize<string[]>(answerStr);
                    return array?.Where((Func<string, bool>)(x => !string.IsNullOrEmpty(x))).ToList() ?? new List<string>();
                }

                // Comma-separated string
                return answerStr.Split(',').Select((Func<string, string>)(x => x.Trim())).Where((Func<string, bool>)(x => !string.IsNullOrEmpty(x))).ToList();
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
        /// Extract other values from responseText
        /// </summary>
        private Dictionary<string, string> ExtractOtherValues(string responseText, string questionId)
        {
            var otherValues = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(responseText) || string.IsNullOrEmpty(questionId))
                return otherValues;

            try
            {
                var parsed = ParseResponseText(responseText);
                foreach (var kvp in parsed)
                {
                    if (kvp.Key.Contains(questionId) && (kvp.Key.Contains("other") || kvp.Key.Contains("option")))
                    {
                        // Extract option ID from key
                        var parts = kvp.Key.Split('_');
                        var optionPart = parts.FirstOrDefault(p => p.StartsWith("option-") || p.StartsWith("column-other-"));
                        if (!string.IsNullOrEmpty(optionPart))
                        {
                            otherValues[optionPart] = kvp.Value;
                        }
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
                return JsonSerializer.Serialize(answer1) == JsonSerializer.Serialize(answer2);
            }
            catch
            {
                return answer1?.ToString() == answer2?.ToString();
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
            catch { }

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
            catch { }
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
            catch { }
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
            catch { }
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
            catch { }

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
            catch { }

            return 10; // Default linear scale max
        }

        #endregion
    }
}