using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.Options;
using FlowFlex.Domain;
using FlowFlex.Domain.Shared;
using FlowFlex.Infrastructure.Services;
using FlowFlex.Application.Contracts.IServices;

namespace FlowFlex.Application.Services.MessageCenter
{
    /// <summary>
    /// Email service implementation
    /// </summary>
    public class EmailService : IEmailService, IScopedService
    {
        private readonly EmailOptions _emailOptions;
        private readonly ILogger<EmailService> _logger;
        private readonly IEmailTemplateService _templateService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EmailService(
            IOptions<EmailOptions> emailOptions,
            ILogger<EmailService> logger,
            IEmailTemplateService templateService,
            IHttpContextAccessor httpContextAccessor)
        {
            _emailOptions = emailOptions.Value;
            _logger = logger;
            _templateService = templateService;
            _httpContextAccessor = httpContextAccessor;
        }

        private string GetRequestOrigin()
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;
                if (context == null)
                {
                    return "https://crm-staging.item.com";
                }

                var request = context.Request;

                var scheme = request.Headers["X-Forwarded-Proto"].ToString();
                if (string.IsNullOrWhiteSpace(scheme))
                {
                    scheme = request.Scheme;
                }

                var forwardedHost = request.Headers["X-Forwarded-Host"].ToString();
                var host = !string.IsNullOrWhiteSpace(forwardedHost) ? forwardedHost : request.Host.Value;

                //// Include forwarded port if provided and not already present in host
                //var forwardedPort = request.Headers["X-Forwarded-Port"].ToString();
                //if (!string.IsNullOrWhiteSpace(forwardedPort) && host != null && !host.Contains(":"))
                //{
                //    var isDefaultPort = (scheme == "https" && forwardedPort == "443") || (scheme == "http" && forwardedPort == "80");
                //    if (!isDefaultPort)
                //    {
                //        host = $"{host}:{forwardedPort}";
                //    }
                //}

                var origin = $"{scheme}://{host}".TrimEnd('/');
                return string.IsNullOrWhiteSpace(origin) ? "https://crm-staging.item.com" : origin;
            }
            catch
            {
                return "https://crm-staging.item.com";
            }
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
                var loginUrl = GetRequestOrigin();
                var body = _templateService.Render("welcome_en", new Dictionary<string, object>
                {
                    ["username"] = username,
                    ["email"] = to,
                    ["loginUrl"] = loginUrl,
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
        /// Send stage completed notification email
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="caseId">Case ID</param>
        /// <param name="caseName">Case name</param>
        /// <param name="stageName">Completed stage name</param>
        /// <param name="nextStageName">Next stage name (optional)</param>
        /// <param name="completedBy">User who completed the stage</param>
        /// <param name="completionTime">Stage completion time</param>
        /// <param name="caseUrl">URL to view case details</param>
        /// <returns>Whether the email was sent successfully</returns>
        public async Task<bool> SendStageCompletedNotificationAsync(string to, string caseId, string caseName, string stageName, string nextStageName, string completedBy, string completionTime, string caseUrl)
        {
            try
            {
                // Format subject: [Case XXX] Previous Stage Completed – Action Required
                var subject = $"[Case {caseName}] Previous Stage Completed – Action Required";
                
                // Format next stage text
                var nextStageNameText = string.IsNullOrWhiteSpace(nextStageName) 
                    ? "" 
                    : $"\"{nextStageName}\"";

                var body = _templateService.Render("stage_completed_notification_en", new Dictionary<string, object>
                {
                    ["caseId"] = caseId ?? string.Empty,
                    ["caseName"] = caseName ?? string.Empty,
                    ["stageName"] = stageName ?? string.Empty,
                    ["nextStageName"] = nextStageName ?? string.Empty,
                    ["nextStageNameText"] = nextStageNameText,
                    ["completedBy"] = completedBy ?? string.Empty,
                    ["completionTime"] = completionTime ?? string.Empty,
                    ["caseUrl"] = caseUrl ?? GetRequestOrigin(),
                    ["year"] = DateTime.UtcNow.Year.ToString()
                });

                return await SendEmailAsync(to, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send stage completed notification email: {Message}", ex.Message);
                return false;
            }
        }

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
        public async Task<bool> SendConditionStageNotificationAsync(string to, string caseId, string caseName, string previousStageName, string currentStageName, string caseUrl)
        {
            try
            {
                // Format subject: [Case XXX] Stage Update – Action Required
                var subject = $"[Case {caseName}] Stage Update – Action Required";

                var body = _templateService.Render("condition_stage_notification_en", new Dictionary<string, object>
                {
                    ["caseId"] = caseId ?? string.Empty,
                    ["caseName"] = caseName ?? string.Empty,
                    ["previousStageName"] = previousStageName ?? string.Empty,
                    ["currentStageName"] = currentStageName ?? string.Empty,
                    ["caseUrl"] = caseUrl ?? GetRequestOrigin(),
                    ["year"] = DateTime.UtcNow.Year.ToString()
                });

                return await SendEmailAsync(to, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send condition stage notification email: {Message}", ex.Message);
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