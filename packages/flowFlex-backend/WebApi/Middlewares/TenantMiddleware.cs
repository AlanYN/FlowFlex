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

            // 详细记录租户ID的来源和值
            _logger.LogInformation($"[TenantMiddleware] Request: {context.Request.Method} {context.Request.Path}, TenantId: {tenantId}");

            // 记录当前请求的所有头部信息，用于调试
            _logger.LogDebug($"[TenantMiddleware] Request headers: {string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}={h.Value}"))}");

            // 记录当前用户的Claims信息，用于调试
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                _logger.LogDebug($"[TenantMiddleware] User claims: {string.Join(", ", context.User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
            }

            // Ensure tenant ID is in request headers
            if (!context.Request.Headers.ContainsKey("X-Tenant-Id"))
            {
                context.Request.Headers["X-Tenant-Id"] = tenantId;
                _logger.LogDebug($"[TenantMiddleware] Added X-Tenant-Id header: {tenantId}");
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
                        _logger.LogDebug($"[TenantMiddleware] Overrode X-Tenant-Id header for Portal token: {tenantId}");
                    }
                }
            }

            // Add tenant ID to response headers (for debugging)
            context.Response.Headers["X-Response-Tenant-Id"] = tenantId;

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
                        _logger.LogDebug($"[TenantMiddleware] Found TenantId from Portal token: {validationResult.TenantId}");
                        return validationResult.TenantId;
                    }
                }
            }

            // Try to get tenant ID from multiple sources

            // 1. Get from X-Tenant-Id header
            var tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantId))
            {
                _logger.LogDebug($"[TenantMiddleware] Found TenantId from X-Tenant-Id header: {tenantId}");
                return tenantId;
            }

            // 2. Get from TenantId header
            tenantId = context.Request.Headers["TenantId"].FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantId))
            {
                _logger.LogDebug($"[TenantMiddleware] Found TenantId from TenantId header: {tenantId}");
                return tenantId;
            }

            // 3. Get from query parameters
            tenantId = context.Request.Query["tenantId"].FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantId))
            {
                _logger.LogDebug($"[TenantMiddleware] Found TenantId from query parameter: {tenantId}");
                return tenantId;
            }

            // 4. Get from JWT Token (if available)
            // Extract tenant ID from JWT token when authentication is implemented
            var tenantIdClaim = context.User?.FindFirst("tenantId");
            if (tenantIdClaim != null && !string.IsNullOrEmpty(tenantIdClaim.Value))
            {
                _logger.LogDebug($"[TenantMiddleware] Found TenantId from JWT claim: {tenantIdClaim.Value}");
                return tenantIdClaim.Value;
            }

            // 5. Infer tenant ID from user email domain (example logic)
            var userEmail = context.Request.Headers["X-User-Email"].FirstOrDefault();
            if (!string.IsNullOrEmpty(userEmail))
            {
                var domain = userEmail.Split('@').LastOrDefault();
                if (!string.IsNullOrEmpty(domain))
                {
                    // Can map email domain to tenant ID
                    tenantId = MapDomainToTenantId(domain);
                    if (!string.IsNullOrEmpty(tenantId))
                    {
                        _logger.LogDebug($"[TenantMiddleware] Inferred TenantId from email domain: {tenantId}");
                        return tenantId;
                    }
                }
            }

            // 6. 使用默认租户ID "default"
            tenantId = "default";
            _logger.LogDebug($"[TenantMiddleware] Using default TenantId: {tenantId}");
            return tenantId;
        }

        /// <summary>
        /// Map email domain to tenant ID
        /// </summary>
        private string MapDomainToTenantId(string domain)
        {
            // Here you can implement specific domain to tenant ID mapping logic
            // For example:
            // - company1.com -> tenant1
            // - company2.com -> tenant2
            // - gmail.com -> personal

            return domain switch
            {
                "company1.com" => "tenant1",
                "company2.com" => "tenant2",
                "test.com" => "test",
                _ => null // Return null indicates unable to infer
            };
        }
    }
}
