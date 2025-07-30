using System;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
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

        public UserService(
            IUserRepository userRepository,
            IEmailService emailService,
            IJwtService jwtService,
            IAccessTokenService accessTokenService,
            IMapper mapper,
            ILogger<UserService> logger,
            IHttpContextAccessor httpContextAccessor,
            IOptions<EmailOptions> emailOptions,
            IUserContextService userContextService)
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
                    existingUser.ModifyDate = DateTimeOffset.Now;
                    existingUser.ModifyBy = request.Email;

                    await _userRepository.UpdateAsync(existingUser);

                    // Send appropriate email based on previous verification status (non-blocking)
                    _ = Task.Run(async () =>
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
                        if (existingUser.VerificationCodeExpiry >= DateTimeOffset.Now)
                        {
                            _logger.LogInformation("Allowing password reset for verified user with valid verification code: {Email}", request.Email);
                            
                            // Update password for existing verified user
                            existingUser.PasswordHash = BC.HashPassword(request.Password);
                            existingUser.ModifyDate = DateTimeOffset.Now;
                            existingUser.ModifyBy = request.Email;
                            // Clear verification code after use
                            existingUser.EmailVerificationCode = null;
                            existingUser.VerificationCodeExpiry = null;

                            await _userRepository.UpdateAsync(existingUser);

                            // Send password reset confirmation email (non-blocking)
                            _ = Task.Run(async () =>
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
                    if (existingUser.VerificationCodeExpiry < DateTimeOffset.Now)
                    {
                        throw new CRMException(System.Net.HttpStatusCode.OK, "Verification code has expired");
                    }

                    // Update user information and verify email
                    existingUser.Username = request.Email; // Use email as username
                    existingUser.PasswordHash = BC.HashPassword(request.Password);
                    existingUser.EmailVerified = true;
                    existingUser.Status = "active";
                    existingUser.ModifyDate = DateTimeOffset.Now;
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
                    TenantId = "DEFAULT" // Set default tenant ID
                };

                // Initialize create information
                newUser.InitCreateInfo(null);

                // Save new user
                await _userRepository.InsertAsync(newUser);

                // Send welcome email (non-blocking)
                _ = Task.Run(async () =>
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
                    VerificationCodeExpiry = DateTimeOffset.Now.AddMinutes(_emailOptions.VerificationCodeExpiryMinutes),
                    TenantId = "DEFAULT" // Set default tenant ID
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
                user.VerificationCodeExpiry = DateTimeOffset.Now.AddMinutes(_emailOptions.VerificationCodeExpiryMinutes);
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
            if (user.VerificationCodeExpiry < DateTimeOffset.Now)
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
                AppCode = "DEFAULT",
                TenantId = user.TenantId ?? "DEFAULT"
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
            if (user.VerificationCodeExpiry < DateTimeOffset.Now)
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
            user.ModifyDate = DateTimeOffset.Now;
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
                AppCode = "DEFAULT",
                TenantId = user.TenantId ?? "DEFAULT"
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
            user.ModifyDate = DateTimeOffset.Now;
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
                TenantId = "DEFAULT" // Set default tenant ID
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
                    AppCode = "DEFAULT",
                    TenantId = user.TenantId ?? "DEFAULT"
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
                if (string.IsNullOrWhiteSpace(email))
                {
                    throw new Exception("Email not found in authorization token");
                }

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
                    user.LastLoginDate = DateTimeOffset.Now;
                    user.Status = "active"; // Ensure user is active
                    user.EmailVerified = true; // Trust third-party verification

                    user.ModifyDate = DateTimeOffset.Now;
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
                    user.LastLoginDate = DateTimeOffset.Now;
                    user.Status = "active";
                    user.EmailVerified = true;
                    user.ModifyDate = DateTimeOffset.Now;
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
                        LastLoginDate = DateTimeOffset.Now,
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
    }
}