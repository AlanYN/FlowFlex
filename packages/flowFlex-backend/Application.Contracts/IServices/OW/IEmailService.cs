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
    }
}
