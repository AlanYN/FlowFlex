using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Shared.Helpers;
using FlowFlex.Domain.Shared.Models;
using Item.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Case code generator service implementation.
    /// Generates unique case codes with fixed prefix "C" and auto-increment number.
    /// Format: C00001, C00002, ..., C99999, C100000, ...
    /// Counter is isolated per (tenantId, appCode) pair using Redis.
    /// </summary>
    public class CaseCodeGeneratorService : ICaseCodeGeneratorService
    {
        private readonly IRedisService _redisService;
        private readonly IConfiguration _configuration;
        private readonly ISqlSugarClient _db;
        private readonly UserContext _userContext;
        private readonly ILogger<CaseCodeGeneratorService> _logger;

        private const string CodePrefix = "C";
        private const int InitialNumberLength = 5;
        private const char PaddingChar = '0';

        public CaseCodeGeneratorService(
            IRedisService redisService,
            IConfiguration configuration,
            ISqlSugarClient db,
            UserContext userContext,
            ILogger<CaseCodeGeneratorService> logger)
        {
            _redisService  = redisService  ?? throw new ArgumentNullException(nameof(redisService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _db            = db            ?? throw new ArgumentNullException(nameof(db));
            _userContext   = userContext   ?? throw new ArgumentNullException(nameof(userContext));
            _logger        = logger        ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<string> GenerateCaseCodeAsync(string leadName)
        {
            var counterKey = GetCounterKey();
            await SyncCounterWithDatabaseAsync(counterKey);
            var uniqueId = await _redisService.StringIncrementAsync(counterKey);
            var numberLength = Math.Max(InitialNumberLength, uniqueId.ToString().Length);
            return $"{CodePrefix}{uniqueId.ToString().PadLeft(numberLength, PaddingChar)}";
        }

        /// <summary>
        /// Keeps the Redis counter ahead of the highest case_code already in the database,
        /// so codes remain sequential even across restarts.
        /// </summary>
        private async Task SyncCounterWithDatabaseAsync(string counterKey)
        {
            try
            {
                var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
                var appCode  = TenantContextHelper.GetAppCodeOrDefault(_userContext);

                var maxCaseCode = await _db.Queryable<Onboarding>()
                    .Where(o => o.TenantId == tenantId
                             && o.AppCode  == appCode
                             && o.CaseCode != null
                             && o.CaseCode != "")
                    .OrderByDescending(o => o.CaseCode)
                    .Select(o => o.CaseCode)
                    .FirstAsync();

                if (!string.IsNullOrEmpty(maxCaseCode) && maxCaseCode.StartsWith(CodePrefix))
                {
                    var numberPart = maxCaseCode[CodePrefix.Length..];
                    if (long.TryParse(numberPart, out var maxNumber))
                    {
                        var current = await _redisService.StringGetAsync<long?>(counterKey);
                        if (!current.HasValue || current.Value <= maxNumber)
                            await _redisService.StringSetAsync(counterKey, maxNumber);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to sync case code counter from database; continuing with current Redis counter");
            }
        }

        private string GetCounterKey()
        {
            var sysPrefix = string.IsNullOrEmpty(_configuration["Redis:KeyPrefix"])
                ? ""
                : $"{_configuration["Redis:KeyPrefix"]}:";

            var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
            var appCode  = TenantContextHelper.GetAppCodeOrDefault(_userContext);
            return $"{sysPrefix}ow:case:{tenantId}:{appCode}:count";
        }
    }
}
