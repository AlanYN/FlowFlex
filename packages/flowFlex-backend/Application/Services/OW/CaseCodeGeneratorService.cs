using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;
using Item.Redis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Case code generator service implementation
    /// Generates unique case codes with fixed prefix "C" and auto-increment number
    /// Format: C00001, C00002, ..., C99999, C100000, C100001, ...
    /// Supports tenant isolation - each tenant has independent counter
    /// Auto-syncs with database to ensure continuity
    /// </summary>
    public class CaseCodeGeneratorService : ICaseCodeGeneratorService
    {
        private readonly IRedisService _redisService;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISqlSugarClient _db;
        private readonly ILogger<CaseCodeGeneratorService> _logger;

        // Configuration constants
        private const string CodePrefix = "C";          // Fixed prefix
        private const int InitialNumberLength = 5;      // Initial number length (00001-99999)
        private const char PaddingChar = '0';

        public CaseCodeGeneratorService(
            IRedisService redisService,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ISqlSugarClient db,
            ILogger<CaseCodeGeneratorService> logger)
        {
            _redisService = redisService ?? throw new ArgumentNullException(nameof(redisService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Generate case code with format: C00001, C00002, ..., C99999, C100000, ...
        /// Auto-syncs with database to ensure counter is always greater than max existing value
        /// </summary>
        public async Task<string> GenerateCaseCodeAsync(string leadName)
        {
            var counterKey = GetCounterKey();
            
            // Sync Redis counter with database max value
            await SyncCounterWithDatabaseAsync(counterKey);
            
            var uniqueId = await GenerateUniqueIdAsync(counterKey);

            // Determine padding length based on number size
            // For numbers 1-99999: use 5 digits (C00001)
            // For numbers >= 100000: use actual length (C100000)
            var numberLength = Math.Max(InitialNumberLength, uniqueId.ToString().Length);

            return $"{CodePrefix}{uniqueId.ToString().PadLeft(numberLength, PaddingChar)}";
        }

        /// <summary>
        /// Sync Redis counter with database max value to ensure continuity
        /// </summary>
        private async Task SyncCounterWithDatabaseAsync(string counterKey)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                var appCode = GetCurrentAppCode();

                // Get max case code number from database
                var maxCaseCode = await _db.Queryable<Onboarding>()
                    .Where(o => o.TenantId == tenantId 
                             && o.AppCode == appCode 
                             && o.CaseCode != null 
                             && o.CaseCode != "")
                    .OrderByDescending(o => o.CaseCode)
                    .Select(o => o.CaseCode)
                    .FirstAsync();

                if (!string.IsNullOrEmpty(maxCaseCode) && maxCaseCode.StartsWith(CodePrefix))
                {
                    // Extract number from case code (e.g., "C00021" -> 21)
                    var numberPart = maxCaseCode.Substring(CodePrefix.Length);
                    if (long.TryParse(numberPart, out var maxNumber))
                    {
                        // Get current Redis counter value
                        var currentCounter = await _redisService.StringGetAsync<long?>(counterKey);
                        
                        // If Redis counter is less than or equal to database max, update it
                        if (!currentCounter.HasValue || currentCounter.Value <= maxNumber)
                        {
                            await _redisService.StringSetAsync(counterKey, maxNumber);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // If sync fails, continue with Redis counter
                // This ensures the service doesn't break if database is unavailable
                _logger.LogWarning(ex, "Failed to sync case code counter from database, continuing with Redis counter");
            }
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
                return "default";
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
            return "default";
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
                return "default";
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
            return "default";
        }
    }
}

