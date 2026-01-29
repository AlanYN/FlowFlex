using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.Dtos.OW.User;
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

        /// <summary>
        /// Validate that team IDs from JSON arrays exist in the team tree from UserService.
        /// Throws BusinessError if any invalid IDs are found.
        /// </summary>
        /// <param name="viewTeamsJson">JSON array of view team IDs</param>
        /// <param name="operateTeamsJson">JSON array of operate team IDs</param>
        Task ValidateTeamSelectionsFromJsonAsync(string viewTeamsJson, string operateTeamsJson);

        /// <summary>
        /// Check if exception is a JSONB type error from PostgreSQL
        /// </summary>
        /// <param name="ex">Exception to check</param>
        /// <returns>True if the exception is related to JSONB type conversion</returns>
        bool IsJsonbTypeError(Exception ex);

        /// <summary>
        /// Safely append text to Notes field with length validation
        /// Ensures the total length doesn't exceed the database constraint (1000 characters)
        /// </summary>
        /// <param name="entity">Onboarding entity to update</param>
        /// <param name="noteText">Text to append to notes</param>
        void SafeAppendToNotes(Domain.Entities.OW.Onboarding entity, string noteText);

        /// <summary>
        /// Create default UserInvitation record without sending email
        /// </summary>
        /// <param name="onboarding">Onboarding entity to create invitation for</param>
        Task CreateDefaultUserInvitationAsync(Domain.Entities.OW.Onboarding onboarding);

        /// <summary>
        /// Ensure CaseCode exists for all entities (batch processing)
        /// </summary>
        /// <param name="entities">List of onboarding entities to ensure CaseCode for</param>
        Task EnsureCaseCodesAsync(List<Domain.Entities.OW.Onboarding> entities);

        /// <summary>
        /// Get all valid team IDs from user tree
        /// </summary>
        /// <returns>HashSet of all valid team IDs</returns>
        Task<HashSet<string>> GetAllTeamIdsFromUserTreeAsync();
    }
}
