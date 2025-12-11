using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.Dtos.OW.User;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Onboarding service interface
    /// </summary>
    public interface IOnboardingService : IScopedService
    {
        /// <summary>
        /// Create a new onboarding instance
        /// </summary>
        Task<long> CreateAsync(OnboardingInputDto input);

        /// <summary>
        /// Update an existing onboarding
        /// </summary>
        Task<bool> UpdateAsync(long id, OnboardingInputDto input);

        /// <summary>
        /// Delete an onboarding (with confirmation)
        /// </summary>
        Task<bool> DeleteAsync(long id, bool confirm = false);

        /// <summary>
        /// Get onboarding by ID
        /// </summary>
        Task<OnboardingOutputDto?> GetByIdAsync(long id);

        /// <summary>
        /// Query onboarding with pagination
        /// </summary>
        Task<PageModelDto<OnboardingOutputDto>> QueryAsync(OnboardingQueryRequest request);

        /// <summary>
        /// Move to next stage
        /// </summary>
        Task<bool> MoveToNextStageAsync(long id);

        /// <summary>
        /// Move to specific stage
        /// </summary>
        Task<bool> MoveToStageAsync(long id, MoveToStageInputDto input);

        /// <summary>
        /// Complete current stage with validation - Internal version without event publishing
        /// </summary>
        Task<bool> CompleteCurrentStageInternalAsync(long id, CompleteCurrentStageInputDto input);

        /// <summary>
        /// Complete current stage with validation
        /// </summary>
        Task<bool> CompleteCurrentStageAsync(long id, CompleteCurrentStageInputDto input);

        /// <summary>
        /// Pause onboarding
        /// </summary>
        Task<bool> PauseAsync(long id);

        /// <summary>
        /// Resume onboarding
        /// </summary>
        Task<bool> ResumeAsync(long id);

        /// <summary>
        /// Cancel onboarding
        /// </summary>
        Task<bool> CancelAsync(long id, string reason);

        /// <summary>
        /// Reject onboarding application
        /// </summary>
        Task<bool> RejectAsync(long id, RejectOnboardingInputDto input);

        /// <summary>
        /// Assign onboarding to user
        /// </summary>
        Task<bool> AssignAsync(long id, AssignOnboardingInputDto input);

        /// <summary>
        /// Update completion rate
        /// </summary>
        Task<bool> UpdateCompletionRateAsync(long id);

        /// <summary>
        /// Get onboarding progress
        /// </summary>
        Task<OnboardingProgressDto> GetProgressAsync(long id);

        /// <summary>
        /// Export onboarding data to Excel
        /// </summary>
        Task<Stream> ExportToExcelAsync(OnboardingQueryRequest query);

        /// <summary>
        /// Update AI Summary for a specific stage in onboarding's stagesProgress
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <param name="aiSummary">AI Summary content</param>
        /// <param name="generatedAt">Generated timestamp</param>
        /// <param name="confidence">Confidence score</param>
        /// <param name="modelUsed">AI model used</param>
        /// <returns>Success status</returns>
        Task<bool> UpdateOnboardingStageAISummaryAsync(long onboardingId, long stageId, string aiSummary, DateTime generatedAt, double? confidence, string modelUsed);

        /// <summary>
        /// Update custom fields for a specific stage in onboarding's stagesProgress
        /// Updates CustomEstimatedDays and CustomEndTime fields
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="input">Update stage custom fields input</param>
        /// <returns>Success status</returns>
        Task<bool> UpdateStageCustomFieldsAsync(long onboardingId, UpdateStageCustomFieldsInputDto input);

        /// <summary>
        /// Save a specific stage in onboarding's stagesProgress
        /// Updates the stage's IsSaved, SaveTime, and SavedById fields
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID to save</param>
        /// <returns>Success status</returns>
        Task<bool> SaveStageAsync(long onboardingId, long stageId);

        /// <summary>
        /// Start onboarding (activate an inactive onboarding)
        /// </summary>
        Task<bool> StartOnboardingAsync(long id, StartOnboardingInputDto input);


        /// <summary>
        /// Abort onboarding (terminate the process)
        /// </summary>
        Task<bool> AbortAsync(long id, AbortOnboardingInputDto input);

        /// <summary>
        /// Reactivate onboarding (restart an aborted onboarding)
        /// </summary>
        Task<bool> ReactivateAsync(long id, ReactivateOnboardingInputDto input);

        /// <summary>
        /// Resume onboarding with confirmation
        /// </summary>
        Task<bool> ResumeWithConfirmationAsync(long id, ResumeOnboardingInputDto input);

        /// <summary>
        /// Force complete onboarding (bypass normal validation and set to Force Completed status)
        /// </summary>
        Task<bool> ForceCompleteAsync(long id, ForceCompleteOnboardingInputDto input);

        /// <summary>
        /// Get authorized users for onboarding based on permission configuration
        /// If case has no permission restrictions (Public mode), returns all users
        /// If case has permission restrictions, returns only authorized users based on ViewPermissionMode and ViewPermissionSubjectType
        /// </summary>
        /// <param name="id">Onboarding ID</param>
        /// <returns>Tree structure with authorized teams and users</returns>
        Task<List<UserTreeNodeDto>> GetAuthorizedUsersAsync(long id);

        /// <summary>
        /// Get all active onboardings by System ID
        /// Returns all onboarding records where SystemId matches and IsActive is true
        /// </summary>
        /// <param name="systemId">External system identifier</param>
        /// <returns>List of active onboarding records</returns>
        Task<List<OnboardingOutputDto>> GetActiveBySystemIdAsync(string systemId);
    }
}
