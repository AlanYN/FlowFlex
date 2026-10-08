using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// Repository for Adobe Sign tenant → account key mappings.
    /// All queries bypass the multi-tenant global filter (this is system-level config).
    /// </summary>
    public interface IAdobeSignTenantConfigRepository : IBaseRepository<AdobeSignTenantConfig>
    {
        /// <summary>
        /// Get the config record for a specific AppCode.
        /// Returns null if no override is configured (service should use DefaultAccount).
        /// </summary>
        Task<AdobeSignTenantConfig?> GetByAppCodeAsync(string appCode);

        /// <summary>
        /// Get all active tenant config records.
        /// </summary>
        Task<List<AdobeSignTenantConfig>> GetAllAsync();

        /// <summary>
        /// Upsert a mapping: insert if not exists, update AccountKey if exists.
        /// </summary>
        Task UpsertAsync(string appCode, string accountKey);

        /// <summary>
        /// Soft-delete the config for an AppCode (falls back to DefaultAccount after deletion).
        /// </summary>
        Task<bool> DeleteByAppCodeAsync(string appCode);
    }
}
