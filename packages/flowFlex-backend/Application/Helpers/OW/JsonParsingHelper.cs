using System.Text.Json;
using FlowFlex.Domain.Entities.OW;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Helpers.OW
{
    /// <summary>
    /// Helper class for JSON parsing operations with consistent error handling
    /// Centralizes double-serialized JSON handling and provides type-safe parsing
    /// </summary>
    public static class JsonParsingHelper
    {
        /// <summary>
        /// Default JSON serializer options for consistent parsing
        /// </summary>
        public static readonly JsonSerializerOptions DefaultOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
        };

        /// <summary>
        /// Unwrap double-serialized JSON string
        /// Handles cases where JSON is serialized multiple times (e.g., "\"[...]\"")
        /// </summary>
        /// <param name="json">The potentially double-serialized JSON string</param>
        /// <param name="maxIterations">Maximum unwrap iterations to prevent infinite loops</param>
        /// <returns>Unwrapped JSON string</returns>
        public static string UnwrapJsonString(string? json, int maxIterations = 3)
        {
            if (string.IsNullOrWhiteSpace(json))
                return "[]";

            var current = json.Trim();
            var iterations = 0;

            while (iterations < maxIterations && 
                   current.StartsWith("\"") && 
                   current.EndsWith("\""))
            {
                try
                {
                    var unwrapped = JsonSerializer.Deserialize<string>(current, DefaultOptions);
                    if (string.IsNullOrWhiteSpace(unwrapped))
                        break;
                    current = unwrapped.Trim();
                    iterations++;
                }
                catch (JsonException)
                {
                    break;
                }
            }

            return current;
        }

        /// <summary>
        /// Parse stages progress JSON with automatic unwrapping
        /// </summary>
        /// <param name="stagesProgressJson">The stages progress JSON string</param>
        /// <param name="logger">Optional logger for error reporting</param>
        /// <returns>List of OnboardingStageProgress or empty list on failure</returns>
        public static List<OnboardingStageProgress> ParseStagesProgress(
            string? stagesProgressJson, 
            ILogger? logger = null)
        {
            if (string.IsNullOrWhiteSpace(stagesProgressJson))
                return new List<OnboardingStageProgress>();

            try
            {
                var jsonString = UnwrapJsonString(stagesProgressJson);
                return JsonSerializer.Deserialize<List<OnboardingStageProgress>>(jsonString, DefaultOptions) 
                       ?? new List<OnboardingStageProgress>();
            }
            catch (JsonException ex)
            {
                logger?.LogWarning(ex, "Failed to parse StagesProgressJson");
                return new List<OnboardingStageProgress>();
            }
        }

        /// <summary>
        /// Try parse stages progress JSON with success indicator
        /// </summary>
        /// <param name="stagesProgressJson">The stages progress JSON string</param>
        /// <param name="result">Parsed result if successful</param>
        /// <param name="logger">Optional logger for error reporting</param>
        /// <returns>True if parsing succeeded, false otherwise</returns>
        public static bool TryParseStagesProgress(
            string? stagesProgressJson,
            out List<OnboardingStageProgress> result,
            ILogger? logger = null)
        {
            result = new List<OnboardingStageProgress>();

            if (string.IsNullOrWhiteSpace(stagesProgressJson))
                return false;

            try
            {
                var jsonString = UnwrapJsonString(stagesProgressJson);
                var parsed = JsonSerializer.Deserialize<List<OnboardingStageProgress>>(jsonString, DefaultOptions);
                if (parsed != null && parsed.Any())
                {
                    result = parsed;
                    return true;
                }
                return false;
            }
            catch (JsonException ex)
            {
                logger?.LogWarning(ex, "Failed to parse StagesProgressJson");
                return false;
            }
        }

        /// <summary>
        /// Parse JSON array safely with automatic unwrapping
        /// </summary>
        /// <typeparam name="T">Element type</typeparam>
        /// <param name="json">JSON string to parse</param>
        /// <param name="logger">Optional logger for error reporting</param>
        /// <returns>Parsed list or empty list on failure</returns>
        public static List<T> ParseJsonArray<T>(string? json, ILogger? logger = null)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<T>();

            try
            {
                var jsonString = UnwrapJsonString(json);
                return JsonSerializer.Deserialize<List<T>>(jsonString, DefaultOptions) 
                       ?? new List<T>();
            }
            catch (JsonException ex)
            {
                logger?.LogWarning(ex, "Failed to parse JSON array of type {Type}", typeof(T).Name);
                return new List<T>();
            }
        }

        /// <summary>
        /// Parse string array from JSON (commonly used for teams, users, etc.)
        /// </summary>
        /// <param name="json">JSON string to parse</param>
        /// <returns>Parsed string list or empty list on failure</returns>
        public static List<string> ParseStringArray(string? json)
        {
            return ParseJsonArray<string>(json);
        }

        /// <summary>
        /// Serialize object to JSON string
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="obj">Object to serialize</param>
        /// <returns>JSON string or "[]" for null/empty collections</returns>
        public static string SerializeToJson<T>(T? obj)
        {
            if (obj == null)
                return "[]";

            try
            {
                return JsonSerializer.Serialize(obj, DefaultOptions);
            }
            catch (JsonException)
            {
                return "[]";
            }
        }
    }
}
