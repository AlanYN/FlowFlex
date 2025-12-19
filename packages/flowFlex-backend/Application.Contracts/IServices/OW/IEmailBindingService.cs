using FlowFlex.Application.Contracts.Dtos.OW.EmailBinding;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW;

/// <summary>
/// Email Binding Service Interface
/// </summary>
public interface IEmailBindingService : IScopedService
{
    /// <summary>
    /// Get OAuth authorization URL
    /// </summary>
    Task<AuthorizeUrlDto> GetAuthorizationUrlAsync();

    /// <summary>
    /// Handle OAuth callback
    /// </summary>
    Task<EmailBindingDto> HandleCallbackAsync(OAuthCallbackDto callback);

    /// <summary>
    /// Get current user's email binding
    /// </summary>
    Task<EmailBindingDto?> GetCurrentBindingAsync();

    /// <summary>
    /// Unbind email account
    /// </summary>
    Task<bool> UnbindAsync();

    /// <summary>
    /// Update binding settings
    /// </summary>
    Task<bool> UpdateSettingsAsync(EmailBindingUpdateDto input);

    /// <summary>
    /// Manually trigger email sync
    /// If LastSyncTime is null, performs full sync; otherwise performs incremental sync
    /// </summary>
    Task<SyncResultDto> SyncEmailsAsync();

    /// <summary>
    /// Full sync - sync all emails from specified folders with pagination
    /// </summary>
    Task<FullSyncResultDto> FullSyncAsync(FullSyncRequestDto? request = null);

    /// <summary>
    /// Refresh token if needed
    /// </summary>
    Task<bool> RefreshTokenIfNeededAsync(long bindingId);
}
