using Microsoft.AspNetCore.Http;
using FlowFlex.Domain.Shared.Models;
using System.Threading.Tasks;

namespace FlowFlex.WebApi.Middlewares
{
    /// <summary>
    /// 租户中间�?- 确保每个请求都有正确的租户ID
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

        public async Task InvokeAsync(HttpContext context)
        {
            // 获取租户ID
            var tenantId = GetTenantId(context);
            
            // 记录租户信息
            _logger.LogInformation($"[TenantMiddleware] Request: {context.Request.Method} {context.Request.Path}, TenantId: {tenantId}");
            
            // 确保租户ID在请求头�?
            if (!context.Request.Headers.ContainsKey("X-Tenant-Id"))
            {
                context.Request.Headers.Add("X-Tenant-Id", tenantId);
            }
            
            // 在响应头中添加租户ID（用于调试）
            context.Response.Headers.Add("X-Response-Tenant-Id", tenantId);
            
            await _next(context);
        }

        private string GetTenantId(HttpContext context)
        {
            // 尝试从多个来源获取租户ID
            
            // 1. �?X-Tenant-Id 头获�?
            var tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantId))
            {
                _logger.LogDebug($"[TenantMiddleware] Found TenantId from X-Tenant-Id header: {tenantId}");
                return tenantId;
            }
            
            // 2. �?TenantId 头获�?
            tenantId = context.Request.Headers["TenantId"].FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantId))
            {
                _logger.LogDebug($"[TenantMiddleware] Found TenantId from TenantId header: {tenantId}");
                return tenantId;
            }
            
            // 3. 从查询参数获�?
            tenantId = context.Request.Query["tenantId"].FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantId))
            {
                _logger.LogDebug($"[TenantMiddleware] Found TenantId from query parameter: {tenantId}");
                return tenantId;
            }
            
            // 4. 从JWT Token获取（如果有的话�?
            // TODO: 实现从JWT Token中提取租户ID的逻辑
            
            // 5. 从用户邮箱域名推断租户ID（示例逻辑�?
            var userEmail = context.Request.Headers["X-User-Email"].FirstOrDefault();
            if (!string.IsNullOrEmpty(userEmail))
            {
                var domain = userEmail.Split('@').LastOrDefault();
                if (!string.IsNullOrEmpty(domain))
                {
                    // 可以根据邮箱域名映射到租户ID
                    tenantId = MapDomainToTenantId(domain);
                    if (!string.IsNullOrEmpty(tenantId))
                    {
                        _logger.LogDebug($"[TenantMiddleware] Inferred TenantId from email domain: {tenantId}");
                        return tenantId;
                    }
                }
            }
            
            // 6. 默认租户ID
            tenantId = "default";
            _logger.LogDebug($"[TenantMiddleware] Using default TenantId: {tenantId}");
            return tenantId;
        }

        /// <summary>
        /// 根据邮箱域名映射到租户ID
        /// </summary>
        private string MapDomainToTenantId(string domain)
        {
            // 这里可以实现具体的域名到租户ID的映射逻辑
            // 例如�?
            // - company1.com -> tenant1
            // - company2.com -> tenant2
            // - gmail.com -> personal
            
            return domain switch
            {
                "company1.com" => "tenant1",
                "company2.com" => "tenant2",
                "test.com" => "test",
                _ => null // 返回null表示无法推断
            };
        }
    }
} 
