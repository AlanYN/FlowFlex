using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Onboarding service - Status operations
    /// Delegates to OnboardingStatusService for actual implementation
    /// </summary>
    public partial class OnboardingService
    {
        /// <summary>
        /// Start onboarding - delegates to OnboardingStatusService
        /// </summary>
        public Task<bool> StartOnboardingAsync(long id, StartOnboardingInputDto input)
            => _statusService.StartOnboardingAsync(id, input);

        /// <summary>
        /// Pause onboarding - delegates to OnboardingStatusService
        /// </summary>
        public Task<bool> PauseAsync(long id)
            => _statusService.PauseAsync(id);

        /// <summary>
        /// Resume onboarding - delegates to OnboardingStatusService
        /// </summary>
        public Task<bool> ResumeAsync(long id)
            => _statusService.ResumeAsync(id);

        /// <summary>
        /// Resume with confirmation - delegates to OnboardingStatusService
        /// </summary>
        public Task<bool> ResumeWithConfirmationAsync(long id, ResumeOnboardingInputDto input)
            => _statusService.ResumeWithConfirmationAsync(id, input);

        /// <summary>
        /// Cancel onboarding - delegates to OnboardingStatusService
        /// </summary>
        public Task<bool> CancelAsync(long id, string reason)
            => _statusService.CancelAsync(id, reason);

        /// <summary>
        /// Abort onboarding - delegates to OnboardingStatusService
        /// </summary>
        public Task<bool> AbortAsync(long id, AbortOnboardingInputDto input)
            => _statusService.AbortAsync(id, input);

        /// <summary>
        /// Reactivate onboarding - delegates to OnboardingStatusService
        /// </summary>
        public Task<bool> ReactivateAsync(long id, ReactivateOnboardingInputDto input)
            => _statusService.ReactivateAsync(id, input);

        /// <summary>
        /// Reject onboarding - delegates to OnboardingStatusService
        /// </summary>
        public Task<bool> RejectAsync(long id, RejectOnboardingInputDto input)
            => _statusService.RejectAsync(id, input);

        /// <summary>
        /// Force complete onboarding - delegates to OnboardingStatusService
        /// </summary>
        public Task<bool> ForceCompleteAsync(long id, ForceCompleteOnboardingInputDto input)
            => _statusService.ForceCompleteAsync(id, input);

        /// <summary>
        /// Assign onboarding - delegates to OnboardingStatusService
        /// </summary>
        public Task<bool> AssignAsync(long id, AssignOnboardingInputDto input)
            => _statusService.AssignAsync(id, input);

        /// <summary>
        /// Update completion rate - delegates to OnboardingStatusService
        /// </summary>
        public Task<bool> UpdateCompletionRateAsync(long id)
            => _statusService.UpdateCompletionRateAsync(id);
    }
}
