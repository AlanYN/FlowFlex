using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlowFlex.Infrastructure.Services;

/// <summary>
/// Background task queue for safely executing fire-and-forget operations
/// </summary>
public interface IBackgroundTaskQueue
{
    /// <summary>
    /// Queue a background task for execution
    /// </summary>
    void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem);

    /// <summary>
    /// Dequeue a background task
    /// </summary>
    Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
}