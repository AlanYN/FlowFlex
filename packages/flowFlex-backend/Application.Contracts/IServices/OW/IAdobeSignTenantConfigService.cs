using FlowFlex.Application.Contracts.Dtos.OW.AdobeSign;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Manages the AppCode → Adobe Sign account key mapping stored in the DB.
    /// When no DB record exists for an AppCode, the service returns the
    /// DefaultAccount value from appsettings.json.
    /// </summary>
    public interface IAdobeSignTenantConfigService : IScopedService
    {
        /// <summary>
        /// Resolve which Adobe Sign account key to use for the given AppCode.
        /// DB record wins over the config default.
        /// </summary>
        Task<string> GetAccountKeyAsync(string appCode);

        /// <summary>
        /// List all active AppCode → AccountKey mappings stored in the DB.
        /// </summary>
        Task<List<AdobeSignTenantConfigOutputDto>> GetAllAsync();

        /// <summary>
        /// Create or update the mapping for an AppCode.
        /// Validates that the AccountKey exists in appsettings.json.
        /// </summary>
        Task UpsertAsync(UpsertAdobeSignTenantConfigInputDto input);

        /// <summary>
        /// Remove the mapping for an AppCode (soft delete).
        /// After deletion, the AppCode falls back to the DefaultAccount.
        /// </summary>
        Task<bool> DeleteAsync(string appCode);

        /// <summary>
        /// Return the list of valid account keys from appsettings.json AdobeSign:Accounts.
        /// Used by the UI to show available options when configuring mappings.
        /// </summary>
        List<string> GetAvailableAccountKeys();

        /// <summary>
        /// Return the DefaultAccount key from appsettings.json.
        /// </summary>
        string GetDefaultAccountKey();
    }
}
