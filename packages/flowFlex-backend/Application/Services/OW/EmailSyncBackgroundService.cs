using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Services.OW;

/// <summary>
/// Email sync background service - automatically syncs emails for users with auto-sync enabled
/// </summary>
public class EmailSyncBackgroundService : BackgroundService, ISingletonService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailSyncBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Check every 5 minutes

    public EmailSyncBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<EmailSyncBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email sync background service started");

        // Wait a bit before starting to allow the application to fully initialize
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformSyncAsync(stoppingToken);
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during email sync background task");
                // Wait before retrying
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
        }

        _logger.LogInformation("Email sync background service stopped");
    }


    private async Task PerformSyncAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var bindingRepository = scope.ServiceProvider.GetRequiredService<IEmailBindingRepository>();
        var outlookService = scope.ServiceProvider.GetRequiredService<IOutlookService>();

        try
        {
            // Get all bindings that need sync
            var bindingsToSync = await bindingRepository.GetBindingsNeedingSyncAsync();

            if (bindingsToSync.Count == 0)
            {
                _logger.LogDebug("No email bindings need sync at this time");
                return;
            }

            _logger.LogInformation("Found {Count} email bindings to sync", bindingsToSync.Count);

            foreach (var binding in bindingsToSync)
            {
                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    await SyncUserEmailsAsync(binding.Id, binding.UserId, binding.AccessToken,
                        binding.RefreshToken, binding.TokenExpireTime, bindingRepository, outlookService);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error syncing emails for user {UserId}", binding.UserId);
                    await bindingRepository.UpdateSyncStatusAsync(binding.Id, "Error", ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email sync background task");
            throw;
        }
    }

    private async Task SyncUserEmailsAsync(
        long bindingId,
        long userId,
        string accessToken,
        string? refreshToken,
        DateTimeOffset tokenExpireTime,
        IEmailBindingRepository bindingRepository,
        IOutlookService outlookService)
    {
        // Check if token needs refresh
        if (tokenExpireTime <= DateTimeOffset.UtcNow.AddMinutes(5))
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("Token expired and no refresh token for binding {BindingId}", bindingId);
                await bindingRepository.UpdateSyncStatusAsync(bindingId, "Error", "Token expired, please re-authorize");
                return;
            }

            var newToken = await outlookService.RefreshTokenAsync(refreshToken);
            if (newToken == null)
            {
                _logger.LogWarning("Failed to refresh token for binding {BindingId}", bindingId);
                await bindingRepository.UpdateSyncStatusAsync(bindingId, "Error", "Failed to refresh token");
                return;
            }

            await bindingRepository.UpdateTokenAsync(bindingId, newToken.AccessToken, newToken.RefreshToken, newToken.ExpiresAt);
            accessToken = newToken.AccessToken;
            _logger.LogInformation("Refreshed token for binding {BindingId}", bindingId);
        }

        // Sync inbox emails
        var syncedCount = await outlookService.SyncEmailsToLocalAsync(accessToken, userId, "inbox", 50);

        // Update sync status
        await bindingRepository.UpdateLastSyncTimeAsync(bindingId);
        await bindingRepository.UpdateSyncStatusAsync(bindingId, "Active");

        _logger.LogInformation("Auto-synced {Count} emails for user {UserId}", syncedCount, userId);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email sync background service is stopping");
        await base.StopAsync(cancellationToken);
    }
}
