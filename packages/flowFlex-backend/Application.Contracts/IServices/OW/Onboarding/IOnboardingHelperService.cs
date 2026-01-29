using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Events;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.IServices.OW.Onboarding
{
    /// <summary>
    /// Service interface for onboarding helper and utility methods
    /// Handles: Event publishing, JSON parsing, component processing, utility methods
    /// </summary>
    public interface IOnboardingHelperService : IScopedService
    {
        /// <summary>
        /// Get onboarding progress
        /// </summary>
        Task<OnboardingProgressDto> GetProgressAsync(long id);

        /// <summary>
        /// Publish stage completion event for current stage completion
        /// </summary>
        Task PublishStageCompletionEventForCurrentStageAsync(
            Domain.Entities.OW.Onboarding onboarding,
            Stage completedStage,
            bool isFinalStage);

        /// <summary>
        /// Build components payload for stage completion event
        /// </summary>
        Task<StageCompletionComponents> BuildStageCompletionComponentsAsync(
            long onboardingId,
            long stageId,
            List<StageComponent> stageComponents,
            string componentsJson);

        /// <summary>
        /// Parse components from JSON with lenient parsing for both camelCase and PascalCase
        /// </summary>
        (List<StageComponent> stageComponents, List<string> staticFieldNames) ParseComponentsFromJson(string componentsJson);

        /// <summary>
        /// Parse JSON array that might be double-encoded
        /// </summary>
        List<string> ParseJsonArraySafe(string jsonString);

        /// <summary>
        /// Validates and formats JSON array string for PostgreSQL JSONB
        /// </summary>
        string ValidateAndFormatJsonArray(string jsonArray);

        /// <summary>
        /// Update stage tracking information
        /// </summary>
        Task UpdateStageTrackingInfoAsync(Domain.Entities.OW.Onboarding entity);

        /// <summary>
        /// Sync Onboarding fields to Static Field Values when Onboarding is updated
        /// </summary>
        Task SyncStaticFieldValuesAsync(
            long onboardingId,
            long stageId,
            string originalLeadId,
            string originalCaseName,
            string originalContactPerson,
            string originalContactEmail,
            string originalLeadPhone,
            long? originalLifeCycleStageId,
            string originalPriority,
            OnboardingInputDto input);

        /// <summary>
        /// Log general onboarding action to change log
        /// </summary>
        Task LogOnboardingActionAsync(
            Domain.Entities.OW.Onboarding onboarding,
            string action,
            string logType,
            bool success,
            object additionalData = null);

        /// <summary>
        /// Get client IP address from HTTP context
        /// </summary>
        string GetClientIpAddress();

        /// <summary>
        /// Get user agent from HTTP context
        /// </summary>
        string GetUserAgent();

        /// <summary>
        /// Serialize stages progress to JSON
        /// </summary>
        string SerializeStagesProgress(List<OnboardingStageProgressDto> stagesProgress);

        /// <summary>
        /// Normalize datetime to start of day (00:00:00)
        /// </summary>
        DateTimeOffset NormalizeToStartOfDay(DateTimeOffset dateTime);

        /// <summary>
        /// Get current user email from context
        /// </summary>
        string GetCurrentUserEmail();
    }
}
