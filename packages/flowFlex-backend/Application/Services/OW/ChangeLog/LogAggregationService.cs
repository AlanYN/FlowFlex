using System.Text.Json;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using FlowFlex.Application.Contracts.Dtos.OW.OperationChangeLog;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.IServices.OW.ChangeLog;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Services.OW.ChangeLog
{
    /// <summary>
    /// Log aggregation service implementation for cross-module log queries and analytics
    /// </summary>
    public class LogAggregationService : BaseOperationLogService, ILogAggregationService
    {
        private readonly IActionExecutionService _actionExecutionService;
        private readonly IServiceProvider _serviceProvider;

        public LogAggregationService(
            IOperationChangeLogRepository operationChangeLogRepository,
            ILogger<LogAggregationService> logger,
            UserContext userContext,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ILogCacheService logCacheService,
            IActionExecutionService actionExecutionService,
            IServiceProvider serviceProvider,
            IUserService userService)
            : base(operationChangeLogRepository, logger, userContext, httpContextAccessor, mapper, logCacheService, userService)
        {
            _actionExecutionService = actionExecutionService;
            _serviceProvider = serviceProvider;
        }

        protected override string GetBusinessModuleName() => "LogAggregation";

        /// <summary>
        /// Get operation logs from database (abstract method implementation)
        /// </summary>
        protected override async Task<PagedResult<OperationChangeLogOutputDto>> GetOperationLogsFromDatabaseAsync(
            long? onboardingId,
            long? stageId,
            OperationTypeEnum? operationType,
            int pageIndex,
            int pageSize)
        {
            try
            {
                var operationTypes = operationType.HasValue ? new List<OperationTypeEnum> { operationType.Value } : null;
                return await GetAggregatedLogsAsync(onboardingId, stageId, null, operationTypes, null, null, pageIndex, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get aggregated operation logs from database");
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

        public async Task<PagedResult<OperationChangeLogOutputDto>> GetAggregatedLogsAsync(
            long? onboardingId = null,
            long? stageId = null,
            List<BusinessModuleEnum> businessModules = null,
            List<OperationTypeEnum> operationTypes = null,
            DateTimeOffset? startDate = null,
            DateTimeOffset? endDate = null,
            int pageIndex = 1,
            int pageSize = 20)
        {
            try
            {
                _logger.LogInformation("Getting aggregated logs with filters: OnboardingId={OnboardingId}, StageId={StageId}", onboardingId, stageId);

                // Generate cache key for aggregated query
                string cacheKey = GenerateAggregatedCacheKey(onboardingId, stageId, businessModules, operationTypes, startDate, endDate, pageIndex, pageSize);
                
                // Skip cache for now - cache disabled for reliability
                // var cachedResult = await _logCacheService.GetCachedLogsAsync(cacheKey);
                // if (cachedResult != null)
                // {
                //     return cachedResult;
                // }

                // For stage component queries (onboarding + stage), use optimized method
                if (onboardingId.HasValue && stageId.HasValue && (businessModules == null || !businessModules.Any()))
                {
                    _logger.LogDebug("Using optimized stage component query for onboarding {OnboardingId} and stage {StageId}", onboardingId, stageId);
                    
                    // Get stage-related task and question IDs
                    var stageLogService = _serviceProvider.GetService<IStageLogService>();
                    if (stageLogService != null)
                    {
                        // Use the optimized stage component logs method
                        var stageResult = await stageLogService.GetStageComponentLogsOptimizedAsync(
                            stageId.Value, 
                            onboardingId, 
                            operationTypes?.FirstOrDefault(),
                            pageIndex, 
                            pageSize);
                        
                        // Skip caching - cache disabled for reliability
                        // await _logCacheService.SetCachedLogsAsync(cacheKey, stageResult, TimeSpan.FromMinutes(10));
                        
                        return stageResult;
                    }
                }

                // Fallback to original logic for other cases
                var logs = new List<Domain.Entities.OW.OperationChangeLog>();

                if (businessModules?.Any() == true)
                {
                    foreach (var module in businessModules)
                    {
                        var moduleLogs = await _operationChangeLogRepository.GetByBusinessAsync(module.ToString(), 0);
                        logs.AddRange(moduleLogs);
                    }
                }
                else
                {
                    // Get all logs if no specific modules requested (this is inefficient but kept for compatibility)
                    _logger.LogWarning("Performing inefficient query for all logs with businessId=0. Consider specifying businessModules.");
                    logs = await _operationChangeLogRepository.GetByBusinessIdAsync(0);
                }

                // Apply filters
                logs = ApplyFilters(logs, onboardingId, stageId, operationTypes, startDate, endDate);

                // Apply pagination
                var totalCount = logs.Count;
                var pagedLogs = logs
                    .OrderByDescending(x => x.OperationTime)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var logDtos = pagedLogs.Select(MapToOutputDto).ToList();

                var result = new PagedResult<OperationChangeLogOutputDto>
                {
                    Items = logDtos,
                    TotalCount = totalCount,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };

                // Skip caching - cache disabled for reliability
                // await _logCacheService.SetCachedLogsAsync(cacheKey, result, TimeSpan.FromMinutes(10));

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting aggregated logs");
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

        public async Task<PagedResult<OperationChangeLogOutputDto>> GetLogsByBusinessIdsAsync(
            List<long> businessIds,
            BusinessModuleEnum? businessModule = null,
            long? onboardingId = null,
            int pageIndex = 1,
            int pageSize = 20)
        {
            try
            {
                if (!businessIds?.Any() == true)
                {
                    return new PagedResult<OperationChangeLogOutputDto>();
                }

                var logs = businessModule.HasValue
                    ? await _operationChangeLogRepository.GetByBusinessIdsAsync(businessModule.ToString(), businessIds, onboardingId)
                    : await _operationChangeLogRepository.GetByBusinessIdsAsync(businessIds);

                // Apply pagination
                var totalCount = logs.Count;
                var pagedLogs = logs
                    .OrderByDescending(x => x.OperationTime)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var logDtos = pagedLogs.Select(MapToOutputDto).ToList();

                return new PagedResult<OperationChangeLogOutputDto>
                {
                    Items = logDtos,
                    TotalCount = totalCount,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting logs by business IDs");
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

        public async Task<Dictionary<string, object>> GetComprehensiveStatisticsAsync(
            long? onboardingId = null,
            long? stageId = null,
            DateTimeOffset? startDate = null,
            DateTimeOffset? endDate = null)
        {
            try
            {
                var statistics = new Dictionary<string, object>();

                // Get basic operation statistics
                var basicStats = await _operationChangeLogRepository.GetOperationStatisticsAsync(onboardingId, stageId);
                statistics["OperationCounts"] = basicStats;

                // Add more comprehensive statistics here
                statistics["TotalOperations"] = basicStats.Values.Sum();
                statistics["GeneratedAt"] = DateTime.UtcNow;

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comprehensive statistics");
                return new Dictionary<string, object>();
            }
        }

        public async Task<List<OperationTimelineDto>> GetOperationTimelineAsync(
            long? onboardingId = null,
            long? stageId = null,
            DateTimeOffset? startDate = null,
            DateTimeOffset? endDate = null)
        {
            try
            {
                // This is a simplified implementation
                // In a real scenario, you'd aggregate data by time periods
                
                var timeline = new List<OperationTimelineDto>();
                
                // Get logs within date range
                var logs = await _operationChangeLogRepository.GetByTimeRangeAsync(
                    startDate ?? DateTimeOffset.UtcNow.AddDays(-30),
                    endDate ?? DateTimeOffset.UtcNow,
                    onboardingId);

                // Group by date and operation type
                var groupedData = logs
                    .GroupBy(x => new { Date = x.OperationTime.Date, x.BusinessModule, x.OperationType })
                    .Select(g => new OperationTimelineDto
                    {
                        Date = g.Key.Date,
                        BusinessModule = g.Key.BusinessModule,
                        OperationType = g.Key.OperationType,
                        Count = g.Count(),
                        TopOperators = g.GroupBy(x => x.OperatorName)
                                       .OrderByDescending(og => og.Count())
                                       .Take(3)
                                       .Select(og => og.Key)
                                       .ToList()
                    })
                    .OrderBy(x => x.Date)
                    .ToList();

                return groupedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting operation timeline");
                return new List<OperationTimelineDto>();
            }
        }

        public async Task<PagedResult<OperationChangeLogOutputDto>> SearchLogsAsync(
            string searchTerm,
            long? onboardingId = null,
            long? stageId = null,
            List<BusinessModuleEnum> businessModules = null,
            int pageIndex = 1,
            int pageSize = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return new PagedResult<OperationChangeLogOutputDto>();
                }

                // Get all relevant logs first
                var allLogs = await GetAggregatedLogsAsync(onboardingId, stageId, businessModules, pageIndex: 1, pageSize: int.MaxValue);

                // Perform in-memory search (in production, consider using full-text search)
                var searchResults = allLogs.Items.Where(log =>
                    (log.OperationTitle?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true) ||
                    (log.OperationDescription?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true) ||
                    (log.OperatorName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true)
                ).ToList();

                // Apply pagination to search results
                var totalCount = searchResults.Count;
                var pagedResults = searchResults
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return new PagedResult<OperationChangeLogOutputDto>
                {
                    Items = pagedResults,
                    TotalCount = totalCount,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching logs with term: {SearchTerm}", searchTerm);
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

        public async Task<PagedResult<OperationChangeLogOutputDto>> GetUserActivityLogsAsync(
            long operatorId,
            DateTimeOffset? startDate = null,
            DateTimeOffset? endDate = null,
            List<BusinessModuleEnum> businessModules = null,
            int pageIndex = 1,
            int pageSize = 20)
        {
            try
            {
                var logs = await _operationChangeLogRepository.GetByOperatorAsync(
                    operatorId,
                    startDate ?? DateTimeOffset.UtcNow.AddDays(-30),
                    endDate ?? DateTimeOffset.UtcNow);

                // Filter by business modules if specified
                if (businessModules?.Any() == true)
                {
                    var moduleNames = businessModules.Select(m => m.ToString()).ToList();
                    logs = logs.Where(x => moduleNames.Contains(x.BusinessModule)).ToList();
                }

                // Apply pagination
                var totalCount = logs.Count;
                var pagedLogs = logs
                    .OrderByDescending(x => x.OperationTime)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var logDtos = pagedLogs.Select(MapToOutputDto).ToList();

                return new PagedResult<OperationChangeLogOutputDto>
                {
                    Items = logDtos,
                    TotalCount = totalCount,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user activity logs for operator {OperatorId}", operatorId);
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

        public async Task<byte[]> ExportLogsAsync(
            string format,
            long? onboardingId = null,
            long? stageId = null,
            List<BusinessModuleEnum> businessModules = null,
            DateTimeOffset? startDate = null,
            DateTimeOffset? endDate = null)
        {
            try
            {
                // Get all logs for export (without pagination)
                var allLogs = await GetAggregatedLogsAsync(
                    onboardingId, stageId, businessModules, null, startDate, endDate,
                    pageIndex: 1, pageSize: int.MaxValue);

                return format.ToLowerInvariant() switch
                {
                    "json" => ExportToJson(allLogs.Items),
                    "csv" => ExportToCsv(allLogs.Items),
                    "excel" => ExportToExcel(allLogs.Items),
                    _ => throw new ArgumentException($"Unsupported export format: {format}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting logs in format: {Format}", format);
                return Array.Empty<byte>();
            }
        }

        #region Private Helper Methods

        private List<Domain.Entities.OW.OperationChangeLog> ApplyFilters(
            List<Domain.Entities.OW.OperationChangeLog> logs,
            long? onboardingId,
            long? stageId,
            List<OperationTypeEnum> operationTypes,
            DateTimeOffset? startDate,
            DateTimeOffset? endDate)
        {
            if (onboardingId.HasValue)
            {
                logs = logs.Where(x => x.OnboardingId == onboardingId.Value).ToList();
            }

            if (stageId.HasValue)
            {
                logs = logs.Where(x => x.StageId == stageId.Value).ToList();
            }

            if (operationTypes?.Any() == true)
            {
                var operationTypeNames = operationTypes.Select(t => t.ToString()).ToList();
                logs = logs.Where(x => operationTypeNames.Contains(x.OperationType)).ToList();
            }

            if (startDate.HasValue)
            {
                logs = logs.Where(x => x.OperationTime >= startDate.Value).ToList();
            }

            if (endDate.HasValue)
            {
                logs = logs.Where(x => x.OperationTime <= endDate.Value).ToList();
            }

            return logs;
        }

        private OperationChangeLogOutputDto MapToOutputDto(Domain.Entities.OW.OperationChangeLog log)
        {
            return _mapper.Map<OperationChangeLogOutputDto>(log);
        }

        private string GenerateAggregatedCacheKey(
            long? onboardingId,
            long? stageId,
            List<BusinessModuleEnum> businessModules,
            List<OperationTypeEnum> operationTypes,
            DateTimeOffset? startDate,
            DateTimeOffset? endDate,
            int pageIndex,
            int pageSize)
        {
            var keyParts = new List<string> { "aggregated" };

            if (onboardingId.HasValue)
                keyParts.Add($"onboarding_{onboardingId.Value}");

            if (stageId.HasValue)
                keyParts.Add($"stage_{stageId.Value}");

            if (businessModules?.Any() == true)
                keyParts.Add($"modules_{string.Join(",", businessModules.Select(m => m.ToString()))}");

            if (operationTypes?.Any() == true)
                keyParts.Add($"types_{string.Join(",", operationTypes.Select(t => t.ToString()))}");

            if (startDate.HasValue)
                keyParts.Add($"start_{startDate.Value:yyyyMMdd}");

            if (endDate.HasValue)
                keyParts.Add($"end_{endDate.Value:yyyyMMdd}");

            keyParts.Add($"page_{pageIndex}_{pageSize}");

            return string.Join(":", keyParts);
        }

        private byte[] ExportToJson(List<OperationChangeLogOutputDto> logs)
        {
            var json = JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true });
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        private byte[] ExportToCsv(List<OperationChangeLogOutputDto> logs)
        {
            // Simplified CSV export - in production, use a proper CSV library
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Id,OperationType,BusinessModule,BusinessId,OperationTime,OperatorName,OperationTitle");

            foreach (var log in logs)
            {
                csv.AppendLine($"{log.Id},{log.OperationType},{log.BusinessModule},{log.BusinessId},{log.OperationTime},{log.OperatorName},{log.OperationTitle}");
            }

            return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        }

        private byte[] ExportToExcel(List<OperationChangeLogOutputDto> logs)
        {
            // Placeholder for Excel export - in production, use EPPlus or similar library
            _logger.LogWarning("Excel export not implemented - falling back to CSV");
            return ExportToCsv(logs);
        }

        #endregion
    }
}