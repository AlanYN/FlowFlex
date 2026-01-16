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

        /// <summary>
        /// Send password reset confirmation email
        /// </summary>
        /// <param name="to">Recipient email</param>
        /// <param name="username">Username</param>
        /// <returns>Whether the email was sent successfully</returns>
        Task<bool> SendPasswordResetConfirmationAsync(string to, string username);

        /// <summary>
        /// Send stage completed notification email
        /// </summary>
        /// <param name="to">Recipient email</param>
        /// <param name="caseId">Case ID</param>
        /// <param name="caseName">Case name</param>
        /// <param name="stageName">Completed stage name</param>
        /// <param name="nextStageName">Next stage name (optional)</param>
        /// <param name="completedBy">User who completed the stage</param>
        /// <param name="completionTime">Stage completion time</param>
        /// <param name="caseUrl">URL to view case details</param>
        /// <returns>Whether the email was sent successfully</returns>
        Task<bool> SendStageCompletedNotificationAsync(string to, string caseId, string caseName, string stageName, string nextStageName, string completedBy, string completionTime, string caseUrl);

        /// <summary>
        /// Send condition triggered stage notification email (for Stage Condition SendNotification action)
        /// Shows "current stage" instead of "next stage"
        /// </summary>
        /// <param name="to">Recipient email</param>
        /// <param name="caseId">Case ID</param>
        /// <param name="caseName">Case name</param>
        /// <param name="previousStageName">Previous stage name (the completed stage)</param>
        /// <param name="currentStageName">Current stage name (the stage to proceed with)</param>
        /// <param name="caseUrl">URL to view case details</param>
        /// <returns>Whether the email was sent successfully</returns>
        Task<bool> SendConditionStageNotificationAsync(string to, string caseId, string caseName, string previousStageName, string currentStageName, string caseUrl);
    }
}
