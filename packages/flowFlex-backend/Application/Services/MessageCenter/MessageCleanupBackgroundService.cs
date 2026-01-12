using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Services.MessageCenter
{
    /// <summary>
    /// Background service to cleanup invalid messages (IsValid = false)
    /// </summary>
    public class MessageCleanupBackgroundService : BackgroundService, ISingletonService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MessageCleanupBackgroundService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24); // Run daily
        private readonly int _retentionDays = 7; // Keep invalid messages for 7 days

        public MessageCleanupBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<MessageCleanupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Message cleanup background service started");

            // Wait a bit before first run to let the application fully start
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformCleanupAsync();
                    await Task.Delay(_cleanupInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during message cleanup");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }

            _logger.LogInformation("Message cleanup background service stopped");
        }

        private async Task PerformCleanupAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var messageRepository = scope.ServiceProvider.GetRequiredService<IMessageRepository>();

            try
            {
                _logger.LogInformation("Starting message cleanup task");

                var deletedCount = await messageRepository.CleanupInvalidMessagesAsync(_retentionDays);

                _logger.LogInformation("Message cleanup completed. Deleted {DeletedCount} invalid messages older than {RetentionDays} days",
                    deletedCount, _retentionDays);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during message cleanup task");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Message cleanup background service is stopping");
            await base.StopAsync(cancellationToken);
        }
    }
}
