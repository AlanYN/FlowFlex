using System;
using System.Collections.Generic;
using FlowFlex.Domain.Entities.OW;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FlowFlex.Application.Helpers
{
    /// <summary>
    /// Helper class for parsing StagesProgressJson with double-encoding support
    /// </summary>
    public static class StagesProgressHelper
    {
        /// <summary>
        /// Parse StagesProgressJson to List of OnboardingStageProgress
        /// Handles both normal JSON and double-encoded JSON (string containing escaped JSON)
        /// </summary>
        /// <param name="stagesProgressJson">The JSON string to parse</param>
        /// <param name="logger">Optional logger for warnings</param>
        /// <param name="contextInfo">Optional context info for logging (e.g., "OnboardingId=123")</param>
        /// <returns>List of OnboardingStageProgress, empty list if parsing fails</returns>
        public static List<OnboardingStageProgress> ParseStagesProgress(
            string? stagesProgressJson, 
            ILogger? logger = null, 
            string? contextInfo = null)
        {
            if (string.IsNullOrWhiteSpace(stagesProgressJson))
            {
                return new List<OnboardingStageProgress>();
            }

            try
            {
                var jsonValue = stagesProgressJson.Trim();

                // Check if it's double-encoded (starts with quote, indicating a string value)
                if (jsonValue.StartsWith("\""))
                {
                    // First deserialize to get the inner JSON string
                    var innerJson = JsonConvert.DeserializeObject<string>(jsonValue);
                    if (!string.IsNullOrEmpty(innerJson))
                    {
                        return JsonConvert.DeserializeObject<List<OnboardingStageProgress>>(innerJson)
                            ?? new List<OnboardingStageProgress>();
                    }
                    return new List<OnboardingStageProgress>();
                }

                // Normal JSON array
                return JsonConvert.DeserializeObject<List<OnboardingStageProgress>>(jsonValue)
                    ?? new List<OnboardingStageProgress>();
            }
            catch (JsonException ex)
            {
                logger?.LogWarning(ex, "Failed to parse StagesProgressJson. {ContextInfo}", contextInfo ?? "");
                return new List<OnboardingStageProgress>();
            }
        }

        /// <summary>
        /// Try to parse StagesProgressJson with success indicator
        /// </summary>
        /// <param name="stagesProgressJson">The JSON string to parse</param>
        /// <param name="result">The parsed result</param>
        /// <param name="errorMessage">Error message if parsing fails</param>
        /// <returns>True if parsing succeeded, false otherwise</returns>
        public static bool TryParseStagesProgress(
            string? stagesProgressJson,
            out List<OnboardingStageProgress> result,
            out string? errorMessage)
        {
            result = new List<OnboardingStageProgress>();
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(stagesProgressJson))
            {
                return true; // Empty is valid, just returns empty list
            }

            try
            {
                var jsonValue = stagesProgressJson.Trim();

                // Check if it's double-encoded
                if (jsonValue.StartsWith("\""))
                {
                    var innerJson = JsonConvert.DeserializeObject<string>(jsonValue);
                    if (!string.IsNullOrEmpty(innerJson))
                    {
                        result = JsonConvert.DeserializeObject<List<OnboardingStageProgress>>(innerJson)
                            ?? new List<OnboardingStageProgress>();
                    }
                }
                else
                {
                    result = JsonConvert.DeserializeObject<List<OnboardingStageProgress>>(jsonValue)
                        ?? new List<OnboardingStageProgress>();
                }

                return true;
            }
            catch (JsonException ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}
