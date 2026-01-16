using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FlowFlex.Domain.Shared.Models;
using System.Threading.Tasks;
using AppContext = FlowFlex.Domain.Shared.Models.AppContext;
using System;
using System.Linq;
using System.Security.Claims;

namespace FlowFlex.WebApi.Middlewares
{
    /// <summary>
    /// Application isolation middleware - Ensure each request has correct app code and tenant ID for data isolation
    /// </summary>
    public class AppIsolationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AppIsolationMiddleware> _logger;

        public AppIsolationMiddleware(RequestDelegate next, ILogger<AppIsolationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var requestId = GenerateRequestId();

            try
            {
                // Extract AppCode from request
                var appCode = await ExtractAppCodeAsync(context);
                context.Items["AppCode"] = appCode;

                // Extract TenantId with priority order
                var tenantId = await ExtractTenantIdAsync(context, appCode);
                context.Items["TenantId"] = tenantId;

                // Create AppContext object and store in HttpContext.Items for data isolation
                var appContext = new AppContext
                {
                    AppCode = appCode,
                    TenantId = tenantId,
                    RequestId = requestId,
                    ClientIp = GetClientIpAddress(context),
                    UserAgent = context.Request.Headers["User-Agent"].ToString(),
                    RequestTime = DateTimeOffset.UtcNow
                };

                context.Items["AppContext"] = appContext;

                // Ensure headers contain correct tenant ID and app code
                EnsureHeaders(context, appContext);

                _logger.LogDebug(
                    "[AppIsolationMiddleware] Request: {Method} {Path}, AppCode: {AppCode}, TenantId: {TenantId}, RequestId: {RequestId}",
                    context.Request.Method,
                    context.Request.Path,
                    appCode,
                    tenantId,
                    requestId);

                await _next(context);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[AppIsolationMiddleware] Error processing request: {Method} {Path}, RequestId: {RequestId}, Duration: {ElapsedMs}ms",
                    context.Request.Method,
                    context.Request.Path,
                    requestId,
                    stopwatch.ElapsedMilliseconds);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                if (stopwatch.ElapsedMilliseconds > 3000) // Log slow requests (threshold increased to 3s)
                {
                    _logger.LogWarning(
                        "[AppIsolationMiddleware] Slow request detected: {Method} {Path}, RequestId: {RequestId}, Duration: {ElapsedMs}ms",
                        context.Request.Method,
                        context.Request.Path,
                        requestId,
                        stopwatch.ElapsedMilliseconds);
                }
            }
        }

        private static string GenerateRequestId()
        {
            return Guid.NewGuid().ToString("N")[..8];
        }

        private async Task<string> ExtractAppCodeAsync(HttpContext context)
        {
            // 1. Get from X-App-Code header
            var appCode = context.Request.Headers["X-App-Code"].FirstOrDefault();
            if (!string.IsNullOrEmpty(appCode))
            {
                return appCode;
            }

            // 2. Get from AppCode header
            appCode = context.Request.Headers["AppCode"].FirstOrDefault();
            if (!string.IsNullOrEmpty(appCode))
            {
                return appCode;
            }

            // 3. Get from query parameters (support both appCode and app_code)
            appCode = context.Request.Query["appCode"].FirstOrDefault()
                   ?? context.Request.Query["app_code"].FirstOrDefault();
            if (!string.IsNullOrEmpty(appCode))
            {
                return appCode;
            }

            // 4. Get from JWT Token (if available)
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var appCodeClaim = context.User?.FindFirst("appCode");
                if (!string.IsNullOrEmpty(appCodeClaim?.Value))
                {
                    return appCodeClaim.Value;
                }
            }

            // 5. Use default value "default"
            return "default";
        }

        private async Task<string> ExtractTenantIdAsync(HttpContext context, string appCode)
        {
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

            // 3. Get from query parameters (support both tenantId and tenant_id)
            tenantId = context.Request.Query["tenantId"].FirstOrDefault()
                    ?? context.Request.Query["tenant_id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantId))
            {
                return tenantId;
            }

            // 4. Get from JWT Token (if available)
            var tenantIdClaim = context.User?.FindFirst("tenantId");
            if (!string.IsNullOrEmpty(tenantIdClaim?.Value))
            {
                return tenantIdClaim.Value;
            }

            // 5. Infer tenant ID from user email domain
            var userEmail = context.Request.Headers["X-User-Email"].FirstOrDefault()
                          ?? context.User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (!string.IsNullOrEmpty(userEmail))
            {
                var domain = userEmail.Split('@').LastOrDefault();
                if (!string.IsNullOrEmpty(domain))
                {
                    var inferredTenantId = MapDomainToTenantId(domain);
                    if (!string.IsNullOrEmpty(inferredTenantId))
                    {
                        return inferredTenantId;
                    }
                }
            }

            // 6. Use default value "default"
            return "default";
        }

        private string GetRequestId(HttpContext context)
        {
            // Try to get existing request ID from headers
            var requestId = context.Request.Headers["X-Request-Id"].FirstOrDefault()
                          ?? context.Request.Headers["Request-Id"].FirstOrDefault();

            if (!string.IsNullOrEmpty(requestId))
            {
                return requestId;
            }

            // Generate new request ID
            return Guid.NewGuid().ToString("N")[..8];
        }

        private string GetClientIpAddress(HttpContext context)
        {
            // Try to get real IP from various headers
            var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                          ?? context.Request.Headers["X-Real-IP"].FirstOrDefault()
                          ?? context.Connection.RemoteIpAddress?.ToString();

            return ipAddress ?? "unknown";
        }

        private string InferAppCodeFromRequest(HttpContext context)
        {
            // Infer app code from request path
            var path = context.Request.Path.Value?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(path))
            {
                // Example: /api/mobile/... -> MOBILE
                if (path.StartsWith("/api/mobile/"))
                    return "MOBILE";

                // Example: /api/web/... -> WEB
                if (path.StartsWith("/api/web/"))
                    return "WEB";

                // Example: /api/admin/... -> ADMIN
                if (path.StartsWith("/api/admin/"))
                    return "ADMIN";
            }

            // Infer from subdomain
            var host = context.Request.Host.Host;
            if (!string.IsNullOrEmpty(host))
            {
                var parts = host.Split('.');
                if (parts.Length > 2)
                {
                    var subdomain = parts[0].ToUpperInvariant();
                    if (subdomain != "WWW" && subdomain != "API")
                    {
                        return subdomain;
                    }
                }
            }

            return null;
        }

        private string MapDomainToTenantId(string domain)
        {
            // Map email domain to tenant ID
            return domain switch
            {
                "company1.com" => "COMPANY1",
                "company2.com" => "COMPANY2",
                "test.com" => "TEST",
                "example.com" => "EXAMPLE",
                _ => null // Return null if unable to infer
            };
        }

        private void EnsureHeaders(HttpContext context, AppContext appContext)
        {
            // Ensure app code header is set
            if (!context.Request.Headers.ContainsKey("X-App-Code"))
            {
                context.Request.Headers["X-App-Code"] = appContext.AppCode;
            }

            // Ensure tenant ID header is set
            if (!context.Request.Headers.ContainsKey("X-Tenant-Id"))
            {
                context.Request.Headers["X-Tenant-Id"] = appContext.TenantId;
            }

            // Ensure request ID header is set
            if (!context.Request.Headers.ContainsKey("X-Request-Id"))
            {
                context.Request.Headers["X-Request-Id"] = appContext.RequestId;
            }
        }
    }
}