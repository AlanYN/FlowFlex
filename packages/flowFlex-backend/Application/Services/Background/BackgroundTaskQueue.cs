using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Application.Services.Background;

/// <summary>
/// Background task work item - captures all necessary data at creation time
/// </summary>
public sealed class BackgroundWorkItem
{
    public required Func<IServiceProvider, CancellationToken, ValueTask> WorkItem { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// High-performance background task queue using System.Threading.Channels
/// </summary>
public interface IBackgroundTaskQueue
{
    /// <summary>
    /// Queue a background work item
    /// </summary>
    /// <param name="workItem">The work to execute</param>
    /// <param name="description">Optional description for logging</param>
    ValueTask QueueAsync(
        Func<IServiceProvider, CancellationToken, ValueTask> workItem,
        string? description = null);

    /// <summary>
    /// Dequeue a work item (used by background service)
    /// </summary>
    ValueTask<BackgroundWorkItem> DequeueAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get current queue size
    /// </summary>
    int Count { get; }
}

/// <summary>
/// Implementation using bounded channel for backpressure support
/// </summary>
public sealed class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<BackgroundWorkItem> _queue;
    private readonly ILogger<BackgroundTaskQueue> _logger;
    private int _count;

    public BackgroundTaskQueue(ILogger<BackgroundTaskQueue> logger, int capacity = 1000)
    {
        _logger = logger;
        
        // Bounded channel provides backpressure - prevents memory exhaustion
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,  // Block if queue is full
            SingleReader = false,  // Multiple consumers supported
            SingleWriter = false   // Multiple producers supported
        };
        
        _queue = Channel.CreateBounded<BackgroundWorkItem>(options);
    }

    public int Count => _count;

    public async ValueTask QueueAsync(
        Func<IServiceProvider, CancellationToken, ValueTask> workItem,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(workItem);

        var item = new BackgroundWorkItem
        {
            WorkItem = workItem,
            Description = description
        };

        await _queue.Writer.WriteAsync(item);
        Interlocked.Increment(ref _count);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Queued background task: {Description}, Queue size: {Count}",
                description ?? "unnamed", _count);
        }
    }

    public async ValueTask<BackgroundWorkItem> DequeueAsync(CancellationToken cancellationToken)
    {
        var item = await _queue.Reader.ReadAsync(cancellationToken);
        Interlocked.Decrement(ref _count);
        return item;
    }
}

