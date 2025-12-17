using Microsoft.Extensions.Logging;
using FlowFlex.Application.Contracts.Dtos.OW.EmailBinding;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Exceptions;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Services.OW;

/// <summary>
/// Email Binding Service Implementation
/// </summary>
public class EmailBindingService : IEmailBindingService, IScopedService
{
    private readonly IEmailBindingRepository _bindingRepository;
    private readonly IOutlookService _outlookService;
    private readonly UserContext _userContext;
    private readonly ILogger<EmailBindingService> _logger;

    // Store state temporarily (in production, use distributed cache like Redis)
    private static readonly Dictionary<string, (long UserId, DateTimeOffset ExpireTime)> _stateStore = new();

    public EmailBindingService(
        IEmailBindingRepository bindingRepository,
        IOutlookService outlookService,
        UserContext userContext,
        ILogger<EmailBindingService> logger)
    {
        _bindingRepository = bindingRepository;
        _outlookService = outlookService;
        _userContext = userContext;
        _logger = logger;
    }

    /// <summary>
    /// Get OAuth authorization URL
    /// </summary>
    public Task<AuthorizeUrlDto> GetAuthorizationUrlAsync()
    {
        var userId = GetCurrentUserId();

        // Generate state for CSRF protection
        var state = Guid.NewGuid().ToString("N");
        _stateStore[state] = (userId, DateTimeOffset.UtcNow.AddMinutes(10));

        // Clean up expired states
        CleanupExpiredStates();

        var authUrl = _outlookService.GetAuthorizationUrl(state);

        return Task.FromResult(new AuthorizeUrlDto
        {
            AuthorizationUrl = authUrl,
            State = state
        });
    }

    /// <summary>
    /// Handle OAuth callback
    /// </summary>
    public async Task<EmailBindingDto> HandleCallbackAsync(OAuthCallbackDto callback)
    {
        // Check for OAuth error
        if (!string.IsNullOrEmpty(callback.Error))
        {
            _logger.LogWarning("OAuth error: {Error} - {Description}", callback.Error, callback.ErrorDescription);
            throw new CRMException(ErrorCodeEnum.BusinessError, callback.ErrorDescription ?? "OAuth authorization failed");
        }

        // Validate state
        if (string.IsNullOrEmpty(callback.State) || !_stateStore.TryGetValue(callback.State, out var stateData))
        {
            throw new CRMException(ErrorCodeEnum.ParamInvalid, "Invalid or expired state parameter");
        }

        if (stateData.ExpireTime < DateTimeOffset.UtcNow)
        {
            _stateStore.Remove(callback.State);
            throw new CRMException(ErrorCodeEnum.ParamInvalid, "State parameter has expired");
        }

        // Remove used state
        _stateStore.Remove(callback.State);

        // Exchange code for token
        var tokenResult = await _outlookService.GetTokenFromAuthorizationCodeAsync(callback.Code);
        if (tokenResult == null)
        {
            throw new CRMException(ErrorCodeEnum.BusinessError, "Failed to exchange authorization code for token");
        }

        var userId = stateData.UserId;

        // Get user email from Microsoft Graph (we need to call the API to get user info)
        // For now, we'll use a placeholder - in production, call /me endpoint
        var userEmail = await GetUserEmailFromGraphAsync(tokenResult.AccessToken);

        // Check if binding already exists
        var existingBinding = await _bindingRepository.GetByUserIdAndProviderAsync(userId, "Outlook");
        
        if (existingBinding != null)
        {
            // Update existing binding
            existingBinding.Email = userEmail;
            existingBinding.AccessToken = tokenResult.AccessToken;
            existingBinding.RefreshToken = tokenResult.RefreshToken;
            existingBinding.TokenExpireTime = tokenResult.ExpiresAt;
            existingBinding.SyncStatus = "Active";
            existingBinding.LastSyncError = null;
            existingBinding.ModifyDate = DateTimeOffset.UtcNow;
            existingBinding.ModifyBy = "OAuth Callback";
            existingBinding.ModifyUserId = userId;

            await _bindingRepository.UpdateAsync(existingBinding);

            return MapToDto(existingBinding);
        }

        // Create new binding
        var binding = new EmailBinding
        {
            UserId = userId,
            Email = userEmail,
            Provider = "Outlook",
            AccessToken = tokenResult.AccessToken,
            RefreshToken = tokenResult.RefreshToken,
            TokenExpireTime = tokenResult.ExpiresAt,
            SyncStatus = "Active",
            AutoSyncEnabled = true,
            SyncIntervalMinutes = 15
        };

        // Initialize create info (sets Id, TenantId, AppCode, timestamps, etc.)
        binding.InitCreateInfo(_userContext);

        await _bindingRepository.InsertAsync(binding);

        _logger.LogInformation("User {UserId} bound Outlook account {Email}", userId, userEmail);

        return MapToDto(binding);
    }

    /// <summary>
    /// Get current user's email binding
    /// </summary>
    public async Task<EmailBindingDto?> GetCurrentBindingAsync()
    {
        var userId = GetCurrentUserId();
        var binding = await _bindingRepository.GetByUserIdAndProviderAsync(userId, "Outlook");

        return binding != null ? MapToDto(binding) : null;
    }

    /// <summary>
    /// Unbind email account
    /// </summary>
    public async Task<bool> UnbindAsync()
    {
        var userId = GetCurrentUserId();
        var binding = await _bindingRepository.GetByUserIdAndProviderAsync(userId, "Outlook");

        if (binding == null)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, "No email binding found");
        }

        var result = await _bindingRepository.DeleteAsync(binding.Id);
        
        if (result)
        {
            _logger.LogInformation("User {UserId} unbound Outlook account {Email}", userId, binding.Email);
        }

        return result;
    }

    /// <summary>
    /// Update binding settings
    /// </summary>
    public async Task<bool> UpdateSettingsAsync(EmailBindingUpdateDto input)
    {
        var userId = GetCurrentUserId();
        var binding = await _bindingRepository.GetByUserIdAndProviderAsync(userId, "Outlook");

        if (binding == null)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, "No email binding found");
        }

        if (input.AutoSyncEnabled.HasValue)
        {
            binding.AutoSyncEnabled = input.AutoSyncEnabled.Value;
        }

        if (input.SyncIntervalMinutes.HasValue)
        {
            if (input.SyncIntervalMinutes.Value < 5 || input.SyncIntervalMinutes.Value > 1440)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Sync interval must be between 5 and 1440 minutes");
            }
            binding.SyncIntervalMinutes = input.SyncIntervalMinutes.Value;
        }

        binding.InitUpdateInfo(_userContext);
        return await _bindingRepository.UpdateAsync(binding);
    }

    /// <summary>
    /// Manually trigger email sync
    /// </summary>
    public async Task<SyncResultDto> SyncEmailsAsync()
    {
        var userId = GetCurrentUserId();
        var binding = await _bindingRepository.GetByUserIdAndProviderAsync(userId, "Outlook");

        if (binding == null)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, "No email binding found. Please bind your Outlook account first.");
        }

        // Refresh token if needed
        await RefreshTokenIfNeededAsync(binding.Id);

        // Reload binding to get updated token
        binding = await _bindingRepository.GetByIdNullableAsync(binding.Id);
        if (binding == null || string.IsNullOrEmpty(binding.AccessToken))
        {
            throw new CRMException(ErrorCodeEnum.BusinessError, "Failed to get valid access token");
        }

        try
        {
            var syncedCount = await _outlookService.SyncEmailsToLocalAsync(binding.AccessToken, userId);

            await _bindingRepository.UpdateLastSyncTimeAsync(binding.Id);
            await _bindingRepository.UpdateSyncStatusAsync(binding.Id, "Active");

            return new SyncResultDto
            {
                SyncedCount = syncedCount,
                SyncTime = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync emails for user {UserId}", userId);
            await _bindingRepository.UpdateSyncStatusAsync(binding.Id, "Error", ex.Message);

            return new SyncResultDto
            {
                SyncedCount = 0,
                SyncTime = DateTimeOffset.UtcNow,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Refresh token if needed
    /// </summary>
    public async Task<bool> RefreshTokenIfNeededAsync(long bindingId)
    {
        var binding = await _bindingRepository.GetByIdNullableAsync(bindingId);
        if (binding == null)
        {
            return false;
        }

        // Check if token needs refresh (5 minutes before expiry)
        if (binding.TokenExpireTime > DateTimeOffset.UtcNow.AddMinutes(5))
        {
            return true; // Token is still valid
        }

        if (string.IsNullOrEmpty(binding.RefreshToken))
        {
            await _bindingRepository.UpdateSyncStatusAsync(bindingId, "Error", "No refresh token available");
            return false;
        }

        var newToken = await _outlookService.RefreshTokenAsync(binding.RefreshToken);
        if (newToken == null)
        {
            await _bindingRepository.UpdateSyncStatusAsync(bindingId, "Error", "Failed to refresh token");
            return false;
        }

        await _bindingRepository.UpdateTokenAsync(
            bindingId,
            newToken.AccessToken,
            newToken.RefreshToken,
            newToken.ExpiresAt);

        _logger.LogInformation("Refreshed token for binding {BindingId}", bindingId);
        return true;
    }

    #region Private Methods

    private long GetCurrentUserId()
    {
        if (string.IsNullOrEmpty(_userContext.UserId) || !long.TryParse(_userContext.UserId, out var userId))
        {
            throw new CRMException(ErrorCodeEnum.AuthenticationFail, "User not authenticated");
        }
        return userId;
    }

    private static void CleanupExpiredStates()
    {
        var expiredStates = _stateStore
            .Where(kvp => kvp.Value.ExpireTime < DateTimeOffset.UtcNow)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var state in expiredStates)
        {
            _stateStore.Remove(state);
        }
    }

    private async Task<string> GetUserEmailFromGraphAsync(string accessToken)
    {
        // Get user profile from Microsoft Graph to get email
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/me?$select=mail,userPrincipalName");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var json = System.Text.Json.JsonDocument.Parse(content);
                
                // Try mail first, then userPrincipalName
                if (json.RootElement.TryGetProperty("mail", out var mailElement) && 
                    mailElement.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    return mailElement.GetString() ?? string.Empty;
                }
                
                if (json.RootElement.TryGetProperty("userPrincipalName", out var upnElement) && 
                    upnElement.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    return upnElement.GetString() ?? string.Empty;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get user email from Graph API");
        }

        return string.Empty;
    }

    private static EmailBindingDto MapToDto(EmailBinding binding)
    {
        return new EmailBindingDto
        {
            Id = binding.Id,
            Email = binding.Email,
            Provider = binding.Provider,
            SyncStatus = binding.SyncStatus,
            LastSyncTime = binding.LastSyncTime,
            LastSyncError = binding.LastSyncError,
            AutoSyncEnabled = binding.AutoSyncEnabled,
            SyncIntervalMinutes = binding.SyncIntervalMinutes,
            IsTokenValid = binding.TokenExpireTime > DateTimeOffset.UtcNow,
            TokenExpireTime = binding.TokenExpireTime
        };
    }

    #endregion
}
