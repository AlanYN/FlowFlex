using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW.Onboarding
{
    /// <summary>
    /// Service interface for onboarding status management operations
    /// Handles status transitions: Start, Pause, Resume, Cancel, Abort, Reactivate, ForceComplete
    /// </summary>
    public interface IOnboardingStatusService : IScopedService
    {
        /// <summary>
        /// Start onboarding (activate an inactive onboarding)
        /// </summary>
        /// <param name="id">Onboarding ID</param>
        /// <param name="input">Start input with optional reason and notes</param>
        /// <returns>Success status</returns>
        Task<bool> StartOnboardingAsync(long id, StartOnboardingInputDto input);

        /// <summary>
        /// Pause onboarding
        /// </summary>
        /// <param name="id">Onboarding ID</param>
        /// <returns>Success status</returns>
        Task<bool> PauseAsync(long id);

        /// <summary>
        /// Resume onboarding from paused state
        /// </summary>
        /// <param name="id">Onboarding ID</param>
        /// <returns>Success status</returns>
        Task<bool> ResumeAsync(long id);

        /// <summary>
        /// Resume onboarding with confirmation and additional details
        /// </summary>
        /// <param name="id">Onboarding ID</param>
        /// <param name="input">Resume input with reason and notes</param>
        /// <returns>Success status</returns>
        Task<bool> ResumeWithConfirmationAsync(long id, ResumeOnboardingInputDto input);

        /// <summary>
        /// Cancel onboarding
        /// </summary>
        /// <param name="id">Onboarding ID</param>
        /// <param name="reason">Cancellation reason</param>
        /// <returns>Success status</returns>
        Task<bool> CancelAsync(long id, string reason);

        /// <summary>
        /// Abort onboarding (terminate the process)
        /// </summary>
        /// <param name="id">Onboarding ID</param>
        /// <param name="input">Abort input with reason and notes</param>
        /// <returns>Success status</returns>
        Task<bool> AbortAsync(long id, AbortOnboardingInputDto input);

        /// <summary>
        /// Reactivate onboarding (restart an aborted onboarding)
        /// </summary>
        /// <param name="id">Onboarding ID</param>
        /// <param name="input">Reactivate input with reason and notes</param>
        /// <returns>Success status</returns>
        Task<bool> ReactivateAsync(long id, ReactivateOnboardingInputDto input);

        /// <summary>
        /// Reject onboarding application
        /// </summary>
        /// <param name="id">Onboarding ID</param>
        /// <param name="input">Reject input with reason and options</param>
        /// <returns>Success status</returns>
        Task<bool> RejectAsync(long id, RejectOnboardingInputDto input);

        /// <summary>
        /// Force complete onboarding (bypass normal validation)
        /// </summary>
        /// <param name="id">Onboarding ID</param>
        /// <param name="input">Force complete input with reason and notes</param>
        /// <returns>Success status</returns>
        Task<bool> ForceCompleteAsync(long id, ForceCompleteOnboardingInputDto input);

        /// <summary>
        /// Assign onboarding to user
        /// </summary>
        /// <param name="id">Onboarding ID</param>
        /// <param name="input">Assignment input</param>
        /// <returns>Success status</returns>
        Task<bool> AssignAsync(long id, AssignOnboardingInputDto input);

        /// <summary>
        /// Update completion rate based on stage progress
        /// </summary>
        /// <param name="id">Onboarding ID</param>
        /// <returns>Success status</returns>
        Task<bool> UpdateCompletionRateAsync(long id);
    }
}
