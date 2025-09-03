using FlowFlex.Application.Contracts.Dtos.OW.OperationChangeLog;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.IServices.OW.ChangeLog
{
    /// <summary>
    /// Log cache service for performance optimization
    /// </summary>
    public interface ILogCacheService : IScopedService
    {
        /// <summary>
        /// Get cached operation logs
        /// </summary>
        Task<PagedResult<OperationChangeLogOutputDto>?> GetCachedLogsAsync(string cacheKey);

        /// <summary>
        /// Cache operation logs with expiration
        /// </summary>
        Task SetCachedLogsAsync(string cacheKey, PagedResult<OperationChangeLogOutputDto> logs, TimeSpan? expiration = null);

        /// <summary>
        /// Get cached operation statistics
        /// </summary>
        Task<Dictionary<string, int>?> GetCachedStatisticsAsync(string cacheKey);

        /// <summary>
        /// Cache operation statistics with expiration
        /// </summary>
        Task SetCachedStatisticsAsync(string cacheKey, Dictionary<string, int> statistics, TimeSpan? expiration = null);

        /// <summary>
        /// Invalidate cache for specific business entity
        /// </summary>
        Task InvalidateCacheForBusinessAsync(string businessModule, long businessId);

        /// <summary>
        /// Invalidate cache for onboarding
        /// </summary>
        Task InvalidateCacheForOnboardingAsync(long onboardingId);

        /// <summary>
        /// Invalidate cache for stage
        /// </summary>
        Task InvalidateCacheForStageAsync(long stageId);

        /// <summary>
        /// Invalidate cache for specific onboarding and stage combination
        /// </summary>
        Task InvalidateCacheForOnboardingAndStageAsync(long onboardingId, long stageId);

        /// <summary>
        /// Generate cache key for operation logs
        /// </summary>
        string GenerateLogsCacheKey(
            string businessModule,
            long? businessId = null,
            long? onboardingId = null,
            long? stageId = null,
            string operationType = null,
            int pageIndex = 1,
            int pageSize = 20);

        /// <summary>
        /// Generate cache key for statistics
        /// </summary>
        string GenerateStatisticsCacheKey(
            string businessModule,
            long? businessId = null,
            long? onboardingId = null,
            long? stageId = null);

        /// <summary>
        /// Warm up cache for frequently accessed data
        /// </summary>
        Task WarmUpCacheAsync(long onboardingId, long stageId);

        /// <summary>
        /// Clear all operation log caches
        /// </summary>
        Task ClearAllCachesAsync();

        /// <summary>
        /// Get cache statistics for monitoring
        /// </summary>
        Task<CacheStatisticsDto> GetCacheStatisticsAsync();
    }

    /// <summary>
    /// Cache statistics DTO
    /// </summary>
    public class CacheStatisticsDto
    {
        public int TotalKeys { get; set; }
        public long TotalMemoryUsage { get; set; }
        public double HitRate { get; set; }
        public int HitCount { get; set; }
        public int MissCount { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}