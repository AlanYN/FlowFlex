using SqlSugar;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.SqlSugarDB.Repositories.OW
{
    /// <summary>
    /// Adobe Sign tenant config repository.
    /// All queries use ClearFilter() — ff_adobe_sign_tenant_config is a system-level
    /// config table with no app_code/tenant_id columns.
    /// </summary>
    public class AdobeSignTenantConfigRepository
        : BaseRepository<AdobeSignTenantConfig>, IAdobeSignTenantConfigRepository, IScopedService
    {
        private readonly UserContext _userContext;

        public AdobeSignTenantConfigRepository(ISqlSugarClient db, UserContext userContext) : base(db)
        {
            _userContext = userContext;
        }

        /// <inheritdoc />
        public async Task<AdobeSignTenantConfig?> GetByAppCodeAsync(string appCode)
        {
            return await db.Queryable<AdobeSignTenantConfig>()
                .ClearFilter()
                .Where(c => c.ConfiguredAppCode == appCode && c.IsValid == true)
                .FirstAsync();
        }

        /// <inheritdoc />
        public async Task<List<AdobeSignTenantConfig>> GetAllAsync()
        {
            return await db.Queryable<AdobeSignTenantConfig>()
                .ClearFilter()
                .Where(c => c.IsValid == true)
                .OrderBy(c => c.ConfiguredAppCode)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task UpsertAsync(string appCode, string accountKey)
        {
            var existing = await GetByAppCodeAsync(appCode);

            if (existing != null)
            {
                existing.AccountKey = accountKey;
                existing.ModifyDate = DateTimeOffset.UtcNow;
                existing.ModifyBy = _userContext?.UserName ?? "SYSTEM";
                await db.Updateable(existing)
                    .UpdateColumns(c => new { c.AccountKey, c.ModifyDate, c.ModifyBy })
                    .ExecuteCommandAsync();
            }
            else
            {
                var config = new AdobeSignTenantConfig
                {
                    ConfiguredAppCode = appCode,
                    AccountKey = accountKey,
                    IsValid = true,
                    CreateDate = DateTimeOffset.UtcNow,
                    ModifyDate = DateTimeOffset.UtcNow,
                    CreateBy = _userContext?.UserName ?? "SYSTEM",
                    ModifyBy = _userContext?.UserName ?? "SYSTEM",
                };
                config.InitNewId();
                await db.Insertable(config).ExecuteCommandAsync();
            }
        }

        /// <inheritdoc />
        public async Task<bool> DeleteByAppCodeAsync(string appCode)
        {
            var existing = await GetByAppCodeAsync(appCode);
            if (existing == null) return false;

            existing.IsValid = false;
            existing.ModifyDate = DateTimeOffset.UtcNow;
            existing.ModifyBy = _userContext?.UserName ?? "SYSTEM";
            await db.Updateable(existing)
                .UpdateColumns(c => new { c.IsValid, c.ModifyDate, c.ModifyBy })
                .ExecuteCommandAsync();
            return true;
        }
    }
}
