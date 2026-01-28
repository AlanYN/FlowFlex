using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Onboarding service - Stage management operations (delegates to OnboardingStageManagementService)
    /// </summary>
    public partial class OnboardingService
    {
        /// <summary>
        /// Move onboarding to the next stage in sequence
        /// </summary>
        public Task<bool> MoveToNextStageAsync(long id)
            => _stageManagementService.MoveToNextStageAsync(id);

        /// <summary>
        /// Move onboarding to a specific stage
        /// </summary>
        public Task<bool> MoveToStageAsync(long id, MoveToStageInputDto input)
            => _stageManagementService.MoveToStageAsync(id, input);

        /// <summary>
        /// Complete the current stage with validation (public API with permission check)
        /// </summary>
        public Task<bool> CompleteCurrentStageAsync(long id, CompleteCurrentStageInputDto input)
            => _stageManagementService.CompleteCurrentStageAsync(id, input);

        /// <summary>
        /// Complete the current stage internally (no permission check, for system use)
        /// </summary>
        public Task<bool> CompleteCurrentStageInternalAsync(long id, CompleteCurrentStageInputDto input)
            => _stageManagementService.CompleteCurrentStageInternalAsync(id, input);
    }
}
