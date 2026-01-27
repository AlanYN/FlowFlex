using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace FlowFlex.Application.Helpers
{
    /// <summary>
    /// Helper class for parsing various JSON and object formats to string lists
    /// Consolidates ParseRecipientIds and ParseObjectToStringList functionality
    /// </summary>
    public static class JsonParseHelper
    {
        /// <summary>
        /// Parse object to string list, handling various formats:
        /// - System.Text.Json.JsonElement (array or string)
        /// - Newtonsoft.Json JArray/JToken
        /// - IEnumerable&lt;string&gt; or IEnumerable&lt;object&gt;
        /// - JSON array string "[...]"
        /// - Single string value
        /// </summary>
        /// <param name="obj">Object to parse</param>
        /// <returns>List of non-empty strings</returns>
        public static List<string> ParseToStringList(object? obj)
        {
            var result = new List<string>();
            
            if (obj == null)
                return result;

            // Handle System.Text.Json JsonElement
            if (obj is System.Text.Json.JsonElement jsonElement)
            {
                return ParseJsonElement(jsonElement);
            }

            // Handle Newtonsoft.Json JArray
            if (obj is Newtonsoft.Json.Linq.JArray jArray)
            {
                foreach (var item in jArray)
                {
                    var value = item?.ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        result.Add(value);
                    }
                }
                return result;
            }

            // Handle Newtonsoft.Json JToken (could be JArray or JValue)
            if (obj is Newtonsoft.Json.Linq.JToken jToken)
            {
                if (jToken.Type == Newtonsoft.Json.Linq.JTokenType.Array)
                {
                    return jToken.ToObject<List<string>>()?.Where(s => !string.IsNullOrWhiteSpace(s)).ToList() 
                        ?? new List<string>();
                }
                var val = jToken.ToString();
                return !string.IsNullOrWhiteSpace(val) ? new List<string> { val } : new List<string>();
            }

            // Handle IEnumerable<string>
            if (obj is IEnumerable<string> stringList)
            {
                result.AddRange(stringList.Where(s => !string.IsNullOrWhiteSpace(s)));
                return result;
            }

            // Handle IEnumerable<object> (but not string)
            if (obj is IEnumerable<object> objectList && !(obj is string))
            {
                foreach (var item in objectList)
                {
                    var value = item?.ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        result.Add(value);
                    }
                }
                return result;
            }

            // Handle non-generic IEnumerable (but not string)
            if (obj is System.Collections.IEnumerable enumerable && !(obj is string))
            {
                foreach (var item in enumerable)
                {
                    var value = item?.ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        result.Add(value);
                    }
                }
                return result;
            }

            // Handle string (could be JSON array or single value)
            if (obj is string strValue)
            {
                return ParseStringValue(strValue);
            }

            // Fallback: convert to string
            var strVal = obj.ToString();
            return !string.IsNullOrWhiteSpace(strVal) ? new List<string> { strVal } : new List<string>();
        }

        /// <summary>
        /// Parse System.Text.Json JsonElement to string list
        /// </summary>
        private static List<string> ParseJsonElement(System.Text.Json.JsonElement jsonElement)
        {
            var result = new List<string>();

            if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in jsonElement.EnumerateArray())
                {
                    var value = item.ValueKind == System.Text.Json.JsonValueKind.String 
                        ? item.GetString() 
                        : item.ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        result.Add(value);
                    }
                }
            }
            else if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var value = jsonElement.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    // Check if it's a JSON array string
                    if (value.TrimStart().StartsWith("["))
                    {
                        return ParseStringValue(value);
                    }
                    result.Add(value);
                }
            }
            else if (jsonElement.ValueKind != System.Text.Json.JsonValueKind.Null &&
                     jsonElement.ValueKind != System.Text.Json.JsonValueKind.Undefined)
            {
                var value = jsonElement.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    result.Add(value);
                }
            }

            return result;
        }

        /// <summary>
        /// Parse string value, handling JSON array strings
        /// </summary>
        private static List<string> ParseStringValue(string strValue)
        {
            if (string.IsNullOrWhiteSpace(strValue))
                return new List<string>();

            strValue = strValue.Trim();
            
            // Check if it's a JSON array string
            if (strValue.StartsWith("[") && strValue.EndsWith("]"))
            {
                try
                {
                    var parsed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(strValue);
                    if (parsed != null)
                    {
                        return parsed.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                    }
                }
                catch
                {
                    // Not a valid JSON array, treat as single value
                }
            }
            
            return new List<string> { strValue };
        }

        /// <summary>
        /// Parse JSON with double-escape handling
        /// Handles cases where JSON is double-serialized (e.g., "\"[{...}]\"")
        /// </summary>
        /// <typeparam name="T">Target type</typeparam>
        /// <param name="json">JSON string to parse</param>
        /// <param name="defaultValue">Default value if parsing fails</param>
        /// <returns>Parsed object or default value</returns>
        public static T ParseWithDoubleEscapeHandling<T>(string? json, T defaultValue) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
                return defaultValue;

            try
            {
                // First try direct parsing
                var result = JsonConvert.DeserializeObject<T>(json);
                if (result != null) return result;
            }
            catch (JsonException)
            {
                // If direct parsing fails, try to unescape first
                try
                {
                    var unescapedJson = JsonConvert.DeserializeObject<string>(json);
                    if (!string.IsNullOrEmpty(unescapedJson))
                    {
                        var result = JsonConvert.DeserializeObject<T>(unescapedJson);
                        if (result != null) return result;
                    }
                }
                catch (JsonException)
                {
                    // Format is unexpected
                }
            }

            return defaultValue;
        }
    }
}
