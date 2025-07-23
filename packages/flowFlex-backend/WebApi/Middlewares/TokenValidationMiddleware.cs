using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.WebApi.Model.Response;
using FlowFlex.Domain.Shared;

namespace FlowFlex.WebApi.Middlewares
{
    /// <summary>
    /// Token validation middleware for checking token status in database
    /// </summary>
    public class TokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TokenValidationMiddleware> _logger;

        public TokenValidationMiddleware(
            RequestDelegate next,
            ILogger<TokenValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check if authentication is required
            if (IsPublicEndpoint(context.Request.Path) || !context.User.Identity.IsAuthenticated)
            {
                await _next(context);
                return;
            }

            try
            {
                // Extract JWT token
                var token = ExtractToken(context);

                if (!string.IsNullOrEmpty(token))
                {
                    // Validate token in database
                    var isTokenValid = await ValidateTokenInDatabaseAsync(context, token);
                    if (!isTokenValid)
                    {
                        await HandleUnauthorizedAsync(context, "Token has been revoked or is no longer valid");
                        return;
                    }
                }

                await _next(context);
            }
            catch (CRMException)
            {
                // Let business logic exceptions pass through to GlobalExceptionHandlingMiddleware
                throw;
            }
            catch (SecurityTokenException)
            {
                // Handle JWT token specific exceptions
                await HandleUnauthorizedAsync(context, "Invalid token");
            }
            catch (UnauthorizedAccessException)
            {
                // Handle authorization exceptions
                await HandleUnauthorizedAsync(context, "Access denied");
            }
            catch (Exception ex)
            {
                // Log error but let other exceptions pass through to GlobalExceptionHandlingMiddleware
                _logger.LogError(ex, "Unexpected error in token validation middleware for endpoint: {Path}", context.Request.Path);
                throw;
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
                "/health"
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

                _logger.LogDebug("Validating token with JTI: {Jti}", jti);

                // Check if token is active in database
                var isActive = await accessTokenService.ValidateTokenAsync(jti);
                
                if (isActive)
                {
                    // Update last used time
                    await accessTokenService.UpdateTokenUsageAsync(jti);
                    _logger.LogDebug("Token {Jti} is valid and last used time updated", jti);
                }
                else
                {
                    _logger.LogWarning("Token {Jti} is not active or not found in database", jti);
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