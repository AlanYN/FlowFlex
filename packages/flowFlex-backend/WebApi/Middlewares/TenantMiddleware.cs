using Microsoft.AspNetCore.Http;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Application.Contracts.IServices.OW;
using System.Threading.Tasks;

namespace FlowFlex.WebApi.Middlewares
{
    /// <summary>
    /// Tenant Middleware - Ensure each request has the correct tenant ID
    /// </summary>
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantMiddleware> _logger;

        public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IPortalTokenService portalTokenService)
        {
            // Get tenant ID
            var tenantId = GetTenantId(context, portalTokenService);

            _logger.LogDebug("[TenantMiddleware] Request: {Method} {Path}, TenantId: {TenantId}",
                context.Request.Method, context.Request.Path, tenantId);

            // Ensure tenant ID is in request headers
            if (!context.Request.Headers.ContainsKey("X-Tenant-Id"))
            {
                context.Request.Headers["X-Tenant-Id"] = tenantId;
            }
            else
            {
                // For Portal tokens, override the header with token's tenantId
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    var token = authHeader.Substring("Bearer ".Length).Trim();
                    if (portalTokenService.IsPortalToken(token))
                    {
                        context.Request.Headers["X-Tenant-Id"] = tenantId;
                    }
                }
            }

            await _next(context);
        }

        private string GetTenantId(HttpContext context, IPortalTokenService portalTokenService)
        {
            // For Portal tokens, always use tenantId from token (security requirement)
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                if (portalTokenService.IsPortalToken(token))
                {
                    var validationResult = portalTokenService.ValidatePortalToken(token);
                    if (validationResult.IsValid && !string.IsNullOrEmpty(validationResult.TenantId))
                    {
                        _logger.LogDebug("[TenantMiddleware] Resolved TenantId from Portal token: {TenantId}", validationResult.TenantId);
                        return validationResult.TenantId;
                    }
                }
            }

            // Try to get tenant ID from trusted sources only

            // 1. Get from X-Tenant-Id header
            var tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantId))
            {
                return tenantId;
            }

            // 2. Get from TenantId header
            tenantId = context.Request.Headers["TenantId"].FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantId))
            {
                return tenantId;
            }

            // 3. Get from JWT Token (if available)
            var tenantIdClaim = context.User?.FindFirst("tenantId");
            if (tenantIdClaim != null && !string.IsNullOrEmpty(tenantIdClaim.Value))
            {
                return tenantIdClaim.Value;
            }

            // 4. Use default tenant ID
            return "default";
        }
    }
}
