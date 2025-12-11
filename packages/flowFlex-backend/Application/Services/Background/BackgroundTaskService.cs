using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application.Services.Background;

/// <summary>
/// High-performance background task processing service
/// - Uses dedicated thread pool for processing
/// - Proper DI scope management
/// - Graceful shutdown support
/// - Error isolation (one task failure doesn't affect others)
/// </summary>
public sealed class BackgroundTaskService : BackgroundService
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundTaskService> _logger;
    private readonly int _concurrency;

    public BackgroundTaskService(
        IBackgroundTaskQueue taskQueue,
        IServiceProvider serviceProvider,
        ILogger<BackgroundTaskService> logger,
        int concurrency = 4)  // Process up to 4 tasks concurrently
    {
        _taskQueue = taskQueue;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _concurrency = concurrency;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Background Task Service started with concurrency: {Concurrency}", _concurrency);

        // Create multiple consumer tasks for parallel processing
        var consumers = Enumerable.Range(0, _concurrency)
            .Select(i => ConsumeTasksAsync(i, stoppingToken))
            .ToArray();

        await Task.WhenAll(consumers);

        _logger.LogInformation("Background Task Service stopped");
    }

    private async Task ConsumeTasksAsync(int consumerId, CancellationToken stoppingToken)
    {
        _logger.LogDebug("Consumer {ConsumerId} started", consumerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            BackgroundWorkItem? workItem = null;

            try
            {
                workItem = await _taskQueue.DequeueAsync(stoppingToken);

                // Create new scope for each work item - prevents DI lifetime issues
                using var scope = _serviceProvider.CreateScope();
                
                await workItem.WorkItem(scope.ServiceProvider, stoppingToken);

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    var elapsed = DateTime.UtcNow - workItem.CreatedAt;
                    _logger.LogDebug(
                        "Consumer {ConsumerId} completed task: {Description} in {Elapsed:N0}ms",
                        consumerId, workItem.Description ?? "unnamed", elapsed.TotalMilliseconds);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                // Log error but continue processing - don't let one failure stop the service
                _logger.LogError(ex,
                    "Consumer {ConsumerId} failed to process task: {Description}",
                    consumerId, workItem?.Description ?? "unnamed");
            }
        }

        _logger.LogDebug("Consumer {ConsumerId} stopped", consumerId);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Background Task Service stopping. Remaining tasks in queue: {Count}",
            _taskQueue.Count);

        await base.StopAsync(cancellationToken);
    }
}

