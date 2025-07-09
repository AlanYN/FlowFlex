using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using FlowFlex.Application.Contracts.Options;
using FlowFlex.WebApi.Model.Response;

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
            try
            {
                // Check if authentication is required
                if (IsPublicEndpoint(context.Request.Path))
                {
                    await _next(context);
                    return;
                }

                // Extract JWT token
                var token = ExtractToken(context);

                if (string.IsNullOrEmpty(token))
                {
                    await HandleUnauthorizedAsync(context, "Missing authentication token");
                    return;
                }

                // Validate JWT token
                var principal = ValidateToken(token);

                if (principal == null)
                {
                    await HandleUnauthorizedAsync(context, "Invalid authentication token");
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "JWT authentication middleware error");
                await HandleUnauthorizedAsync(context, "Authentication error");
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
                "/swagger",
                "/health"
            };

            return publicPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
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
        /// Handle unauthorized request
        /// </summary>
        private async Task HandleUnauthorizedAsync(HttpContext context, string message)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = "application/json";

            var response = new ApiResponse<object>
            {
                Code = (int)HttpStatusCode.Unauthorized,
                Message = message,
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
