using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FlowFlex.Application.Contracts.Dtos.OW.User;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.Options;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Infrastructure.Services;
using BC = BCrypt.Net.BCrypt;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// User Service Implementation
    /// </summary>
    public class UserService : IUserService, IScopedService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly IJwtService _jwtService;
        private readonly IAccessTokenService _accessTokenService;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly EmailOptions _emailOptions;
        private readonly IUserContextService _userContextService;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private readonly IdmUserDataClient _idmUserDataClient;

        public UserService(
            IUserRepository userRepository,
            IEmailService emailService,
            IJwtService jwtService,
            IAccessTokenService accessTokenService,
            IMapper mapper,
            ILogger<UserService> logger,
            IHttpContextAccessor httpContextAccessor,
            IOptions<EmailOptions> emailOptions,
            IUserContextService userContextService,
            IBackgroundTaskQueue backgroundTaskQueue,
            IdmUserDataClient idmUserDataClient)
        {
            _userRepository = userRepository;
            _emailService = emailService;
            _jwtService = jwtService;
            _accessTokenService = accessTokenService;
            _mapper = mapper;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _emailOptions = emailOptions.Value;
            _userContextService = userContextService;
            _backgroundTaskQueue = backgroundTaskQueue;
            _idmUserDataClient = idmUserDataClient;
        }

        /// <summary>
        /// Register user
        /// </summary>
        /// <param name="request">Registration request</param>
        /// <returns>User information</returns>
        public async Task<UserDto> RegisterAsync(RegisterRequestDto request)
        {
            _logger.LogInformation("RegisterAsync called for email: {Email}, SkipEmailVerification: {SkipEmailVerification}",
                request.Email, request.SkipEmailVerification);

            // Check if email already exists (no tenant validation)
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);

            if (existingUser != null)
            {
                _logger.LogInformation("User already exists for email: {Email}, EmailVerified: {EmailVerified}",
                    request.Email, existingUser.EmailVerified);

                // For portal users who skip email verification, allow password reset for existing users
                if (request.SkipEmailVerification)
                {
                    // Check if user was already verified before updating
                    var wasAlreadyVerified = existingUser.EmailVerified;

                    _logger.LogInformation("Portal user password reset for email: {Email}, WasAlreadyVerified: {WasAlreadyVerified}",
                        request.Email, wasAlreadyVerified);

                    // Update existing user (both verified and unverified) to reset password
                    existingUser.Username = request.Email;
                    existingUser.PasswordHash = BC.HashPassword(request.Password);
                    existingUser.EmailVerified = true;
                    existingUser.Status = "active";
                    existingUser.ModifyDate = DateTimeOffset.UtcNow;
                    existingUser.ModifyBy = request.Email;

                    await _userRepository.UpdateAsync(existingUser);

                    // Send appropriate email based on previous verification status (non-blocking)
                    // Background task queued
                    _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                        {
                            try
                            {
                                if (wasAlreadyVerified)
                                {
                                    // Send password reset confirmation for already verified users
                                    await _emailService.SendPasswordResetConfirmationAsync(existingUser.Email, existingUser.Username);
                                }
                                else
                                {
                                    // Send welcome email for first-time verified users
                                    await _emailService.SendWelcomeEmailAsync(existingUser.Email, existingUser.Username);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to send email to {Email}", existingUser.Email);
                            }
                        });

                    _logger.LogInformation("Portal user registration/password reset completed successfully for: {Email}", request.Email);
                    return _mapper.Map<UserDto>(existingUser);
                }

                // For verified users, check if verification code is correct and allow password reset
                if (existingUser.EmailVerified)
                {
                    // For verified users, check if verification code is correct and allow password reset
                    if (!string.IsNullOrEmpty(existingUser.EmailVerificationCode) &&
                        existingUser.EmailVerificationCode == request.VerificationCode)
                    {
                        // Verify if verification code has not expired
                        if (existingUser.VerificationCodeExpiry >= DateTimeOffset.UtcNow)
                        {
                            _logger.LogInformation("Allowing password reset for verified user with valid verification code: {Email}", request.Email);

                            // Update password for existing verified user
                            existingUser.PasswordHash = BC.HashPassword(request.Password);
                            existingUser.ModifyDate = DateTimeOffset.UtcNow;
                            existingUser.ModifyBy = request.Email;
                            // Clear verification code after use
                            existingUser.EmailVerificationCode = null;
                            existingUser.VerificationCodeExpiry = null;

                            await _userRepository.UpdateAsync(existingUser);

                            // Send password reset confirmation email (non-blocking)
                            // Background task queued
                            _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                                        {
                                            try
                                            {
                                                await _emailService.SendPasswordResetConfirmationAsync(existingUser.Email, existingUser.Username);
                                            }
                                            catch (Exception ex)
                                            {
                                                _logger.LogWarning(ex, "Failed to send password reset confirmation email to {Email}", existingUser.Email);
                                            }
                                        });

                            _logger.LogInformation("Password reset completed successfully for verified user: {Email}", request.Email);
                            return _mapper.Map<UserDto>(existingUser);
                        }
                        else
                        {
                            throw new CRMException(System.Net.HttpStatusCode.OK, "Verification code has expired");
                        }
                    }
                    else
                    {
                        throw new CRMException(System.Net.HttpStatusCode.OK, "Email is already registered. Please use a different email address or reset your password if you forgot it.");
                    }
                }
                else
                {

                    // Verify verification code for normal registration
                    if (existingUser.EmailVerificationCode != request.VerificationCode)
                    {
                        throw new CRMException(System.Net.HttpStatusCode.OK, "Verification code is incorrect");
                    }

                    // Verify if verification code has expired
                    if (existingUser.VerificationCodeExpiry < DateTimeOffset.UtcNow)
                    {
                        throw new CRMException(System.Net.HttpStatusCode.OK, "Verification code has expired");
                    }

                    // Update user information and verify email
                    existingUser.Username = request.Email; // Use email as username
                    existingUser.PasswordHash = BC.HashPassword(request.Password);
                    existingUser.EmailVerified = true;
                    existingUser.Status = "active";
                    existingUser.ModifyDate = DateTimeOffset.UtcNow;
                    existingUser.ModifyBy = request.Email;

                    await _userRepository.UpdateAsync(existingUser);

                    // Send welcome email
                    await _emailService.SendWelcomeEmailAsync(existingUser.Email, existingUser.Username);

                    return _mapper.Map<UserDto>(existingUser);
                }
            }

            // For portal users who skip email verification - create new user directly
            if (request.SkipEmailVerification)
            {
                // Create new user with verified email
                var newUser = new User
                {
                    Email = request.Email,
                    Username = request.Email,
                    PasswordHash = BC.HashPassword(request.Password),
                    EmailVerified = true, // Portal users are pre-verified
                    Status = "active",
                    EmailVerificationCode = null,
                    VerificationCodeExpiry = null,
                    TenantId = "default" // Set default tenant ID
                };

                // Initialize create information
                newUser.InitCreateInfo(null);

                // Save new user
                await _userRepository.InsertAsync(newUser);

                // Send welcome email (non-blocking)
                // Background task queued
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        await _emailService.SendWelcomeEmailAsync(newUser.Email, newUser.Username);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send welcome email to {Email}", newUser.Email);
                    }
                });

                return _mapper.Map<UserDto>(newUser);
            }

            // If email doesn't exist, need to send verification code first
            throw new CRMException(System.Net.HttpStatusCode.OK, "Please get verification code first");
        }

        /// <summary>
        /// Send verification code
        /// </summary>
        /// <param name="request">Send verification code request</param>
        /// <returns>Whether sending was successful</returns>
        public async Task<bool> SendVerificationCodeAsync(SendVerificationCodeRequestDto request)
        {
            // Get user by email directly (no tenant validation)
            var user = await _userRepository.GetByEmailAsync(request.Email);

            // Generate verification code
            var verificationCode = GenerateVerificationCode();

            if (user == null)
            {
                // If user doesn't exist, create temporary user record (for registration flow)
                user = new User
                {
                    Email = request.Email,
                    Username = request.Email, // Use email as username
                    PasswordHash = null, // User hasn't set password yet
                    EmailVerified = false,
                    Status = "pending", // Pending verification status
                    EmailVerificationCode = verificationCode,
                    VerificationCodeExpiry = DateTimeOffset.UtcNow.AddMinutes(_emailOptions.VerificationCodeExpiryMinutes),
                    TenantId = "default" // Set default tenant ID
                };

                // Initialize create information with proper ID and timestamps
                user.InitCreateInfo(null);

                // Save temporary user
                await _userRepository.InsertAsync(user);
            }
            else
            {
                // Update existing user's verification code
                user.EmailVerificationCode = verificationCode;
                user.VerificationCodeExpiry = DateTimeOffset.UtcNow.AddMinutes(_emailOptions.VerificationCodeExpiryMinutes);
                user.InitUpdateInfo(null);
                await _userRepository.UpdateAsync(user);
            }

            // Send verification code email
            return await _emailService.SendVerificationCodeEmailAsync(user.Email, verificationCode);
        }

        /// <summary>
        /// Verify email
        /// </summary>
        /// <param name="request">Email verification request</param>
        /// <returns>Whether verification was successful</returns>
        public async Task<bool> VerifyEmailAsync(VerifyEmailRequestDto request)
        {
            // Get user by email directly (no tenant validation)
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                throw new CRMException(System.Net.HttpStatusCode.OK, "User does not exist");
            }

            // Verify verification code
            if (string.IsNullOrEmpty(user.EmailVerificationCode))
            {
                throw new CRMException(System.Net.HttpStatusCode.OK, "No verification code found for this user. Please request a new verification code.");
            }

            if (user.EmailVerificationCode.Trim() != request.VerificationCode.Trim())
            {
                _logger.LogWarning($"Verification code mismatch for user {request.Email}. Expected: '{user.EmailVerificationCode}', Received: '{request.VerificationCode}'");
                throw new CRMException(System.Net.HttpStatusCode.OK, "Verification code is incorrect");
            }

            // Verify if verification code has expired
            if (user.VerificationCodeExpiry < DateTimeOffset.UtcNow)
            {
                throw new CRMException(System.Net.HttpStatusCode.OK, "Verification code has expired");
            }

            // Update user email verification status
            user.EmailVerified = true;
            user.InitUpdateInfo(null);
            await _userRepository.UpdateAsync(user);

            // Send welcome email
            await _emailService.SendWelcomeEmailAsync(user.Email, user.Username);

            return true;
        }

        /// <summary>
        /// User login
        /// </summary>
        /// <param name="request">Login request</param>
        /// <returns>Login response</returns>
        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            // Get user by email directly (no tenant validation)
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                throw new CRMException(System.Net.HttpStatusCode.OK, "User does not exist");
            }

            // Check if user has set password
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                throw new CRMException(System.Net.HttpStatusCode.OK, "User has not set password, please use verification code login or reset password");
            }

            // Verify password
            if (!BC.Verify(request.Password, user.PasswordHash))
            {
                throw new CRMException(System.Net.HttpStatusCode.OK, "Password is incorrect");
            }

            // Check user status
            if (user.Status != "active")
            {
                throw new CRMException(System.Net.HttpStatusCode.OK, "User account is not active");
            }

            // Generate JWT token with details
            var tokenDetails = _jwtService.GenerateTokenWithDetails(user.Id, user.Email, user.Username, user.TenantId, "login");

            // Record token in database (this will revoke other active tokens)
            await _accessTokenService.RecordTokenAsync(
                tokenDetails.Jti,
                tokenDetails.UserId,
                tokenDetails.UserEmail,
                tokenDetails.Token,
                tokenDetails.ExpiresAt,
                tokenDetails.TokenType,
                tokenDetails.IssuedIp,
                tokenDetails.UserAgent,
                revokeOtherTokens: true
            );

            return new LoginResponseDto
            {
                AccessToken = tokenDetails.Token,
                TokenType = "Bearer",
                ExpiresIn = _jwtService.GetTokenExpiryInSeconds(),
                User = _mapper.Map<UserDto>(user),
                AppCode = "default",
                TenantId = user.TenantId ?? "default"
            };
        }

        /// <summary>
        /// Login with verification code
        /// </summary>
        /// <param name="request">Login with verification code request</param>
        /// <returns>Login response</returns>
        public async Task<LoginResponseDto> LoginWithCodeAsync(LoginWithCodeRequestDto request)
        {
            // Get user by email directly (no tenant validation)
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                throw new CRMException(System.Net.HttpStatusCode.OK, "User does not exist");
            }

            // Verify verification code
            if (string.IsNullOrEmpty(user.EmailVerificationCode))
            {
                throw new CRMException(System.Net.HttpStatusCode.OK, "No verification code found for this user. Please request a new verification code.");
            }

            if (user.EmailVerificationCode.Trim() != request.VerificationCode.Trim())
            {
                _logger.LogWarning($"Verification code mismatch for user {request.Email}. Expected: '{user.EmailVerificationCode}', Received: '{request.VerificationCode}'");
                throw new CRMException(System.Net.HttpStatusCode.OK, "Verification code is incorrect");
            }

            // Verify if verification code has expired
            if (user.VerificationCodeExpiry < DateTimeOffset.UtcNow)
            {
                throw new CRMException(System.Net.HttpStatusCode.OK, "Verification code has expired");
            }

            // Verify user status - allow pending status users to login with verification code
            if (user.Status != "active" && user.Status != "pending")
            {
                throw new CRMException(System.Net.HttpStatusCode.OK, "User account is not active");
            }

            // If user status is pending, automatically activate user after successful verification code login
            if (user.Status == "pending")
            {
                user.Status = "active";
                user.EmailVerified = true;
            }

            // Clear verification code (one-time use)
            user.EmailVerificationCode = null;
            user.VerificationCodeExpiry = null;
            user.ModifyDate = DateTimeOffset.UtcNow;
            user.ModifyBy = request.Email;
            await _userRepository.UpdateAsync(user);

            // Generate JWT token with details
            var tokenDetails = _jwtService.GenerateTokenWithDetails(user.Id, user.Email, user.Username, user.TenantId, "login");

            // Record token in database (this will revoke other active tokens)
            await _accessTokenService.RecordTokenAsync(
                tokenDetails.Jti,
                tokenDetails.UserId,
                tokenDetails.UserEmail,
                tokenDetails.Token,
                tokenDetails.ExpiresAt,
                tokenDetails.TokenType,
                tokenDetails.IssuedIp,
                tokenDetails.UserAgent,
                revokeOtherTokens: true
            );

            return new LoginResponseDto
            {
                AccessToken = tokenDetails.Token,
                TokenType = "Bearer",
                ExpiresIn = _jwtService.GetTokenExpiryInSeconds(),
                User = _mapper.Map<UserDto>(user),
                AppCode = "default",
                TenantId = user.TenantId ?? "default"
            };
        }

        /// <summary>
        /// Check if email exists
        /// </summary>
        /// <param name="email">Email</param>
        /// <returns>Whether it exists</returns>
        public async Task<bool> CheckEmailExistsAsync(string email)
        {
            return await _userRepository.EmailExistsAsync(email);
        }

        /// <summary>
        /// Get current user information
        /// </summary>
        /// <returns>User DTO</returns>
        public async Task<UserDto> GetCurrentUserAsync()
        {
            // Get user ID from user context
            var userId = _userContextService.GetCurrentUserId();
            if (userId <= 0)
            {
                throw new Exception("Not logged in");
            }

            // Get user
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new Exception("User does not exist");
            }

            // Return user DTO
            return _mapper.Map<UserDto>(user);
        }

        /// <summary>
        /// Get current user email
        /// </summary>
        /// <returns>User email</returns>
        public string GetCurrentUserEmail()
        {
            return _userContextService.GetCurrentUserEmail();
        }

        /// <summary>
        /// Change password
        /// </summary>
        /// <param name="request">Change password request</param>
        /// <returns>Whether change was successful</returns>
        public async Task<bool> ChangePasswordAsync(ChangePasswordRequestDto request)
        {
            // Get current user ID
            var userId = _userContextService.GetCurrentUserId();
            if (userId <= 0)
            {
                throw new Exception("Not logged in");
            }

            // Get user
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new Exception("User does not exist");
            }

            // Encrypt new password
            var passwordHash = BC.HashPassword(request.NewPassword);

            // Update user password
            user.PasswordHash = passwordHash;
            user.ModifyDate = DateTimeOffset.UtcNow;
            user.ModifyBy = user.Email;
            await _userRepository.UpdateAsync(user);

            return true;
        }

        /// <summary>
        /// Generate verification code
        /// </summary>
        /// <returns>Verification code</returns>
        private string GenerateVerificationCode()
        {
            return new Random().Next(100000, 999999).ToString();
        }

        /// <summary>
        /// Create test user (for testing environment only)
        /// </summary>
        /// <param name="email">Email</param>
        /// <param name="password">Password</param>
        /// <returns>User DTO</returns>
        public async Task<UserDto> CreateTestUserAsync(string email, string password)
        {
            // Check if user already exists (no tenant validation)
            var existingUser = await _userRepository.GetByEmailAsync(email);
            if (existingUser != null)
            {
                return _mapper.Map<UserDto>(existingUser);
            }

            // Create new test user
            var user = new User
            {
                Email = email,
                Username = email,
                PasswordHash = BC.HashPassword(password),
                EmailVerified = true,
                Status = "active",
                TenantId = "default" // Set default tenant ID
            };
            user.InitCreateInfo(null);

            await _userRepository.InsertAsync(user);

            return _mapper.Map<UserDto>(user);
        }

        /// <summary>
        /// Refresh access token
        /// </summary>
        /// <param name="request">Refresh token request</param>
        /// <returns>Login response with new token</returns>
        public async Task<LoginResponseDto> RefreshAccessTokenAsync(RefreshTokenRequestDto request)
        {
            try
            {
                // Get JTI from old token to revoke it
                var oldJti = _jwtService.GetJtiFromToken(request.AccessToken);

                // Extract user information from the old token
                var userId = _jwtService.GetUserIdFromToken(request.AccessToken);
                if (!userId.HasValue)
                {
                    throw new Exception("Unable to extract user information from token");
                }

                // Get user details
                var user = await _userRepository.GetByIdAsync(userId.Value);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                // Check user status
                if (user.Status != "active")
                {
                    throw new Exception("User account is not active");
                }

                // Validate old token is still active
                if (!string.IsNullOrEmpty(oldJti))
                {
                    var isTokenValid = await _accessTokenService.ValidateTokenAsync(oldJti);
                    if (!isTokenValid)
                    {
                        throw new Exception("Original token is no longer valid");
                    }
                }

                // Generate new token with details
                var newTokenDetails = _jwtService.GenerateTokenWithDetails(user.Id, user.Email, user.Username, user.TenantId, "refresh");

                // Record new token and revoke old token
                await _accessTokenService.RecordTokenAsync(
                    newTokenDetails.Jti,
                    newTokenDetails.UserId,
                    newTokenDetails.UserEmail,
                    newTokenDetails.Token,
                    newTokenDetails.ExpiresAt,
                    newTokenDetails.TokenType,
                    newTokenDetails.IssuedIp,
                    newTokenDetails.UserAgent,
                    revokeOtherTokens: true
                );

                // Explicitly revoke the old token
                if (!string.IsNullOrEmpty(oldJti))
                {
                    await _accessTokenService.RevokeTokenAsync(oldJti, "refresh");
                }

                return new LoginResponseDto
                {
                    AccessToken = newTokenDetails.Token,
                    TokenType = "Bearer",
                    ExpiresIn = _jwtService.GetTokenExpiryInSeconds(),
                    User = _mapper.Map<UserDto>(user),
                    AppCode = "default",
                    TenantId = user.TenantId ?? "default"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh access token");
                throw new Exception($"Token refresh failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Logout user and revoke token
        /// </summary>
        public async Task<bool> LogoutAsync(string token)
        {
            try
            {
                // Get JTI from token
                var jti = _jwtService.GetJtiFromToken(token);
                if (string.IsNullOrEmpty(jti))
                {
                    _logger.LogWarning("Unable to extract JTI from token for logout");
                    return false;
                }

                // Revoke the token
                var result = await _accessTokenService.RevokeTokenAsync(jti, "logout");

                if (result)
                {
                    _logger.LogInformation("User logged out successfully, token {Jti} revoked", jti);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to logout user");
                return false;
            }
        }

        /// <summary>
        /// Logout from all devices (revoke all user tokens)
        /// </summary>
        public async Task<int> LogoutFromAllDevicesAsync(long userId)
        {
            try
            {
                var revokedCount = await _accessTokenService.RevokeAllUserTokensAsync(userId, "logout_all_devices");

                _logger.LogInformation("User {UserId} logged out from all devices, {Count} tokens revoked",
                    userId, revokedCount);

                return revokedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to logout user from all devices");
                return 0;
            }
        }

        /// <summary>
        /// Third-party login with automatic registration
        /// </summary>
        /// <param name="request">Third-party login request</param>
        /// <returns>Login response with system token</returns>
        public async Task<LoginResponseDto> ThirdPartyLoginAsync(ThirdPartyLoginRequestDto request)
        {
            string email = null; // Declare email variable outside try-catch for use in catch block

            try
            {
                _logger.LogInformation("Processing third-party login request for AppCode: {AppCode}, TenantId: {TenantId}",
                    request.AppCode, request.TenantId);

                // Clean the authorization token (remove Bearer prefix if present)
                var cleanToken = request.AuthorizationToken;
                if (cleanToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    cleanToken = cleanToken.Substring("Bearer ".Length).Trim();
                }

                // Parse the authorization token to extract user information (without signature validation)
                var tokenInfo = _jwtService.ParseThirdPartyToken(cleanToken);
                if (!tokenInfo.IsValid)
                {
                    throw new Exception($"Invalid authorization token: {tokenInfo.ErrorMessage}");
                }

                // Extract email from token (required for user identification)
                email = tokenInfo.Email;

                // Extract username (fallback to email if not available)
                var username = !string.IsNullOrWhiteSpace(tokenInfo.Username) ? tokenInfo.Username : email;

                _logger.LogInformation("Extracted user info from token - Email: {Email}, Username: {Username}",
                    email, username);

                // First, check if user exists across all tenants with the same appCode
                var existingUserAcrossTenants = await _userRepository.GetByEmailAndAppCodeAsync(email, request.AppCode);

                // Then check if user exists in the specific tenant
                var existingUser = await _userRepository.GetByEmailAndTenantAsync(email, request.TenantId);
                User user;

                if (existingUser != null)
                {
                    // User exists in the specific tenant, update login information
                    user = existingUser;
                    user.LastLoginDate = DateTimeOffset.UtcNow;
                    user.Status = "active"; // Ensure user is active
                    user.EmailVerified = true; // Trust third-party verification

                    user.ModifyDate = DateTimeOffset.UtcNow;
                    user.ModifyBy = email;
                    await _userRepository.UpdateAsync(user);

                    _logger.LogInformation("Updated existing user {UserId} in tenant {TenantId} for third-party login",
                        user.Id, request.TenantId);
                }
                else if (existingUserAcrossTenants != null)
                {
                    // User exists in a different tenant with the same appCode
                    // This represents the same physical user accessing a different tenant
                    user = existingUserAcrossTenants;

                    // Update login information but keep the original tenant as primary
                    user.LastLoginDate = DateTimeOffset.UtcNow;
                    user.Status = "active";
                    user.EmailVerified = true;
                    user.ModifyDate = DateTimeOffset.UtcNow;
                    user.ModifyBy = email;
                    await _userRepository.UpdateAsync(user);

                    _logger.LogInformation("Cross-tenant login for existing user {UserId} (Email: {Email}) from tenant {OriginalTenantId} accessing tenant {RequestedTenantId}",
                        user.Id, email, user.TenantId, request.TenantId);

                    // Note: We keep the user's original tenant ID but allow access to the requested tenant
                    // The JWT token will contain the requested tenant information for this session
                }
                else
                {
                    // User doesn't exist, create new user
                    user = new User
                    {
                        Email = email,
                        Username = username,
                        PasswordHash = null, // No password for third-party users
                        EmailVerified = true, // Trust third-party verification
                        Status = "active",
                        TenantId = request.TenantId,
                        LastLoginDate = DateTimeOffset.UtcNow,
                        AppCode = request.AppCode // Set AppCode from request
                    };

                    // Create UserContext with correct AppCode and TenantId
                    var thirdPartyUserContext = new UserContext
                    {
                        UserName = email,
                        UserId = "0", // System created
                        Email = email,
                        TenantId = request.TenantId,
                        AppCode = request.AppCode
                    };

                    // Initialize create information with correct context
                    user.InitCreateInfo(thirdPartyUserContext);

                    // Ensure the values are set correctly (in case InitCreateInfo didn't work as expected)
                    user.TenantId = request.TenantId;
                    user.AppCode = request.AppCode;
                    await _userRepository.InsertAsync(user);

                    _logger.LogInformation("Created new user {UserId} for third-party login with email {Email}",
                        user.Id, email);

                    // Send welcome email for new users
                    try
                    {
                        await _emailService.SendWelcomeEmailAsync(user.Email, user.Username);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send welcome email to {Email}", user.Email);
                        // Don't fail the login process if email sending fails
                    }
                }

                // Generate system JWT token with extended claims
                // Use the requested tenant ID for the session, not the user's stored tenant ID
                var sessionTenantId = request.TenantId;
                var tokenDetails = _jwtService.GenerateTokenWithDetails(
                    user.Id,
                    user.Email,
                    user.Username,
                    sessionTenantId,  // Use session tenant ID
                    "third_party_login"
                );

                // Record token in database
                await _accessTokenService.RecordTokenAsync(
                    tokenDetails.Jti,
                    tokenDetails.UserId,
                    tokenDetails.UserEmail,
                    tokenDetails.Token,
                    tokenDetails.ExpiresAt,
                    tokenDetails.TokenType,
                    tokenDetails.IssuedIp,
                    tokenDetails.UserAgent,
                    revokeOtherTokens: true // Revoke other tokens to maintain single session
                );

                _logger.LogInformation("Successfully completed third-party login for user {UserId} with AppCode {AppCode}",
                    user.Id, request.AppCode);

                return new LoginResponseDto
                {
                    AccessToken = tokenDetails.Token,
                    TokenType = "Bearer",
                    ExpiresIn = _jwtService.GetTokenExpiryInSeconds(),
                    User = _mapper.Map<UserDto>(user),
                    AppCode = request.AppCode,
                    TenantId = request.TenantId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Third-party login failed for AppCode: {AppCode}, TenantId: {TenantId}",
                    request.AppCode, request.TenantId);

                // Handle specific database constraint violations
                if (ex.Message.Contains("duplicate key value violates unique constraint \"idx_users_email_tenant\""))
                {
                    throw new CRMException(HttpStatusCode.OK,
                        $"User with email '{email}' already exists in tenant '{request.TenantId}'. Please use the existing user account or contact administrator.");
                }

                // Handle other known exceptions with user-friendly messages
                if (ex.Message.Contains("duplicate key"))
                {
                    throw new CRMException(HttpStatusCode.OK,
                        "User account conflict detected. Please contact administrator for assistance.");
                }

                // For unknown exceptions, provide a generic message but preserve the original for logging
                throw new CRMException(HttpStatusCode.OK,
                    "Third-party login failed. Please try again or contact administrator if the problem persists.");
            }
        }

        /// <summary>
        /// Get User List with Pagination and Search
        /// </summary>
        /// <param name="request">User list request</param>
        /// <returns>User list response</returns>
        public async Task<UserListResponseDto> GetUserListAsync(UserListRequestDto request)
        {
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            _logger.LogInformation("GetUserListAsync called with PageIndex: {PageIndex}, PageSize: {PageSize}, SearchText: {SearchText}, TenantId: {TenantId}, AppCode: {AppCode}",
                request.PageIndex, request.PageSize, request.SearchText, currentTenantId, currentAppCode);

            // Validate and set default pagination parameters
            var pageIndex = Math.Max(1, request.PageIndex);
            var pageSize = Math.Max(1, Math.Min(100, request.PageSize));

            try
            {
                // Get paginated users from repository
                var (users, totalCount) = await _userRepository.GetPagedAsync(
                    pageIndex,
                    pageSize,
                    request.SearchText,
                    request.Email,
                    request.Username,
                    request.Team,
                    request.Status,
                    request.EmailVerified,
                    request.SortField,
                    request.SortDirection);

                // Check and assign teams for users without team
                await EnsureUsersHaveTeams(users);

                // Map to DTOs
                var userDtos = _mapper.Map<List<UserDto>>(users);

                // Create response
                var response = new UserListResponseDto
                {
                    Users = userDtos,
                    TotalCount = totalCount,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };

                _logger.LogInformation("GetUserListAsync completed successfully. Total users: {TotalCount}, Page: {PageIndex}/{TotalPages}",
                    totalCount, pageIndex, response.TotalPages);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user list");
                throw new CRMException(HttpStatusCode.InternalServerError,
                    "An error occurred while retrieving the user list. Please try again.");
            }
        }

        /// <summary>
        /// Assign Random Teams to Users Without Team
        /// </summary>
        /// <returns>Number of users assigned teams</returns>
        public async Task<int> AssignRandomTeamsToUsersAsync()
        {
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            _logger.LogInformation("AssignRandomTeamsToUsersAsync called with TenantId: {TenantId}, AppCode: {AppCode}",
                currentTenantId, currentAppCode);

            // Available teams
            var availableTeams = new List<string>
            {
                "Sales",
                "Account Management",
                "IT",
                "Legal",
                "Operations",
                "Finance",
                "Customer",
                "CSR",
                "Implementation",
                "WISE Support",
                "Billing"
            };

            try
            {
                // Get users without team
                var usersWithoutTeam = await _userRepository.GetUsersWithoutTeamAsync();

                if (!usersWithoutTeam.Any())
                {
                    _logger.LogInformation("No users found without team assignment");
                    return 0;
                }

                var random = new Random();
                var assignedCount = 0;

                foreach (var user in usersWithoutTeam)
                {
                    // Randomly select a team
                    var randomTeam = availableTeams[random.Next(availableTeams.Count)];

                    // Update user team
                    var success = await _userRepository.UpdateUserTeamAsync(user.Id, randomTeam);

                    if (success)
                    {
                        assignedCount++;
                        _logger.LogInformation("Assigned team '{Team}' to user {UserId} ({Email})",
                            randomTeam, user.Id, user.Email);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to assign team to user {UserId} ({Email})",
                            user.Id, user.Email);
                    }
                }

                _logger.LogInformation("AssignRandomTeamsToUsersAsync completed. Assigned teams to {AssignedCount} users", assignedCount);
                return assignedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while assigning teams to users");
                throw new CRMException(HttpStatusCode.InternalServerError,
                    "An error occurred while assigning teams. Please try again.");
            }
        }

        /// <summary>
        /// Get current tenant ID from HTTP context
        /// </summary>
        private string GetCurrentTenantId()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null)
                return "default";

            // Try to get from AppContext first
            if (httpContext.Items.TryGetValue("AppContext", out var appContextObj) &&
                appContextObj is FlowFlex.Domain.Shared.Models.AppContext appContext)
            {
                return appContext.TenantId;
            }

            // Fallback to headers
            var tenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault()
                        ?? httpContext.Request.Headers["TenantId"].FirstOrDefault();

            if (!string.IsNullOrEmpty(tenantId))
            {
                return tenantId;
            }

            return "default";
        }

        /// <summary>
        /// Get current app code from HTTP context
        /// </summary>
        private string GetCurrentAppCode()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null)
                return "default";

            // Try to get from AppContext first
            if (httpContext.Items.TryGetValue("AppContext", out var appContextObj) &&
                appContextObj is FlowFlex.Domain.Shared.Models.AppContext appContext)
            {
                return appContext.AppCode;
            }

            // Fallback to headers
            var appCode = httpContext.Request.Headers["X-App-Code"].FirstOrDefault()
                       ?? httpContext.Request.Headers["AppCode"].FirstOrDefault();

            if (!string.IsNullOrEmpty(appCode))
            {
                return appCode;
            }

            return "default";
        }

        /// <summary>
        /// Ensure users have teams assigned, randomly assign if missing
        /// </summary>
        /// <param name="users">List of users to check</param>
        private async Task EnsureUsersHaveTeams(List<User> users)
        {
            // Available teams
            var availableTeams = new List<string>
            {
                "Sales",
                "Account Management",
                "IT",
                "Legal",
                "Operations",
                "Finance",
                "Customer",
                "CSR",
                "Implementation",
                "WISE Support",
                "Billing"
            };

            var usersWithoutTeam = users.Where(u => string.IsNullOrEmpty(u.Team)).ToList();

            if (!usersWithoutTeam.Any())
            {
                return; // All users already have teams
            }

            var random = new Random();
            var assignedCount = 0;

            foreach (var user in usersWithoutTeam)
            {
                try
                {
                    // Randomly select a team
                    var randomTeam = availableTeams[random.Next(availableTeams.Count)];

                    // Update user team in database
                    var success = await _userRepository.UpdateUserTeamAsync(user.Id, randomTeam);

                    if (success)
                    {
                        // Update the user object in memory for current response
                        user.Team = randomTeam;
                        assignedCount++;

                        _logger.LogInformation("Auto-assigned team '{Team}' to user {UserId} ({Email}) during list query",
                            randomTeam, user.Id, user.Email);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to auto-assign team to user {UserId} ({Email}) during list query",
                            user.Id, user.Email);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error auto-assigning team to user {UserId} ({Email})",
                        user.Id, user.Email);
                }
            }

            if (assignedCount > 0)
            {
                _logger.LogInformation("Auto-assigned teams to {AssignedCount} users during list query", assignedCount);
            }
        }

        /// <summary>
        /// Get User Tree Structure using IDM teamTree API (returns hierarchical structure directly)
        /// </summary>
        /// <returns>Tree structure with teams and users from IDM</returns>
        public async Task<List<UserTreeNodeDto>> GetUserTreeAsync()
        {
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            _logger.LogInformation("=== GetUserTreeAsync Started (using TeamTree API) ===");
            _logger.LogInformation("Current context - TenantId: {TenantId}, AppCode: {AppCode}", currentTenantId, currentAppCode);

            // Log HTTP context details
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext != null)
            {
                _logger.LogDebug("HTTP Context available");
                _logger.LogDebug("Request headers:");
                foreach (var header in httpContext.Request.Headers)
                {
                    if (header.Key.Contains("Authorization", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogDebug("  {HeaderName}: Bearer [REDACTED]", header.Key);
                    }
                    else
                    {
                        _logger.LogDebug("  {HeaderName}: {HeaderValue}", header.Key, string.Join(", ", header.Value));
                    }
                }
            }
            else
            {
                _logger.LogWarning("HTTP Context is null");
            }

            try
            {
                // Get IdmUserDataClient service to fetch team tree from IDM
                _logger.LogInformation("Attempting to resolve IdmUserDataClient service");
                var idmUserDataClient = _httpContextAccessor.HttpContext?.RequestServices.GetService<FlowFlex.Application.Services.OW.IdmUserDataClient>();
                if (idmUserDataClient == null)
                {
                    _logger.LogError("IdmUserDataClient service not available from DI container");
                    throw new CRMException(HttpStatusCode.InternalServerError,
                        "IDM service is not available. Please try again later.");
                }

                _logger.LogInformation("IdmUserDataClient service resolved successfully");

                // Get team tree and team users from IDM in parallel
                _logger.LogInformation("Calling IDM APIs to fetch team tree and team users");

                var startTime = DateTimeOffset.UtcNow;
                var teamTreeTask = idmUserDataClient.GetTeamTreeAsync(currentTenantId);
                var teamUsersTask = idmUserDataClient.GetAllTeamUsersAsync(currentTenantId, 10000, 1);

                await Task.WhenAll(teamTreeTask, teamUsersTask);

                var teamTreeNodes = await teamTreeTask;
                var teamUsers = await teamUsersTask;
                var elapsed = DateTimeOffset.UtcNow - startTime;

                _logger.LogInformation("IDM APIs call completed in {ElapsedMs}ms", elapsed.TotalMilliseconds);

                // Handle case where team tree is empty but team users exist
                if (teamTreeNodes == null || !teamTreeNodes.Any())
                {
                    _logger.LogWarning("No team tree data returned from IDM API");

                    // If we have team users, create "Other" team for them
                    if (teamUsers != null && teamUsers.Any())
                    {
                        _logger.LogInformation("Creating 'Other' team for users without team structure");

                        // Add all users to "Other" team, deduplicate by ID
                        var uniqueUsers = teamUsers
                            .GroupBy(tu => tu.Id)
                            .Select(g => g.First())
                            .ToList();

                        if (uniqueUsers.Any())
                        {
                            var otherTeamNode = new UserTreeNodeDto
                            {
                                Id = "Other",
                                Name = "Other",
                                Type = "team",
                                MemberCount = uniqueUsers.Count,
                                Children = new List<UserTreeNodeDto>()
                            };

                            foreach (var teamUser in uniqueUsers)
                            {
                                var userNode = new UserTreeNodeDto
                                {
                                    Id = teamUser.Id,
                                    Name = GetUserDisplayName(teamUser), // Use priority: FirstName + LastName > UserName
                                    Type = "user",
                                    MemberCount = 0,
                                    Username = teamUser.UserName,
                                    Email = teamUser.Email,
                                    UserType = teamUser.UserType,
                                    Children = null
                                };

                                otherTeamNode.Children.Add(userNode);
                            }

                            _logger.LogInformation("Created 'Other' team with {UserCount} users", uniqueUsers.Count);
                            return new List<UserTreeNodeDto> { otherTeamNode };
                        }
                    }

                    return new List<UserTreeNodeDto>();
                }

                _logger.LogInformation("Retrieved {TeamCount} root team nodes and {UserCount} team users from IDM",
                    teamTreeNodes.Count, teamUsers?.Count ?? 0);

                // Convert IdmTeamTreeNodeDto to UserTreeNodeDto and populate with users
                _logger.LogInformation("Converting IDM team tree structure to UserTreeNodeDto format");
                var treeNodes = ConvertIdmTeamTreeToUserTree(teamTreeNodes, teamUsers ?? new List<IdmTeamUserDto>());

                _logger.LogInformation("=== GetUserTreeAsync Completed Successfully (TeamTree API) ===");
                _logger.LogInformation("Final tree structure statistics:");
                _logger.LogInformation("  Total root nodes: {RootCount}", treeNodes.Count);
                _logger.LogInformation("  Total nodes (including children): {TotalNodes}", CountTreeNodes(treeNodes));

                return treeNodes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user tree structure from IDM TeamTree API");

                // Fallback to local database users if IDM fails
                _logger.LogInformation("Falling back to local database users due to IDM error");
                return await GetUserTreeFromLocalDatabaseAsync();
            }
        }

        /// <summary>
        /// Get all users as a flat list (without team hierarchy)
        /// </summary>
        /// <returns>Flat list of all users</returns>
        public async Task<List<UserTreeNodeDto>> GetAllUsersAsync()
        {
            _logger.LogInformation("=== GetAllUsersAsync Started ===");

            try
            {
                // Get the full user tree first
                var userTree = await GetUserTreeAsync();

                // Extract all users from the tree into a flat list
                var allUsers = ExtractAllUsersFromTree(userTree);

                _logger.LogInformation("=== GetAllUsersAsync Completed Successfully ===");
                _logger.LogInformation("Total users extracted: {UserCount}", allUsers.Count);

                return allUsers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all users");
                throw;
            }
        }

        /// <summary>
        /// Extract all user nodes from tree structure into a flat list with deduplication
        /// </summary>
        private List<UserTreeNodeDto> ExtractAllUsersFromTree(List<UserTreeNodeDto> nodes)
        {
            var result = new List<UserTreeNodeDto>();
            var seenUserIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (nodes == null || nodes.Count == 0) return result;

            void Traverse(UserTreeNodeDto node)
            {
                if (node == null) return;

                if (node.Type == "user")
                {
                    // Deduplicate by user ID
                    if (!string.IsNullOrEmpty(node.Id) && !seenUserIds.Contains(node.Id))
                    {
                        seenUserIds.Add(node.Id);
                        result.Add(new UserTreeNodeDto
                        {
                            Id = node.Id,
                            Name = node.Name,
                            Type = node.Type,
                            Username = node.Username,
                            Email = node.Email,
                            UserDetails = node.UserDetails,
                            UserType = node.UserType,
                            MemberCount = 0,
                            Children = null
                        });
                    }
                }

                // Traverse children
                if (node.Children != null)
                {
                    foreach (var child in node.Children)
                    {
                        Traverse(child);
                    }
                }
            }

            foreach (var root in nodes)
            {
                Traverse(root);
            }

            return result;
        }

        /// <summary>
        /// Get user display name with priority: FirstName + LastName > UserName
        /// </summary>
        private string GetUserDisplayName(IdmTeamUserDto teamUser)
        {
            // Priority 1: FirstName + LastName (if both or either is not empty)
            var firstName = teamUser.FirstName?.Trim() ?? string.Empty;
            var lastName = teamUser.LastName?.Trim() ?? string.Empty;

            if (!string.IsNullOrEmpty(firstName) || !string.IsNullOrEmpty(lastName))
            {
                return $"{firstName} {lastName}".Trim();
            }

            // Priority 2: UserName
            return teamUser.UserName ?? string.Empty;
        }

        /// <summary>
        /// Convert IDM team tree structure to UserTreeNodeDto format and populate with users
        /// </summary>
        private List<UserTreeNodeDto> ConvertIdmTeamTreeToUserTree(List<IdmTeamTreeNodeDto> idmNodes, List<IdmTeamUserDto> teamUsers)
        {
            return ConvertIdmTeamTreeToUserTreeInternal(idmNodes, teamUsers, true);
        }

        /// <summary>
        /// Internal method to convert IDM team tree structure to UserTreeNodeDto format
        /// </summary>
        /// <param name="idmNodes">IDM team tree nodes</param>
        /// <param name="teamUsers">All team users</param>
        /// <param name="isRootLevel">Whether this is the root level (only add "Other" team at root level)</param>
        private List<UserTreeNodeDto> ConvertIdmTeamTreeToUserTreeInternal(
            List<IdmTeamTreeNodeDto> idmNodes,
            List<IdmTeamUserDto> teamUsers,
            bool isRootLevel)
        {
            if (idmNodes == null || !idmNodes.Any())
            {
                return new List<UserTreeNodeDto>();
            }

            // Create a lookup for team users by teamId
            var teamUserLookup = teamUsers.GroupBy(tu => tu.TeamId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new List<UserTreeNodeDto>();

            foreach (var idmNode in idmNodes)
            {
                var userTreeNode = new UserTreeNodeDto
                {
                    Id = idmNode.Value,
                    Name = idmNode.Label,
                    Type = "team", // IDM teamTree returns teams
                    MemberCount = 0,
                    Children = new List<UserTreeNodeDto>()
                };

                // First, recursively convert child teams (pass isRootLevel=false to prevent "Other" team creation in nested levels)
                if (idmNode.Children != null && idmNode.Children.Any())
                {
                    var childTeams = ConvertIdmTeamTreeToUserTreeInternal(idmNode.Children, teamUsers, false);
                    userTreeNode.Children.AddRange(childTeams);
                }

                // Then, add users for this team
                if (teamUserLookup.ContainsKey(idmNode.Value))
                {
                    var usersInTeam = teamUserLookup[idmNode.Value];

                    foreach (var teamUser in usersInTeam)
                    {
                        var userNode = new UserTreeNodeDto
                        {
                            Id = teamUser.Id,
                            Name = GetUserDisplayName(teamUser), // Use priority: FirstName + LastName > UserName
                            Type = "user",
                            MemberCount = 0,
                            Username = teamUser.UserName,
                            Email = teamUser.Email,
                            UserType = teamUser.UserType,
                            Children = null // Users have no children
                        };

                        userTreeNode.Children.Add(userNode);
                    }
                }

                // Update member count: count all unique users in this team and all sub-teams
                userTreeNode.MemberCount = CountUniqueUsersInTree(userTreeNode);

                result.Add(userTreeNode);
            }

            // Only handle users without team at root level to avoid duplicate "Other" teams
            if (isRootLevel)
            {
                // Track all team IDs in the tree (including nested teams)
                var allTeamIds = new HashSet<string>();
                CollectAllTeamIds(idmNodes, allTeamIds);

                // Find users without team or users whose team is not in the tree
                var usersWithoutTeam = teamUsers
                    .Where(tu => string.IsNullOrEmpty(tu.TeamId) || !allTeamIds.Contains(tu.TeamId))
                    .GroupBy(u => u.Id) // Deduplicate users by ID
                    .Select(g => g.First())
                    .ToList();

                if (usersWithoutTeam.Any())
                {
                    _logger.LogInformation("Found {Count} users without team, adding to 'Other' team at root level", usersWithoutTeam.Count);

                    var otherTeamNode = new UserTreeNodeDto
                    {
                        Id = "Other",
                        Name = "Other",
                        Type = "team",
                        MemberCount = usersWithoutTeam.Count,
                        Children = new List<UserTreeNodeDto>()
                    };

                    foreach (var teamUser in usersWithoutTeam)
                    {
                        var userNode = new UserTreeNodeDto
                        {
                            Id = teamUser.Id,
                            Name = GetUserDisplayName(teamUser), // Use priority: FirstName + LastName > UserName
                            Type = "user",
                            MemberCount = 0,
                            Username = teamUser.UserName,
                            Email = teamUser.Email,
                            UserType = teamUser.UserType,
                            Children = null
                        };

                        otherTeamNode.Children.Add(userNode);
                    }

                    result.Add(otherTeamNode);
                }
            }

            return result;
        }

        /// <summary>
        /// Count all unique users in a team tree (including all sub-teams, with deduplication)
        /// </summary>
        private int CountUniqueUsersInTree(UserTreeNodeDto node)
        {
            if (node == null || node.Children == null || !node.Children.Any())
            {
                return 0;
            }

            var uniqueUserIds = new HashSet<string>();
            CollectUniqueUserIds(node, uniqueUserIds);
            return uniqueUserIds.Count;
        }

        /// <summary>
        /// Recursively collect all unique user IDs from a tree node and its descendants
        /// </summary>
        private void CollectUniqueUserIds(UserTreeNodeDto node, HashSet<string> userIds)
        {
            if (node == null || node.Children == null || !node.Children.Any())
            {
                return;
            }

            foreach (var child in node.Children)
            {
                if (child.Type == "user" && !string.IsNullOrEmpty(child.Id))
                {
                    // Add user ID to the set (automatically deduplicates)
                    userIds.Add(child.Id);
                }
                else if (child.Type == "team")
                {
                    // Recursively collect from sub-teams
                    CollectUniqueUserIds(child, userIds);
                }
            }
        }

        /// <summary>
        /// Collect all team IDs from the tree structure (including nested teams)
        /// </summary>
        private void CollectAllTeamIds(List<IdmTeamTreeNodeDto> nodes, HashSet<string> teamIds)
        {
            if (nodes == null || !nodes.Any())
            {
                return;
            }

            foreach (var node in nodes)
            {
                if (!string.IsNullOrEmpty(node.Value))
                {
                    teamIds.Add(node.Value);
                }

                // Recursively collect from children
                if (node.Children != null && node.Children.Any())
                {
                    CollectAllTeamIds(node.Children, teamIds);
                }
            }
        }

        /// <summary>
        /// Count total nodes in tree structure (including all descendants)
        /// </summary>
        private int CountTreeNodes(List<UserTreeNodeDto> nodes)
        {
            if (nodes == null || !nodes.Any())
            {
                return 0;
            }

            int count = nodes.Count;
            foreach (var node in nodes)
            {
                if (node.Children != null && node.Children.Any())
                {
                    count += CountTreeNodes(node.Children);
                }
            }

            return count;
        }

        /// <summary>
        /// Fallback method to get user tree from local database when IDM is unavailable
        /// </summary>
        /// <returns>Tree structure with teams and users from local database</returns>
        private async Task<List<UserTreeNodeDto>> GetUserTreeFromLocalDatabaseAsync()
        {
            try
            {
                // Get all users (without pagination for tree structure)
                var (users, _) = await _userRepository.GetPagedAsync(1, int.MaxValue);

                // Ensure all users have teams
                await EnsureUsersHaveTeams(users);

                // Available teams (fallback teams when local data is used)
                var availableTeams = new List<string>
                {
                    "Sales",
                    "Account Management",
                    "IT",
                    "Legal",
                    "Operations",
                    "Finance",
                    "Customer",
                    "CSR",
                    "Implementation",
                    "WISE Support",
                    "Billing"
                };

                var treeNodes = new List<UserTreeNodeDto>();

                // Group users by team
                var usersByTeam = users.GroupBy(u => u.Team ?? "Unassigned").ToLookup(g => g.Key, g => g.ToList());

                // Create tree structure for each team
                foreach (var teamName in availableTeams)
                {
                    var teamUsers = usersByTeam[teamName].FirstOrDefault() ?? new List<User>();

                    var teamNode = new UserTreeNodeDto
                    {
                        Id = teamName,
                        Name = teamName + " (Local)",
                        Type = "team",
                        MemberCount = teamUsers.Count,
                        Children = new List<UserTreeNodeDto>()
                    };

                    // Add user nodes under each team
                    foreach (var user in teamUsers)
                    {
                        var userNode = new UserTreeNodeDto
                        {
                            Id = user.Id.ToString(),
                            Name = user.Username ?? user.Email,
                            Type = "user",
                            UserDetails = _mapper.Map<UserDto>(user),
                            Children = new List<UserTreeNodeDto>()
                        };

                        teamNode.Children.Add(userNode);
                    }

                    if (teamUsers.Any())
                    {
                        treeNodes.Add(teamNode);
                    }
                }

                // Handle users without assigned teams
                var unassignedUsers = usersByTeam["Unassigned"].FirstOrDefault() ?? new List<User>();
                if (unassignedUsers.Any())
                {
                    var unassignedNode = new UserTreeNodeDto
                    {
                        Id = "Unassigned",
                        Name = "Unassigned (Local)",
                        Type = "team",
                        MemberCount = unassignedUsers.Count,
                        Children = new List<UserTreeNodeDto>()
                    };

                    foreach (var user in unassignedUsers)
                    {
                        var userNode = new UserTreeNodeDto
                        {
                            Id = user.Id.ToString(),
                            Name = user.Username ?? user.Email,
                            Type = "user",
                            UserDetails = _mapper.Map<UserDto>(user),
                            Children = new List<UserTreeNodeDto>()
                        };

                        unassignedNode.Children.Add(userNode);
                    }

                    treeNodes.Add(unassignedNode);
                }

                _logger.LogInformation("Fallback GetUserTreeFromLocalDatabaseAsync completed. Total teams: {TeamCount}, Total users: {UserCount}",
                    treeNodes.Count, users.Count);

                return treeNodes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in fallback method while getting user tree structure from local database");
                throw new CRMException(HttpStatusCode.InternalServerError,
                    "An error occurred while retrieving the user tree structure. Please try again.");
            }
        }

        public async Task<UserDto> GetUserByEmail(string email)
        {
            var user = await _userRepository.GetFirstAsync(u => u.Email == email);
            return _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> GetUserByIdAsync(long userId)
        {
            try
            {
                _logger.LogInformation("GetUserByIdAsync called with userId: {UserId}", userId);

                if (userId <= 0)
                {
                    _logger.LogWarning("Invalid userId provided: {UserId}", userId);
                    throw new CRMException(HttpStatusCode.BadRequest, "User ID must be greater than 0");
                }

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found with ID: {UserId}", userId);
                    return null;
                }

                var userDto = _mapper.Map<UserDto>(user);
                _logger.LogInformation("GetUserByIdAsync completed successfully for userId: {UserId}", userId);

                return userDto;
            }
            catch (CRMException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user by ID: {UserId}", userId);
                throw new CRMException(HttpStatusCode.InternalServerError,
                    "An error occurred while retrieving the user. Please try again.");
            }
        }

        public async Task<List<UserDto>> GetUsersByIdsAsync(List<long> userIds)
        {
            var currentTenantId = GetCurrentTenantId();
            return await GetUsersByIdsAsync(userIds, currentTenantId);
        }

        public async Task<List<UserDto>> GetUsersByIdsAsync(List<long> userIds, string tenantId)
        {
            try
            {
                _logger.LogInformation("GetUsersByIdsAsync called with {Count} user IDs, TenantId: {TenantId}",
                    userIds?.Count ?? 0, tenantId);

                if (userIds == null || userIds.Count == 0)
                {
                    _logger.LogWarning("Empty or null userIds list provided");
                    return new List<UserDto>();
                }

                // Filter and deduplicate valid IDs
                var validUserIds = userIds.Where(id => id > 0).Distinct().ToList();
                if (validUserIds.Count == 0)
                {
                    _logger.LogWarning("No valid user IDs found in the provided list");
                    return new List<UserDto>();
                }

                _logger.LogInformation("Querying {ValidCount} valid user IDs", validUserIds.Count);

                // Query local database first
                var users = await _userRepository.GetListAsync(u => validUserIds.Contains(u.Id));
                var userDtos = _mapper.Map<List<UserDto>>(users);
                var foundUserIds = userDtos.Select(u => u.Id).ToHashSet();

                _logger.LogInformation("Found {FoundCount} users in local database", userDtos.Count);

                // For missing users, try to get from IDM
                var missingUserIds = validUserIds.Where(id => !foundUserIds.Contains(id)).ToList();
                if (missingUserIds.Any())
                {
                    _logger.LogInformation("Attempting to fetch {MissingCount} missing users from IDM with TenantId: {TenantId}",
                        missingUserIds.Count, tenantId);

                    try
                    {
                        if (_idmUserDataClient != null)
                        {
                            var teamUsersResponse = await _idmUserDataClient.GetAllTeamUsersAsync(tenantId, 10000, 1);

                            if (teamUsersResponse != null && teamUsersResponse.Any())
                            {
                                // Convert string IDs to long for comparison
                                // Note: Same user may appear multiple times (one per team), so we need to deduplicate
                                // Filter out UserType == 1
                                var idmUsers = teamUsersResponse
                                    .Where(tu => long.TryParse(tu.Id, out var userId) && missingUserIds.Contains(userId))
                                    .GroupBy(tu => tu.Id) // Group by user ID to deduplicate
                                    .Select(g => g.First()) // Take first occurrence of each user
                                    .Select(tu =>
                                    {
                                        // Get display name with priority: FirstName + LastName > UserName
                                        var firstName = tu.FirstName?.Trim() ?? string.Empty;
                                        var lastName = tu.LastName?.Trim() ?? string.Empty;
                                        var displayName = string.Empty;

                                        if (!string.IsNullOrEmpty(firstName) || !string.IsNullOrEmpty(lastName))
                                        {
                                            displayName = $"{firstName} {lastName}".Trim();
                                        }
                                        else
                                        {
                                            displayName = tu.UserName ?? string.Empty;
                                        }

                                        return new UserDto
                                        {
                                            Id = long.Parse(tu.Id),
                                            Username = displayName, // Use display name with priority
                                            Email = tu.Email,
                                            EmailVerified = true,
                                            Status = "active"
                                        };
                                    })
                                    .ToList();

                                if (idmUsers.Any())
                                {
                                    userDtos.AddRange(idmUsers);
                                    _logger.LogInformation("Found {IdmCount} additional users from IDM (after deduplication and filtering UserType=1)", idmUsers.Count);
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning("IdmUserDataClient not available, cannot fetch missing users from IDM");
                        }
                    }
                    catch (Exception idmEx)
                    {
                        _logger.LogWarning(idmEx, "Failed to fetch missing users from IDM, continuing with local results");
                    }
                }

                _logger.LogInformation("GetUsersByIdsAsync completed successfully. Found {FoundCount} users out of {RequestedCount} requested",
                    userDtos.Count, validUserIds.Count);

                return userDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting users by IDs");
                throw new CRMException(HttpStatusCode.InternalServerError,
                    "An error occurred while retrieving the users. Please try again.");
            }
        }

        public async Task<Dictionary<string, string>> GetTeamNamesByIdsAsync(List<string> teamIds, string tenantId)
        {
            try
            {
                _logger.LogInformation("GetTeamNamesByIdsAsync called with {Count} team IDs, TenantId: {TenantId}",
                    teamIds?.Count ?? 0, tenantId);

                if (teamIds == null || teamIds.Count == 0)
                {
                    _logger.LogWarning("Empty or null teamIds list provided");
                    return new Dictionary<string, string>();
                }

                // Filter and deduplicate valid IDs
                var validTeamIds = teamIds
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Select(id => id.Trim())
                    .Distinct()
                    .ToList();

                if (validTeamIds.Count == 0)
                {
                    _logger.LogWarning("No valid team IDs found in the provided list");
                    return new Dictionary<string, string>();
                }

                _logger.LogInformation("Querying {ValidCount} valid team IDs", validTeamIds.Count);

                var teamNameMap = new Dictionary<string, string>();

                try
                {
                    if (_idmUserDataClient != null)
                    {
                        _logger.LogInformation("Fetching teams from IDM with TenantId: {TenantId}", tenantId);

                        var teamsResponse = await _idmUserDataClient.GetAllTeamsAsync(tenantId, 10000, 1);

                        if (teamsResponse?.Data != null && teamsResponse.Data.Any())
                        {
                            _logger.LogInformation("Retrieved {TeamCount} teams from IDM", teamsResponse.Data.Count);

                            // Build team name map for requested IDs
                            foreach (var teamId in validTeamIds)
                            {
                                var team = teamsResponse.Data.FirstOrDefault(t => t.Id == teamId);
                                if (team != null)
                                {
                                    teamNameMap[teamId] = team.TeamName ?? teamId;
                                    _logger.LogDebug("Mapped team {TeamId} to name '{TeamName}'", teamId, team.TeamName);
                                }
                                else
                                {
                                    // If team not found, use ID as fallback
                                    teamNameMap[teamId] = $"Team_{teamId}";
                                    _logger.LogDebug("Team {TeamId} not found in IDM, using fallback name", teamId);
                                }
                            }

                            _logger.LogInformation("Successfully mapped {MappedCount} team names", teamNameMap.Count);
                        }
                        else
                        {
                            _logger.LogWarning("No teams returned from IDM");
                            // Use IDs as fallback
                            foreach (var teamId in validTeamIds)
                            {
                                teamNameMap[teamId] = $"Team_{teamId}";
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("IdmUserDataClient not available, using team IDs as names");
                        // Use IDs as fallback
                        foreach (var teamId in validTeamIds)
                        {
                            teamNameMap[teamId] = $"Team_{teamId}";
                        }
                    }
                }
                catch (Exception idmEx)
                {
                    _logger.LogWarning(idmEx, "Failed to fetch teams from IDM, using team IDs as names");
                    // Use IDs as fallback
                    foreach (var teamId in validTeamIds)
                    {
                        teamNameMap[teamId] = $"Team_{teamId}";
                    }
                }

                _logger.LogInformation("GetTeamNamesByIdsAsync completed successfully. Mapped {MappedCount} teams out of {RequestedCount} requested",
                    teamNameMap.Count, validTeamIds.Count);

                return teamNameMap;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting team names by IDs");
                // Return empty dictionary instead of throwing to allow graceful degradation
                return new Dictionary<string, string>();
            }
        }
    }
}