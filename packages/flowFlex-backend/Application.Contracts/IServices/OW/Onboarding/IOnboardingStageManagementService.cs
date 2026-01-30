using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW.Onboarding
{
    /// <summary>
    /// Service interface for managing onboarding stage operations
    /// Handles: MoveToNextStage, MoveToStage, CompleteCurrentStage, stage validation
    /// </summary>
    public interface IOnboardingStageManagementService : IScopedService
    {
        /// <summary>
        /// Move onboarding to the next stage in sequence
        /// </summary>
        /// <param name="id">Onboarding ID</param>
        /// <returns>True if successful</returns>
        Task<bool> MoveToNextStageAsync(long id);

        /// <summary>
        /// Move onboarding to a specific stage
        /// </summary>
        /// <param name="id">Onboarding ID</param>
        /// <param name="input">Stage move input with target stage ID</param>
        /// <returns>True if successful</returns>
        Task<bool> MoveToStageAsync(long id, MoveToStageInputDto input);

        /// <summary>
        /// Complete the current stage with validation (public API with permission check)
        /// </summary>
        /// <param name="id">Onboarding ID</param>
        /// <param name="input">Stage completion input</param>
        /// <returns>True if successful</returns>
        Task<bool> CompleteCurrentStageAsync(long id, CompleteCurrentStageInputDto input);

        /// <summary>
        /// Complete the current stage internally (no permission check, for system use)
        /// </summary>
        /// <param name="id">Onboarding ID</param>
        /// <param name="input">Stage completion input</param>
        /// <returns>True if successful</returns>
        Task<bool> CompleteCurrentStageInternalAsync(long id, CompleteCurrentStageInputDto input);

        /// <summary>
        /// Validate if a stage can be completed
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID to validate</param>
        /// <returns>Tuple of (canComplete, errorMessage)</returns>
        Task<(bool CanComplete, string ErrorMessage)> ValidateStageCanBeCompletedAsync(long onboardingId, long stageId);

        /// <summary>
        /// Evaluate and execute stage condition actions after stage completion
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Completed stage ID</param>
        /// <returns>True if condition actions were executed</returns>
        Task<bool> EvaluateAndExecuteStageConditionAsync(long onboardingId, long stageId);
    }
}
