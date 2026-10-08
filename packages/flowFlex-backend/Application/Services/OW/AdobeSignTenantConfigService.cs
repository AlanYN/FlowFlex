using FlowFlex.Application.Contracts.Dtos.OW.AdobeSign;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Manages the AppCode → Adobe Sign account key mapping.
    /// DB records override the appsettings DefaultAccount.
    /// </summary>
    public class AdobeSignTenantConfigService : IAdobeSignTenantConfigService
    {
        private readonly IAdobeSignTenantConfigRepository _repository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdobeSignTenantConfigService> _logger;

        public AdobeSignTenantConfigService(
            IAdobeSignTenantConfigRepository repository,
            IConfiguration configuration,
            ILogger<AdobeSignTenantConfigService> logger)
        {
            _repository = repository;
            _configuration = configuration;
            _logger = logger;
        }

        // ------------------------------------------------------------------ //

        /// <inheritdoc />
        public async Task<string> GetAccountKeyAsync(string appCode)
        {
            if (!string.IsNullOrEmpty(appCode))
            {
                var record = await _repository.GetByAppCodeAsync(appCode);
                if (record != null)
                {
                    _logger.LogDebug("[AdobeSign] AppCode={AppCode} → AccountKey={Key} (from DB)", appCode, record.AccountKey);
                    return record.AccountKey;
                }
            }

            var defaultKey = GetDefaultAccountKey();
            _logger.LogDebug("[AdobeSign] AppCode={AppCode} not configured in DB — using DefaultAccount={Key}", appCode, defaultKey);
            return defaultKey;
        }

        /// <inheritdoc />
        public async Task<List<AdobeSignTenantConfigOutputDto>> GetAllAsync()
        {
            var records = await _repository.GetAllAsync();
            return records.Select(r => new AdobeSignTenantConfigOutputDto
            {
                Id = r.Id,
                ConfiguredAppCode = r.ConfiguredAppCode,
                AccountKey = r.AccountKey,
                CreateDate = r.CreateDate,
                ModifyDate = r.ModifyDate,
            }).ToList();
        }

        /// <inheritdoc />
        public async Task UpsertAsync(UpsertAdobeSignTenantConfigInputDto input)
        {
            // Validate account key exists in config
            var availableKeys = GetAvailableAccountKeys();
            if (!availableKeys.Contains(input.AccountKey))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    $"AccountKey '{input.AccountKey}' is not configured in appsettings.json AdobeSign:Accounts. " +
                    $"Available keys: {string.Join(", ", availableKeys)}");
            }

            await _repository.UpsertAsync(input.AppCode, input.AccountKey);
            _logger.LogInformation("[AdobeSign] TenantConfig upserted: AppCode={AppCode} → AccountKey={Key}",
                input.AppCode, input.AccountKey);
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(string appCode)
        {
            var result = await _repository.DeleteByAppCodeAsync(appCode);
            if (result)
                _logger.LogInformation("[AdobeSign] TenantConfig deleted: AppCode={AppCode}", appCode);
            return result;
        }

        /// <inheritdoc />
        public List<string> GetAvailableAccountKeys()
        {
            var keys = new List<string>();
            var section = _configuration.GetSection("AdobeSign:Accounts");
            foreach (var child in section.GetChildren())
                keys.Add(child.Key);
            return keys;
        }

        /// <inheritdoc />
        public string GetDefaultAccountKey()
        {
            var defaultKey = _configuration["AdobeSign:DefaultAccount"];
            if (string.IsNullOrEmpty(defaultKey))
                throw new InvalidOperationException("AdobeSign:DefaultAccount is not configured in appsettings.json");
            return defaultKey;
        }
    }
}
