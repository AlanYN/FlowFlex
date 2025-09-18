using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlowFlex.Infrastructure.Services;

/// <summary>
/// Background service to process queued tasks safely
/// </summary>
public class BackgroundTaskService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundTaskService> _logger;

    public BackgroundTaskService(IServiceProvider serviceProvider, ILogger<BackgroundTaskService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await BackgroundProcessing(stoppingToken);
    }

    private async Task BackgroundProcessing(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var taskQueue = scope.ServiceProvider.GetRequiredService<IBackgroundTaskQueue>();

                var workItem = await taskQueue.DequeueAsync(stoppingToken);

                if (workItem != null)
                {
                    try
                    {
                        await workItem(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error occurred executing background work item");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation token is cancelled
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in background processing loop");
            }
        }
    }
}