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
                // 记录所有请求头，用于调试
                _logger.LogInformation("[AppIsolationMiddleware] Request headers:");
                foreach (var header in context.Request.Headers)
                {
                    _logger.LogInformation($"[AppIsolationMiddleware] Header: {header.Key}={header.Value}");
                }

                // Extract AppCode from request
                var appCode = await ExtractAppCodeAsync(context);
                context.Items["AppCode"] = appCode;

                // Extract TenantId with priority order
                var tenantId = await ExtractTenantIdAsync(context, appCode);
                context.Items["TenantId"] = tenantId;

                // 创建AppContext对象并存储到HttpContext.Items中，确保数据隔离正常工作
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

                // 确保AppContext正确设置
                _logger.LogInformation($"[AppIsolationMiddleware] AppContext set: AppCode={appContext.AppCode}, TenantId={appContext.TenantId}, RequestId={appContext.RequestId}");

                // 确保请求头包含正确的租户ID和应用代码
                EnsureHeaders(context, appContext);

                // 再次检查请求头是否已正确设置
                _logger.LogInformation($"[AppIsolationMiddleware] After EnsureHeaders: X-App-Code={context.Request.Headers["X-App-Code"]}, X-Tenant-Id={context.Request.Headers["X-Tenant-Id"]}");

                _logger.LogInformation(
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
                if (stopwatch.ElapsedMilliseconds > 1000) // Log slow requests
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
            _logger.LogDebug("[AppIsolationMiddleware] Starting AppCode extraction");

            // 记录所有请求头，用于调试
            foreach (var header in context.Request.Headers)
            {
                _logger.LogDebug($"[AppIsolationMiddleware] Request header: {header.Key}={header.Value}");
            }

            // 1. Get from X-App-Code header
            var appCode = context.Request.Headers["X-App-Code"].FirstOrDefault();
            if (!string.IsNullOrEmpty(appCode))
            {
                _logger.LogDebug("[AppIsolationMiddleware] Found AppCode from X-App-Code header: {AppCode}", appCode);
                return appCode;
            }

            // 2. Get from AppCode header
            appCode = context.Request.Headers["AppCode"].FirstOrDefault();
            if (!string.IsNullOrEmpty(appCode))
            {
                _logger.LogDebug("[AppIsolationMiddleware] Found AppCode from AppCode header: {AppCode}", appCode);
                return appCode;
            }

            // 3. Get from query parameters (support both appCode and app_code)
            appCode = context.Request.Query["appCode"].FirstOrDefault()
                   ?? context.Request.Query["app_code"].FirstOrDefault();
            if (!string.IsNullOrEmpty(appCode))
            {
                _logger.LogDebug("[AppIsolationMiddleware] Found AppCode from query parameter: {AppCode}", appCode);
                return appCode;
            }

            // 4. Get from JWT Token (if available)
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                _logger.LogDebug("[AppIsolationMiddleware] User is authenticated, checking JWT claims");
                foreach (var claim in context.User.Claims)
                {
                    _logger.LogDebug($"[AppIsolationMiddleware] JWT claim: {claim.Type}={claim.Value}");
                }

                var appCodeClaim = context.User?.FindFirst("appCode");
                if (!string.IsNullOrEmpty(appCodeClaim?.Value))
                {
                    _logger.LogDebug("[AppIsolationMiddleware] Found AppCode from JWT token: {AppCode}", appCodeClaim.Value);
                    return appCodeClaim.Value;
                }
            }

            // 5. 使用默认值 "DEFAULT"
            _logger.LogDebug("[AppIsolationMiddleware] No AppCode found in headers, query or JWT. Using default AppCode: DEFAULT");
            return "DEFAULT";
        }

        private async Task<string> ExtractTenantIdAsync(HttpContext context, string appCode)
        {
            // 1. Get from X-Tenant-Id header
            var tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantId))
            {
                _logger.LogDebug("[AppIsolationMiddleware] Found TenantId from X-Tenant-Id header: {TenantId}", tenantId);
                return tenantId;
            }

            // 2. Get from TenantId header
            tenantId = context.Request.Headers["TenantId"].FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantId))
            {
                _logger.LogDebug("[AppIsolationMiddleware] Found TenantId from TenantId header: {TenantId}", tenantId);
                return tenantId;
            }

            // 3. Get from query parameters (support both tenantId and tenant_id)
            tenantId = context.Request.Query["tenantId"].FirstOrDefault()
                    ?? context.Request.Query["tenant_id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantId))
            {
                _logger.LogDebug("[AppIsolationMiddleware] Found TenantId from query parameter: {TenantId}", tenantId);
                return tenantId;
            }

            // 4. Get from JWT Token (if available)
            var tenantIdClaim = context.User?.FindFirst("tenantId");
            if (!string.IsNullOrEmpty(tenantIdClaim?.Value))
            {
                _logger.LogDebug("[AppIsolationMiddleware] Found TenantId from JWT token: {TenantId}", tenantIdClaim.Value);
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
                        _logger.LogDebug("[AppIsolationMiddleware] Inferred TenantId from email domain: {TenantId}", inferredTenantId);
                        return inferredTenantId;
                    }
                }
            }

            // 6. 使用默认值 "DEFAULT"
            _logger.LogDebug("[AppIsolationMiddleware] Using default TenantId: {TenantId}", "DEFAULT");
            return "DEFAULT";
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