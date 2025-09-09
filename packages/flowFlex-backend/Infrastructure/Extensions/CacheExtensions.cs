using FlowFlex.Application.Contracts.IServices;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Infrastructure.Extensions
{
    /// <summary>
    /// Cache extension methods for common caching patterns
    /// </summary>
    public static class CacheExtensions
    {
        /// <summary>
        /// Get or create cached value with factory method
        /// </summary>
        /// <typeparam name="T">Type of cached object</typeparam>
        /// <param name="cacheService">Cache service instance</param>
        /// <param name="key">Cache key</param>
        /// <param name="factory">Factory method to create value if not cached</param>
        /// <param name="expiry">Cache expiry time</param>
        /// <param name="logger">Logger instance</param>
        /// <returns>Cached or newly created value</returns>
        public static async Task<T> GetOrCreateAsync<T>(
            this IDistributedCacheService cacheService,
            string key,
            Func<Task<T>> factory,
            TimeSpan? expiry = null,
            ILogger logger = null) where T : class
        {
            // Try to get from cache first
            var cached = await cacheService.GetAsync<T>(key);
            if (cached != null)
            {
                logger?.LogDebug("Cache hit for key: {Key}", key);
                return cached;
            }

            logger?.LogDebug("Cache miss for key: {Key}, creating new value", key);

            // Create new value
            var value = await factory();

            if (value != null)
            {
                // Cache the new value
                await cacheService.SetAsync(key, value, expiry);
                logger?.LogDebug("Cached new value for key: {Key}", key);
            }

            return value;
        }

        /// <summary>
        /// Invalidate cache entries by multiple keys
        /// </summary>
        /// <param name="cacheService">Cache service instance</param>
        /// <param name="keys">Cache keys to invalidate</param>
        /// <param name="logger">Logger instance</param>
        public static async Task InvalidateAsync(
            this IDistributedCacheService cacheService,
            IEnumerable<string> keys,
            ILogger logger = null)
        {
            var tasks = keys.Select(async key =>
            {
                try
                {
                    await cacheService.RemoveAsync(key);
                    logger?.LogDebug("Invalidated cache key: {Key}", key);
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Failed to invalidate cache key: {Key}", key);
                }
            });

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Invalidate cache entries by patterns
        /// </summary>
        /// <param name="cacheService">Cache service instance</param>
        /// <param name="patterns">Cache key patterns to invalidate</param>
        /// <param name="logger">Logger instance</param>
        public static async Task InvalidateByPatternsAsync(
            this IDistributedCacheService cacheService,
            IEnumerable<string> patterns,
            ILogger logger = null)
        {
            var tasks = patterns.Select(async pattern =>
            {
                try
                {
                    await cacheService.RemoveByPatternAsync(pattern);
                    logger?.LogDebug("Invalidated cache pattern: {Pattern}", pattern);
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Failed to invalidate cache pattern: {Pattern}", pattern);
                }
            });

            await Task.WhenAll(tasks);
        }
    }
}