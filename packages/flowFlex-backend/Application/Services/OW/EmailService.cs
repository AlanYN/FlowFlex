using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.Options;
using FlowFlex.Domain;
using FlowFlex.Domain.Shared;
using FlowFlex.Infrastructure.Services;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Email service implementation
    /// </summary>
    public class EmailService : IEmailService, IScopedService
    {
        private readonly EmailOptions _emailOptions;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailOptions> emailOptions, ILogger<EmailService> logger)
        {
            _emailOptions = emailOptions.Value;
            _logger = logger;
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
                var subject = "FlowFlex - Email Verification Code";
                var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #4a86e8; color: white; padding: 10px; text-align: center; }}
                        .content {{ padding: 20px; border: 1px solid #ddd; }}
                        .code {{ font-size: 24px; font-weight: bold; text-align: center; padding: 10px; background-color: #f5f5f5; margin: 20px 0; letter-spacing: 5px; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Email Verification Code</h2>
                        </div>
                        <div class='content'>
                            <p>Hello,</p>
                            <p>Thank you for registering with FlowFlex system. Please use the following verification code to complete your email verification:</p>
                            <div class='code'>{verificationCode}</div>
                            <p>This verification code is valid for {_emailOptions.VerificationCodeExpiryMinutes} minutes. Please complete the verification as soon as possible.</p>
                            <p>If you did not request this verification code, please ignore this email.</p>
                        </div>
                        <div class='footer'>
                            <p>This email was sent automatically by the system. Please do not reply.</p>
                            <p>&copy; {DateTime.Now.Year} FlowFlex. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

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
                var subject = "Welcome to FlowFlex!";
                var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #4a86e8; color: white; padding: 10px; text-align: center; }}
                        .content {{ padding: 20px; border: 1px solid #ddd; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Welcome to FlowFlex!</h2>
                        </div>
                        <div class='content'>
                            <p>Dear {username},</p>
                            <p>Welcome to FlowFlex! Your email has been successfully verified.</p>
                            <p>You can now log in to the system using your email and password.</p>
                            <p>If you have any questions, please feel free to contact our support team.</p>
                        </div>
                        <div class='footer'>
                            <p>This email was sent automatically by the system. Please do not reply.</p>
                            <p>&copy; {DateTime.Now.Year} FlowFlex. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

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
                var subject = "FlowFlex - Onboarding Portal Access Invitation";
                var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #4a86e8; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 30px; border: 1px solid #ddd; }}
                        .button {{ display: inline-block; padding: 12px 24px; background-color: #4a86e8; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; font-weight: bold; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
                        .info-box {{ background-color: #f8f9fa; padding: 15px; border-left: 4px solid #4a86e8; margin: 20px 0; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Onboarding Portal Access Invitation</h2>
                        </div>
                        <div class='content'>
                            <p>Hello,</p>
                            <p>You have been invited to access the FlowFlex onboarding portal to complete your onboarding process.</p>
                            
                            <div class='info-box'>
                                <strong>Onboarding Process:</strong> {onboardingName}
                            </div>
                            
                            <p>Please click the button below to access your onboarding portal:</p>
                            
                            <div style='text-align: center;'>
                                <a href='{invitationUrl}' class='button'>Access Onboarding Portal</a>
                            </div>
                            
                            <p>If the button doesn't work, you can copy and paste the following link into your browser:</p>
                            <p style='word-break: break-all; background-color: #f5f5f5; padding: 10px; border-radius: 3px;'>{invitationUrl}</p>
                            
                            <div class='info-box'>
                                <strong>What to expect:</strong><br>
                                • Verify your email address<br>
                                • Complete the onboarding questionnaire<br>
                                • Upload required documents<br>
                                • Track your progress through each stage
                            </div>
                            
                            <p>This invitation link is valid for 7 days. If you have any questions or need assistance, please contact our support team.</p>
                            
                            <p>Thank you for choosing FlowFlex!</p>
                        </div>
                        <div class='footer'>
                            <p>This email was sent automatically by the system. Please do not reply.</p>
                            <p>&copy; {DateTime.Now.Year} FlowFlex. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

                return await SendEmailAsync(to, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send onboarding invitation email: {Message}", ex.Message);
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