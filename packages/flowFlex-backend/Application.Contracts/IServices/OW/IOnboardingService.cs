using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
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
        Task<OnboardingOutputDto> GetByIdAsync(long id);

        /// <summary>
        /// Get onboarding list
        /// </summary>
        Task<List<OnboardingOutputDto>> GetListAsync();

        /// <summary>
        /// Query onboarding with pagination
        /// </summary>
        Task<PageModelDto<OnboardingOutputDto>> QueryAsync(OnboardingQueryRequest request);

        /// <summary>
        /// Move to next stage
        /// </summary>
        Task<bool> MoveToNextStageAsync(long id);

        /// <summary>
        /// Move to previous stage
        /// </summary>
        Task<bool> MoveToPreviousStageAsync(long id);

        /// <summary>
        /// Move to specific stage
        /// </summary>
        Task<bool> MoveToStageAsync(long id, MoveToStageInputDto input);

        /// <summary>
        /// Complete current stage
        /// </summary>
        Task<bool> CompleteCurrentStageAsync(long id);

        /// <summary>
        /// Complete current stage with validation - Internal version without event publishing
        /// </summary>
        Task<bool> CompleteCurrentStageInternalAsync(long id, CompleteCurrentStageInputDto input);

        /// <summary>
        /// Complete current stage with validation
        /// </summary>
        Task<bool> CompleteCurrentStageAsync(long id, CompleteCurrentStageInputDto input);

        /// <summary>
        /// Complete current stage with details
        /// </summary>
        Task<bool> CompleteStageAsync(long id, CompleteStageInputDto input);

        /// <summary>
        /// Complete onboarding
        /// </summary>
        Task<bool> CompleteAsync(long id);

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
        /// Get onboarding statistics
        /// </summary>
        Task<OnboardingStatisticsDto> GetStatisticsAsync();

        /// <summary>
        /// Update completion rate
        /// </summary>
        Task<bool> UpdateCompletionRateAsync(long id);

        /// <summary>
        /// Get overdue onboarding list
        /// </summary>
        Task<List<OnboardingOutputDto>> GetOverdueListAsync();

        /// <summary>
        /// Batch update status
        /// </summary>
        Task<bool> BatchUpdateStatusAsync(List<long> ids, string status);

        /// <summary>
        /// Set priority for onboarding (required for Stage 1 completion)
        /// </summary>
        Task<bool> SetPriorityAsync(long id, string priority);

        /// <summary>
        /// Get onboarding timeline
        /// </summary>
        Task<List<OnboardingTimelineDto>> GetTimelineAsync(long id);

        /// <summary>
        /// Add note to onboarding
        /// </summary>
        Task<bool> AddNoteAsync(long id, AddNoteInputDto input);

        /// <summary>
        /// Update onboarding status
        /// </summary>
        Task<bool> UpdateStatusAsync(long id, UpdateStatusInputDto input);

        /// <summary>
        /// Update onboarding priority
        /// </summary>
        Task<bool> UpdatePriorityAsync(long id, UpdatePriorityInputDto input);

        /// <summary>
        /// Complete onboarding with details
        /// </summary>
        Task<bool> CompleteAsync(long id, CompleteOnboardingInputDto input);

        /// <summary>
        /// Restart onboarding
        /// </summary>
        Task<bool> RestartAsync(long id, RestartOnboardingInputDto input);

        /// <summary>
        /// Get onboarding progress
        /// </summary>
        Task<OnboardingProgressDto> GetProgressAsync(long id);

        /// <summary>
        /// Check if leads have onboarding (batch operation)
        /// </summary>
        Task<Dictionary<string, bool>> BatchCheckLeadOnboardingAsync(List<string> leadIds);

        /// <summary>
        /// Export onboarding data to Excel
        /// </summary>
        Task<Stream> ExportToExcelAsync(OnboardingQueryRequest query);

        /// <summary>
        /// Sync stages progress from workflow stages configuration
        /// Updates VisibleInPortal and AttachmentManagementNeeded fields from stage definitions
        /// </summary>
        Task<bool> SyncStagesProgressAsync(long id);

        /// <summary>
        /// Query onboardings by stage status using JSONB operators
        /// Utilizes PostgreSQL JSONB querying capabilities for efficient filtering
        /// </summary>
        Task<List<OnboardingOutputDto>> QueryByStageStatusAsync(string status);

        /// <summary>
        /// Query onboardings by completion status using JSONB operators
        /// </summary>
        Task<List<OnboardingOutputDto>> QueryByCompletionStatusAsync(bool isCompleted);

        /// <summary>
        /// Query onboardings by specific stage ID using JSONB operators
        /// </summary>
        Task<List<OnboardingOutputDto>> QueryByStageIdAsync(long stageId);

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
    }
}
