using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Item.Internal.StandardApi.Response;
using System.Net;
using FlowFlex.Application.Contracts.Dtos.OW.OperationChangeLog;
using FlowFlex.Application.Contracts.IServices.OW.ChangeLog;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.WebApi.Controllers.OW.ChangeLog
{
    /// <summary>
    /// Log aggregation controller - handles cross-module log queries and analytics
    /// </summary>
    [ApiController]
    [Route("ow/logs/aggregation/v{version:apiVersion}")]
    [Display(Name = "Log Aggregation & Analytics")]
    public class LogAggregationController : Controllers.ControllerBase
    {
        private readonly ILogAggregationService _logAggregationService;
        private readonly ILogCacheService _logCacheService;

        public LogAggregationController(
            ILogAggregationService logAggregationService,
            ILogCacheService logCacheService)
        {
            _logAggregationService = logAggregationService;
            _logCacheService = logCacheService;
        }

        /// <summary>
        /// Get aggregated logs across multiple business modules
        /// </summary>
        /// <param name="request">Aggregated log query request</param>
        /// <returns>Paginated aggregated log list</returns>
        [HttpPost("logs")]
        [ProducesResponseType<SuccessResponse<PagedResult<OperationChangeLogOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAggregatedLogsAsync([FromBody] AggregatedLogQueryRequest request)
        {
            var result = await _logAggregationService.GetAggregatedLogsAsync(
                request.OnboardingId,
                request.StageId,
                request.BusinessModules,
                request.OperationTypes,
                request.StartDate,
                request.EndDate,
                request.PageIndex,
                request.PageSize);

            return Success(result);
        }

        /// <summary>
        /// Get logs by multiple business IDs across different modules
        /// </summary>
        /// <param name="request">Multi-business ID query request</param>
        /// <returns>Paginated log list</returns>
        [HttpPost("business-ids")]
        [ProducesResponseType<SuccessResponse<PagedResult<OperationChangeLogOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetLogsByBusinessIdsAsync([FromBody] MultiBusinessIdQueryRequest request)
        {
            var result = await _logAggregationService.GetLogsByBusinessIdsAsync(
                request.BusinessIds,
                request.BusinessModule,
                request.OnboardingId,
                request.PageIndex,
                request.PageSize);

            return Success(result);
        }

        /// <summary>
        /// Get comprehensive operation statistics across all modules
        /// </summary>
        /// <param name="request">Comprehensive statistics request</param>
        /// <returns>Comprehensive statistics</returns>
        [HttpPost("statistics/comprehensive")]
        [ProducesResponseType<SuccessResponse<Dictionary<string, object>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetComprehensiveStatisticsAsync([FromBody] ComprehensiveStatisticsRequest request)
        {
            var result = await _logAggregationService.GetComprehensiveStatisticsAsync(
                request.OnboardingId,
                request.StageId,
                request.StartDate,
                request.EndDate);

            return Success(result);
        }

        /// <summary>
        /// Get operation timeline for analysis
        /// </summary>
        /// <param name="request">Operation timeline request</param>
        /// <returns>Operation timeline data</returns>
        [HttpPost("timeline")]
        [ProducesResponseType<SuccessResponse<List<OperationTimelineDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetOperationTimelineAsync([FromBody] OperationTimelineRequest request)
        {
            var result = await _logAggregationService.GetOperationTimelineAsync(
                request.OnboardingId,
                request.StageId,
                request.StartDate,
                request.EndDate);

            return Success(result);
        }

        /// <summary>
        /// Search logs across all modules with full-text search
        /// </summary>
        /// <param name="request">Log search request</param>
        /// <returns>Paginated search results</returns>
        [HttpPost("search")]
        [ProducesResponseType<SuccessResponse<PagedResult<OperationChangeLogOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SearchLogsAsync([FromBody] LogSearchRequest request)
        {
            var result = await _logAggregationService.SearchLogsAsync(
                request.SearchTerm,
                request.OnboardingId,
                request.StageId,
                request.BusinessModules,
                request.PageIndex,
                request.PageSize);

            return Success(result);
        }

        /// <summary>
        /// Get user activity logs across all modules
        /// </summary>
        /// <param name="request">User activity request</param>
        /// <returns>Paginated user activity logs</returns>
        [HttpPost("user-activity")]
        [ProducesResponseType<SuccessResponse<PagedResult<OperationChangeLogOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetUserActivityLogsAsync([FromBody] UserActivityRequest request)
        {
            var result = await _logAggregationService.GetUserActivityLogsAsync(
                request.OperatorId,
                request.StartDate,
                request.EndDate,
                request.BusinessModules,
                request.PageIndex,
                request.PageSize);

            return Success(result);
        }

        /// <summary>
        /// Export logs to various formats (CSV, Excel, JSON)
        /// </summary>
        /// <param name="request">Log export request</param>
        /// <returns>Exported file data</returns>
        [HttpPost("export")]
        [ProducesResponseType(typeof(FileResult), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ExportLogsAsync([FromBody] LogExportRequest request)
        {
            var exportData = await _logAggregationService.ExportLogsAsync(
                request.Format,
                request.OnboardingId,
                request.StageId,
                request.BusinessModules,
                request.StartDate,
                request.EndDate);

            var contentType = GetContentType(request.Format);
            var fileName = GetFileName(request.Format);

            return File(exportData, contentType, fileName);
        }

        /// <summary>
        /// Get cache statistics for monitoring
        /// </summary>
        /// <returns>Cache statistics</returns>
        [HttpGet("cache/statistics")]
        [ProducesResponseType<SuccessResponse<CacheStatisticsDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetCacheStatisticsAsync()
        {
            var result = await _logCacheService.GetCacheStatisticsAsync();
            return Success(result);
        }

        /// <summary>
        /// Warm up cache for frequently accessed data
        /// </summary>
        /// <param name="request">Cache warm-up request</param>
        /// <returns>Success result</returns>
        [HttpPost("cache/warmup")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> WarmUpCacheAsync([FromBody] CacheWarmUpRequest request)
        {
            await _logCacheService.WarmUpCacheAsync(request.OnboardingId, request.StageId);
            return Success(true);
        }

        /// <summary>
        /// Clear all operation log caches
        /// </summary>
        /// <returns>Success result</returns>
        [HttpPost("cache/clear")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> ClearAllCachesAsync()
        {
            await _logCacheService.ClearAllCachesAsync();
            return Success(true);
        }

        /// <summary>
        /// Clear cache for specific onboarding
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>Success result</returns>
        [HttpPost("cache/clear/onboarding/{onboardingId}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> ClearOnboardingCacheAsync(long onboardingId)
        {
            await _logCacheService.InvalidateCacheForOnboardingAsync(onboardingId);
            return Success(true);
        }

        /// <summary>
        /// Clear cache for specific stage
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Success result</returns>
        [HttpPost("cache/clear/stage/{stageId}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> ClearStageCacheAsync(long stageId)
        {
            await _logCacheService.InvalidateCacheForStageAsync(stageId);
            return Success(true);
        }

        #region Private Helper Methods

        private static string GetContentType(string format)
        {
            return format.ToLowerInvariant() switch
            {
                "csv" => "text/csv",
                "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "json" => "application/json",
                _ => "application/octet-stream"
            };
        }

        private static string GetFileName(string format)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            return format.ToLowerInvariant() switch
            {
                "csv" => $"operation_logs_{timestamp}.csv",
                "excel" => $"operation_logs_{timestamp}.xlsx",
                "json" => $"operation_logs_{timestamp}.json",
                _ => $"operation_logs_{timestamp}.dat"
            };
        }

        #endregion
    }

    #region Request DTOs

    /// <summary>
    /// Aggregated log query request
    /// </summary>
    public class AggregatedLogQueryRequest
    {
        public long? OnboardingId { get; set; }
        public long? StageId { get; set; }
        public List<BusinessModuleEnum> BusinessModules { get; set; }
        public List<OperationTypeEnum> OperationTypes { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// Multi-business ID query request
    /// </summary>
    public class MultiBusinessIdQueryRequest
    {
        [Required]
        public List<long> BusinessIds { get; set; }

        public BusinessModuleEnum? BusinessModule { get; set; }
        public long? OnboardingId { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// Comprehensive statistics request
    /// </summary>
    public class ComprehensiveStatisticsRequest
    {
        public long? OnboardingId { get; set; }
        public long? StageId { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
    }

    /// <summary>
    /// Operation timeline request
    /// </summary>
    public class OperationTimelineRequest
    {
        public long? OnboardingId { get; set; }
        public long? StageId { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
    }

    /// <summary>
    /// Log search request
    /// </summary>
    public class LogSearchRequest
    {
        [Required]
        [StringLength(500)]
        public string SearchTerm { get; set; }

        public long? OnboardingId { get; set; }
        public long? StageId { get; set; }
        public List<BusinessModuleEnum> BusinessModules { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// User activity request
    /// </summary>
    public class UserActivityRequest
    {
        [Required]
        public long OperatorId { get; set; }

        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public List<BusinessModuleEnum> BusinessModules { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// Log export request
    /// </summary>
    public class LogExportRequest
    {
        [Required]
        [StringLength(10)]
        public string Format { get; set; } // "csv", "excel", "json"

        public long? OnboardingId { get; set; }
        public long? StageId { get; set; }
        public List<BusinessModuleEnum> BusinessModules { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
    }

    /// <summary>
    /// Cache warm-up request
    /// </summary>
    public class CacheWarmUpRequest
    {
        [Required]
        public long OnboardingId { get; set; }

        [Required]
        public long StageId { get; set; }
    }

    #endregion
}