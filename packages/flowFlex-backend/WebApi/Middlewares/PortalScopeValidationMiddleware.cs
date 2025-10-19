using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Filter;
using Microsoft.AspNetCore.Authorization;

namespace FlowFlex.WebApi.Middlewares
{
    /// <summary>
    /// Middleware to validate Portal token scope and restrict access to non-Portal endpoints
    /// Portal tokens (scope: portal) can only access endpoints marked with [PortalAccess] attribute
    /// </summary>
    public class PortalScopeValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PortalScopeValidationMiddleware> _logger;

        public PortalScopeValidationMiddleware(
            RequestDelegate next,
            ILogger<PortalScopeValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IPortalTokenService portalTokenService)
        {
            // Skip validation for non-API endpoints
            if (!context.Request.Path.StartsWithSegments("/api"))
            {
                await _next(context);
                return;
            }

            // Skip validation for endpoints that don't require authentication
            var endpoint = context.GetEndpoint();
            if (endpoint == null)
            {
                await _next(context);
                return;
            }

            // Check if endpoint allows anonymous access
            var allowAnonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>() != null;
            if (allowAnonymous)
            {
                await _next(context);
                return;
            }

            // Get authorization header
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                // No token provided, let authentication middleware handle it
                await _next(context);
                return;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();

            // Check if this is a Portal token
            var isPortalToken = portalTokenService.IsPortalToken(token);

            if (isPortalToken)
            {
                // This is a Portal token - check if endpoint allows Portal access
                var portalAccessAttr = endpoint.Metadata.GetMetadata<PortalAccessAttribute>();

                if (portalAccessAttr == null)
                {
                    // Portal token trying to access non-Portal endpoint
                    _logger.LogWarning(
                        "Portal token attempted to access non-Portal endpoint: {Path}. Access denied.",
                        context.Request.Path);

                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(@"{
                        ""success"": false,
                        ""message"": ""Portal tokens can only access Portal-specific endpoints. This endpoint requires a regular user token."",
                        ""code"": ""PORTAL_SCOPE_VIOLATION""
                    }");
                    return;
                }

                _logger.LogInformation(
                    "Portal token accessing Portal endpoint: {Path}",
                    context.Request.Path);
            }
            else
            {
                // This is a regular token - check if endpoint is Portal-only
                var portalAccessAttr = endpoint.Metadata.GetMetadata<PortalAccessAttribute>();

                if (portalAccessAttr != null && portalAccessAttr.PortalOnly)
                {
                    // Regular token trying to access Portal-only endpoint
                    _logger.LogWarning(
                        "Regular token attempted to access Portal-only endpoint: {Path}. Access denied.",
                        context.Request.Path);

                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(@"{
                        ""success"": false,
                        ""message"": ""This endpoint is restricted to Portal access only."",
                        ""code"": ""PORTAL_ONLY_ENDPOINT""
                    }");
                    return;
                }
            }

            // Token scope is valid for this endpoint
            await _next(context);
        }
    }
}

