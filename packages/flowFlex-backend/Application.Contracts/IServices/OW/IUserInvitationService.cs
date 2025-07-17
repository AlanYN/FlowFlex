using FlowFlex.Application.Contracts.Dtos.OW;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// User invitation service interface
    /// </summary>
    public interface IUserInvitationService
    {
        /// <summary>
        /// Send invitations for onboarding portal access
        /// </summary>
        /// <param name="request">Invitation request</param>
        /// <returns>Invitation response</returns>
        Task<UserInvitationResponseDto> SendInvitationsAsync(UserInvitationRequestDto request);

        /// <summary>
        /// Get portal users for onboarding
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>List of portal users</returns>
        Task<List<PortalUserDto>> GetPortalUsersAsync(long onboardingId);

        /// <summary>
        /// Verify portal access with invitation token
        /// </summary>
        /// <param name="request">Verification request</param>
        /// <returns>Verification response</returns>
        Task<PortalAccessVerificationResponseDto> VerifyPortalAccessAsync(PortalAccessVerificationRequestDto request);

        /// <summary>
        /// Resend invitation
        /// </summary>
        /// <param name="request">Resend invitation request</param>
        /// <returns>Whether resend was successful</returns>
        Task<bool> ResendInvitationAsync(ResendInvitationRequestDto request);

        /// <summary>
        /// Remove portal access
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="email">Email address</param>
        /// <returns>Whether removal was successful</returns>
        Task<bool> RemovePortalAccessAsync(long onboardingId, string email);

        /// <summary>
        /// Validate invitation token
        /// </summary>
        /// <param name="token">Invitation token</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>Token validation result</returns>
        Task<TokenValidationResponseDto> ValidateTokenAsync(string token, long onboardingId);
    }
}