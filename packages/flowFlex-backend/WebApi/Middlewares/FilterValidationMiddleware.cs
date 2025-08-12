using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using FlowFlex.Domain.Shared.Models;
using AppContext = FlowFlex.Domain.Shared.Models.AppContext;

namespace FlowFlex.WebApi.Middlewares
{
    /// <summary>
    /// 过滤器验证中间件 - 确保每个请求都正确应用了租户和应用过滤
    /// </summary>
    public class FilterValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<FilterValidationMiddleware> _logger;

        public FilterValidationMiddleware(RequestDelegate next, ILogger<FilterValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 检查 AppContext 是否已设置
            if (context.Items.TryGetValue("AppContext", out var appContextObj) && 
                appContextObj is AppContext appContext)
            {
                // 验证 AppContext 中的值
                if (string.IsNullOrEmpty(appContext.AppCode))
                {
                    _logger.LogWarning("[FilterValidationMiddleware] AppContext.AppCode is empty, using DEFAULT");
                    appContext.AppCode = "DEFAULT";
                }

                if (string.IsNullOrEmpty(appContext.TenantId))
                {
                    _logger.LogWarning("[FilterValidationMiddleware] AppContext.TenantId is empty, using DEFAULT");
                    appContext.TenantId = "DEFAULT";
                }

                // 记录当前请求的过滤器状态
                _logger.LogInformation(
                    "[FilterValidationMiddleware] Request: {Method} {Path}, AppCode: {AppCode}, TenantId: {TenantId}",
                    context.Request.Method,
                    context.Request.Path,
                    appContext.AppCode,
                    appContext.TenantId);
                    
                // 确保请求头中包含这些值
                if (!context.Request.Headers.ContainsKey("X-App-Code") || 
                    string.IsNullOrEmpty(context.Request.Headers["X-App-Code"]))
                {
                    context.Request.Headers["X-App-Code"] = appContext.AppCode;
                    _logger.LogInformation($"[FilterValidationMiddleware] Added X-App-Code header: {appContext.AppCode}");
                }
                
                if (!context.Request.Headers.ContainsKey("X-Tenant-Id") || 
                    string.IsNullOrEmpty(context.Request.Headers["X-Tenant-Id"]))
                {
                    context.Request.Headers["X-Tenant-Id"] = appContext.TenantId;
                    _logger.LogInformation($"[FilterValidationMiddleware] Added X-Tenant-Id header: {appContext.TenantId}");
                }
            }
            else
            {
                // AppContext 未设置，这是一个异常情况
                _logger.LogWarning(
                    "[FilterValidationMiddleware] AppContext not set for request: {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);
                
                // 创建默认的 AppContext
                var defaultAppContext = new AppContext
                {
                    AppCode = "DEFAULT",
                    TenantId = "DEFAULT",
                    RequestId = System.Guid.NewGuid().ToString("N")[..8],
                    ClientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    UserAgent = context.Request.Headers["User-Agent"].ToString(),
                    RequestTime = System.DateTimeOffset.UtcNow
                };
                
                context.Items["AppContext"] = defaultAppContext;
                
                // 确保请求头包含这些值
                if (!context.Request.Headers.ContainsKey("X-App-Code"))
                {
                    context.Request.Headers["X-App-Code"] = "DEFAULT";
                    _logger.LogInformation("[FilterValidationMiddleware] Added default X-App-Code header: DEFAULT");
                }
                
                if (!context.Request.Headers.ContainsKey("X-Tenant-Id"))
                {
                    context.Request.Headers["X-Tenant-Id"] = "DEFAULT";
                    _logger.LogInformation("[FilterValidationMiddleware] Added default X-Tenant-Id header: DEFAULT");
                }
            }

            // 记录所有请求头，用于调试
            _logger.LogInformation("[FilterValidationMiddleware] Final request headers:");
            foreach (var header in context.Request.Headers)
            {
                _logger.LogInformation($"[FilterValidationMiddleware] Header: {header.Key}={header.Value}");
            }

            await _next(context);
        }
    }
} 