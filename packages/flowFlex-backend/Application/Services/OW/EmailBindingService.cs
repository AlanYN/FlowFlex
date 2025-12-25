using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using FlowFlex.Application.Contracts.Dtos.OW.EmailBinding;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Constants;
using FlowFlex.Domain.Shared.Exceptions;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Services.OW;

/// <summary>
/// Email Binding Service Implementation
/// </summary>
public class EmailBindingService : IEmailBindingService, IScopedService
{
    private readonly IEmailBindingRepository _bindingRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IOutlookService _outlookService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly UserContext _userContext;
    private readonly ILogger<EmailBindingService> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IEncryptionService _encryptionService;

    // Store state temporarily (in production, use distributed cache like Redis)
    private static readonly Dictionary<string, (long UserId, string TenantId, DateTimeOffset ExpireTime)> _stateStore = new();
    private static readonly object _stateStoreLock = new();
    private static DateTimeOffset _lastCleanupTime = DateTimeOffset.MinValue;

    public EmailBindingService(
        IEmailBindingRepository bindingRepository,
        IMessageRepository messageRepository,
        IOutlookService outlookService,
        IHttpClientFactory httpClientFactory,
        UserContext userContext,
        ILogger<EmailBindingService> logger,
        IUserRepository userRepository,
        IEncryptionService encryptionService)
    {
        _bindingRepository = bindingRepository;
        _messageRepository = messageRepository;
        _outlookService = outlookService;
        _httpClientFactory = httpClientFactory;
        _userContext = userContext;
        _logger = logger;
        _userRepository = userRepository;
        _encryptionService = encryptionService;
    }

    /// <summary>
    /// Get OAuth authorization URL
    /// </summary>
    public Task<AuthorizeUrlDto> GetAuthorizationUrlAsync()
    {
        var userId = GetCurrentUserId();
        var tenantId = _userContext.TenantId ?? "DEFAULT";

        // Generate state for CSRF protection
        var state = Guid.NewGuid().ToString("N");
        _stateStore[state] = (userId, tenantId, DateTimeOffset.UtcNow.AddMinutes(30));

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
        var tenantId = stateData.TenantId;

        // Get user email from Microsoft Graph (we need to call the API to get user info)
        // For now, we'll use a placeholder - in production, call /me endpoint
        var userEmail = await GetUserEmailFromGraphAsync(tokenResult.AccessToken);

        // Check if this email is already bound by another user
        var emailBinding = await _bindingRepository.GetByEmailAsync(userEmail, EmailConstants.Provider.Outlook);
        if (emailBinding != null && emailBinding.UserId != userId)
        {
            _logger.LogWarning("Email {Email} is already bound by user {ExistingUserId}, current user {CurrentUserId} cannot bind it",
                userEmail, emailBinding.UserId, userId);
            throw new CRMException(ErrorCodeEnum.BusinessError, $"This email ({userEmail}) is already bound by another user");
        }

        // Check if binding already exists for current user
        var existingBinding = await _bindingRepository.GetByUserIdAndProviderAsync(userId, EmailConstants.Provider.Outlook);
        
        if (existingBinding != null)
        {
            // Update existing binding with encrypted tokens
            existingBinding.Email = userEmail;
            existingBinding.AccessToken = EncryptToken(tokenResult.AccessToken);
            existingBinding.RefreshToken = EncryptToken(tokenResult.RefreshToken);
            existingBinding.TokenExpireTime = tokenResult.ExpiresAt;
            existingBinding.SyncStatus = EmailConstants.SyncStatus.Active;
            existingBinding.LastSyncError = null;
            existingBinding.ModifyDate = DateTimeOffset.UtcNow;
            existingBinding.ModifyBy = "OAuth Callback";
            existingBinding.ModifyUserId = userId;

            await _bindingRepository.UpdateAsync(existingBinding);

            return MapToDto(existingBinding);
        }

        // Create new binding with encrypted tokens
        var binding = new EmailBinding
        {
            UserId = userId,
            Email = userEmail,
            Provider = EmailConstants.Provider.Outlook,
            AccessToken = EncryptToken(tokenResult.AccessToken),
            RefreshToken = EncryptToken(tokenResult.RefreshToken),
            TokenExpireTime = tokenResult.ExpiresAt,
            SyncStatus = EmailConstants.SyncStatus.Active,
            AutoSyncEnabled = true,
            SyncIntervalMinutes = EmailConstants.SyncSettings.DefaultIntervalMinutes
        };

        // Initialize create info (sets Id, TenantId, AppCode, timestamps, etc.)
        binding.InitCreateInfo(_userContext);

        // Use TenantId from state if current context is empty/DEFAULT
        if (string.IsNullOrEmpty(binding.TenantId) || binding.TenantId == "DEFAULT")
        {
            binding.TenantId = tenantId;
            _logger.LogInformation("Using TenantId from OAuth state: {TenantId}", tenantId);
        }

        // If TenantId or AppCode is still empty/DEFAULT, try to get from user's record
        if (string.IsNullOrEmpty(binding.TenantId) || binding.TenantId == "DEFAULT" ||
            string.IsNullOrEmpty(binding.AppCode) || binding.AppCode == "DEFAULT")
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user != null)
            {
                if (string.IsNullOrEmpty(binding.TenantId) || binding.TenantId == "DEFAULT")
                {
                    binding.TenantId = user.TenantId;
                }
                if (string.IsNullOrEmpty(binding.AppCode) || binding.AppCode == "DEFAULT")
                {
                    binding.AppCode = user.AppCode;
                }
                _logger.LogInformation("Filled TenantId and AppCode from user record: TenantId={TenantId}, AppCode={AppCode}",
                    binding.TenantId, binding.AppCode);
            }
        }

        _logger.LogInformation("Creating EmailBinding with TenantId: {TenantId}, AppCode: {AppCode}, UserId: {UserId}",
            binding.TenantId, binding.AppCode, binding.UserId);

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
        var binding = await _bindingRepository.GetByUserIdAndProviderAsync(userId, EmailConstants.Provider.Outlook);

        return binding != null ? MapToDto(binding) : null;
    }

    /// <summary>
    /// Unbind email account and delete synced emails
    /// </summary>
    public async Task<bool> UnbindAsync()
    {
        var userId = GetCurrentUserId();
        var binding = await _bindingRepository.GetByUserIdAndProviderAsync(userId, EmailConstants.Provider.Outlook);

        if (binding == null)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, "No email binding found");
        }

        // Delete all synced emails for this user
        var deletedEmailCount = await _messageRepository.DeleteSyncedEmailsByOwnerAsync(userId);
        _logger.LogInformation("Deleted {Count} synced emails for user {UserId}", deletedEmailCount, userId);

        // Delete the binding record
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
        var binding = await _bindingRepository.GetByUserIdAndProviderAsync(userId, EmailConstants.Provider.Outlook);

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
            if (input.SyncIntervalMinutes.Value < EmailConstants.SyncSettings.MinIntervalMinutes || 
                input.SyncIntervalMinutes.Value > EmailConstants.SyncSettings.MaxIntervalMinutes)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, 
                    $"Sync interval must be between {EmailConstants.SyncSettings.MinIntervalMinutes} and {EmailConstants.SyncSettings.MaxIntervalMinutes} minutes");
            }
            binding.SyncIntervalMinutes = input.SyncIntervalMinutes.Value;
        }

        binding.InitUpdateInfo(_userContext);
        return await _bindingRepository.UpdateAsync(binding);
    }

    /// <summary>
    /// Manually trigger email sync
    /// Uses Delta Query for efficient incremental sync with change tracking
    /// </summary>
    public async Task<SyncResultDto> SyncEmailsAsync()
    {
        var userId = GetCurrentUserId();
        var binding = await _bindingRepository.GetByUserIdAndProviderAsync(userId, EmailConstants.Provider.Outlook);

        if (binding == null)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, "No email binding found. Please bind your Outlook account first.");
        }

        // Check if sync is already in progress, wait for it to complete
        if (binding.SyncStatus == EmailConstants.SyncStatus.Syncing)
        {
            _logger.LogInformation("Sync already in progress for user {UserId}, waiting for completion", userId);
            var waitResult = await WaitForSyncCompletionAsync(binding.Id, userId);
            return new SyncResultDto
            {
                SyncedCount = waitResult.TotalSyncedCount,
                SyncTime = waitResult.SyncTime,
                ErrorMessage = waitResult.ErrorMessage
            };
        }

        // Check if last sync was within the cooldown period (1 minute)
        if (binding.LastSyncTime.HasValue)
        {
            var timeSinceLastSync = DateTimeOffset.UtcNow - binding.LastSyncTime.Value;
            var cooldownMinutes = EmailConstants.SyncSettings.IncrementalSyncCooldownMinutes;
            
            if (timeSinceLastSync.TotalMinutes < cooldownMinutes)
            {
                var remainingSeconds = (int)((cooldownMinutes * 60) - timeSinceLastSync.TotalSeconds);
                _logger.LogDebug("Sync called too soon for user {UserId}, last sync was {Seconds} seconds ago, skipping", 
                    userId, (int)timeSinceLastSync.TotalSeconds);
                return new SyncResultDto
                {
                    SyncedCount = 0,
                    SyncTime = DateTimeOffset.UtcNow,
                    ErrorMessage = $"Sync can only be performed once per minute. Please try again in {remainingSeconds} seconds."
                };
            }
        }

        // Mark as syncing to prevent duplicate calls
        await _bindingRepository.UpdateSyncStatusAsync(binding.Id, EmailConstants.SyncStatus.Syncing);

        // Refresh token if needed
        await RefreshTokenIfNeededAsync(binding.Id);

        // Reload binding to get updated token
        binding = await _bindingRepository.GetByIdNullableAsync(binding.Id);
        if (binding == null || string.IsNullOrEmpty(binding.AccessToken))
        {
            throw new CRMException(ErrorCodeEnum.BusinessError, "Failed to get valid access token");
        }

        // Decrypt access token for API calls
        var accessToken = DecryptToken(binding.AccessToken);

        try
        {
            // Use Delta Query for efficient incremental sync
            var folders = new List<(string folderId, string? deltaLink)>
            {
                (EmailConstants.OutlookFolder.Inbox, binding.DeltaLinkInbox),
                (EmailConstants.OutlookFolder.SentItems, binding.DeltaLinkSent),
                (EmailConstants.OutlookFolder.DeletedItems, binding.DeltaLinkDeleted)
            };

            _logger.LogDebug("Delta sync for user {UserId} (parallel)", userId);

            // Parallel sync all folders for better performance
            var syncTasks = folders.Select(async folder =>
            {
                var (folderId, deltaLink) = folder;
                var result = await _outlookService.SyncEmailsWithDeltaAsync(
                    accessToken, userId, folderId, deltaLink);

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    // If delta link expired, clear it and retry
                    if (result.ErrorMessage.Contains("full sync required"))
                    {
                        _logger.LogWarning("Delta link expired for folder {Folder}, clearing", folderId);
                        await _bindingRepository.UpdateDeltaLinkAsync(binding.Id, folderId, null);
                        
                        // Retry with null delta link (initial sync)
                        result = await _outlookService.SyncEmailsWithDeltaAsync(
                            accessToken, userId, folderId, null);
                    }
                    else
                    {
                        _logger.LogWarning("Delta sync error for folder {Folder}: {Error}", folderId, result.ErrorMessage);
                    }
                }

                // Update delta link for next sync
                if (!string.IsNullOrEmpty(result.DeltaLink))
                {
                    await _bindingRepository.UpdateDeltaLinkAsync(binding.Id, folderId, result.DeltaLink);
                }

                _logger.LogDebug("Delta sync folder {Folder}: added={Added}, updated={Updated}, deleted={Deleted}",
                    folderId, result.AddedCount, result.UpdatedCount, result.DeletedCount);

                return result;
            });

            var results = await Task.WhenAll(syncTasks);

            var totalAdded = results.Sum(r => r.AddedCount);
            var totalUpdated = results.Sum(r => r.UpdatedCount);
            var totalDeleted = results.Sum(r => r.DeletedCount);

            await _bindingRepository.UpdateLastSyncTimeAsync(binding.Id);
            await _bindingRepository.UpdateSyncStatusAsync(binding.Id, EmailConstants.SyncStatus.Active);

            _logger.LogInformation("Delta sync completed for user {UserId}: added={Added}, updated={Updated}, deleted={Deleted}",
                userId, totalAdded, totalUpdated, totalDeleted);

            return new SyncResultDto
            {
                SyncedCount = totalAdded + totalUpdated + totalDeleted,
                SyncTime = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync emails for user {UserId}", userId);
            await _bindingRepository.UpdateSyncStatusAsync(binding.Id, EmailConstants.SyncStatus.Error, ex.Message);

            return new SyncResultDto
            {
                SyncedCount = 0,
                SyncTime = DateTimeOffset.UtcNow,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Full sync - sync all emails from all folders
    /// </summary>
    public async Task<FullSyncResultDto> FullSyncAsync()
    {
        var userId = GetCurrentUserId();
        var binding = await _bindingRepository.GetByUserIdAndProviderAsync(userId, EmailConstants.Provider.Outlook);

        if (binding == null)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, "No email binding found. Please bind your Outlook account first.");
        }

        // Check if sync is already in progress, wait for it to complete
        if (binding.SyncStatus == EmailConstants.SyncStatus.Syncing)
        {
            _logger.LogInformation("Full sync already in progress for user {UserId}, waiting for completion", userId);
            return await WaitForSyncCompletionAsync(binding.Id, userId);
        }

        // Check if last sync was within the cooldown period (1 hour for full sync)
        if (binding.LastSyncTime.HasValue)
        {
            var timeSinceLastSync = DateTimeOffset.UtcNow - binding.LastSyncTime.Value;
            var cooldownMinutes = EmailConstants.SyncSettings.FullSyncCooldownMinutes;

            if (timeSinceLastSync.TotalMinutes < cooldownMinutes)
            {
                var remainingMinutes = (int)(cooldownMinutes - timeSinceLastSync.TotalMinutes);
                _logger.LogWarning("Full sync called too soon for user {UserId}, last sync was {Minutes} minutes ago",
                    userId, (int)timeSinceLastSync.TotalMinutes);
                return new FullSyncResultDto
                {
                    TotalSyncedCount = 0,
                    SyncedCountByFolder = new Dictionary<string, int>(),
                    SyncTime = DateTimeOffset.UtcNow,
                    ErrorMessage = $"Full sync can only be performed once per hour. Please try again in {remainingMinutes} minutes.",
                    IsComplete = false
                };
            }
        }

        // Mark as syncing to prevent duplicate calls
        await _bindingRepository.UpdateSyncStatusAsync(binding.Id, EmailConstants.SyncStatus.Syncing);

        // Refresh token if needed
        await RefreshTokenIfNeededAsync(binding.Id);

        binding = await _bindingRepository.GetByIdNullableAsync(binding.Id);
        if (binding == null || string.IsNullOrEmpty(binding.AccessToken))
        {
            throw new CRMException(ErrorCodeEnum.BusinessError, "Failed to get valid access token");
        }

        // Decrypt access token for API calls
        var accessToken = DecryptToken(binding.AccessToken);

        try
        {
            var folders = new List<string>
            {
                EmailConstants.OutlookFolder.Inbox,
                EmailConstants.OutlookFolder.SentItems,
                EmailConstants.OutlookFolder.DeletedItems
            };
            var maxCount = EmailConstants.SyncSettings.FullSyncMaxCount;
            var syncedByFolder = new Dictionary<string, int>();
            var totalSynced = 0;

            _logger.LogDebug("Starting full sync for user {UserId}, folders: {Folders}, maxCount: {MaxCount}",
                userId, string.Join(", ", folders), maxCount);

            foreach (var folder in folders)
            {
                var (synced, deltaLink) = await _outlookService.FullSyncEmailsAsync(accessToken, userId, folder, maxCount);
                syncedByFolder[folder] = synced;
                totalSynced += synced;

                // Save delta link for subsequent incremental syncs
                if (!string.IsNullOrEmpty(deltaLink))
                {
                    await _bindingRepository.UpdateDeltaLinkAsync(binding.Id, folder, deltaLink);
                    _logger.LogDebug("Full sync: saved delta link for folder {Folder}", folder);
                }

                _logger.LogDebug("Full sync: synced {Count} emails from folder {Folder}", synced, folder);
            }

            await _bindingRepository.UpdateLastSyncTimeAsync(binding.Id);
            await _bindingRepository.UpdateSyncStatusAsync(binding.Id, EmailConstants.SyncStatus.Active);

            return new FullSyncResultDto
            {
                TotalSyncedCount = totalSynced,
                SyncedCountByFolder = syncedByFolder,
                SyncTime = DateTimeOffset.UtcNow,
                IsComplete = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to full sync emails for user {UserId}", userId);
            await _bindingRepository.UpdateSyncStatusAsync(binding.Id, EmailConstants.SyncStatus.Error, ex.Message);

            return new FullSyncResultDto
            {
                TotalSyncedCount = 0,
                SyncedCountByFolder = new Dictionary<string, int>(),
                SyncTime = DateTimeOffset.UtcNow,
                ErrorMessage = ex.Message,
                IsComplete = false
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
            await _bindingRepository.UpdateSyncStatusAsync(bindingId, EmailConstants.SyncStatus.Error, "No refresh token available");
            return false;
        }

        // Decrypt refresh token for API call
        var refreshToken = DecryptToken(binding.RefreshToken);
        var newToken = await _outlookService.RefreshTokenAsync(refreshToken);
        if (newToken == null)
        {
            await _bindingRepository.UpdateSyncStatusAsync(bindingId, EmailConstants.SyncStatus.Error, "Failed to refresh token");
            return false;
        }

        // Encrypt new tokens before storing
        await _bindingRepository.UpdateTokenAsync(
            bindingId,
            EncryptToken(newToken.AccessToken),
            EncryptToken(newToken.RefreshToken),
            newToken.ExpiresAt);

        _logger.LogDebug("Refreshed token for binding {BindingId}", bindingId);
        return true;
    }

    /// <summary>
    /// Sync emails for a specific user (used by background service)
    /// Uses Delta Query for efficient incremental sync
    /// </summary>
    public async Task<SyncResultDto> SyncEmailsForUserAsync(long userId)
    {
        var binding = await _bindingRepository.GetByUserIdAndProviderAsync(userId, EmailConstants.Provider.Outlook);

        if (binding == null)
        {
            return new SyncResultDto
            {
                SyncedCount = 0,
                SyncTime = DateTimeOffset.UtcNow,
                ErrorMessage = "No email binding found"
            };
        }

        // Check if sync is already in progress
        if (binding.SyncStatus == EmailConstants.SyncStatus.Syncing)
        {
            _logger.LogDebug("Sync already in progress for user {UserId}, skipping", userId);
            return new SyncResultDto
            {
                SyncedCount = 0,
                SyncTime = DateTimeOffset.UtcNow,
                ErrorMessage = "Sync already in progress"
            };
        }

        // Check cooldown period
        if (binding.LastSyncTime.HasValue)
        {
            var timeSinceLastSync = DateTimeOffset.UtcNow - binding.LastSyncTime.Value;
            var cooldownMinutes = EmailConstants.SyncSettings.IncrementalSyncCooldownMinutes;

            if (timeSinceLastSync.TotalMinutes < cooldownMinutes)
            {
                _logger.LogDebug("Sync called too soon for user {UserId}, skipping", userId);
                return new SyncResultDto
                {
                    SyncedCount = 0,
                    SyncTime = DateTimeOffset.UtcNow
                };
            }
        }

        // Refresh token if needed
        var tokenRefreshed = await RefreshTokenIfNeededAsync(binding.Id);
        if (!tokenRefreshed)
        {
            return new SyncResultDto
            {
                SyncedCount = 0,
                SyncTime = DateTimeOffset.UtcNow,
                ErrorMessage = "Failed to refresh token"
            };
        }

        // Reload binding to get updated token
        binding = await _bindingRepository.GetByIdNullableAsync(binding.Id);
        if (binding == null || string.IsNullOrEmpty(binding.AccessToken))
        {
            return new SyncResultDto
            {
                SyncedCount = 0,
                SyncTime = DateTimeOffset.UtcNow,
                ErrorMessage = "Failed to get valid access token"
            };
        }

        // Decrypt access token for API calls
        var accessToken = DecryptToken(binding.AccessToken);

        // Mark as syncing
        await _bindingRepository.UpdateSyncStatusAsync(binding.Id, EmailConstants.SyncStatus.Syncing);

        try
        {
            // Use Delta Query for efficient incremental sync (parallel)
            var folders = new List<(string folderId, string? deltaLink)>
            {
                (EmailConstants.OutlookFolder.Inbox, binding.DeltaLinkInbox),
                (EmailConstants.OutlookFolder.SentItems, binding.DeltaLinkSent),
                (EmailConstants.OutlookFolder.DeletedItems, binding.DeltaLinkDeleted)
            };

            // Parallel sync all folders for better performance
            var syncTasks = folders.Select(async folder =>
            {
                var (folderId, deltaLink) = folder;
                var result = await _outlookService.SyncEmailsWithDeltaAsync(
                    accessToken, userId, folderId, deltaLink);

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    // If delta link expired, clear it and retry
                    if (result.ErrorMessage.Contains("full sync required"))
                    {
                        _logger.LogWarning("Delta link expired for folder {Folder}, user {UserId}, clearing", folderId, userId);
                        await _bindingRepository.UpdateDeltaLinkAsync(binding.Id, folderId, null);
                        
                        // Retry with null delta link
                        result = await _outlookService.SyncEmailsWithDeltaAsync(
                            accessToken, userId, folderId, null);
                    }
                    else
                    {
                        _logger.LogWarning("Delta sync error for folder {Folder}, user {UserId}: {Error}", 
                            folderId, userId, result.ErrorMessage);
                    }
                }

                // Update delta link for next sync
                if (!string.IsNullOrEmpty(result.DeltaLink))
                {
                    await _bindingRepository.UpdateDeltaLinkAsync(binding.Id, folderId, result.DeltaLink);
                }

                return result;
            });

            var results = await Task.WhenAll(syncTasks);

            var totalAdded = results.Sum(r => r.AddedCount);
            var totalUpdated = results.Sum(r => r.UpdatedCount);
            var totalDeleted = results.Sum(r => r.DeletedCount);

            await _bindingRepository.UpdateLastSyncTimeAsync(binding.Id);
            await _bindingRepository.UpdateSyncStatusAsync(binding.Id, EmailConstants.SyncStatus.Active);

            var syncedCount = totalAdded + totalUpdated + totalDeleted;
            _logger.LogDebug("Delta synced for user {UserId}: added={Added}, updated={Updated}, deleted={Deleted}",
                userId, totalAdded, totalUpdated, totalDeleted);

            return new SyncResultDto
            {
                SyncedCount = syncedCount,
                SyncTime = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync emails for user {UserId}", userId);
            await _bindingRepository.UpdateSyncStatusAsync(binding.Id, EmailConstants.SyncStatus.Error, ex.Message);

            return new SyncResultDto
            {
                SyncedCount = 0,
                SyncTime = DateTimeOffset.UtcNow,
                ErrorMessage = ex.Message
            };
        }
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
        // Only cleanup every 5 minutes to avoid frequent lock contention
        if (DateTimeOffset.UtcNow - _lastCleanupTime < TimeSpan.FromMinutes(5))
        {
            return;
        }

        lock (_stateStoreLock)
        {
            if (DateTimeOffset.UtcNow - _lastCleanupTime < TimeSpan.FromMinutes(5))
            {
                return;
            }

            var expiredStates = _stateStore
                .Where(kvp => kvp.Value.ExpireTime < DateTimeOffset.UtcNow)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var state in expiredStates)
            {
                _stateStore.Remove(state);
            }

            _lastCleanupTime = DateTimeOffset.UtcNow;
        }
    }

    private async Task<string> GetUserEmailFromGraphAsync(string accessToken)
    {
        // Get user profile from Microsoft Graph to get email
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me?$select=mail,userPrincipalName");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.SendAsync(request);
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

    /// <summary>
    /// Wait for ongoing sync to complete and return the result
    /// </summary>
    private async Task<FullSyncResultDto> WaitForSyncCompletionAsync(long bindingId, long userId)
    {
        const int maxWaitSeconds = 300; // Maximum wait time: 5 minutes
        const int pollIntervalMs = 2000; // Poll every 2 seconds
        var startTime = DateTimeOffset.UtcNow;

        while ((DateTimeOffset.UtcNow - startTime).TotalSeconds < maxWaitSeconds)
        {
            await Task.Delay(pollIntervalMs);

            var binding = await _bindingRepository.GetByIdNullableAsync(bindingId);
            if (binding == null)
            {
                return new FullSyncResultDto
                {
                    TotalSyncedCount = 0,
                    SyncedCountByFolder = new Dictionary<string, int>(),
                    SyncTime = DateTimeOffset.UtcNow,
                    ErrorMessage = "Email binding not found",
                    IsComplete = false
                };
            }

            // Check if sync completed (status changed from Syncing)
            if (binding.SyncStatus != EmailConstants.SyncStatus.Syncing)
            {
                _logger.LogInformation("Sync completed for user {UserId}, status: {Status}", userId, binding.SyncStatus);

                if (binding.SyncStatus == EmailConstants.SyncStatus.Error)
                {
                    return new FullSyncResultDto
                    {
                        TotalSyncedCount = 0,
                        SyncedCountByFolder = new Dictionary<string, int>(),
                        SyncTime = binding.LastSyncTime ?? DateTimeOffset.UtcNow,
                        ErrorMessage = binding.LastSyncError ?? "Sync failed",
                        IsComplete = false
                    };
                }

                // Sync completed successfully
                return new FullSyncResultDto
                {
                    TotalSyncedCount = 0, // We don't know the exact count from the other request
                    SyncedCountByFolder = new Dictionary<string, int>(),
                    SyncTime = binding.LastSyncTime ?? DateTimeOffset.UtcNow,
                    IsComplete = true,
                    ErrorMessage = null
                };
            }
        }

        // Timeout waiting for sync
        _logger.LogWarning("Timeout waiting for sync completion for user {UserId}", userId);
        return new FullSyncResultDto
        {
            TotalSyncedCount = 0,
            SyncedCountByFolder = new Dictionary<string, int>(),
            SyncTime = DateTimeOffset.UtcNow,
            ErrorMessage = "Timeout waiting for sync to complete. The sync is still running in the background.",
            IsComplete = false
        };
    }

    /// <summary>
    /// Encrypt token for secure storage
    /// </summary>
    private string EncryptToken(string token)
    {
        if (string.IsNullOrEmpty(token)) return token;
        try
        {
            return _encryptionService.Encrypt(token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to encrypt token, storing as plain text");
            return token;
        }
    }

    /// <summary>
    /// Decrypt token for use
    /// </summary>
    private string DecryptToken(string encryptedToken)
    {
        if (string.IsNullOrEmpty(encryptedToken)) return encryptedToken;
        try
        {
            return _encryptionService.Decrypt(encryptedToken);
        }
        catch (Exception ex)
        {
            // Token might not be encrypted (legacy data), return as-is
            _logger.LogDebug(ex, "Failed to decrypt token, assuming plain text");
            return encryptedToken;
        }
    }

    #endregion
}
