using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.Options;
using FlowFlex.WebApi.Model.Response;
using FlowFlex.Domain.Shared;

namespace FlowFlex.WebApi.Middlewares
{
    /// <summary>
    /// JWT authentication middleware
    /// </summary>
    public class JwtAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtAuthenticationMiddleware> _logger;
        private readonly JwtOptions _jwtOptions;

        public JwtAuthenticationMiddleware(
            RequestDelegate next,
            ILogger<JwtAuthenticationMiddleware> logger,
            IOptions<JwtOptions> jwtOptions)
        {
            _next = next;
            _logger = logger;
            _jwtOptions = jwtOptions.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check if authentication is required
            if (IsPublicEndpoint(context.Request.Path))
            {
                await _next(context);
                return;
            }

            try
            {
                // Extract JWT token
                var token = ExtractToken(context);

                if (string.IsNullOrEmpty(token))
                {
                    await HandleUnauthorizedAsync(context, "Missing authentication token");
                    return;
                }

                // Validate JWT token signature and expiration
                var principal = ValidateToken(token);

                if (principal == null)
                {
                    await HandleUnauthorizedAsync(context, "Invalid authentication token");
                    return;
                }

                // Additional validation: Check if token is active in database
                var isTokenActive = await ValidateTokenInDatabaseAsync(context, token);
                if (!isTokenActive)
                {
                    await HandleUnauthorizedAsync(context, "Token has been revoked");
                    return;
                }

                // Set user context
                context.User = principal;

                // Add user information to request headers
                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userEmail = principal.FindFirst(ClaimTypes.Email)?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    context.Items["UserId"] = userId;
                }

                if (!string.IsNullOrEmpty(userEmail))
                {
                    context.Items["UserEmail"] = userEmail;
                }

                _logger.LogDebug("JWT authentication successful for user: {UserId}", userId);

                await _next(context);
            }
            catch (SecurityTokenExpiredException)
            {
                await HandleUnauthorizedAsync(context, "Token has expired");
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                await HandleUnauthorizedAsync(context, "Invalid token signature");
            }
            catch (SecurityTokenException ex)
            {
                await HandleUnauthorizedAsync(context, $"Invalid token: {ex.Message}");
            }
            catch (CRMException)
            {
                // Let business logic exceptions pass through to GlobalExceptionHandlingMiddleware
                throw;
            }
            catch (InvalidOperationException)
            {
                // Let business logic exceptions (like file type validation) pass through
                throw;
            }
            catch (Exception ex)
            {
                // Only handle authentication/authorization related errors here
                // Let all other exceptions pass through to GlobalExceptionHandlingMiddleware
                if (ex.Message.Contains("token") || ex.Message.Contains("authentication") || ex.Message.Contains("authorization"))
                {
                    _logger.LogError(ex, "JWT authentication middleware error for endpoint: {Path}", context.Request.Path);
                    await HandleUnauthorizedAsync(context, "Authentication error");
                }
                else
                {
                    // Let business logic exceptions pass through
                    throw;
                }
            }
        }

        /// <summary>
        /// Check if endpoint is public
        /// </summary>
        private static bool IsPublicEndpoint(PathString path)
        {
            var publicPaths = new[]
            {
                "/api/ow/users/login",
                "/api/ow/users/login-with-code",
                "/api/ow/users/register",
                "/api/ow/users/send-verification-code",
                "/api/ow/users/verify-email",
                "/api/ow/users/check-email",
                "/api/ow/users/third-party-login",
                "/api/ow/user-invitations/v1/verify-access",
                "/api/ow/user-invitations/v1/verify-access-short",
                "/swagger",
                "/health",
                "/uploads"
            };

            var isPublic = publicPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
            
            return isPublic;
        }

        /// <summary>
        /// Extract JWT token from request
        /// </summary>
        private static string ExtractToken(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authHeader.Substring("Bearer ".Length).Trim();
            }

            return null;
        }

        /// <summary>
        /// Validate JWT token
        /// </summary>
        private ClaimsPrincipal ValidateToken(string token)
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

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

                // Ensure token is JWT token
                if (validatedToken is not JwtSecurityToken jwtToken ||
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new SecurityTokenException("Invalid token algorithm");
                }

                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "JWT token validation failed");
                return null;
            }
        }

        /// <summary>
        /// Validate token in database
        /// </summary>
        private async Task<bool> ValidateTokenInDatabaseAsync(HttpContext context, string token)
        {
            try
            {
                // Get services from DI container
                using var scope = context.RequestServices.CreateScope();
                var jwtService = scope.ServiceProvider.GetService<IJwtService>();
                var accessTokenService = scope.ServiceProvider.GetService<IAccessTokenService>();

                if (jwtService == null || accessTokenService == null)
                {
                    _logger.LogWarning("Required services not found for token validation");
                    return false; // Fail safe - if services not available, reject token
                }

                // Extract JTI from token
                var jti = jwtService.GetJtiFromToken(token);
                if (string.IsNullOrEmpty(jti))
                {
                    _logger.LogWarning("JTI not found in token");
                    return false;
                }

                // Check if token is active in database
                var isActive = await accessTokenService.ValidateTokenAsync(jti);
                
                if (isActive)
                {
                    // Update last used time
                    await accessTokenService.UpdateTokenUsageAsync(jti);
                }

                return isActive;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token in database");
                return false; // Fail safe
            }
        }

        /// <summary>
        /// Handle unauthorized request
        /// </summary>
        private async Task HandleUnauthorizedAsync(HttpContext context, string message)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = "application/json";

            var response = new ApiResponse<object>
            {
                Code = (int)HttpStatusCode.Unauthorized,
                Message = message, // 设置Message字段
                Msg = message, // 同时设置Msg字段以保持兼容性
                Data = null
            };

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(response, options);
            await context.Response.WriteAsync(json);

            _logger.LogWarning("Unauthorized request: {Message}, Path: {Path}", message, context.Request.Path);
        }
    }
}
