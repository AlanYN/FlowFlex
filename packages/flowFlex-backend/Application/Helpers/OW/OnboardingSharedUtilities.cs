using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Infrastructure.Services;
using PermissionOperationType = FlowFlex.Domain.Shared.Enums.Permission.OperationTypeEnum;

namespace FlowFlex.Application.Helpers.OW
{
    /// <summary>
    /// Shared utility methods for Onboarding services
    /// Centralizes common functionality to eliminate code duplication
    /// </summary>
    public static class OnboardingSharedUtilities
    {
        /// <summary>
        /// Maximum length for Notes field in database
        /// </summary>
        public const int MaxNotesLength = 1000;
        /// <summary>
        /// Shared JsonSerializerOptions for all Onboarding services
        /// Use this instead of creating new JsonSerializerOptions instances
        /// </summary>
        public static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Normalize DateTime to start of day in UTC
        /// </summary>
        /// <param name="dateTime">The DateTime to normalize</param>
        /// <returns>DateTime normalized to start of day in UTC</returns>
        public static DateTime NormalizeToStartOfDay(DateTime dateTime)
        {
            return dateTime.Date.ToUniversalTime();
        }

        /// <summary>
        /// Get current UTC time normalized to start of day
        /// </summary>
        /// <returns>Current UTC date at midnight</returns>
        public static DateTime GetNormalizedUtcNow()
        {
            return DateTime.UtcNow.Date;
        }

        /// <summary>
        /// Safely serialize object to JSON string
        /// </summary>
        /// <typeparam name="T">Type of object to serialize</typeparam>
        /// <param name="obj">Object to serialize</param>
        /// <returns>JSON string or null if serialization fails</returns>
        public static string? SafeSerialize<T>(T obj)
        {
            if (obj == null) return null;
            
            try
            {
                return JsonSerializer.Serialize(obj, JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Safely deserialize JSON string to object
        /// </summary>
        /// <typeparam name="T">Target type</typeparam>
        /// <param name="json">JSON string to deserialize</param>
        /// <returns>Deserialized object or default value if deserialization fails</returns>
        public static T? SafeDeserialize<T>(string? json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            
            try
            {
                return JsonSerializer.Deserialize<T>(json, JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Safely append text to Notes field with length validation
        /// Ensures the total length doesn't exceed the database constraint (1000 characters)
        /// </summary>
        /// <param name="entity">Onboarding entity to update</param>
        /// <param name="noteText">Text to append to notes</param>
        public static void SafeAppendToNotes(Onboarding entity, string noteText)
        {
            if (entity == null || string.IsNullOrEmpty(noteText))
                return;

            var currentNotes = entity.Notes ?? string.Empty;
            var newContent = string.IsNullOrEmpty(currentNotes)
                ? noteText
                : $"{currentNotes}\n{noteText}";

            // If the new content exceeds the limit, truncate it intelligently
            if (newContent.Length > MaxNotesLength)
            {
                // Try to keep the most recent notes by truncating from the beginning
                var truncationMessage = "[...truncated older notes...]\n";
                var availableSpace = MaxNotesLength - truncationMessage.Length - noteText.Length - 1; // -1 for newline

                if (availableSpace > 0 && currentNotes.Length > availableSpace)
                {
                    // Keep the most recent part of existing notes
                    var recentNotes = currentNotes.Substring(currentNotes.Length - availableSpace);
                    // Find the first newline to avoid cutting in the middle of a note
                    var firstNewlineIndex = recentNotes.IndexOf('\n');
                    if (firstNewlineIndex > 0)
                    {
                        recentNotes = recentNotes.Substring(firstNewlineIndex + 1);
                    }
                    entity.Notes = $"{truncationMessage}{recentNotes}\n{noteText}";
                }
                else
                {
                    // If even the new note is too long, truncate it
                    entity.Notes = noteText.Substring(0, Math.Min(noteText.Length, MaxNotesLength));
                }
            }
            else
            {
                entity.Notes = newContent;
            }
        }

        /// <summary>
        /// Check if exception is related to JSONB type conversion error
        /// </summary>
        /// <param name="ex">Exception to check</param>
        /// <returns>True if the exception is related to JSONB type conversion</returns>
        public static bool IsJsonbTypeError(Exception ex)
        {
            if (ex == null) return false;
            
            var errorMessage = ex.ToString().ToLower();
            return errorMessage.Contains("42804") ||
                   errorMessage.Contains("jsonb") ||
                   (errorMessage.Contains("column") && errorMessage.Contains("text") && errorMessage.Contains("expression")) ||
                   ex.GetType().Name.Contains("Postgres");
        }

        /// <summary>
        /// Normalize DateTimeOffset to start of day (00:00:00)
        /// </summary>
        /// <param name="dateTime">DateTimeOffset to normalize</param>
        /// <returns>DateTimeOffset normalized to start of day</returns>
        public static DateTimeOffset NormalizeToStartOfDay(DateTimeOffset dateTime)
        {
            return new DateTimeOffset(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, dateTime.Offset);
        }

        /// <summary>
        /// Normalize nullable DateTimeOffset to start of day (00:00:00)
        /// </summary>
        /// <param name="dateTime">Nullable DateTimeOffset to normalize</param>
        /// <returns>Normalized DateTimeOffset or null if input is null</returns>
        public static DateTimeOffset? NormalizeToStartOfDay(DateTimeOffset? dateTime)
        {
            if (!dateTime.HasValue) return null;
            return NormalizeToStartOfDay(dateTime.Value);
        }

        /// <summary>
        /// Normalize estimated days to integer (round to nearest whole number)
        /// </summary>
        /// <param name="days">Decimal days value</param>
        /// <returns>Rounded decimal value or null if input is null</returns>
        public static decimal? NormalizeEstimatedDays(decimal? days)
        {
            if (!days.HasValue) return null;
            return Math.Round(days.Value, 0);
        }

        /// <summary>
        /// Get current UTC time normalized to start of day
        /// </summary>
        /// <returns>Current UTC DateTimeOffset at midnight</returns>
        public static DateTimeOffset GetNormalizedUtcNowOffset()
        {
            return NormalizeToStartOfDay(DateTimeOffset.UtcNow);
        }

        #region Permission Check Methods

        /// <summary>
        /// Check if current user has operate permission on the case
        /// </summary>
        /// <param name="permissionService">Permission service instance</param>
        /// <param name="userContext">User context instance</param>
        /// <param name="caseId">Case ID to check permission for</param>
        /// <returns>True if user has permission, false otherwise</returns>
        public static async Task<bool> CheckCaseOperatePermissionAsync(
            IPermissionService permissionService,
            UserContext userContext,
            long caseId)
        {
            var userId = userContext?.UserId;
            if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out var userIdLong))
            {
                return false;
            }

            var permissionResult = await permissionService.CheckCaseAccessAsync(
                userIdLong,
                caseId,
                PermissionOperationType.Operate);

            return permissionResult.Success && permissionResult.CanOperate;
        }

        /// <summary>
        /// Ensure current user has operate permission on the case, throw exception if not
        /// </summary>
        /// <param name="permissionService">Permission service instance</param>
        /// <param name="userContext">User context instance</param>
        /// <param name="caseId">Case ID to check permission for</param>
        /// <exception cref="CRMException">Thrown when user does not have permission</exception>
        public static async Task EnsureCaseOperatePermissionAsync(
            IPermissionService permissionService,
            UserContext userContext,
            long caseId)
        {
            if (!await CheckCaseOperatePermissionAsync(permissionService, userContext, caseId))
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed,
                    $"User does not have permission to operate on case {caseId}");
            }
        }

        #endregion

        #region User Context Helper Methods

        /// <summary>
        /// Get current user email from UserContext and OperatorContextService
        /// Falls back to "system@example.com" if email is not available
        /// </summary>
        /// <param name="userContext">User context instance</param>
        /// <param name="operatorContextService">Operator context service instance (optional)</param>
        /// <returns>User email or fallback value</returns>
        public static string GetCurrentUserEmail(UserContext? userContext, IOperatorContextService? operatorContextService = null)
        {
            // First try to get email from OperatorContextService
            if (operatorContextService != null)
            {
                var displayName = operatorContextService.GetOperatorDisplayName();
                if (!string.IsNullOrEmpty(displayName) && displayName.Contains("@"))
                {
                    return displayName;
                }
            }

            // Fall back to UserContext
            if (!string.IsNullOrEmpty(userContext?.Email))
            {
                return userContext.Email;
            }

            return "system@example.com";
        }

        #endregion

        #region JSON Parsing Methods

        /// <summary>
        /// Parse JSON array that might be double-encoded
        /// Handles both standard JSON arrays and double-encoded JSON strings
        /// </summary>
        /// <param name="jsonString">JSON string to parse</param>
        /// <returns>List of strings, or empty list if parsing fails</returns>
        public static List<string> ParseJsonArraySafe(string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return new List<string>();
            }

            try
            {
                var workingString = jsonString.Trim();
                // Handle double-encoded JSON (string wrapped in quotes)
                if (workingString.StartsWith("\"") && workingString.EndsWith("\""))
                {
                    workingString = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(workingString);
                }
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(workingString);
                return result ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        #endregion

        #region Time Zone Methods

        /// <summary>
        /// Get current time with +08:00 timezone (China Standard Time)
        /// </summary>
        /// <returns>Current time with +08:00 offset</returns>
        public static DateTimeOffset GetCurrentTimeWithTimeZone()
        {
            // China Standard Time is UTC+8
            var chinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
            var utcNow = DateTime.UtcNow;
            var chinaTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, chinaTimeZone);

            // Create DateTimeOffset with +08:00 offset
            return new DateTimeOffset(chinaTime, TimeSpan.FromHours(8));
        }

        #endregion

        #region Stage Tracking Methods

        /// <summary>
        /// Update stage tracking information on an onboarding entity
        /// Sets StageUpdatedTime, StageUpdatedBy, StageUpdatedById, and StageUpdatedByEmail
        /// </summary>
        /// <param name="entity">Onboarding entity to update</param>
        /// <param name="operatorContextService">Operator context service for getting current user info</param>
        /// <param name="userContext">User context for getting email</param>
        public static void UpdateStageTrackingInfo(
            Onboarding entity,
            IOperatorContextService operatorContextService,
            UserContext? userContext = null)
        {
            if (entity == null) return;

            entity.StageUpdatedTime = DateTimeOffset.UtcNow;
            entity.StageUpdatedBy = operatorContextService?.GetOperatorDisplayName();
            entity.StageUpdatedById = operatorContextService?.GetOperatorId() ?? 0;
            entity.StageUpdatedByEmail = GetCurrentUserEmail(userContext, operatorContextService);
        }

        #endregion
    }
}
