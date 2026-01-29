using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.IServices.OW.Onboarding;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Shared.Events;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Onboarding service - Helper and utility methods (delegates to IOnboardingHelperService)
    /// </summary>
    public partial class OnboardingService
    {
        // Note: _helperService is injected in OnboardingService.Main.cs

        /// <summary>
        /// Get onboarding progress - delegates to OnboardingQueryService
        /// </summary>
        public Task<OnboardingProgressDto> GetProgressAsync(long id)
            => _queryService.GetProgressAsync(id);

        /// <summary>
        /// Log general onboarding action to change log
        /// </summary>
        private Task LogOnboardingActionAsync(Onboarding onboarding, string action, string logType, bool success, object additionalData = null)
            => _helperService.LogOnboardingActionAsync(onboarding, action, logType, success, additionalData);

        /// <summary>
        /// Publish stage completion event for current stage completion
        /// </summary>
        private Task PublishStageCompletionEventForCurrentStageAsync(Onboarding onboarding, Stage completedStage, bool isFinalStage)
            => _helperService.PublishStageCompletionEventForCurrentStageAsync(onboarding, completedStage, isFinalStage);

        /// <summary>
        /// Build components payload for stage completion event
        /// </summary>
        private Task<StageCompletionComponents> BuildStageCompletionComponentsAsync(long onboardingId, long stageId, List<StageComponent> stageComponents, string componentsJson)
            => _helperService.BuildStageCompletionComponentsAsync(onboardingId, stageId, stageComponents, componentsJson);

        /// <summary>
        /// Parse components from JSON with lenient parsing for both camelCase and PascalCase
        /// </summary>
        private (List<StageComponent> stageComponents, List<string> staticFieldNames) ParseComponentsFromJson(string componentsJson)
            => _helperService.ParseComponentsFromJson(componentsJson);

        /// <summary>
        /// Parse JSON array that might be double-encoded
        /// </summary>
        private static List<string> ParseJsonArraySafe(string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return new List<string>();
            }

            try
            {
                var workingString = jsonString.Trim();
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

        /// <summary>
        /// Validates and formats JSON array string for PostgreSQL JSONB
        /// </summary>
        private static string ValidateAndFormatJsonArray(string jsonArray)
        {
            if (string.IsNullOrWhiteSpace(jsonArray))
            {
                return "[]";
            }

            try
            {
                var parsed = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonArray);
                if (parsed is Newtonsoft.Json.Linq.JArray)
                {
                    return jsonArray;
                }
                return "[]";
            }
            catch
            {
                return "[]";
            }
        }

        /// <summary>
        /// Update stage tracking information
        /// </summary>
        private Task UpdateStageTrackingInfoAsync(Onboarding entity)
            => _helperService.UpdateStageTrackingInfoAsync(entity);

        /// <summary>
        /// Sync Onboarding fields to Static Field Values when Onboarding is updated
        /// </summary>
        private Task SyncStaticFieldValuesAsync(
            long onboardingId,
            long stageId,
            string originalLeadId,
            string originalCaseName,
            string originalContactPerson,
            string originalContactEmail,
            string originalLeadPhone,
            long? originalLifeCycleStageId,
            string originalPriority,
            OnboardingInputDto input)
            => _helperService.SyncStaticFieldValuesAsync(
                onboardingId, stageId, originalLeadId, originalCaseName, originalContactPerson,
                originalContactEmail, originalLeadPhone, originalLifeCycleStageId, originalPriority, input);

        /// <summary>
        /// Get client IP address from HTTP context
        /// </summary>
        private string GetClientIpAddress()
            => _helperService.GetClientIpAddress();

        /// <summary>
        /// Get user agent from HTTP context
        /// </summary>
        private string GetUserAgent()
            => _helperService.GetUserAgent();

        /// <summary>
        /// Serialize stages progress to JSON
        /// </summary>
        private string SerializeStagesProgress(List<OnboardingStageProgressDto> stagesProgress)
            => _helperService.SerializeStagesProgress(stagesProgress);

        // Note: NormalizeToStartOfDay and GetCurrentUserEmail are defined in OnboardingService.StageProgress.cs
    }
}
