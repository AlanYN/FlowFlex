using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.OperationChangeLog;
using FlowFlex.Application.Contracts.IServices.OW.ChangeLog;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Services.OW.ChangeLog
{
    /// <summary>
    /// Base operation log service - provides common functionality for all specialized log services
    /// </summary>
    public abstract class BaseOperationLogService : IBaseOperationLogService
    {
        protected readonly IOperationChangeLogRepository _operationChangeLogRepository;
        protected readonly ILogger _logger;
        protected readonly UserContext _userContext;
        protected readonly IHttpContextAccessor _httpContextAccessor;
        protected readonly IMapper _mapper;
        protected readonly ILogCacheService _logCacheService;

        protected BaseOperationLogService(
            IOperationChangeLogRepository operationChangeLogRepository,
            ILogger logger,
            UserContext userContext,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ILogCacheService logCacheService)
        {
            _operationChangeLogRepository = operationChangeLogRepository;
            _logger = logger;
            _userContext = userContext;
            _httpContextAccessor = httpContextAccessor;
            _mapper = mapper;
            _logCacheService = logCacheService;
        }

        /// <summary>
        /// Core logging method that all services use
        /// </summary>
        public virtual async Task<bool> LogOperationAsync(
            OperationTypeEnum operationType,
            BusinessModuleEnum businessModule,
            long businessId,
            long? onboardingId,
            long? stageId,
            string operationTitle,
            string operationDescription,
            string operationSource = null,
            string beforeData = null,
            string afterData = null,
            string changedFields = null,
            string extendedData = null)
        {
            try
            {
                var operationLog = new OperationChangeLog
                {
                    OperationType = operationType.ToString(),
                    BusinessModule = businessModule.ToString(),
                    BusinessId = businessId,
                    OnboardingId = onboardingId,
                    StageId = stageId,
                    OperationStatus = OperationStatusEnum.Success.ToString(),
                    OperationTitle = operationTitle,
                    OperationDescription = operationDescription,
                    OperationSource = operationSource ?? GetOperationSource(),
                    BeforeData = beforeData,
                    AfterData = afterData,
                    ChangedFields = changedFields,
                    ExtendedData = extendedData,
                    OperatorId = GetOperatorId(),
                    OperatorName = GetOperatorDisplayName(),
                    OperationTime = DateTimeOffset.UtcNow,
                    IpAddress = GetClientIpAddress(),
                    UserAgent = GetUserAgent(),
                    TenantId = _userContext.TenantId,
                    AppCode = _userContext.AppCode
                };

                bool result = await _operationChangeLogRepository.InsertOperationLogAsync(operationLog);

                if (result)
                {
                    // Invalidate relevant caches
                    await InvalidateRelevantCachesAsync(businessModule.ToString(), businessId, onboardingId, stageId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log {OperationType} operation for {BusinessModule} {BusinessId}",
                    operationType, businessModule, businessId);
                return false;
            }
        }

        /// <summary>
        /// Get operation logs with caching support
        /// </summary>
        public virtual async Task<PagedResult<OperationChangeLogOutputDto>> GetOperationLogsAsync(
            long? onboardingId = null,
            long? stageId = null,
            OperationTypeEnum? operationType = null,
            int pageIndex = 1,
            int pageSize = 20)
        {
            try
            {
                // Generate cache key
                string cacheKey = _logCacheService.GenerateLogsCacheKey(
                    GetBusinessModuleName(),
                    null,
                    onboardingId,
                    stageId,
                    operationType?.ToString(),
                    pageIndex,
                    pageSize);

                // Try to get from cache first
                var cachedResult = await _logCacheService.GetCachedLogsAsync(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogDebug("Retrieved operation logs from cache for key: {CacheKey}", cacheKey);
                    return cachedResult;
                }

                // If not in cache, get from database
                var result = await GetOperationLogsFromDatabaseAsync(onboardingId, stageId, operationType, pageIndex, pageSize);

                // Cache the result
                await _logCacheService.SetCachedLogsAsync(cacheKey, result, TimeSpan.FromMinutes(15));

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting operation logs for {BusinessModule}", GetBusinessModuleName());
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

        /// <summary>
        /// Get operation statistics with caching support
        /// </summary>
        public virtual async Task<Dictionary<string, int>> GetOperationStatisticsAsync(
            long? onboardingId = null,
            long? stageId = null)
        {
            try
            {
                // Generate cache key
                string cacheKey = _logCacheService.GenerateStatisticsCacheKey(
                    GetBusinessModuleName(),
                    null,
                    onboardingId,
                    stageId);

                // Try to get from cache first
                var cachedResult = await _logCacheService.GetCachedStatisticsAsync(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogDebug("Retrieved operation statistics from cache for key: {CacheKey}", cacheKey);
                    return cachedResult;
                }

                // If not in cache, get from database
                var result = await _operationChangeLogRepository.GetOperationStatisticsAsync(onboardingId, stageId);

                // Cache the result
                await _logCacheService.SetCachedStatisticsAsync(cacheKey, result, TimeSpan.FromMinutes(30));

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting operation statistics for {BusinessModule}", GetBusinessModuleName());
                return new Dictionary<string, int>();
            }
        }

        #region Protected Helper Methods

        /// <summary>
        /// Get operation logs from database - to be implemented by derived classes
        /// </summary>
        protected abstract Task<PagedResult<OperationChangeLogOutputDto>> GetOperationLogsFromDatabaseAsync(
            long? onboardingId,
            long? stageId,
            OperationTypeEnum? operationType,
            int pageIndex,
            int pageSize);

        /// <summary>
        /// Get business module name - to be implemented by derived classes
        /// </summary>
        protected abstract string GetBusinessModuleName();

        /// <summary>
        /// Map OperationChangeLog entity to DTO
        /// </summary>
        protected virtual OperationChangeLogOutputDto MapToOutputDto(Domain.Entities.OW.OperationChangeLog log)
        {
            return _mapper.Map<OperationChangeLogOutputDto>(log);
        }

        /// <summary>
        /// Get operator ID from current user context
        /// </summary>
        protected virtual long GetOperatorId()
        {
            return long.TryParse(_userContext.UserId, out var userId) ? userId : 0L;
        }

        /// <summary>
        /// Get operator display name
        /// </summary>
        protected virtual string GetOperatorDisplayName()
        {
            return _userContext.UserName ?? "System";
        }

        /// <summary>
        /// Get operation source
        /// </summary>
        protected virtual string GetOperationSource()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var request = httpContext.Request;
                return $"{request.Method} {request.Path}";
            }
            return "System";
        }

        /// <summary>
        /// Get client IP address
        /// </summary>
        protected virtual string GetClientIpAddress()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var request = httpContext.Request;
                return request.Headers.ContainsKey("X-Forwarded-For") 
                    ? request.Headers["X-Forwarded-For"].ToString()
                    : httpContext.Connection.RemoteIpAddress?.ToString();
            }
            return null;
        }

        /// <summary>
        /// Get user agent
        /// </summary>
        protected virtual string GetUserAgent()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.Request.Headers["User-Agent"].ToString();
        }

        /// <summary>
        /// Invalidate relevant caches
        /// </summary>
        protected virtual async Task InvalidateRelevantCachesAsync(
            string businessModule,
            long businessId,
            long? onboardingId,
            long? stageId)
        {
            try
            {
                await _logCacheService.InvalidateCacheForBusinessAsync(businessModule, businessId);
                
                if (onboardingId.HasValue)
                {
                    await _logCacheService.InvalidateCacheForOnboardingAsync(onboardingId.Value);
                }
                
                if (stageId.HasValue)
                {
                    await _logCacheService.InvalidateCacheForStageAsync(stageId.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate caches for {BusinessModule} {BusinessId}", 
                    businessModule, businessId);
            }
        }

        /// <summary>
        /// Check if there's meaningful change between before and after data
        /// </summary>
        protected virtual bool HasMeaningfulValueChange(string beforeData, string afterData)
        {
            if (string.IsNullOrEmpty(beforeData) && string.IsNullOrEmpty(afterData))
                return false;

            if (string.IsNullOrEmpty(beforeData) || string.IsNullOrEmpty(afterData))
                return true;

            try
            {
                var beforeJson = JsonSerializer.Deserialize<Dictionary<string, object>>(beforeData);
                var afterJson = JsonSerializer.Deserialize<Dictionary<string, object>>(afterData);

                return !beforeJson.SequenceEqual(afterJson);
            }
            catch
            {
                return beforeData != afterData;
            }
        }

        /// <summary>
        /// Get changed fields between before and after data
        /// </summary>
        protected virtual List<string> GetChangedFields(Dictionary<string, object> beforeData, Dictionary<string, object> afterData)
        {
            var changedFields = new List<string>();

            try
            {
                var allKeys = beforeData.Keys.Union(afterData.Keys);

                foreach (var key in allKeys)
                {
                    var beforeValue = beforeData.ContainsKey(key) ? beforeData[key] : null;
                    var afterValue = afterData.ContainsKey(key) ? afterData[key] : null;

                    if (!Equals(beforeValue, afterValue))
                    {
                        changedFields.Add(key);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to detect changed fields");
            }

            return changedFields;
        }

        /// <summary>
        /// Format file size to human readable format
        /// </summary>
        protected virtual string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 Bytes";

            string[] sizes = { "Bytes", "KB", "MB", "GB", "TB" };
            int i = (int)Math.Floor(Math.Log(bytes) / Math.Log(1024));
            return Math.Round(bytes / Math.Pow(1024, i), 2) + " " + sizes[i];
        }

        /// <summary>
        /// Get relative time display
        /// </summary>
        protected virtual string GetRelativeTimeDisplay(DateTimeOffset dateTime)
        {
            var now = DateTimeOffset.UtcNow;
            var timeSpan = now - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minutes ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hours ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} days ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)} weeks ago";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} months ago";

            return $"{(int)(timeSpan.TotalDays / 365)} years ago";
        }

        /// <summary>
        /// Normalize value for comparison
        /// </summary>
        protected virtual string NormalizeValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            // Remove leading and trailing spaces
            value = value.Trim();

            // If value is surrounded by double quotes, remove quotes
            if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
            {
                value = value.Substring(1, value.Length - 2);
            }

            // Try to parse as number, if it's a number then normalize format
            if (decimal.TryParse(value, out decimal decimalValue))
            {
                // For numbers, use standard format (remove unnecessary decimal places)
                return decimalValue.ToString("0.##");
            }

            // For boolean values, normalize to lowercase
            if (bool.TryParse(value, out bool boolValue))
            {
                return boolValue.ToString().ToLowerInvariant();
            }

            return value;
        }

        /// <summary>
        /// Enhanced meaningful value change check with normalization
        /// </summary>
        protected virtual bool HasMeaningfulValueChangeEnhanced(string beforeData, string afterData)
        {
            // If both values are empty or null, there is no change
            if (string.IsNullOrEmpty(beforeData) && string.IsNullOrEmpty(afterData))
                return false;

            // If one is empty and the other is not, there is a change
            if (string.IsNullOrEmpty(beforeData) || string.IsNullOrEmpty(afterData))
                return true;

            // Normalize values for comparison
            string normalizedBefore = NormalizeValue(beforeData);
            string normalizedAfter = NormalizeValue(afterData);

            return !string.Equals(normalizedBefore, normalizedAfter, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Get changed fields from JSON strings
        /// </summary>
        protected virtual List<string> GetChangedFieldsFromJson(string beforeData, string afterData)
        {
            var changedFields = new List<string>();

            if (string.IsNullOrEmpty(beforeData) && string.IsNullOrEmpty(afterData))
                return changedFields;

            try
            {
                var beforeJson = string.IsNullOrEmpty(beforeData) 
                    ? new Dictionary<string, object>()
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(beforeData);
                    
                var afterJson = string.IsNullOrEmpty(afterData)
                    ? new Dictionary<string, object>()
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(afterData);

                return GetChangedFields(beforeJson, afterJson);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse JSON data for change detection");
                return changedFields;
            }
        }

        /// <summary>
        /// Get detailed operation source information
        /// </summary>
        protected virtual string GetDetailedOperationSource(HttpContext context)
        {
            if (context == null) return "System";

            string path = context.Request.Path.Value ?? string.Empty;

            if (path.Contains("/customer-portal/", StringComparison.OrdinalIgnoreCase))
            {
                return "CustomerPortal";
            }
            else if (path.Contains("/admin/", StringComparison.OrdinalIgnoreCase))
            {
                return "AdminPortal";
            }
            else if (path.Contains("/api/", StringComparison.OrdinalIgnoreCase))
            {
                return "WebApi";
            }

            return "WebApi";
        }

        /// <summary>
        /// Get enhanced client IP address with proper header precedence
        /// </summary>
        protected virtual string GetEnhancedClientIpAddress(HttpContext context)
        {
            if (context == null) return string.Empty;

            string ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            }
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = context.Connection.RemoteIpAddress?.ToString();
            }

            return ipAddress ?? string.Empty;
        }

        /// <summary>
        /// Generic method to build operation log entity
        /// </summary>
        protected virtual Domain.Entities.OW.OperationChangeLog BuildOperationLogEntity(
            OperationTypeEnum operationType,
            BusinessModuleEnum businessModule,
            long businessId,
            long? onboardingId,
            long? stageId,
            string operationTitle,
            string operationDescription,
            string beforeData = null,
            string afterData = null,
            List<string> changedFields = null,
            string extendedData = null,
            OperationStatusEnum operationStatus = OperationStatusEnum.Success,
            string errorMessage = null)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            
            var operationLog = new Domain.Entities.OW.OperationChangeLog
            {
                OperationType = operationType.ToString(),
                BusinessModule = businessModule.ToString(),
                BusinessId = businessId,
                OnboardingId = onboardingId,
                StageId = stageId,
                OperationTitle = operationTitle,
                OperationDescription = operationDescription,
                BeforeData = !string.IsNullOrEmpty(beforeData) ? beforeData : null,
                AfterData = !string.IsNullOrEmpty(afterData) ? afterData : null,
                ChangedFields = changedFields != null ? JsonSerializer.Serialize(changedFields) : null,
                OperatorId = GetOperatorId(),
                OperatorName = GetOperatorDisplayName(),
                OperationTime = DateTimeOffset.UtcNow,
                IpAddress = GetEnhancedClientIpAddress(httpContext),
                UserAgent = httpContext?.Request.Headers["User-Agent"].FirstOrDefault() ?? string.Empty,
                OperationSource = GetDetailedOperationSource(httpContext),
                ExtendedData = !string.IsNullOrEmpty(extendedData) ? extendedData : null,
                OperationStatus = operationStatus.ToString(),
                ErrorMessage = errorMessage
            };

            // Initialize base entity fields
            operationLog.InitCreateInfo(_userContext);

            return operationLog;
        }

        #endregion
    }
}