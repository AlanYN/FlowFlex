using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using FlowFlex.Application.Contracts.Dtos.OW.OperationChangeLog;
using FlowFlex.Application.Contracts.IServices.OW.ChangeLog;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Services.OW.ChangeLog
{
    /// <summary>
    /// Log cache service implementation for performance optimization
    /// </summary>
    public class LogCacheService : ILogCacheService, IScopedService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<LogCacheService> _logger;
        
        // Cache prefixes for different types of data
        private const string LogsCachePrefix = "operation_logs:";
        private const string StatsCachePrefix = "operation_stats:";
        private const string CacheVersionKey = "cache_version";
        
        // Default cache expiration times
        private static readonly TimeSpan DefaultLogsExpiration = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan DefaultStatsExpiration = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan LongTermExpiration = TimeSpan.FromHours(2);

        // Cache statistics tracking
        private static int _hitCount = 0;
        private static int _missCount = 0;
        private static DateTime _lastStatsReset = DateTime.UtcNow;

        public LogCacheService(
            IDistributedCache distributedCache,
            ILogger<LogCacheService> logger)
        {
            _distributedCache = distributedCache;
            _logger = logger;
        }

        #region Cache Operations for Logs

        public async Task<PagedResult<OperationChangeLogOutputDto>?> GetCachedLogsAsync(string cacheKey)
        {
            try
            {
                var fullCacheKey = LogsCachePrefix + cacheKey;
                var cachedData = await _distributedCache.GetStringAsync(fullCacheKey);

                if (!string.IsNullOrEmpty(cachedData))
                {
                    Interlocked.Increment(ref _hitCount);
                    var result = JsonSerializer.Deserialize<PagedResult<OperationChangeLogOutputDto>>(cachedData);
                    _logger.LogDebug("Cache hit for logs key: {CacheKey}", cacheKey);
                    return result;
                }

                Interlocked.Increment(ref _missCount);
                _logger.LogDebug("Cache miss for logs key: {CacheKey}", cacheKey);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve cached logs for key: {CacheKey}", cacheKey);
                Interlocked.Increment(ref _missCount);
                return null;
            }
        }

        public async Task SetCachedLogsAsync(string cacheKey, PagedResult<OperationChangeLogOutputDto> logs, TimeSpan? expiration = null)
        {
            try
            {
                var fullCacheKey = LogsCachePrefix + cacheKey;
                var serializedData = JsonSerializer.Serialize(logs);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? DefaultLogsExpiration
                };

                await _distributedCache.SetStringAsync(fullCacheKey, serializedData, options);
                _logger.LogDebug("Cached logs for key: {CacheKey}, expiration: {Expiration}", 
                    cacheKey, options.AbsoluteExpirationRelativeToNow);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache logs for key: {CacheKey}", cacheKey);
            }
        }

        #endregion

        #region Cache Operations for Statistics

        public async Task<Dictionary<string, int>?> GetCachedStatisticsAsync(string cacheKey)
        {
            try
            {
                var fullCacheKey = StatsCachePrefix + cacheKey;
                var cachedData = await _distributedCache.GetStringAsync(fullCacheKey);

                if (!string.IsNullOrEmpty(cachedData))
                {
                    Interlocked.Increment(ref _hitCount);
                    var result = JsonSerializer.Deserialize<Dictionary<string, int>>(cachedData);
                    _logger.LogDebug("Cache hit for statistics key: {CacheKey}", cacheKey);
                    return result;
                }

                Interlocked.Increment(ref _missCount);
                _logger.LogDebug("Cache miss for statistics key: {CacheKey}", cacheKey);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve cached statistics for key: {CacheKey}", cacheKey);
                Interlocked.Increment(ref _missCount);
                return null;
            }
        }

        public async Task SetCachedStatisticsAsync(string cacheKey, Dictionary<string, int> statistics, TimeSpan? expiration = null)
        {
            try
            {
                var fullCacheKey = StatsCachePrefix + cacheKey;
                var serializedData = JsonSerializer.Serialize(statistics);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? DefaultStatsExpiration
                };

                await _distributedCache.SetStringAsync(fullCacheKey, serializedData, options);
                _logger.LogDebug("Cached statistics for key: {CacheKey}, expiration: {Expiration}", 
                    cacheKey, options.AbsoluteExpirationRelativeToNow);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache statistics for key: {CacheKey}", cacheKey);
            }
        }

        #endregion

        #region Cache Invalidation

        public async Task InvalidateCacheForBusinessAsync(string businessModule, long businessId)
        {
            try
            {
                var patterns = new[]
                {
                    $"{LogsCachePrefix}*{businessModule}*{businessId}*",
                    $"{StatsCachePrefix}*{businessModule}*{businessId}*"
                };

                await InvalidateCacheByPatternsAsync(patterns);
                _logger.LogDebug("Invalidated cache for business {BusinessModule}:{BusinessId}", businessModule, businessId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate cache for business {BusinessModule}:{BusinessId}", businessModule, businessId);
            }
        }

        public async Task InvalidateCacheForOnboardingAsync(long onboardingId)
        {
            try
            {
                var patterns = new[]
                {
                    $"{LogsCachePrefix}*onboarding_{onboardingId}*",
                    $"{StatsCachePrefix}*onboarding_{onboardingId}*"
                };

                await InvalidateCacheByPatternsAsync(patterns);
                _logger.LogDebug("Invalidated cache for onboarding {OnboardingId}", onboardingId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate cache for onboarding {OnboardingId}", onboardingId);
            }
        }

        public async Task InvalidateCacheForStageAsync(long stageId)
        {
            try
            {
                var patterns = new[]
                {
                    $"{LogsCachePrefix}*stage_{stageId}*",
                    $"{StatsCachePrefix}*stage_{stageId}*"
                };

                await InvalidateCacheByPatternsAsync(patterns);
                _logger.LogDebug("Invalidated cache for stage {StageId}", stageId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate cache for stage {StageId}", stageId);
            }
        }

        #endregion

        #region Cache Key Generation

        public string GenerateLogsCacheKey(
            string businessModule,
            long? businessId = null,
            long? onboardingId = null,
            long? stageId = null,
            string operationType = null,
            int pageIndex = 1,
            int pageSize = 20)
        {
            var keyParts = new List<string>
            {
                "logs",
                businessModule.ToLowerInvariant()
            };

            if (businessId.HasValue)
                keyParts.Add($"business_{businessId.Value}");

            if (onboardingId.HasValue)
                keyParts.Add($"onboarding_{onboardingId.Value}");

            if (stageId.HasValue)
                keyParts.Add($"stage_{stageId.Value}");

            if (!string.IsNullOrEmpty(operationType))
                keyParts.Add($"type_{operationType.ToLowerInvariant()}");

            keyParts.Add($"page_{pageIndex}_{pageSize}");

            return string.Join(":", keyParts);
        }

        public string GenerateStatisticsCacheKey(
            string businessModule,
            long? businessId = null,
            long? onboardingId = null,
            long? stageId = null)
        {
            var keyParts = new List<string>
            {
                "stats",
                businessModule.ToLowerInvariant()
            };

            if (businessId.HasValue)
                keyParts.Add($"business_{businessId.Value}");

            if (onboardingId.HasValue)
                keyParts.Add($"onboarding_{onboardingId.Value}");

            if (stageId.HasValue)
                keyParts.Add($"stage_{stageId.Value}");

            return string.Join(":", keyParts);
        }

        #endregion

        #region Cache Management

        public async Task WarmUpCacheAsync(long onboardingId, long stageId)
        {
            try
            {
                _logger.LogInformation("Starting cache warm-up for onboarding {OnboardingId}, stage {StageId}", onboardingId, stageId);

                // This would pre-load frequently accessed data
                // Implementation would depend on your specific caching strategy
                var warmUpTasks = new List<Task>();

                // Add common cache warm-up operations here
                // For example:
                // warmUpTasks.Add(PreloadStageLogsAsync(stageId, onboardingId));
                // warmUpTasks.Add(PreloadStageStatisticsAsync(stageId, onboardingId));

                await Task.WhenAll(warmUpTasks);
                _logger.LogInformation("Completed cache warm-up for onboarding {OnboardingId}, stage {StageId}", onboardingId, stageId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to warm up cache for onboarding {OnboardingId}, stage {StageId}", onboardingId, stageId);
            }
        }

        public async Task ClearAllCachesAsync()
        {
            try
            {
                await InvalidateCacheByPatternsAsync(new[] { LogsCachePrefix + "*", StatsCachePrefix + "*" });
                
                // Reset statistics
                Interlocked.Exchange(ref _hitCount, 0);
                Interlocked.Exchange(ref _missCount, 0);
                _lastStatsReset = DateTime.UtcNow;

                _logger.LogInformation("Cleared all operation log caches");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clear all caches");
            }
        }

        public async Task<CacheStatisticsDto> GetCacheStatisticsAsync()
        {
            try
            {
                var currentHitCount = _hitCount;
                var currentMissCount = _missCount;
                var totalRequests = currentHitCount + currentMissCount;
                var hitRate = totalRequests > 0 ? (double)currentHitCount / totalRequests : 0.0;

                var statistics = new CacheStatisticsDto
                {
                    HitCount = currentHitCount,
                    MissCount = currentMissCount,
                    HitRate = Math.Round(hitRate * 100, 2), // Convert to percentage
                    LastUpdated = DateTime.UtcNow,
                    TotalKeys = await GetApproximateKeyCountAsync(),
                    TotalMemoryUsage = await GetApproximateMemoryUsageAsync()
                };

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get cache statistics");
                return new CacheStatisticsDto
                {
                    LastUpdated = DateTime.UtcNow
                };
            }
        }

        #endregion

        #region Private Helper Methods

        private async Task InvalidateCacheByPatternsAsync(string[] patterns)
        {
            try
            {
                // Note: Pattern-based cache invalidation depends on the cache implementation
                // For Redis, you would use KEYS or SCAN commands
                // For in-memory cache, you might need to track keys separately
                // This is a simplified implementation - in production, you'd want more sophisticated pattern matching

                foreach (var pattern in patterns)
                {
                    // This is a placeholder - implement based on your cache provider
                    _logger.LogDebug("Would invalidate cache pattern: {Pattern}", pattern);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate cache by patterns");
            }
        }

        private async Task<int> GetApproximateKeyCountAsync()
        {
            try
            {
                // This would depend on your cache implementation
                // For Redis: use INFO keyspace or DBSIZE
                // For in-memory: track keys manually
                // Placeholder implementation
                return await Task.FromResult(0);
            }
            catch
            {
                return 0;
            }
        }

        private async Task<long> GetApproximateMemoryUsageAsync()
        {
            try
            {
                // This would depend on your cache implementation
                // For Redis: use INFO memory
                // For in-memory: use GC or performance counters
                // Placeholder implementation
                return await Task.FromResult(0L);
            }
            catch
            {
                return 0L;
            }
        }

        #endregion
    }
}