using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FlowFlex.Application.Contracts;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// File cleanup background service
    /// </summary>
    public class FileCleanupBackgroundService : BackgroundService, ISingletonService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<FileCleanupBackgroundService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(6); // Clean up every 6 hours

        public FileCleanupBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<FileCleanupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("File cleanup background service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformCleanupAsync();
                    await Task.Delay(_cleanupInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Normal cancellation operation
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during file cleanup");
                    // Wait for a shorter time before retrying when an error occurs
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                }
            }

            _logger.LogInformation("File cleanup background service stopped");
        }

        private async Task PerformCleanupAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var fileStorageService = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

            try
            {
                _logger.LogInformation("Starting file cleanup task");

                // Clean up temporary files
                var deletedCount = await fileStorageService.CleanupTempFilesAsync();
                
                _logger.LogInformation($"File cleanup completed. Deleted {deletedCount} temporary files");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during file cleanup task");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("File cleanup background service is stopping");
            await base.StopAsync(cancellationToken);
        }
    }
} 