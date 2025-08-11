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
                var subject = "ITEM WFE - Email Verification Code";
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
                            <p>Thank you for registering with ITEM WFE system. Please use the following verification code to complete your email verification:</p>
                            <div class='code'>{verificationCode}</div>
                            <p>This verification code is valid for {_emailOptions.VerificationCodeExpiryMinutes} minutes. Please complete the verification as soon as possible.</p>
                            <p>If you did not request this verification code, please ignore this email.</p>
                            <p>If you have any questions, please contact us at WFESupport@item.com</p>
                        </div>
                        <div class='footer'>
                            <p>This email was sent automatically by the system. Please do not reply.</p>
                            <p>&copy; {DateTime.UtcNow.Year} ITEM WFE. All rights reserved.</p>
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
                var subject = "Welcome to ITEM WFE!";
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
                            <h2>Welcome to ITEM WFE!</h2>
                        </div>
                        <div class='content'>
                            <p>Dear {username},</p>
                            <p>Welcome to ITEM WFE! Your email has been successfully verified.</p>
                            <p>You can now log in to the system using your email and password.</p>
                            <p>If you have any questions, please contact us at WFESupport@item.com</p>
                        </div>
                        <div class='footer'>
                            <p>This email was sent automatically by the system. Please do not reply.</p>
                            <p>&copy; {DateTime.UtcNow.Year} ITEM WFE. All rights reserved.</p>
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
                var subject = "ITEM WFE - Customer Portal Access Invitation";
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
                            <h2>Customer Portal Invitation</h2>
                        </div>
                        <div class='content'>
                            <p>Dear Customer,</p>
                            <p>You have been invited to access the Customer Portal for <strong>{onboardingName}</strong>.</p>
                            
                            <div class='info-box'>
                                <p><strong>What you can do in the Customer Portal:</strong></p>
                                <ul>
                                    <li>Track your Customer Portal progress</li>
                                    <li>Upload required documents</li>
                                    <li>Communicate with your account manager</li>
                                    <li>Access important updates and notifications</li>
                                </ul>
                            </div>
                            
                            <p>Click the button below to access your portal:</p>
                            <p style='text-align: center;'>
                                <a href='{invitationUrl}' class='button'>Access Customer Portal</a>
                            </p>
                            
                            <p><strong>Note:</strong> This invitation link is secure and will require email verification before granting access.</p>
                            
                            <p>Thank you for choosing ITEM WFE!</p>
                            <p>If you have any questions, please contact us at WFESupport@item.com</p>
                        </div>
                        <div class='footer'>
                            <p>This email was sent automatically by the system. Please do not reply.</p>
                            <p>&copy; {DateTime.UtcNow.Year} ITEM WFE. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

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
                var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 30px; border: 1px solid #ddd; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
                        .success-box {{ background-color: #d4edda; padding: 15px; border-left: 4px solid #28a745; margin: 20px 0; border-radius: 5px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Password Reset Confirmation</h2>
                        </div>
                        <div class='content'>
                            <p>Dear {username},</p>
                            
                            <div class='success-box'>
                                <p><strong>✅ Password Reset Successful!</strong></p>
                                <p>Your password has been successfully reset for your ITEM WFE Customer Portal account.</p>
                            </div>
                            
                            <p><strong>Account Details:</strong></p>
                            <ul>
                                <li>Email: {to}</li>
                                <li>Reset Time: {DateTime.UtcNow:MM/dd/yyyy HH:mm:ss} UTC</li>
                            </ul>
                            
                            <p>You can now log in to the Customer Portal using your new password.</p>
                            
                            <p><strong>Security Notice:</strong></p>
                            <ul>
                                <li>If you did not request this password reset, please contact our support team immediately</li>
                                <li>For security reasons, please do not share your login credentials with anyone</li>
                                <li>We recommend using a strong, unique password</li>
                            </ul>
                            
                            <p>Thank you for using ITEM WFE!</p>
                            <p>If you have any questions, please contact us at WFESupport@item.com</p>
                        </div>
                        <div class='footer'>
                            <p>This email was sent automatically by the system. Please do not reply.</p>
                            <p>&copy; {DateTime.UtcNow.Year} ITEM WFE. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

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