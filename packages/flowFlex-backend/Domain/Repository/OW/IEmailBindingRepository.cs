using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW;

/// <summary>
/// Email Binding Repository Interface
/// </summary>
public interface IEmailBindingRepository
{
    /// <summary>
    /// Get binding by ID (nullable version)
    /// </summary>
    Task<EmailBinding?> GetByIdNullableAsync(long id);

    /// <summary>
    /// Get binding by user ID
    /// </summary>
    Task<EmailBinding?> GetByUserIdAsync(long userId);

    /// <summary>
    /// Get binding by user ID and provider
    /// </summary>
    Task<EmailBinding?> GetByUserIdAndProviderAsync(long userId, string provider);

    /// <summary>
    /// Get all bindings for a user
    /// </summary>
    Task<List<EmailBinding>> GetAllByUserIdAsync(long userId);

    /// <summary>
    /// Check if user has active binding
    /// </summary>
    Task<bool> HasActiveBindingAsync(long userId, string provider = "Outlook");

    /// <summary>
    /// Update token information
    /// </summary>
    Task<bool> UpdateTokenAsync(long id, string accessToken, string refreshToken, DateTimeOffset expireTime);

    /// <summary>
    /// Update sync status
    /// </summary>
    Task<bool> UpdateSyncStatusAsync(long id, string status, string? errorMessage = null);

    /// <summary>
    /// Update last sync time
    /// </summary>
    Task<bool> UpdateLastSyncTimeAsync(long id);

    /// <summary>
    /// Delete binding (soft delete)
    /// </summary>
    Task<bool> DeleteAsync(long id);

    /// <summary>
    /// Get bindings that need sync (for background job)
    /// </summary>
    Task<List<EmailBinding>> GetBindingsNeedingSyncAsync();

    /// <summary>
    /// Insert a new email binding
    /// </summary>
    Task<bool> InsertAsync(EmailBinding binding);

    /// <summary>
    /// Update an existing email binding
    /// </summary>
    Task<bool> UpdateAsync(EmailBinding binding);
}
