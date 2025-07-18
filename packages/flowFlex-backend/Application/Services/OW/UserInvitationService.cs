using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Microsoft.Extensions.Logging;
using FlowFlex.Application.Contracts.Dtos.OW;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Infrastructure.Services;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// User invitation service implementation
    /// </summary>
    public class UserInvitationService : IUserInvitationService, IScopedService
    {
        private readonly IUserInvitationRepository _invitationRepository;
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly IJwtService _jwtService;
        private readonly IMapper _mapper;
        private readonly ILogger<UserInvitationService> _logger;
        private readonly IUserContextService _userContextService;

        public UserInvitationService(
            IUserInvitationRepository invitationRepository,
            IOnboardingRepository onboardingRepository,
            IUserRepository userRepository,
            IEmailService emailService,
            IJwtService jwtService,
            IMapper mapper,
            ILogger<UserInvitationService> logger,
            IUserContextService userContextService)
        {
            _invitationRepository = invitationRepository;
            _onboardingRepository = onboardingRepository;
            _userRepository = userRepository;
            _emailService = emailService;
            _jwtService = jwtService;
            _mapper = mapper;
            _logger = logger;
            _userContextService = userContextService;
        }

        /// <summary>
        /// Send invitations for onboarding portal access
        /// </summary>
        /// <param name="request">Invitation request</param>
        /// <returns>Invitation response</returns>
        public async Task<UserInvitationResponseDto> SendInvitationsAsync(UserInvitationRequestDto request)
        {
            var response = new UserInvitationResponseDto();

            // Validate onboarding exists
            var onboarding = await _onboardingRepository.GetByIdAsync(request.OnboardingId);
            if (onboarding == null)
            {
                throw new Exception($"Onboarding with ID {request.OnboardingId} not found");
            }

            foreach (var email in request.EmailAddresses)
            {
                try
                {
                    // Validate email format
                    if (!IsValidEmail(email))
                    {
                        response.FailedInvitations[email] = "Invalid email format";
                        continue;
                    }

                    // Check if invitation already exists
                    var existingInvitation = await _invitationRepository.GetByEmailAndOnboardingIdAsync(email, request.OnboardingId);
                    if (existingInvitation != null)
                    {
                        // Update existing invitation
                        existingInvitation.InvitationToken = GenerateInvitationToken();
                        existingInvitation.SentDate = DateTimeOffset.UtcNow;
                        existingInvitation.TokenExpiry = DateTimeOffset.UtcNow.AddDays(7);
                        existingInvitation.Status = "Pending";
                        existingInvitation.SendCount += 1;
                        existingInvitation.InvitationUrl = GenerateInvitationUrl(existingInvitation.InvitationToken, request.OnboardingId, request.BaseUrl);
                        existingInvitation.ModifyDate = DateTimeOffset.UtcNow;
                        existingInvitation.ModifyBy = _userContextService.GetCurrentUserEmail() ?? "System";

                        await _invitationRepository.UpdateAsync(existingInvitation);

                        // Send invitation email
                        var emailSent = await _emailService.SendOnboardingInvitationEmailAsync(
                            email,
                            existingInvitation.InvitationUrl,
                            onboarding.LeadName ?? "Onboarding Process");

                        if (emailSent)
                        {
                            response.SuccessfulInvitations.Add(email);
                        }
                        else
                        {
                            response.FailedInvitations[email] = "Failed to send email";
                        }
                    }
                    else
                    {
                        // Create new invitation
                        var invitation = new UserInvitation
                        {
                            OnboardingId = request.OnboardingId,
                            Email = email,
                            InvitationToken = GenerateInvitationToken(),
                            Status = "Pending",
                            SentDate = DateTimeOffset.UtcNow,
                            TokenExpiry = DateTimeOffset.UtcNow.AddDays(7),
                            SendCount = 1,
                            TenantId = onboarding.TenantId
                        };

                        invitation.InvitationUrl = GenerateInvitationUrl(invitation.InvitationToken, request.OnboardingId, request.BaseUrl);

                        // Create system user context for initialization
                        var systemUserContext = new UserContext
                        {
                            UserName = "SYSTEM",
                            UserId = "0",
                            TenantId = onboarding.TenantId ?? "DEFAULT",
                            AppCode = "DEFAULT"
                        };

                        invitation.InitCreateInfo(systemUserContext);

                        await _invitationRepository.InsertAsync(invitation);

                        // Send invitation email
                        var emailSent = await _emailService.SendOnboardingInvitationEmailAsync(
                            email,
                            invitation.InvitationUrl,
                            onboarding.LeadName ?? "Onboarding Process");

                        if (emailSent)
                        {
                            response.SuccessfulInvitations.Add(email);
                        }
                        else
                        {
                            response.FailedInvitations[email] = "Failed to send email";
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send invitation to {Email}", email);
                    response.FailedInvitations[email] = ex.Message;
                }
            }

            response.TotalSent = response.SuccessfulInvitations.Count;
            response.TotalFailed = response.FailedInvitations.Count;

            return response;
        }

        /// <summary>
        /// Get portal users for onboarding
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>List of portal users</returns>
        public async Task<List<PortalUserDto>> GetPortalUsersAsync(long onboardingId)
        {
            var invitations = await _invitationRepository.GetByOnboardingIdAsync(onboardingId);
            return _mapper.Map<List<PortalUserDto>>(invitations);
        }

        /// <summary>
        /// Verify portal access with invitation token
        /// </summary>
        /// <param name="request">Verification request</param>
        /// <returns>Verification response</returns>
        public async Task<PortalAccessVerificationResponseDto> VerifyPortalAccessAsync(PortalAccessVerificationRequestDto request)
        {
            var response = new PortalAccessVerificationResponseDto();

            try
            {
                // Find invitation by token
                var invitation = await _invitationRepository.GetByTokenAsync(request.Token);
                if (invitation == null)
                {
                    response.IsValid = false;
                    response.ErrorMessage = "Invalid invitation token";
                    return response;
                }

                // Verify email matches
                if (invitation.Email != request.Email)
                {
                    response.IsValid = false;
                    response.ErrorMessage = "Email does not match invitation";
                    return response;
                }

                // Check if token is expired
                if (invitation.TokenExpiry < DateTimeOffset.UtcNow)
                {
                    response.IsValid = false;
                    response.ErrorMessage = "Invitation token has expired";
                    return response;
                }

                // Check if invitation is still valid
                if (invitation.Status != "Pending")
                {
                    response.IsValid = false;
                    response.ErrorMessage = "Invitation is no longer valid";
                    return response;
                }

                // Update invitation access date
                invitation.LastAccessDate = DateTimeOffset.UtcNow;
                await _invitationRepository.UpdateAsync(invitation);

                // Find or create user
                var user = await _userRepository.GetByEmailAsync(request.Email);
                if (user == null)
                {
                    // Create temporary user for portal access
                    user = new User
                    {
                        Email = request.Email,
                        Username = request.Email,
                        EmailVerified = true,
                        Status = "active",
                        TenantId = invitation.TenantId
                    };

                    // Create system user context for initialization
                    var systemUserContext = new UserContext
                    {
                        UserName = "SYSTEM",
                        UserId = "0",
                        TenantId = invitation.TenantId ?? "DEFAULT",
                        AppCode = "DEFAULT"
                    };

                    user.InitCreateInfo(systemUserContext);
                    await _userRepository.InsertAsync(user);
                }

                // Generate access token
                var accessToken = _jwtService.GenerateJwtToken(user.Id, user.Email, user.Username);

                // Mark invitation as used
                await _invitationRepository.MarkAsUsedAsync(request.Token, user.Id);

                response.IsValid = true;
                response.OnboardingId = invitation.OnboardingId;
                response.Email = invitation.Email;
                response.AccessToken = accessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify portal access for token {Token}", request.Token);
                response.IsValid = false;
                response.ErrorMessage = "An error occurred during verification";
            }

            return response;
        }

        /// <summary>
        /// Resend invitation
        /// </summary>
        /// <param name="request">Resend invitation request</param>
        /// <returns>Whether resend was successful</returns>
        public async Task<bool> ResendInvitationAsync(ResendInvitationRequestDto request)
        {
            try
            {
                var invitation = await _invitationRepository.GetByEmailAndOnboardingIdAsync(request.Email, request.OnboardingId);
                if (invitation == null)
                {
                    throw new Exception("Invitation not found");
                }

                // Update invitation
                invitation.InvitationToken = GenerateInvitationToken();
                invitation.SentDate = DateTimeOffset.UtcNow;
                invitation.TokenExpiry = DateTimeOffset.UtcNow.AddDays(7);
                invitation.Status = "Pending";
                invitation.SendCount += 1;
                invitation.InvitationUrl = GenerateInvitationUrl(invitation.InvitationToken, request.OnboardingId, request.BaseUrl);
                invitation.ModifyDate = DateTimeOffset.UtcNow;

                await _invitationRepository.UpdateAsync(invitation);

                // Get onboarding info
                var onboarding = await _onboardingRepository.GetByIdAsync(request.OnboardingId);

                // Send invitation email
                return await _emailService.SendOnboardingInvitationEmailAsync(
                    request.Email,
                    invitation.InvitationUrl,
                    onboarding?.LeadName ?? "Onboarding Process");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend invitation to {Email}", request.Email);
                return false;
            }
        }

        /// <summary>
        /// Remove portal access
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="email">Email address</param>
        /// <returns>Whether removal was successful</returns>
        public async Task<bool> RemovePortalAccessAsync(long onboardingId, string email)
        {
            try
            {
                var invitation = await _invitationRepository.GetByEmailAndOnboardingIdAsync(email, onboardingId);
                if (invitation == null)
                {
                    _logger.LogWarning("No invitation found for email {Email} and onboarding ID {OnboardingId}", email, onboardingId);
                    throw new Exception($"No invitation found for email {email} and onboarding ID {onboardingId}");
                }

                // Soft delete the invitation
                invitation.IsValid = false;
                invitation.ModifyDate = DateTimeOffset.UtcNow;
                invitation.ModifyBy = _userContextService.GetCurrentUserEmail() ?? "System";

                await _invitationRepository.UpdateAsync(invitation);

                _logger.LogInformation("Successfully removed portal access for email {Email} and onboarding ID {OnboardingId}", email, onboardingId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove portal access for {Email} and onboarding ID {OnboardingId}", email, onboardingId);
                throw;
            }
        }

        /// <summary>
        /// Validate invitation token
        /// </summary>
        /// <param name="token">Invitation token</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>Token validation result</returns>
        public async Task<TokenValidationResponseDto> ValidateTokenAsync(string token, long onboardingId)
        {
            try
            {
                var invitation = await _invitationRepository.GetByTokenAsync(token);

                if (invitation == null)
                {
                    return new TokenValidationResponseDto
                    {
                        IsValid = false,
                        ErrorMessage = "Invalid token"
                    };
                }

                // Check if token belongs to the specified onboarding
                if (invitation.OnboardingId != onboardingId)
                {
                    return new TokenValidationResponseDto
                    {
                        IsValid = false,
                        ErrorMessage = "Token does not match onboarding ID"
                    };
                }

                // Check if token is expired
                if (invitation.TokenExpiry < DateTimeOffset.UtcNow)
                {
                    return new TokenValidationResponseDto
                    {
                        IsValid = false,
                        ErrorMessage = "Token has expired"
                    };
                }

                // Check if invitation is still valid
                if (!invitation.IsValid)
                {
                    return new TokenValidationResponseDto
                    {
                        IsValid = false,
                        ErrorMessage = "Invitation has been revoked"
                    };
                }

                return new TokenValidationResponseDto
                {
                    IsValid = true,
                    Email = invitation.Email
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate token {Token} for onboarding ID {OnboardingId}", token, onboardingId);
                return new TokenValidationResponseDto
                {
                    IsValid = false,
                    ErrorMessage = "Token validation failed"
                };
            }
        }

        /// <summary>
        /// Generate invitation token
        /// </summary>
        /// <returns>Invitation token</returns>
        private string GenerateInvitationToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("/", "_").Replace("+", "-").Replace("=", "");
        }

        /// <summary>
        /// Generate invitation URL
        /// </summary>
        /// <param name="token">Invitation token</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="baseUrl">Base URL (optional, will use default if not provided)</param>
        /// <returns>Invitation URL</returns>
        private string GenerateInvitationUrl(string token, long onboardingId, string? baseUrl = null)
        {
            // Use provided baseUrl or fall back to default
            var finalBaseUrl = !string.IsNullOrEmpty(baseUrl)
                ? baseUrl.TrimEnd('/')
                : "http://localhost:5173"; // Updated default for frontend port

            return $"{finalBaseUrl}/customer-portal?onboardingId={onboardingId}&token={token}";
        }

        /// <summary>
        /// Validate email format
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>Whether email is valid</returns>
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}