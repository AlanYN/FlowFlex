using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW.Onboarding
{
    /// <summary>
    /// Service for managing onboarding stage progress operations
    /// Handles stage progress initialization, updates, validation, and serialization
    /// </summary>
    public interface IOnboardingStageProgressService : IScopedService
    {
        /// <summary>
        /// Initialize stages progress for a new onboarding entity
        /// </summary>
        /// <param name="entity">Onboarding entity to initialize</param>
        /// <param name="stages">List of stages from workflow</param>
        Task InitializeStagesProgressAsync(Domain.Entities.OW.Onboarding entity, List<Stage> stages);

        /// <summary>
        /// Update stages progress when a stage is completed
        /// </summary>
        /// <param name="entity">Onboarding entity</param>
        /// <param name="completedStageId">ID of the completed stage</param>
        /// <param name="completedBy">Name of the user who completed the stage</param>
        /// <param name="completedById">ID of the user who completed the stage</param>
        /// <param name="notes">Optional notes for the completion</param>
        Task UpdateStagesProgressAsync(Domain.Entities.OW.Onboarding entity, long completedStageId, string? completedBy = null, long? completedById = null, string? notes = null);

        /// <summary>
        /// Load stages progress from JSON string
        /// </summary>
        /// <param name="entity">Onboarding entity</param>
        void LoadStagesProgressFromJson(Domain.Entities.OW.Onboarding entity);

        /// <summary>
        /// Load stages progress from JSON string (read-only version, no fixes applied)
        /// </summary>
        /// <param name="entity">Onboarding entity</param>
        void LoadStagesProgressFromJsonReadOnly(Domain.Entities.OW.Onboarding entity);

        /// <summary>
        /// Validate if a stage can be completed based on business rules
        /// </summary>
        /// <param name="entity">Onboarding entity</param>
        /// <param name="stageId">Stage ID to validate</param>
        /// <returns>Tuple of (CanComplete, ErrorMessage)</returns>
        Task<(bool CanComplete, string ErrorMessage)> ValidateStageCanBeCompletedAsync(Domain.Entities.OW.Onboarding entity, long stageId);

        /// <summary>
        /// Calculate completion rate based on completed stages count
        /// </summary>
        /// <param name="stagesProgress">List of stage progress entries</param>
        /// <returns>Completion rate as percentage</returns>
        decimal CalculateCompletionRateByCompletedStages(List<OnboardingStageProgress> stagesProgress);

        /// <summary>
        /// Enrich stages progress with data from Stage entities
        /// </summary>
        /// <param name="entity">Onboarding entity</param>
        /// <param name="stages">List of stages from workflow</param>
        void EnrichStagesProgressWithStageData(Domain.Entities.OW.Onboarding entity, List<Stage> stages);

        /// <summary>
        /// Enrich stages progress with data from Stage entities (async version)
        /// </summary>
        /// <param name="entity">Onboarding entity</param>
        Task EnrichStagesProgressWithStageDataAsync(Domain.Entities.OW.Onboarding entity);

        /// <summary>
        /// Sync stages progress with workflow stages - handle new stages addition
        /// </summary>
        /// <param name="entity">Onboarding entity</param>
        /// <param name="preloadedStages">Optional preloaded stages</param>
        Task SyncStagesProgressWithWorkflowAsync(Domain.Entities.OW.Onboarding entity, List<Stage>? preloadedStages = null);

        /// <summary>
        /// Serialize stages progress to JSON string
        /// </summary>
        /// <param name="stagesProgress">List of stage progress entries</param>
        /// <returns>JSON string</returns>
        string SerializeStagesProgress(List<OnboardingStageProgress> stagesProgress);

        /// <summary>
        /// Ensure stages progress is properly initialized and synced with workflow
        /// </summary>
        /// <param name="entity">Onboarding entity</param>
        /// <param name="preloadedStages">Optional preloaded stages</param>
        Task EnsureStagesProgressInitializedAsync(Domain.Entities.OW.Onboarding entity, IEnumerable<Stage>? preloadedStages = null);

        /// <summary>
        /// Filter valid stages progress, keeping only stages that exist in current workflow
        /// </summary>
        /// <param name="entity">Onboarding entity</param>
        Task FilterValidStagesProgressAsync(Domain.Entities.OW.Onboarding entity);

        /// <summary>
        /// Check if stage order needs to be fixed
        /// </summary>
        /// <param name="stagesProgress">List of stage progress entries</param>
        /// <returns>True if fix is needed</returns>
        bool NeedsStageOrderFix(List<OnboardingStageProgress> stagesProgress);

        /// <summary>
        /// Fix stage order to be sequential (1, 2, 3, 4, 5...)
        /// </summary>
        /// <param name="stagesProgress">List of stage progress entries</param>
        void FixStageOrderSequence(List<OnboardingStageProgress> stagesProgress);

        /// <summary>
        /// Parse DefaultAssignee JSON string to List of user IDs
        /// </summary>
        /// <param name="defaultAssigneeJson">JSON string</param>
        /// <returns>List of user IDs</returns>
        List<string> ParseDefaultAssignee(string defaultAssigneeJson);

        /// <summary>
        /// Get CoAssignees filtered to exclude any IDs already in DefaultAssignee
        /// </summary>
        /// <param name="coAssigneesJson">CoAssignees JSON string</param>
        /// <param name="defaultAssigneeJson">DefaultAssignee JSON string</param>
        /// <returns>Filtered list of co-assignee IDs</returns>
        List<string> GetFilteredCoAssignees(string coAssigneesJson, string defaultAssigneeJson);
    }
}
