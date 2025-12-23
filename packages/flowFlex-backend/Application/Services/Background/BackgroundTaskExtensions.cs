using Microsoft.Extensions.DependencyInjection;

namespace Application.Services.Background;

/// <summary>
/// Extension methods for registering background task services
/// </summary>
public static class BackgroundTaskExtensions
{
    /// <summary>
    /// Add background task queue and processing service
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="queueCapacity">Max queue size (default: 1000)</param>
    /// <param name="concurrency">Number of concurrent consumers (default: 4)</param>
    public static IServiceCollection AddBackgroundTaskQueue(
        this IServiceCollection services,
        int queueCapacity = 1000,
        int concurrency = 4)
    {
        services.AddSingleton<IBackgroundTaskQueue>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<BackgroundTaskQueue>>();
            return new BackgroundTaskQueue(logger, queueCapacity);
        });

        services.AddHostedService(sp =>
        {
            var queue = sp.GetRequiredService<IBackgroundTaskQueue>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<BackgroundTaskService>>();
            return new BackgroundTaskService(queue, sp, logger, concurrency);
        });

        return services;
    }
}

