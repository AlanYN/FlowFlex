using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// User invitation repository interface
    /// </summary>
    public interface IUserInvitationRepository : IOwBaseRepository<UserInvitation>
    {
        /// <summary>
        /// Get invitation by token
        /// </summary>
        /// <param name="token">Invitation token</param>
        /// <returns>User invitation</returns>
        Task<UserInvitation> GetByTokenAsync(string token);

        /// <summary>
        /// Get invitations by onboarding ID
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>List of user invitations</returns>
        Task<List<UserInvitation>> GetByOnboardingIdAsync(long onboardingId);

        /// <summary>
        /// Get invitation by email and onboarding ID
        /// </summary>
        /// <param name="email">Email address</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>User invitation</returns>
        Task<UserInvitation> GetByEmailAndOnboardingIdAsync(string email, long onboardingId);

        /// <summary>
        /// Check if invitation exists for email and onboarding
        /// </summary>
        /// <param name="email">Email address</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>Whether invitation exists</returns>
        Task<bool> ExistsAsync(string email, long onboardingId);

        /// <summary>
        /// Update invitation status
        /// </summary>
        /// <param name="id">Invitation ID</param>
        /// <param name="status">New status</param>
        /// <returns>Whether update was successful</returns>
        Task<bool> UpdateStatusAsync(long id, string status);

        /// <summary>
        /// Mark invitation as used
        /// </summary>
        /// <param name="token">Invitation token</param>
        /// <param name="userId">User ID</param>
        /// <returns>Whether update was successful</returns>
        Task<bool> MarkAsUsedAsync(string token, long userId);
    }
} 