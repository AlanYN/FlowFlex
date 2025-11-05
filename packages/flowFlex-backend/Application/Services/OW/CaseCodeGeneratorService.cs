using FlowFlex.Application.Contracts.IServices.OW;
using Item.Redis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Case code generator service implementation
    /// Generates unique case codes with fixed prefix "C" and auto-increment number
    /// Format: C00001, C00002, ..., C99999, C100000, C100001, ...
    /// Supports tenant isolation - each tenant has independent counter
    /// </summary>
    public class CaseCodeGeneratorService : ICaseCodeGeneratorService
    {
        private readonly IRedisService _redisService;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Configuration constants
        private const string CodePrefix = "C";          // Fixed prefix
        private const int InitialNumberLength = 5;      // Initial number length (00001-99999)
        private const char PaddingChar = '0';

        public CaseCodeGeneratorService(
            IRedisService redisService, 
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _redisService = redisService ?? throw new ArgumentNullException(nameof(redisService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <summary>
        /// Generate case code with format: C00001, C00002, ..., C99999, C100000, ...
        /// </summary>
        public async Task<string> GenerateCaseCodeAsync(string leadName)
        {
            var counterKey = GetCounterKey();
            var uniqueId = await GenerateUniqueIdAsync(counterKey);
            
            // Determine padding length based on number size
            // For numbers 1-99999: use 5 digits (C00001)
            // For numbers >= 100000: use actual length (C100000)
            var numberLength = Math.Max(InitialNumberLength, uniqueId.ToString().Length);
            
            return $"{CodePrefix}{uniqueId.ToString().PadLeft(numberLength, PaddingChar)}";
        }

        /// <summary>
        /// Generate unique ID using Redis counter
        /// </summary>
        private async Task<long> GenerateUniqueIdAsync(string counterKey)
        {
            return await _redisService.StringIncrementAsync(counterKey);
        }

        /// <summary>
        /// Get Redis counter key with tenant isolation
        /// Format: {prefix}:ow:case:{tenantId}:{appCode}:count
        /// </summary>
        private string GetCounterKey()
        {
            var sysPrefix = string.IsNullOrEmpty(_configuration["Redis:KeyPrefix"]) 
                ? "" 
                : $"{_configuration["Redis:KeyPrefix"]}:";
            
            var tenantId = GetCurrentTenantId();
            var appCode = GetCurrentAppCode();
            
            return $"{sysPrefix}ow:case:{tenantId}:{appCode}:count";
        }

        /// <summary>
        /// Get current tenant ID from HTTP headers
        /// Priority: X-Tenant-Id > TenantId > x-tenant-id > DEFAULT
        /// </summary>
        private string GetCurrentTenantId()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null)
            {
                return "DEFAULT";
            }

            // 1. Try X-Tenant-Id header
            var tenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantId))
            {
                return tenantId;
            }

            // 2. Try TenantId header
            tenantId = httpContext.Request.Headers["TenantId"].FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantId))
            {
                return tenantId;
            }

            // 3. Try lowercase x-tenant-id header
            tenantId = httpContext.Request.Headers["x-tenant-id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantId))
            {
                return tenantId;
            }

            // 4. Default
            return "DEFAULT";
        }

        /// <summary>
        /// Get current app code from HTTP headers
        /// Priority: X-App-Code > AppCode > x-app-code > DEFAULT
        /// </summary>
        private string GetCurrentAppCode()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null)
            {
                return "DEFAULT";
            }

            // 1. Try X-App-Code header
            var appCode = httpContext.Request.Headers["X-App-Code"].FirstOrDefault();
            if (!string.IsNullOrEmpty(appCode))
            {
                return appCode;
            }

            // 2. Try AppCode header
            appCode = httpContext.Request.Headers["AppCode"].FirstOrDefault();
            if (!string.IsNullOrEmpty(appCode))
            {
                return appCode;
            }

            // 3. Try lowercase x-app-code header
            appCode = httpContext.Request.Headers["x-app-code"].FirstOrDefault();
            if (!string.IsNullOrEmpty(appCode))
            {
                return appCode;
            }

            // 4. Default
            return "DEFAULT";
        }
    }
}

