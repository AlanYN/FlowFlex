using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.Options;
using FlowFlex.Application.Contracts.Dtos.OW.User;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Shared;
using System.Text.Json;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// JWT service implementation
    /// </summary>
    public class JwtService : ISingletonService, IJwtService
    {
        private readonly JwtOptions _jwtOptions;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<JwtService> _logger;

        public JwtService(
            IOptions<JwtOptions> jwtOptions,
            IHttpContextAccessor httpContextAccessor,
            ILogger<JwtService> logger)
        {
            _jwtOptions = jwtOptions.Value;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;

            // Add validation to ensure configuration is loaded properly
            if (string.IsNullOrEmpty(_jwtOptions.SecretKey))
            {
                throw new InvalidOperationException("JWT SecretKey is not configured properly. Please check the Security:JwtSecretKey setting in appsettings.json");
            }

            if (string.IsNullOrEmpty(_jwtOptions.Issuer))
            {
                throw new InvalidOperationException("JWT Issuer is not configured properly. Please check the Security:JwtIssuer setting in appsettings.json");
            }

            if (string.IsNullOrEmpty(_jwtOptions.Audience))
            {
                throw new InvalidOperationException("JWT Audience is not configured properly. Please check the Security:JwtAudience setting in appsettings.json");
            }
        }

        /// <summary>
        /// Generate JWT token
        /// </summary>
        /// <param name="user">User entity</param>
        /// <returns>JWT token</returns>
        public string GenerateJwtToken(User user)
        {
            return GenerateToken(user.Id, user.Email, user.Username, user.TenantId);
        }

        /// <summary>
        /// Generate JWT token for portal access
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="email">User email</param>
        /// <param name="username">Username</param>
        /// <returns>JWT token</returns>
        public string GenerateJwtToken(long userId, string email, string username)
        {
            return GenerateToken(userId, email, username, "DEFAULT");
        }

        /// <summary>
        /// Generate JWT token
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="email">User email</param>
        /// <param name="username">Username</param>
        /// <returns>JWT token</returns>
        public string GenerateToken(long userId, string email, string username)
        {
            return GenerateToken(userId, email, username, "DEFAULT");
        }

        /// <summary>
        /// Generate JWT token
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="email">User email</param>
        /// <param name="username">Username</param>
        /// <param name="tenantId">Tenant ID</param>
        /// <returns>JWT token</returns>
        public string GenerateToken(long userId, string email, string username, string tenantId)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("username", username),
                new Claim("tenantId", tenantId ?? "DEFAULT"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Validate JWT token
        /// </summary>
        /// <param name="token">JWT token</param>
        /// <returns>Validation result</returns>
        public bool ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtOptions.SecretKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _jwtOptions.Issuer,
                    ValidAudience = _jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };

                tokenHandler.ValidateToken(token, validationParameters, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get user ID from token
        /// </summary>
        /// <param name="token">JWT token</param>
        /// <returns>User ID</returns>
        public long? GetUserIdFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);

                var userIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub || x.Type == ClaimTypes.NameIdentifier);
                if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId))
                {
                    return userId;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get user email from token
        /// </summary>
        /// <param name="token">JWT token</param>
        /// <returns>User email</returns>
        public string GetEmailFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);

                var emailClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Email || x.Type == ClaimTypes.Email);
                return emailClaim?.Value;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get token expiry time in seconds
        /// </summary>
        /// <returns>Expiry time in seconds</returns>
        public int GetTokenExpiryInSeconds()
        {
            return _jwtOptions.ExpiryMinutes * 60;
        }

        /// <summary>
        /// Generate JWT token with detailed information for token management
        /// </summary>
        public TokenDetailsDto GenerateTokenWithDetails(long userId, string email, string username, string tenantId = "DEFAULT", string tokenType = "login")
        {
            var jti = Guid.NewGuid().ToString();
            var issuedAt = DateTimeOffset.UtcNow;
            var expiresAt = issuedAt.AddMinutes(_jwtOptions.ExpiryMinutes);

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("username", username),
                new Claim("tenantId", tenantId ?? "DEFAULT"),
                new Claim(JwtRegisteredClaimNames.Jti, jti),
            };

            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: expiresAt.DateTime,
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            // Get IP and User Agent from HTTP context
            var httpContext = _httpContextAccessor.HttpContext;
            var ipAddress = GetClientIpAddress(httpContext);
            var userAgent = httpContext?.Request.Headers["User-Agent"].ToString() ?? string.Empty;

            return new TokenDetailsDto
            {
                Token = tokenString,
                Jti = jti,
                UserId = userId,
                UserEmail = email,
                IssuedAt = issuedAt,
                ExpiresAt = expiresAt,
                TokenType = tokenType,
                IssuedIp = ipAddress,
                UserAgent = userAgent
            };
        }

        /// <summary>
        /// Extract JTI from token
        /// </summary>
        public string GetJtiFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);

                var jtiClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti);
                return jtiClaim?.Value ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract JTI from token");
                return string.Empty;
            }
        }

        /// <summary>
        /// Get client IP address from HTTP context
        /// </summary>
        private static string GetClientIpAddress(HttpContext? httpContext)
        {
            if (httpContext == null) return string.Empty;

            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            
            // Check for forwarded IP (when behind proxy/load balancer)
            if (httpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                var forwardedIp = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(forwardedIp))
                {
                    ipAddress = forwardedIp.Split(',')[0].Trim();
                }
            }
            else if (httpContext.Request.Headers.ContainsKey("X-Real-IP"))
            {
                var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
                if (!string.IsNullOrEmpty(realIp))
                {
                    ipAddress = realIp;
                }
            }

            return ipAddress ?? string.Empty;
        }

        /// <summary>
        /// Refresh JWT Token
        /// </summary>
        /// <param name="token">Current JWT Token</param>
        /// <returns>New JWT Token</returns>
        public string RefreshToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();

                // First, read the token to extract claims (even if expired)
                JwtSecurityToken jwtToken;
                try
                {
                    jwtToken = tokenHandler.ReadJwtToken(token);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Unable to read token: {ex.Message}");
                }

                // Extract user information from the token
                var userIdClaim = jwtToken.Claims.FirstOrDefault(x =>
                    x.Type == JwtRegisteredClaimNames.Sub || x.Type == ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
                {
                    throw new InvalidOperationException("User ID not found in token");
                }

                var emailClaim = jwtToken.Claims.FirstOrDefault(x =>
                    x.Type == JwtRegisteredClaimNames.Email || x.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    throw new InvalidOperationException("Email not found in token");
                }

                var usernameClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "username");
                var username = usernameClaim?.Value ?? emailClaim.Value;

                var tenantIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "tenantId");
                var tenantId = tenantIdClaim?.Value ?? "DEFAULT";

                // Generate a new token with the same user information
                return GenerateToken(userId, emailClaim.Value, username, tenantId);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Token refresh failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Parse third-party JWT token without signature validation
        /// </summary>
        /// <param name="token">Third-party JWT token to parse</param>
        /// <returns>JWT token information</returns>
        public JwtTokenInfoDto ParseThirdPartyToken(string token)
        {
            var result = new JwtTokenInfoDto();

            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Token is null or empty";
                    return result;
                }

                var tokenHandler = new JwtSecurityTokenHandler();

                // Read the token without validation to get basic info
                JwtSecurityToken jwtToken;
                try
                {
                    jwtToken = tokenHandler.ReadJwtToken(token);
                }
                catch (Exception ex)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Unable to read token: {ex.Message}";
                    return result;
                }

                // Extract basic information
                result.Issuer = jwtToken.Issuer;
                result.Audience = jwtToken.Audiences?.FirstOrDefault();
                result.IssuedAt = jwtToken.IssuedAt;
                result.ExpiresAt = jwtToken.ValidTo;
                result.IsExpired = jwtToken.ValidTo < DateTime.UtcNow;

                // Extract claims
                foreach (var claim in jwtToken.Claims)
                {
                    result.Claims[claim.Type] = claim.Value;
                }

                // Extract specific user information
                var userIdClaim = jwtToken.Claims.FirstOrDefault(x =>
                    x.Type == JwtRegisteredClaimNames.Sub || x.Type == ClaimTypes.NameIdentifier || x.Type == "userId");
                if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId))
                {
                    result.UserId = userId;
                }

                // Try multiple email claim types
                var emailClaim = jwtToken.Claims.FirstOrDefault(x =>
                    x.Type == JwtRegisteredClaimNames.Email || x.Type == ClaimTypes.Email || x.Type == "email" ||
                    x.Type == "mail" || x.Type == "emailaddress" || x.Type == "email_address");
                result.Email = emailClaim?.Value;

                // Try multiple username claim types
                var usernameClaim = jwtToken.Claims.FirstOrDefault(x =>
                    x.Type == "username" || x.Type == "userName" || x.Type == "preferred_username" || 
                    x.Type == "name" || x.Type == "given_name" || x.Type == "sub" || 
                    x.Type == JwtRegisteredClaimNames.Sub || x.Type == ClaimTypes.NameIdentifier);
                result.Username = usernameClaim?.Value;

                // If email is empty, use username as fallback
                if (string.IsNullOrWhiteSpace(result.Email) && !string.IsNullOrWhiteSpace(result.Username))
                {
                    result.Email = result.Username;
                    _logger.LogWarning("Email claim not found in token, using username as email: {Username}", result.Username);
                }

                // Check for data field which might contain nested user information
                var dataClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "data");
                if (dataClaim != null && string.IsNullOrWhiteSpace(result.Email))
                {
                    try
                    {
                        // Try to parse data as JSON
                        var dataJson = JsonSerializer.Deserialize<JsonElement>(dataClaim.Value);
                        
                        // Check for email in data
                        if (dataJson.TryGetProperty("user_name", out var userName) && userName.ValueKind == JsonValueKind.String)
                        {
                            if (string.IsNullOrWhiteSpace(result.Username))
                            {
                                result.Username = userName.GetString();
                                _logger.LogInformation("Extracted username from data.user_name: {Username}", result.Username);
                            }
                            
                            // If still no email, use username from data
                            if (string.IsNullOrWhiteSpace(result.Email))
                            {
                                result.Email = userName.GetString();
                                _logger.LogWarning("No email found, using data.user_name as email: {Email}", result.Email);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse data claim as JSON");
                    }
                }

                // Try multiple tenant ID claim types
                var tenantIdClaim = jwtToken.Claims.FirstOrDefault(x =>
                    x.Type == "tenantId" || x.Type == "tenant_id" || x.Type == "tenants");
                result.TenantId = tenantIdClaim?.Value;

                var jtiClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti);
                result.JwtId = jtiClaim?.Value;

                // For third-party tokens, we consider them valid if we can extract basic info and they're not expired
                result.IsValid = !string.IsNullOrWhiteSpace(result.Email) && !result.IsExpired;
                
                if (!result.IsValid && string.IsNullOrWhiteSpace(result.Email))
                {
                    result.ErrorMessage = "Email not found in token claims and no username available as fallback";
                }
                else if (!result.IsValid && result.IsExpired)
                {
                    result.ErrorMessage = "Token has expired";
                }

                _logger.LogInformation("Parsed third-party token - Issuer: {Issuer}, Email: {Email}, Valid: {IsValid}", 
                    result.Issuer, result.Email, result.IsValid);
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Token parsing failed: {ex.Message}";
                _logger.LogError(ex, "Failed to parse third-party token");
            }

            return result;
        }

        /// <summary>
        /// Parse JWT Token and return detailed information
        /// </summary>
        /// <param name="token">JWT Token</param>
        /// <returns>JWT Token information</returns>
        public JwtTokenInfoDto ParseToken(string token)
        {
            var result = new JwtTokenInfoDto();

            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Token is null or empty";
                    return result;
                }

                var tokenHandler = new JwtSecurityTokenHandler();

                // First, try to read the token without validation to get basic info
                JwtSecurityToken jwtToken;
                try
                {
                    jwtToken = tokenHandler.ReadJwtToken(token);
                }
                catch (Exception ex)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Unable to read token: {ex.Message}";
                    return result;
                }

                // Extract basic information
                result.Issuer = jwtToken.Issuer;
                result.Audience = jwtToken.Audiences?.FirstOrDefault();
                result.IssuedAt = jwtToken.IssuedAt;
                result.ExpiresAt = jwtToken.ValidTo;
                result.IsExpired = jwtToken.ValidTo < DateTime.UtcNow;

                // Extract claims
                foreach (var claim in jwtToken.Claims)
                {
                    result.Claims[claim.Type] = claim.Value;
                }

                // Extract specific user information
                var userIdClaim = jwtToken.Claims.FirstOrDefault(x =>
                    x.Type == JwtRegisteredClaimNames.Sub || x.Type == ClaimTypes.NameIdentifier);
                if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId))
                {
                    result.UserId = userId;
                }

                // Try multiple email claim types
                var emailClaim = jwtToken.Claims.FirstOrDefault(x =>
                    x.Type == JwtRegisteredClaimNames.Email || x.Type == ClaimTypes.Email || x.Type == "email" ||
                    x.Type == "mail" || x.Type == "emailaddress" || x.Type == "email_address");
                result.Email = emailClaim?.Value;

                // Try multiple username claim types
                var usernameClaim = jwtToken.Claims.FirstOrDefault(x =>
                    x.Type == "username" || x.Type == "userName" || x.Type == "preferred_username" || 
                    x.Type == "name" || x.Type == "given_name" || x.Type == "sub" || 
                    x.Type == JwtRegisteredClaimNames.Sub || x.Type == ClaimTypes.NameIdentifier);
                result.Username = usernameClaim?.Value;

                // If email is empty, use username as fallback
                if (string.IsNullOrWhiteSpace(result.Email) && !string.IsNullOrWhiteSpace(result.Username))
                {
                    result.Email = result.Username;
                    _logger.LogWarning("Email claim not found in token, using username as email: {Username}", result.Username);
                }

                // Check for data field which might contain nested user information
                var dataClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "data");
                if (dataClaim != null && string.IsNullOrWhiteSpace(result.Email))
                {
                    try
                    {
                        // Try to parse data as JSON
                        var dataJson = JsonSerializer.Deserialize<JsonElement>(dataClaim.Value);
                        
                        // Check for email in data
                        if (dataJson.TryGetProperty("user_name", out var userName) && userName.ValueKind == JsonValueKind.String)
                        {
                            if (string.IsNullOrWhiteSpace(result.Username))
                            {
                                result.Username = userName.GetString();
                                _logger.LogInformation("Extracted username from data.user_name: {Username}", result.Username);
                            }
                            
                            // If still no email, use username from data
                            if (string.IsNullOrWhiteSpace(result.Email))
                            {
                                result.Email = userName.GetString();
                                _logger.LogWarning("No email found, using data.user_name as email: {Email}", result.Email);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse data claim as JSON");
                    }
                }

                var tenantIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "tenantId");
                result.TenantId = tenantIdClaim?.Value;

                var jtiClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti);
                result.JwtId = jtiClaim?.Value;

                // Now validate the token
                try
                {
                    var key = Encoding.UTF8.GetBytes(_jwtOptions.SecretKey);
                    var validationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = _jwtOptions.Issuer,
                        ValidAudience = _jwtOptions.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ClockSkew = TimeSpan.Zero
                    };

                    tokenHandler.ValidateToken(token, validationParameters, out _);
                    result.IsValid = true;
                }
                catch (SecurityTokenExpiredException)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Token has expired";
                }
                catch (SecurityTokenInvalidSignatureException)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Token signature is invalid";
                }
                catch (SecurityTokenInvalidIssuerException)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Token issuer is invalid";
                }
                catch (SecurityTokenInvalidAudienceException)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Token audience is invalid";
                }
                catch (Exception ex)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Token validation failed: {ex.Message}";
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Token parsing failed: {ex.Message}";
            }

            return result;
        }
    }
}