using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices
{
    /// <summary>
    /// Distributed cache service interface for Redis-based caching
    /// </summary>
    public interface IDistributedCacheService : IScopedService
    {
        /// <summary>
        /// Get cached value by key
        /// </summary>
        /// <typeparam name="T">Type of cached object</typeparam>
        /// <param name="key">Cache key</param>
        /// <returns>Cached object or null if not found</returns>
        Task<T> GetAsync<T>(string key) where T : class;

        /// <summary>
        /// Set cache value with optional expiry
        /// </summary>
        /// <typeparam name="T">Type of object to cache</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="value">Object to cache</param>
        /// <param name="expiry">Cache expiry time (optional)</param>
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;

        /// <summary>
        /// Remove cached value by key
        /// </summary>
        /// <param name="key">Cache key to remove</param>
        Task RemoveAsync(string key);

        /// <summary>
        /// Remove multiple cached values by pattern
        /// </summary>
        /// <param name="pattern">Key pattern (supports wildcards)</param>
        Task RemoveByPatternAsync(string pattern);

        /// <summary>
        /// Check if cache key exists
        /// </summary>
        /// <param name="key">Cache key to check</param>
        /// <returns>True if key exists, false otherwise</returns>
        Task<bool> ExistsAsync(string key);

        /// <summary>
        /// Get cache statistics
        /// </summary>
        /// <returns>Cache metrics</returns>
        Task<CacheMetrics> GetMetricsAsync();
    }

    /// <summary>
    /// Cache metrics for monitoring
    /// </summary>
    public class CacheMetrics
    {
        /// <summary>
        /// Total number of cache keys
        /// </summary>
        public long KeyCount { get; set; }

        /// <summary>
        /// Used memory in bytes
        /// </summary>
        public long UsedMemory { get; set; }

        /// <summary>
        /// Cache hit rate (0.0 to 1.0)
        /// </summary>
        public double HitRate { get; set; }

        /// <summary>
        /// Total cache hits
        /// </summary>
        public long HitCount { get; set; }

        /// <summary>
        /// Total cache misses
        /// </summary>
        public long MissCount { get; set; }
    }
}