using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.Options;
using FlowFlex.Domain;
using FlowFlex.Domain.Shared;
using FlowFlex.Infrastructure.Services;
using FlowFlex.Application.Contracts.IServices;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Email service implementation
    /// </summary>
    public class EmailService : IEmailService, IScopedService
    {
        private readonly EmailOptions _emailOptions;
        private readonly ILogger<EmailService> _logger;
        private readonly IEmailTemplateService _templateService;

        public EmailService(IOptions<EmailOptions> emailOptions, ILogger<EmailService> logger, IEmailTemplateService templateService)
        {
            _emailOptions = emailOptions.Value;
            _logger = logger;
            _templateService = templateService;
        }

        /// <summary>
        /// Send verification code email
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="verificationCode">Verification code</param>
        /// <returns>Whether the email was sent successfully</returns>
        public async Task<bool> SendVerificationCodeEmailAsync(string to, string verificationCode)
        {
            try
            {
                var subject = "ITEM WFE - Email Verification Code";
                var body = _templateService.Render("verification_code_en", new Dictionary<string, object>
                {
                    ["verificationCode"] = verificationCode,
                    ["expiryMinutes"] = _emailOptions.VerificationCodeExpiryMinutes.ToString(),
                    ["year"] = DateTime.UtcNow.Year.ToString()
                });

                return await SendEmailAsync(to, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification code email: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Send welcome email
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="username">Username</param>
        /// <returns>Whether the email was sent successfully</returns>
        public async Task<bool> SendWelcomeEmailAsync(string to, string username)
        {
            try
            {
                var subject = "Welcome to ITEM WFE!";
                var body = _templateService.Render("welcome_en", new Dictionary<string, object>
                {
                    ["username"] = username,
                    ["email"] = to,
                    ["loginUrl"] = "https://crm-staging.item.com",
                    ["year"] = DateTime.UtcNow.Year.ToString()
                });

                return await SendEmailAsync(to, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Send onboarding invitation email
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="invitationUrl">Invitation URL</param>
        /// <param name="onboardingName">Onboarding name</param>
        /// <returns>Whether the email was sent successfully</returns>
        public async Task<bool> SendOnboardingInvitationEmailAsync(string to, string invitationUrl, string onboardingName)
        {
            try
            {
                var subject = "ITEM WFE - Customer Portal Access Invitation";
                var body = _templateService.Render("portal_invitation_en", new Dictionary<string, object>
                {
                    ["recipientName"] = to,
                    ["onboardingName"] = onboardingName,
                    ["invitationUrl"] = invitationUrl,
                    ["year"] = DateTime.UtcNow.Year.ToString()
                });

                return await SendEmailAsync(to, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Customer Portal invitation email: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Send password reset confirmation email
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="username">Username</param>
        /// <returns>Whether the email was sent successfully</returns>
        public async Task<bool> SendPasswordResetConfirmationAsync(string to, string username)
        {
            try
            {
                var subject = "ITEM WFE - Password Reset Confirmation";
                var body = _templateService.Render("password_reset_confirmation_en", new Dictionary<string, object>
                {
                    ["username"] = username,
                    ["email"] = to,
                    ["resetTimeUtc"] = DateTime.UtcNow.ToString("MM/dd/yyyy HH:mm:ss") + " UTC",
                    ["year"] = DateTime.UtcNow.Year.ToString()
                });

                return await SendEmailAsync(to, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset confirmation email: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Send email
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email content</param>
        /// <returns>Whether the email was sent successfully</returns>
        private async Task<bool> SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                _logger.LogInformation("Preparing to send email: To={To}, Subject={Subject}, SmtpServer={SmtpServer}, Port={Port}",
                    to, subject, _emailOptions.SmtpServer, _emailOptions.SmtpPort);

                var message = new MailMessage
                {
                    From = new MailAddress(_emailOptions.FromEmail, _emailOptions.FromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };

                message.To.Add(new MailAddress(to));

                using var client = new SmtpClient(_emailOptions.SmtpServer, _emailOptions.SmtpPort)
                {
                    EnableSsl = _emailOptions.EnableSsl,
                    UseDefaultCredentials = false, // Ensure not to use default credentials
                    Credentials = new NetworkCredential(_emailOptions.Username, _emailOptions.Password),
                    DeliveryMethod = SmtpDeliveryMethod.Network, // Explicitly specify network transmission
                    Timeout = 30000 // 30 seconds timeout
                };

                _logger.LogInformation("SMTP client configuration: Server={Server}, Port={Port}, EnableSsl={EnableSsl}, Username={Username}",
                    _emailOptions.SmtpServer, _emailOptions.SmtpPort, _emailOptions.EnableSsl, _emailOptions.Username);

                await client.SendMailAsync(message);

                _logger.LogInformation("Email sent successfully: To={To}, Subject={Subject}", to, subject);
                return true;
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "SMTP error sending email failed: To={To}, Subject={Subject}, StatusCode={StatusCode}, Message={Message}",
                    to, subject, smtpEx.StatusCode, smtpEx.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sending email failed: To={To}, Subject={Subject}, Message={Message}", to, subject, ex.Message);
                return false;
            }
        }
    }
}