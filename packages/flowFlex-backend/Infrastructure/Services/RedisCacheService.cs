using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.Options;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using Item.Redis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FlowFlex.Infrastructure.Services
{
    /// <summary>
    /// Redis-based distributed cache service implementation
    /// </summary>
    public class RedisCacheService : IDistributedCacheService
    {
        private readonly IRedisService _redisService;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly UserContext _userContext;
        private readonly CacheOptions _cacheOptions;

        // Cache statistics (in-memory counters)
        private static long _hitCount = 0;
        private static long _missCount = 0;

        // JSON serialization options
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public RedisCacheService(
            IRedisService redisService, 
            ILogger<RedisCacheService> logger, 
            UserContext userContext,
            IOptions<CacheOptions> cacheOptions)
        {
            _redisService = redisService ?? throw new ArgumentNullException(nameof(redisService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _cacheOptions = cacheOptions?.Value ?? throw new ArgumentNullException(nameof(cacheOptions));
        }

        public async Task<T> GetAsync<T>(string key) where T : class
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));
            }

            // Check if caching is enabled
            if (!_cacheOptions.Enabled)
            {
                Interlocked.Increment(ref _missCount);
                if (_cacheOptions.EnableDebugLogging)
                {
                    _logger.LogDebug("Cache disabled, treating as miss for key: {Key}", key);
                }
                return null;
            }

            try
            {
                var tenantKey = BuildTenantKey(key);
                var json = await _redisService.StringGetAsync(tenantKey);
                
                if (string.IsNullOrEmpty(json))
                {
                    Interlocked.Increment(ref _missCount);
                    _logger.LogDebug("Cache miss for key: {Key}", key);
                    return null;
                }
                
                Interlocked.Increment(ref _hitCount);
                _logger.LogDebug("Cache hit for key: {Key}", key);
                
                return JsonSerializer.Deserialize<T>(json, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize cached value for key: {Key}, treating as cache miss", key);
                Interlocked.Increment(ref _missCount);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis cache get failed for key: {Key}, treating as cache miss", key);
                Interlocked.Increment(ref _missCount);
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));
            }

            if (value == null)
            {
                _logger.LogWarning("Attempting to cache null value for key: {Key}", key);
                return;
            }

            // Check if caching is enabled
            if (!_cacheOptions.Enabled)
            {
                if (_cacheOptions.EnableDebugLogging)
                {
                    _logger.LogDebug("Cache disabled, skipping set for key: {Key}", key);
                }
                return;
            }

            try
            {
                var tenantKey = BuildTenantKey(key);
                var json = JsonSerializer.Serialize(value, JsonOptions);
                
                if (expiry.HasValue)
                {
                    await _redisService.StringSetAsync(tenantKey, json, expiry.Value);
                    _logger.LogDebug("Cached value for key: {Key} with expiry: {Expiry}", key, expiry.Value);
                }
                else
                {
                    await _redisService.StringSetAsync(tenantKey, json);
                    _logger.LogDebug("Cached value for key: {Key} without expiry", key);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to serialize value for caching, key: {Key}", key);
                // Don't throw for serialization errors, just log and continue
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis cache set failed for key: {Key}, continuing without caching", key);
                // Don't throw for Redis errors, just log and continue
                return;
            }
        }

        public async Task RemoveAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));
            }

            try
            {
                var tenantKey = BuildTenantKey(key);
                var removed = await _redisService.KeyDelAsync(tenantKey);
                
                if (removed)
                {
                    _logger.LogDebug("Removed cache key: {Key}", key);
                }
                else
                {
                    _logger.LogDebug("Cache key not found for removal: {Key}", key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis cache remove failed for key: {Key}, continuing without removal", key);
                // Don't throw for Redis errors, just log and continue
            }
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                throw new ArgumentException("Cache pattern cannot be null or empty", nameof(pattern));
            }

            try
            {
                // Since IRedisService doesn't have KeysAsync, we'll use a simpler approach
                // This is a limitation of the current IRedisService interface
                var tenantPattern = BuildTenantKey(pattern);
                _logger.LogWarning("Pattern-based cache removal not fully supported with current IRedisService. Pattern: {Pattern}", pattern);
                
                // For now, we'll just log the attempt
                // In a full implementation, you would need to extend IRedisService to support pattern operations
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis cache pattern removal failed for pattern: {Pattern}, continuing without removal", pattern);
                // Don't throw for Redis errors, just log and continue
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));
            }

            try
            {
                var tenantKey = BuildTenantKey(key);
                return await _redisService.KeyExistsAsync(tenantKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis cache exists check failed for key: {Key}, returning false", key);
                return false;
            }
        }

        public async Task<CacheMetrics> GetMetricsAsync()
        {
            try
            {
                // Use simple metrics since IRedisService doesn't have DbSizeAsync/InfoAsync
                var totalRequests = _hitCount + _missCount;
                var hitRate = totalRequests > 0 ? (double)_hitCount / totalRequests : 0.0;

                return new CacheMetrics
                {
                    KeyCount = 0, // Not available with current IRedisService
                    UsedMemory = 0, // Not available with current IRedisService
                    HitRate = hitRate,
                    HitCount = _hitCount,
                    MissCount = _missCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get cache metrics");
                return new CacheMetrics();
            }
        }

        /// <summary>
        /// Build tenant-specific cache key to ensure data isolation
        /// </summary>
        private string BuildTenantKey(string key)
        {
            var tenantId = _userContext?.TenantId?.ToLowerInvariant() ?? "default";
            return $"cache:{tenantId}:{key}";
        }


    }
}