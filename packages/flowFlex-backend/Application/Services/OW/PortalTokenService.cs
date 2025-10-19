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
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Portal Token Service - Generate and validate Portal-specific JWT tokens
    /// Portal tokens have limited scope and can only access Portal-related endpoints
    /// </summary>
    public class PortalTokenService : ISingletonService, IPortalTokenService
    {
        private readonly JwtOptions _jwtOptions;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<PortalTokenService> _logger;

        // Portal token constants
        private const string PORTAL_SCOPE = "portal";
        private const string PORTAL_TOKEN_TYPE = "portal-access";
        private const int PORTAL_TOKEN_EXPIRY_HOURS = 24; // 24 hours expiry for portal tokens

        public PortalTokenService(
            IOptions<JwtOptions> jwtOptions,
            IHttpContextAccessor httpContextAccessor,
            ILogger<PortalTokenService> logger)
        {
            _jwtOptions = jwtOptions.Value;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;

            // Validate configuration
            if (string.IsNullOrEmpty(_jwtOptions.SecretKey))
            {
                throw new InvalidOperationException("JWT SecretKey is not configured properly");
            }
        }

        /// <summary>
        /// Generate Portal-specific JWT token with limited scope
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="email">User email</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="tenantId">Tenant ID</param>
        /// <returns>Portal token details</returns>
        public TokenDetailsDto GeneratePortalToken(long userId, string email, long onboardingId, string tenantId = "DEFAULT")
        {
            var jti = Guid.NewGuid().ToString();
            var issuedAt = DateTimeOffset.UtcNow;
            var expiresAt = issuedAt.AddHours(PORTAL_TOKEN_EXPIRY_HOURS);

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Portal token claims - minimal information with scope restriction
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("username", email), // Use email as username for portal users
                new Claim("tenantId", tenantId ?? "DEFAULT"),
                new Claim("onboardingId", onboardingId.ToString()),
                new Claim("scope", PORTAL_SCOPE), // Critical: Portal scope restriction
                new Claim("token_type", PORTAL_TOKEN_TYPE), // Mark as portal token
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

            _logger.LogInformation(
                "Generated Portal token for user {Email} with onboarding {OnboardingId}, scope: {Scope}, expires: {ExpiresAt}",
                email, onboardingId, PORTAL_SCOPE, expiresAt);

            return new TokenDetailsDto
            {
                Token = tokenString,
                Jti = jti,
                UserId = userId,
                UserEmail = email,
                IssuedAt = issuedAt,
                ExpiresAt = expiresAt,
                TokenType = PORTAL_TOKEN_TYPE,
                IssuedIp = ipAddress,
                UserAgent = userAgent
            };
        }

        /// <summary>
        /// Validate Portal token and extract claims
        /// </summary>
        /// <param name="token">Portal JWT token</param>
        /// <returns>Portal token validation result</returns>
        public PortalTokenValidationResult ValidatePortalToken(string token)
        {
            var result = new PortalTokenValidationResult
            {
                IsValid = false
            };

            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    result.ErrorMessage = "Token is null or empty";
                    return result;
                }

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

                // Validate token
                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                var jwtToken = validatedToken as JwtSecurityToken;

                if (jwtToken == null)
                {
                    result.ErrorMessage = "Invalid token format";
                    return result;
                }

                // Extract and validate scope
                var scopeClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "scope");
                if (scopeClaim == null || scopeClaim.Value != PORTAL_SCOPE)
                {
                    result.ErrorMessage = "Invalid token scope - not a portal token";
                    _logger.LogWarning("Token validation failed: Invalid scope. Expected '{ExpectedScope}', got '{ActualScope}'",
                        PORTAL_SCOPE, scopeClaim?.Value ?? "null");
                    return result;
                }

                // Extract token type
                var tokenTypeClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "token_type");
                if (tokenTypeClaim == null || tokenTypeClaim.Value != PORTAL_TOKEN_TYPE)
                {
                    result.ErrorMessage = "Invalid token type";
                    _logger.LogWarning("Token validation failed: Invalid token type. Expected '{ExpectedType}', got '{ActualType}'",
                        PORTAL_TOKEN_TYPE, tokenTypeClaim?.Value ?? "null");
                    return result;
                }

                // Extract user information
                var userIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub || x.Type == ClaimTypes.NameIdentifier);
                if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId))
                {
                    result.UserId = userId;
                }

                var emailClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Email || x.Type == ClaimTypes.Email);
                result.Email = emailClaim?.Value;

                var onboardingIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "onboardingId");
                if (onboardingIdClaim != null && long.TryParse(onboardingIdClaim.Value, out var onboardingId))
                {
                    result.OnboardingId = onboardingId;
                }

                var tenantIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "tenantId");
                result.TenantId = tenantIdClaim?.Value;

                var jtiClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti);
                result.Jti = jtiClaim?.Value;

                result.Scope = PORTAL_SCOPE;
                result.IsValid = true;

                _logger.LogInformation(
                    "Portal token validated successfully for user {Email}, onboarding {OnboardingId}",
                    result.Email, result.OnboardingId);
            }
            catch (SecurityTokenExpiredException)
            {
                result.ErrorMessage = "Token has expired";
                _logger.LogWarning("Portal token validation failed: Token expired");
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                result.ErrorMessage = "Token signature is invalid";
                _logger.LogWarning("Portal token validation failed: Invalid signature");
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Token validation failed: {ex.Message}";
                _logger.LogError(ex, "Portal token validation error");
            }

            return result;
        }

        /// <summary>
        /// Check if a token is a Portal token by examining its claims
        /// </summary>
        /// <param name="token">JWT token</param>
        /// <returns>True if token is a portal token</returns>
        public bool IsPortalToken(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                    return false;

                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);

                var scopeClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "scope");
                var tokenTypeClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "token_type");

                return scopeClaim?.Value == PORTAL_SCOPE && tokenTypeClaim?.Value == PORTAL_TOKEN_TYPE;
            }
            catch
            {
                return false;
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
    }
}

