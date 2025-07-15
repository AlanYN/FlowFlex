using System.Threading.Tasks;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Email service interface
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Send verification code email
        /// </summary>
        /// <param name="to">Recipient</param>
        /// <param name="verificationCode">Verification code</param>
        /// <returns>Whether the email was sent successfully</returns>
        Task<bool> SendVerificationCodeEmailAsync(string to, string verificationCode);

        /// <summary>
        /// Send welcome email
        /// </summary>
        /// <param name="to">Recipient</param>
        /// <param name="username">Username</param>
        /// <returns>Whether the email was sent successfully</returns>
        Task<bool> SendWelcomeEmailAsync(string to, string username);

        /// <summary>
        /// Send onboarding invitation email
        /// </summary>
        /// <param name="to">Recipient email</param>
        /// <param name="invitationUrl">Invitation URL</param>
        /// <param name="onboardingName">Onboarding name</param>
        /// <returns>Whether the email was sent successfully</returns>
        Task<bool> SendOnboardingInvitationEmailAsync(string to, string invitationUrl, string onboardingName);
    }
}
