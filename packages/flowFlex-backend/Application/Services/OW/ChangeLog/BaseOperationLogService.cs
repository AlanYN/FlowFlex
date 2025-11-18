using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.OperationChangeLog;
using FlowFlex.Application.Contracts.IServices.OW;
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
        protected readonly IUserService _userService;
        protected readonly IOperatorContextService _operatorContextService;

        protected BaseOperationLogService(
            IOperationChangeLogRepository operationChangeLogRepository,
            ILogger logger,
            UserContext userContext,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ILogCacheService logCacheService,
            IUserService userService,
            IOperatorContextService operatorContextService)
        {
            _operationChangeLogRepository = operationChangeLogRepository;
            _logger = logger;
            _userContext = userContext;
            _httpContextAccessor = httpContextAccessor;
            _mapper = mapper;
            _logCacheService = logCacheService;
            _userService = userService;
            _operatorContextService = operatorContextService;
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
            return await LogOperationWithUserContextAsync(
                operationType, businessModule, businessId, onboardingId, stageId,
                operationTitle, operationDescription, operationSource, beforeData,
                afterData, changedFields, extendedData, null, null, null);
        }

        /// <summary>
        /// Core logging method with custom user context support (for async scenarios)
        /// </summary>
        public virtual async Task<bool> LogOperationWithUserContextAsync(
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
            string extendedData = null,
            string customOperatorName = null,
            long? customOperatorId = null,
            string customTenantId = null)
        {
            try
            {
                var currentUtcTime = DateTimeOffset.UtcNow;
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
                    OperatorId = customOperatorId ?? GetOperatorId(),
                    OperatorName = customOperatorName ?? GetOperatorDisplayName(),
                    OperationTime = currentUtcTime, // Store as UTC in database
                    IpAddress = GetClientIpAddress(),
                    UserAgent = GetUserAgent(),
                    TenantId = customTenantId ?? _userContext.TenantId,
                    AppCode = _userContext.AppCode
                };

                // Initialize unique snowflake ID
                operationLog.InitNewId();

                // Initialize base entity fields with custom user context if provided
                if (!string.IsNullOrEmpty(customOperatorName) && customOperatorId.HasValue)
                {
                    // Use custom user context for async scenarios
                    operationLog.CreateBy = customOperatorName;
                    operationLog.ModifyBy = customOperatorName;
                    operationLog.CreateUserId = customOperatorId.Value;
                    operationLog.ModifyUserId = customOperatorId.Value;
                    operationLog.TenantId = customTenantId ?? "DEFAULT";
                    operationLog.AppCode = _userContext?.AppCode ?? "DEFAULT";
                    operationLog.CreateDate = currentUtcTime;
                    operationLog.ModifyDate = currentUtcTime;
                    operationLog.IsValid = true;
                }
                else
                {
                    // Use default user context initialization
                    operationLog.InitCreateInfo(_userContext);

                    // Ensure correct operator information is preserved after InitCreateInfo
                    // because InitCreateInfo might override with default values
                    operationLog.CreateBy = GetOperatorDisplayName();
                    operationLog.ModifyBy = GetOperatorDisplayName();
                    operationLog.CreateUserId = GetOperatorId();
                    operationLog.ModifyUserId = GetOperatorId();
                }

                _logger.LogDebug("Attempting to insert operation log with ID {LogId} for {BusinessModule} {BusinessId} at {OperationTime}",
                    operationLog.Id, businessModule, businessId, FormatToUSTime(currentUtcTime));

                bool result = await InsertWithRetryAsync(operationLog);

                if (result)
                {
                    // Cache disabled - no need to invalidate
                    // await InvalidateRelevantCachesAsync(businessModule.ToString(), businessId, onboardingId, stageId);
                    _logger.LogDebug("Operation log inserted successfully without cache invalidation");
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

                // Skip cache for reliability - directly get from database
                // var cachedResult = await _logCacheService.GetCachedLogsAsync(cacheKey);
                // if (cachedResult != null)
                // {
                //     _logger.LogDebug("Retrieved operation logs from cache for key: {CacheKey}", cacheKey);
                //     return cachedResult;
                // }

                // Get directly from database
                var result = await GetOperationLogsFromDatabaseAsync(onboardingId, stageId, operationType, pageIndex, pageSize);

                // Skip caching - cache disabled for reliability
                // await _logCacheService.SetCachedLogsAsync(cacheKey, result, TimeSpan.FromMinutes(15));

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
        /// Get operator display name (FirstName + LastName > UserName > Email)
        /// </summary>
        protected virtual string GetOperatorDisplayName()
        {
            return _operatorContextService.GetOperatorDisplayName();
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

                // Most importantly: invalidate the specific onboarding+stage combination cache
                // This is the cache that was causing the issue in the logs
                if (onboardingId.HasValue && stageId.HasValue)
                {
                    await _logCacheService.InvalidateCacheForOnboardingAndStageAsync(onboardingId.Value, stageId.Value);
                    _logger.LogDebug("Invalidated specific cache for onboarding {OnboardingId} + stage {StageId}",
                        onboardingId.Value, stageId.Value);
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

                    if (!CompareValues(beforeValue, afterValue))
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
        /// Enhanced value comparison that handles JSON objects, arrays, and primitives
        /// </summary>
        protected virtual bool CompareValues(object beforeValue, object afterValue)
        {
            // Handle null cases
            if (beforeValue == null && afterValue == null) return true;
            if (beforeValue == null || afterValue == null) return false;

            // Convert to strings for comparison
            string beforeStr = beforeValue.ToString();
            string afterStr = afterValue.ToString();

            // For JSON-like strings, use normalized comparison
            if (IsJsonString(beforeStr) && IsJsonString(afterStr))
            {
                return string.Equals(
                    NormalizeValue(beforeStr),
                    NormalizeValue(afterStr),
                    StringComparison.OrdinalIgnoreCase
                );
            }

            // For non-JSON values, use normalized comparison
            return string.Equals(
                NormalizeValue(beforeStr),
                NormalizeValue(afterStr),
                StringComparison.OrdinalIgnoreCase
            );
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
        /// Get relative time display in US time format
        /// </summary>
        protected virtual string GetRelativeTimeDisplay(DateTimeOffset dateTime)
        {
            // Convert UTC time to US Eastern Time (ET)
            var easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var easternTime = TimeZoneInfo.ConvertTime(dateTime, easternZone);
            var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, easternZone);
            var timeSpan = now - easternTime;

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
        /// Format DateTimeOffset to US time format (MM/dd/yyyy hh:mm:ss tt ET)
        /// </summary>
        protected virtual string FormatToUSTime(DateTimeOffset dateTime)
        {
            try
            {
                // Convert UTC time to US Eastern Time (ET)
                var easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                var easternTime = TimeZoneInfo.ConvertTime(dateTime, easternZone);

                // Format as US time: MM/dd/yyyy hh:mm:ss tt ET
                return easternTime.ToString("MM/dd/yyyy hh:mm:ss tt") + " ET";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert to US Eastern Time, using UTC");
                // Fallback to UTC format if timezone conversion fails
                return dateTime.ToString("MM/dd/yyyy hh:mm:ss tt") + " UTC";
            }
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

            // Try to normalize JSON content (for JSON fields like StructureJson, ComponentsJson, ConfigJson)
            if (IsJsonString(value))
            {
                try
                {
                    // First normalize the JSON string (handle double-escaped values)
                    string normalizedJson = NormalizeJsonString(value);

                    // Parse and re-serialize to normalize formatting
                    var jsonElement = JsonSerializer.Deserialize<JsonElement>(normalizedJson);
                    return JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        PropertyNamingPolicy = null, // Keep original property names
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });
                }
                catch (JsonException)
                {
                    // If JSON parsing fails, fall back to original value
                }
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
        /// Check if a string is a valid JSON
        /// </summary>
        protected virtual bool IsJsonString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.Trim();

            // Check for direct JSON objects/arrays
            if ((value.StartsWith("{") && value.EndsWith("}")) ||
                (value.StartsWith("[") && value.EndsWith("]")))
            {
                return true;
            }

            // Check for JSON strings that have been serialized as string values (double-escaped)
            if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length > 2)
            {
                try
                {
                    // Try to deserialize the string value to get the inner content
                    var innerValue = JsonSerializer.Deserialize<string>(value);
                    if (!string.IsNullOrEmpty(innerValue))
                    {
                        innerValue = innerValue.Trim();
                        return (innerValue.StartsWith("{") && innerValue.EndsWith("}")) ||
                               (innerValue.StartsWith("[") && innerValue.EndsWith("]"));
                    }
                }
                catch
                {
                    // If deserialization fails, it's not a JSON string value
                }
            }

            return false;
        }

        /// <summary>
        /// Normalize JSON string by handling double-escaped JSON values
        /// </summary>
        protected virtual string NormalizeJsonString(string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
                return jsonString;

            jsonString = jsonString.Trim();

            // If it's a double-escaped JSON string (serialized as string value), deserialize it
            if (jsonString.StartsWith("\"") && jsonString.EndsWith("\"") && jsonString.Length > 2)
            {
                try
                {
                    var innerValue = JsonSerializer.Deserialize<string>(jsonString);
                    if (!string.IsNullOrEmpty(innerValue))
                    {
                        innerValue = innerValue.Trim();
                        if ((innerValue.StartsWith("{") && innerValue.EndsWith("}")) ||
                            (innerValue.StartsWith("[") && innerValue.EndsWith("]")))
                        {
                            return innerValue;
                        }
                    }
                }
                catch
                {
                    // If deserialization fails, return original string
                }
            }

            return jsonString;
        }

        /// <summary>
        /// Auto-detect changed fields from before and after JSON data
        /// This is a helper method that business services can use instead of manual field comparison
        /// </summary>
        protected virtual List<string> AutoDetectChangedFields(string beforeData, string afterData)
        {
            if (string.IsNullOrEmpty(beforeData) && string.IsNullOrEmpty(afterData))
                return new List<string>();

            try
            {
                return GetChangedFieldsFromJson(beforeData, afterData);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to auto-detect changed fields, falling back to manual detection");
                return new List<string>();
            }
        }

        /// <summary>
        /// Insert operation log with retry mechanism for primary key conflicts
        /// </summary>
        private async Task<bool> InsertWithRetryAsync(OperationChangeLog operationLog, int maxRetries = 3)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    bool result = await _operationChangeLogRepository.InsertOperationLogAsync(operationLog);
                    if (result)
                    {
                        _logger.LogDebug("Successfully inserted operation log with ID {LogId} on attempt {Attempt}",
                            operationLog.Id, attempt);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    // Check if it's a primary key violation
                    if (ex.Message.Contains("duplicate key value violates unique constraint") &&
                        ex.Message.Contains("_pkey"))
                    {
                        _logger.LogWarning("Primary key conflict on attempt {Attempt} for operation log ID {LogId}. Regenerating ID...",
                            attempt, operationLog.Id);

                        if (attempt < maxRetries)
                        {
                            // Regenerate a new ID and try again
                            operationLog.InitNewId();

                            // Add a small delay to avoid rapid retries
                            await Task.Delay(10 * attempt);
                            continue;
                        }
                        else
                        {
                            _logger.LogError(ex, "Failed to insert operation log after {MaxRetries} attempts due to primary key conflicts", maxRetries);
                        }
                    }
                    else
                    {
                        // For other types of exceptions, don't retry
                        _logger.LogError(ex, "Failed to insert operation log with ID {LogId} due to non-retry-able error", operationLog.Id);
                    }

                    throw;
                }
            }

            return false;
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
            var currentUtcTime = DateTimeOffset.UtcNow;

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
                OperationTime = currentUtcTime, // Store as UTC in database
                IpAddress = GetEnhancedClientIpAddress(httpContext),
                UserAgent = httpContext?.Request.Headers["User-Agent"].FirstOrDefault() ?? string.Empty,
                OperationSource = GetDetailedOperationSource(httpContext),
                ExtendedData = !string.IsNullOrEmpty(extendedData) ? extendedData : null,
                OperationStatus = operationStatus.ToString(),
                ErrorMessage = errorMessage
            };

            // Initialize base entity fields
            operationLog.InitCreateInfo(_userContext);

            // Ensure correct operator information is preserved after InitCreateInfo
            // because InitCreateInfo might override with default values
            operationLog.CreateBy = GetOperatorDisplayName();
            operationLog.ModifyBy = GetOperatorDisplayName();
            operationLog.CreateUserId = GetOperatorId();
            operationLog.ModifyUserId = GetOperatorId();

            return operationLog;
        }

        /// <summary>
        /// Build enhanced operation description with specific change values (async version)
        /// </summary>
        protected virtual async Task<string> BuildEnhancedOperationDescriptionAsync(
            BusinessModuleEnum businessModule,
            string entityName,
            string operationAction,
            string beforeData = null,
            string afterData = null,
            List<string> changedFields = null,
            long? relatedEntityId = null,
            string relatedEntityType = null,
            string reason = null)
        {
            var description = $"{businessModule} '{entityName}' has been {operationAction.ToLower()} by {GetOperatorDisplayName()}";

            // Add specific change details instead of just field names
            if (!string.IsNullOrEmpty(beforeData) && !string.IsNullOrEmpty(afterData) && changedFields?.Any() == true)
            {
                var changeDetails = await GetChangeDetailsAsync(beforeData, afterData, changedFields, businessModule);
                if (!string.IsNullOrEmpty(changeDetails))
                {
                    description += $". {changeDetails}";
                }
            }
            else if (changedFields?.Any() == true)
            {
                // Fallback to field names if no before/after data
                // For Checklist module, only show Name, Description, and Team
                var fieldsToShow = changedFields;
                if (businessModule == BusinessModuleEnum.Checklist)
                {
                    fieldsToShow = changedFields.Where(f => 
                        f.Equals("Name", StringComparison.OrdinalIgnoreCase) ||
                        f.Equals("Description", StringComparison.OrdinalIgnoreCase) ||
                        f.Equals("Team", StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }
                
                if (fieldsToShow.Any())
                {
                    description += $". Changed fields: {string.Join(", ", fieldsToShow)}";
                }
            }
            // For create operations, show important fields from afterData
            else if (string.IsNullOrEmpty(beforeData) && !string.IsNullOrEmpty(afterData) && 
                     operationAction.Equals("Created", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var afterJson = JsonSerializer.Deserialize<JsonElement>(afterData);
                    var details = new List<string>();

                    // Extract ViewPermissionMode and Teams for Stage (combine them together)
                    if (businessModule == BusinessModuleEnum.Stage)
                    {
                        string viewPermissionMode = null;
                        if (afterJson.TryGetProperty("ViewPermissionMode", out var viewPermissionModeElement) ||
                            afterJson.TryGetProperty("viewPermissionMode", out viewPermissionModeElement))
                        {
                            viewPermissionMode = GetViewPermissionModeDisplayName(viewPermissionModeElement);
                        }

                        string viewTeamsSummary = null;
                        if (afterJson.TryGetProperty("ViewTeams", out var viewTeamsElement) ||
                            afterJson.TryGetProperty("viewTeams", out viewTeamsElement))
                        {
                            viewTeamsSummary = await GetTeamsSummaryAsync(viewTeamsElement);
                        }

                        string operateTeamsSummary = null;
                        if (afterJson.TryGetProperty("OperateTeams", out var operateTeamsElement) ||
                            afterJson.TryGetProperty("operateTeams", out operateTeamsElement))
                        {
                            operateTeamsSummary = await GetTeamsSummaryAsync(operateTeamsElement);
                        }

                        // Combine view permission mode and teams information
                        var permissionInfo = new List<string>();
                        if (!string.IsNullOrEmpty(viewPermissionMode))
                        {
                            var permissionParts = new List<string> { viewPermissionMode };
                            
                            if (!string.IsNullOrEmpty(viewTeamsSummary))
                            {
                                permissionParts.Add($"view teams: {viewTeamsSummary}");
                            }
                            
                            if (!string.IsNullOrEmpty(operateTeamsSummary))
                            {
                                permissionParts.Add($"operate teams: {operateTeamsSummary}");
                            }
                            
                            details.Add(string.Join("; ", permissionParts));
                        }
                        else
                        {
                            // If no view permission mode, still show teams if they exist
                            if (!string.IsNullOrEmpty(viewTeamsSummary))
                            {
                                details.Add($"view teams: {viewTeamsSummary}");
                            }
                            if (!string.IsNullOrEmpty(operateTeamsSummary))
                            {
                                details.Add($"operate teams: {operateTeamsSummary}");
                            }
                        }

                        // Extract UseSameTeamForOperate
                        if (afterJson.TryGetProperty("UseSameTeamForOperate", out var useSameTeamElement) ||
                            afterJson.TryGetProperty("useSameTeamForOperate", out useSameTeamElement))
                        {
                            var useSameTeam = useSameTeamElement.ValueKind == JsonValueKind.True;
                            if (useSameTeam)
                            {
                                details.Add("use same team for operate: Yes");
                            }
                        }
                    }
                    else if (businessModule == BusinessModuleEnum.Workflow)
                    {
                        // Extract Description for Workflow (if not empty)
                        if (afterJson.TryGetProperty("Description", out var descriptionElement) ||
                            afterJson.TryGetProperty("description", out descriptionElement))
                        {
                            var workflowDescription = descriptionElement.GetString();
                            if (!string.IsNullOrEmpty(workflowDescription) && workflowDescription.Length <= 100)
                            {
                                details.Add($"description: '{workflowDescription}'");
                            }
                        }

                        // Extract Status for Workflow
                        if (afterJson.TryGetProperty("Status", out var statusElement) ||
                            afterJson.TryGetProperty("status", out statusElement))
                        {
                            var status = statusElement.GetString();
                            if (!string.IsNullOrEmpty(status))
                            {
                                details.Add($"status: {status}");
                            }
                        }

                        // Extract IsDefault for Workflow
                        if (afterJson.TryGetProperty("IsDefault", out var isDefaultElement) ||
                            afterJson.TryGetProperty("isDefault", out isDefaultElement))
                        {
                            var isDefault = isDefaultElement.ValueKind == JsonValueKind.True;
                            details.Add($"set as default workflow: {(isDefault ? "Default" : "Not Default")}");
                        }

                        // Extract ViewPermissionMode and Teams for Workflow
                        string viewPermissionMode = null;
                        if (afterJson.TryGetProperty("ViewPermissionMode", out var viewPermissionModeElement) ||
                            afterJson.TryGetProperty("viewPermissionMode", out viewPermissionModeElement))
                        {
                            viewPermissionMode = GetViewPermissionModeDisplayName(viewPermissionModeElement);
                        }

                        string viewTeamsSummary = null;
                        if (afterJson.TryGetProperty("ViewTeams", out var viewTeamsElement) ||
                            afterJson.TryGetProperty("viewTeams", out viewTeamsElement))
                        {
                            viewTeamsSummary = await GetTeamsSummaryAsync(viewTeamsElement);
                        }

                        string operateTeamsSummary = null;
                        if (afterJson.TryGetProperty("OperateTeams", out var operateTeamsElement) ||
                            afterJson.TryGetProperty("operateTeams", out operateTeamsElement))
                        {
                            operateTeamsSummary = await GetTeamsSummaryAsync(operateTeamsElement);
                        }

                        // Combine view permission mode and teams information
                        var permissionInfo = new List<string>();
                        if (!string.IsNullOrEmpty(viewPermissionMode))
                        {
                            var permissionParts = new List<string> { viewPermissionMode };
                            
                            if (!string.IsNullOrEmpty(viewTeamsSummary))
                            {
                                permissionParts.Add($"view teams: {viewTeamsSummary}");
                            }
                            
                            if (!string.IsNullOrEmpty(operateTeamsSummary))
                            {
                                permissionParts.Add($"operate teams: {operateTeamsSummary}");
                            }
                            
                            details.Add(string.Join("; ", permissionParts));
                        }
                        else
                        {
                            // If no view permission mode, still show teams if they exist
                            if (!string.IsNullOrEmpty(viewTeamsSummary))
                            {
                                details.Add($"view teams: {viewTeamsSummary}");
                            }
                            if (!string.IsNullOrEmpty(operateTeamsSummary))
                            {
                                details.Add($"operate teams: {operateTeamsSummary}");
                            }
                        }

                        // Extract UseSameTeamForOperate
                        if (afterJson.TryGetProperty("UseSameTeamForOperate", out var useSameTeamElement) ||
                            afterJson.TryGetProperty("useSameTeamForOperate", out useSameTeamElement))
                        {
                            var useSameTeam = useSameTeamElement.ValueKind == JsonValueKind.True;
                            if (useSameTeam)
                            {
                                details.Add("use same team for operate: Yes");
                            }
                        }
                    }
                    else if (businessModule == BusinessModuleEnum.Checklist)
                    {
                        // Extract Description for Checklist (if not empty)
                        if (afterJson.TryGetProperty("Description", out var descriptionElement) ||
                            afterJson.TryGetProperty("description", out descriptionElement))
                        {
                            var checklistDescription = descriptionElement.GetString();
                            if (!string.IsNullOrEmpty(checklistDescription) && checklistDescription.Length <= 100)
                            {
                                details.Add($"description: '{checklistDescription}'");
                            }
                        }

                        // Extract Team for Checklist - try to get team name
                        if (afterJson.TryGetProperty("Team", out var teamElement) ||
                            afterJson.TryGetProperty("team", out teamElement))
                        {
                            string teamValue = null;
                            if (teamElement.ValueKind == JsonValueKind.String)
                            {
                                teamValue = teamElement.GetString();
                            }
                            else if (teamElement.ValueKind == JsonValueKind.Number)
                            {
                                teamValue = teamElement.GetInt64().ToString();
                            }

                            if (!string.IsNullOrEmpty(teamValue))
                            {
                                // Try to get team name from UserService
                                string teamDisplayName = teamValue;
                                try
                                {
                                    if (_userService != null && !teamValue.Equals("Other", StringComparison.OrdinalIgnoreCase))
                                    {
                                        var tenantId = _userContext?.TenantId ?? "999";
                                        var teamNameMap = await _userService.GetTeamNamesByIdsAsync(
                                            new List<string> { teamValue }, tenantId);
                                        if (teamNameMap != null && teamNameMap.TryGetValue(teamValue, out var teamName) && 
                                            !string.IsNullOrEmpty(teamName))
                                        {
                                            teamDisplayName = teamName;
                                        }
                                    }
                                    else if (teamValue.Equals("Other", StringComparison.OrdinalIgnoreCase))
                                    {
                                        teamDisplayName = "Other";
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogDebug(ex, "Failed to get team name for team ID {TeamId}, using ID as fallback", teamValue);
                                }

                                details.Add($"team: {teamDisplayName}");
                            }
                        }
                    }
                    else if (businessModule == BusinessModuleEnum.Task)
                    {
                        // Extract Description for Task (if not empty)
                        if (afterJson.TryGetProperty("Description", out var descriptionElement) ||
                            afterJson.TryGetProperty("description", out descriptionElement))
                        {
                            var taskDescription = descriptionElement.GetString();
                            if (!string.IsNullOrEmpty(taskDescription) && taskDescription.Length <= 100)
                            {
                                details.Add($"description: '{taskDescription}'");
                            }
                        }

                        // Extract AssigneeName for Task
                        if (afterJson.TryGetProperty("AssigneeName", out var assigneeNameElement) ||
                            afterJson.TryGetProperty("assigneeName", out assigneeNameElement))
                        {
                            var assigneeName = assigneeNameElement.GetString();
                            if (!string.IsNullOrEmpty(assigneeName))
                            {
                                details.Add($"assignee: {assigneeName}");
                            }
                        }
                    }
                    else if (businessModule == BusinessModuleEnum.Action)
                    {
                        // Extract Description for Action (if not empty)
                        if (afterJson.TryGetProperty("Description", out var descriptionElement) ||
                            afterJson.TryGetProperty("description", out descriptionElement))
                        {
                            var actionDescription = descriptionElement.GetString();
                            if (!string.IsNullOrEmpty(actionDescription) && actionDescription.Length <= 100)
                            {
                                details.Add($"description: '{actionDescription}'");
                            }
                        }

                        // Extract ActionType for Action
                        if (afterJson.TryGetProperty("ActionType", out var actionTypeElement) ||
                            afterJson.TryGetProperty("actionType", out actionTypeElement))
                        {
                            var actionType = actionTypeElement.GetString();
                            if (!string.IsNullOrEmpty(actionType))
                            {
                                details.Add($"type: {actionType}");
                            }
                        }

                        // Extract SourceCode for Python actions
                        if (afterJson.TryGetProperty("SourceCode", out var sourceCodeElement) ||
                            afterJson.TryGetProperty("sourceCode", out sourceCodeElement))
                        {
                            var sourceCode = sourceCodeElement.GetString();
                            if (!string.IsNullOrEmpty(sourceCode))
                            {
                                // Clean up the code by replacing newlines with spaces and trimming
                                var cleanedCode = sourceCode.Replace("\r\n", " ").Replace("\n", " ").Replace("  ", " ").Trim();
                                
                                // Limit the length to avoid overly long descriptions (keep reasonable length)
                                if (cleanedCode.Length > 200)
                                {
                                    details.Add($"Python: {cleanedCode.Substring(0, 197)}...");
                                }
                                else
                                {
                                    details.Add($"Python: {cleanedCode}");
                                }
                            }
                        }
                    }

                    // Extract VisibleInPortal for Stage (Available in Customer Portal)
                    if (businessModule == BusinessModuleEnum.Stage &&
                        (afterJson.TryGetProperty("VisibleInPortal", out var visibleInPortalElement) ||
                         afterJson.TryGetProperty("visibleInPortal", out visibleInPortalElement)))
                    {
                        var visibleInPortal = visibleInPortalElement.ValueKind == JsonValueKind.True;
                        details.Add($"Available in Customer Portal: {(visibleInPortal ? "Yes" : "No")}");
                    }

                    // Extract DefaultAssignee for Stage
                    if (businessModule == BusinessModuleEnum.Stage &&
                        (afterJson.TryGetProperty("DefaultAssignee", out var defaultAssigneeElement) ||
                         afterJson.TryGetProperty("defaultAssignee", out defaultAssigneeElement)))
                    {
                        var defaultAssigneeSummary = GetDefaultAssigneeSummary(defaultAssigneeElement);
                        if (!string.IsNullOrEmpty(defaultAssigneeSummary))
                        {
                            details.Add($"default assignee: {defaultAssigneeSummary}");
                        }
                    }

                    // Extract Components for Stage
                    if (businessModule == BusinessModuleEnum.Stage &&
                        (afterJson.TryGetProperty("Components", out var componentsElement) ||
                         afterJson.TryGetProperty("components", out componentsElement)))
                    {
                        var componentsSummary = GetComponentsSummary(componentsElement);
                        if (!string.IsNullOrEmpty(componentsSummary))
                        {
                            details.Add($"components: {componentsSummary}");
                        }
                    }

                    if (details.Any())
                    {
                        description += $". {string.Join("; ", details)}";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to extract creation details from afterData for {BusinessModule} {EntityName}", 
                        businessModule, entityName);
                }
            }

            // Add related entity info without showing ID
            if (relatedEntityId.HasValue && !string.IsNullOrEmpty(relatedEntityType))
            {
                description += $" in {relatedEntityType}";
            }

            if (!string.IsNullOrEmpty(reason))
            {
                // Skip reason display for ChecklistTask delete operations to avoid redundant information
                if (!(businessModule == BusinessModuleEnum.Task && operationAction.ToLower() == "deleted"))
                {
                    description += $" with reason: {reason}";
                }
            }

            return description;
        }

        /// <summary>
        /// Get specific change details from before and after data (async version)
        /// </summary>
        protected virtual async Task<string> GetChangeDetailsAsync(string beforeData, string afterData, List<string> changedFields, BusinessModuleEnum? businessModule = null)
        {
            try
            {
                var beforeJson = JsonSerializer.Deserialize<Dictionary<string, object>>(beforeData);
                var afterJson = JsonSerializer.Deserialize<Dictionary<string, object>>(afterData);

                var changeList = new List<string>();

                // For Checklist module, only show Name, Description, and Team changes
                // For Workflow module, filter out IsActive field
                var fieldsToProcess = changedFields;
                if (businessModule == BusinessModuleEnum.Checklist)
                {
                    fieldsToProcess = changedFields.Where(f => 
                        f.Equals("Name", StringComparison.OrdinalIgnoreCase) ||
                        f.Equals("Description", StringComparison.OrdinalIgnoreCase) ||
                        f.Equals("Team", StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }
                else if (businessModule == BusinessModuleEnum.Workflow)
                {
                    // Filter out IsActive field for Workflow
                    fieldsToProcess = changedFields.Where(f => 
                        !f.Equals("IsActive", StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                foreach (var field in fieldsToProcess)
                {
                    if (beforeJson.TryGetValue(field, out var beforeValue) &&
                        afterJson.TryGetValue(field, out var afterValue))
                    {
                        // Special handling for JSON fields to show meaningful changes
                        if (field.Equals("StructureJson", StringComparison.OrdinalIgnoreCase))
                        {
                            var beforeJsonStr = beforeValue?.ToString() ?? string.Empty;
                            var afterJsonStr = afterValue?.ToString() ?? string.Empty;

                            if (IsJsonString(beforeJsonStr) && IsJsonString(afterJsonStr))
                            {
                                var structuralChange = GetStructuralChangeDetails(beforeJsonStr, afterJsonStr);
                                changeList.Add(structuralChange);
                            }
                            else
                            {
                                changeList.Add("Structure modified");
                            }
                        }
                        else if (field.Equals("ComponentsJson", StringComparison.OrdinalIgnoreCase))
                        {
                            var beforeJsonStr = beforeValue?.ToString() ?? string.Empty;
                            var afterJsonStr = afterValue?.ToString() ?? string.Empty;

                            if (IsJsonString(beforeJsonStr) && IsJsonString(afterJsonStr))
                            {
                                var componentsChange = GetComponentsChangeDetails(beforeJsonStr, afterJsonStr);
                                changeList.Add(componentsChange);
                            }
                            else
                            {
                                var beforeStr = GetDisplayValue(beforeValue, field);
                                var afterStr = GetDisplayValue(afterValue, field);
                                changeList.Add($"{field} from '{beforeStr}' to '{afterStr}'");
                            }
                        }
                        else if (field.Equals("DefaultAssignee", StringComparison.OrdinalIgnoreCase))
                        {
                            var beforeJsonStr = beforeValue?.ToString() ?? string.Empty;
                            var afterJsonStr = afterValue?.ToString() ?? string.Empty;

                            if (IsJsonString(beforeJsonStr) && IsJsonString(afterJsonStr))
                            {
                                // Now properly async - no more deadlock risk
                                var assigneeChange = await GetAssigneeChangeDetailsAsync(beforeJsonStr, afterJsonStr);
                                changeList.Add(assigneeChange);
                            }
                            else
                            {
                                var beforeStr = GetDisplayValue(beforeValue, field);
                                var afterStr = GetDisplayValue(afterValue, field);
                                changeList.Add($"{field} from '{beforeStr}' to '{afterStr}'");
                            }
                        }
                        else if (field.Equals("ViewPermissionMode", StringComparison.OrdinalIgnoreCase))
                        {
                            var beforeStr = GetViewPermissionModeDisplayName(beforeValue);
                            var afterStr = GetViewPermissionModeDisplayName(afterValue);
                            changeList.Add($"view permission mode from {beforeStr} to {afterStr}");
                        }
                        else if (field.Equals("VisibleInPortal", StringComparison.OrdinalIgnoreCase))
                        {
                            var beforeStr = GetBooleanDisplayValue(beforeValue, "Yes", "No");
                            var afterStr = GetBooleanDisplayValue(afterValue, "Yes", "No");
                            changeList.Add($"Available in Customer Portal from {beforeStr} to {afterStr}");
                        }
                        else if (field.Equals("ViewTeams", StringComparison.OrdinalIgnoreCase))
                        {
                            var beforeTeams = ParseTeamList(beforeValue?.ToString());
                            var afterTeams = ParseTeamList(afterValue?.ToString());
                            // Now properly async
                            var teamChanges = await GetTeamChangesAsync(beforeTeams, afterTeams, "view");
                            if (!string.IsNullOrEmpty(teamChanges))
                            {
                                changeList.Add(teamChanges);
                            }
                        }
                        else if (field.Equals("OperateTeams", StringComparison.OrdinalIgnoreCase))
                        {
                            var beforeTeams = ParseTeamList(beforeValue?.ToString());
                            var afterTeams = ParseTeamList(afterValue?.ToString());
                            // Now properly async
                            var teamChanges = await GetTeamChangesAsync(beforeTeams, afterTeams, "operate");
                            if (!string.IsNullOrEmpty(teamChanges))
                            {
                                changeList.Add(teamChanges);
                            }
                        }
                        else if (field.Equals("Team", StringComparison.OrdinalIgnoreCase) && businessModule == BusinessModuleEnum.Checklist)
                        {
                            // For Checklist Team field, try to get team names instead of IDs
                            string beforeTeamDisplay = GetDisplayValue(beforeValue, field);
                            string afterTeamDisplay = GetDisplayValue(afterValue, field);

                            try
                            {
                                if (_userService != null)
                                {
                                    var tenantId = _userContext?.TenantId ?? "999";
                                    var teamIds = new List<string>();

                                    // Get before team ID
                                    string beforeTeamId = null;
                                    if (beforeValue != null)
                                    {
                                        beforeTeamId = beforeValue.ToString();
                                        if (!string.IsNullOrEmpty(beforeTeamId) && !beforeTeamId.Equals("Other", StringComparison.OrdinalIgnoreCase))
                                        {
                                            teamIds.Add(beforeTeamId);
                                        }
                                    }

                                    // Get after team ID
                                    string afterTeamId = null;
                                    if (afterValue != null)
                                    {
                                        afterTeamId = afterValue.ToString();
                                        if (!string.IsNullOrEmpty(afterTeamId) && !afterTeamId.Equals("Other", StringComparison.OrdinalIgnoreCase) && 
                                            !teamIds.Contains(afterTeamId))
                                        {
                                            teamIds.Add(afterTeamId);
                                        }
                                    }

                                    // Fetch team names if we have IDs to look up
                                    if (teamIds.Any())
                                    {
                                        var teamNameMap = await _userService.GetTeamNamesByIdsAsync(teamIds, tenantId);
                                        
                                        if (beforeTeamId != null)
                                        {
                                            if (beforeTeamId.Equals("Other", StringComparison.OrdinalIgnoreCase))
                                            {
                                                beforeTeamDisplay = "Other";
                                            }
                                            else if (teamNameMap != null && teamNameMap.TryGetValue(beforeTeamId, out var beforeTeamName) && 
                                                     !string.IsNullOrEmpty(beforeTeamName))
                                            {
                                                beforeTeamDisplay = beforeTeamName;
                                            }
                                        }

                                        if (afterTeamId != null)
                                        {
                                            if (afterTeamId.Equals("Other", StringComparison.OrdinalIgnoreCase))
                                            {
                                                afterTeamDisplay = "Other";
                                            }
                                            else if (teamNameMap != null && teamNameMap.TryGetValue(afterTeamId, out var afterTeamName) && 
                                                     !string.IsNullOrEmpty(afterTeamName))
                                            {
                                                afterTeamDisplay = afterTeamName;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // Handle "Other" case
                                        if (beforeTeamId != null && beforeTeamId.Equals("Other", StringComparison.OrdinalIgnoreCase))
                                        {
                                            beforeTeamDisplay = "Other";
                                        }
                                        if (afterTeamId != null && afterTeamId.Equals("Other", StringComparison.OrdinalIgnoreCase))
                                        {
                                            afterTeamDisplay = "Other";
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogDebug(ex, "Failed to get team names for Team field change, using IDs as fallback");
                            }

                            changeList.Add($"{field} from '{beforeTeamDisplay}' to '{afterTeamDisplay}'");
                        }
                        else if (field.Equals("IsDefault", StringComparison.OrdinalIgnoreCase) && businessModule == BusinessModuleEnum.Workflow)
                        {
                            // For Workflow IsDefault field, show "Default" or "Not Default"
                            var beforeStr = GetBooleanDisplayValue(beforeValue, "Default", "Not Default");
                            var afterStr = GetBooleanDisplayValue(afterValue, "Default", "Not Default");
                            changeList.Add($"Set as default workflow from {beforeStr} to {afterStr}");
                        }
                        else if (field.Equals("AssigneeId", StringComparison.OrdinalIgnoreCase))
                        {
                            // Skip AssigneeId to avoid exposing internal IDs
                            // AssigneeName will be shown instead
                            continue;
                        }
                        else
                        {
                            var beforeStr = GetDisplayValue(beforeValue, field);
                            var afterStr = GetDisplayValue(afterValue, field);

                            changeList.Add($"{field} from '{beforeStr}' to '{afterStr}'");
                        }
                    }
                    else if (afterJson.TryGetValue(field, out var newValue))
                    {
                        var newStr = GetDisplayValue(newValue, field);
                        changeList.Add($"{field} set to '{newStr}'");
                    }
                }

                if (changeList.Any())
                {
                    var result = $"Changes: {string.Join(", ", changeList)}";
                    // Don't show "and X more fields" for Workflow module
                    if (businessModule != BusinessModuleEnum.Workflow && changedFields.Count > 3)
                    {
                        result += $" and {changedFields.Count - 3} more fields";
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse change details from JSON data");
            }

            return string.Empty;
        }

        /// <summary>
        /// Get display-friendly value for UI
        /// </summary>
        protected virtual string GetDisplayValue(object value, string fieldName = null)
        {
            if (value == null) return "null";

            var str = value.ToString();

            // Handle JSON strings by detecting and analyzing their content
            if (IsJsonString(str))
            {
                return GetJsonSummary(str, fieldName);
            }

            // For Name fields, use longer limit to avoid truncation of important titles
            if (!string.IsNullOrEmpty(fieldName) &&
                fieldName.Equals("Name", StringComparison.OrdinalIgnoreCase))
            {
                // Allow up to 200 characters for Name fields
                if (str.Length > 200)
                {
                    return str.Substring(0, 197) + "...";
                }
                return str;
            }

            // Truncate very long non-JSON values for other fields
            if (str.Length > 50)
            {
                return str.Substring(0, 47) + "...";
            }

            return str;
        }

        /// <summary>
        /// Get display name for ViewPermissionMode enum value
        /// </summary>
        protected virtual string GetViewPermissionModeDisplayName(object value)
        {
            if (value == null)
                return "Public";

            try
            {
                // Try to parse as integer first
                if (int.TryParse(value.ToString(), out int intValue))
                {
                    // Check if the value is a valid enum value
                    if (Enum.IsDefined(typeof(ViewPermissionModeEnum), intValue))
                    {
                        var enumValue = (ViewPermissionModeEnum)intValue;
                        return enumValue.ToString();
                    }
                }

                // Try to parse as enum directly
                if (Enum.TryParse<ViewPermissionModeEnum>(value.ToString(), true, out var parsedEnum))
                {
                    return parsedEnum.ToString();
                }

                // Fallback to original value if parsing fails
                return value.ToString();
            }
            catch
            {
                // Fallback to original value if any error occurs
                return value.ToString();
            }
        }

        /// <summary>
        /// Get display value for boolean with custom true/false labels
        /// </summary>
        protected virtual string GetBooleanDisplayValue(object value, string trueLabel = "Yes", string falseLabel = "No")
        {
            if (value == null)
                return falseLabel;

            try
            {
                // Try to parse as boolean
                if (bool.TryParse(value.ToString(), out bool boolValue))
                {
                    return boolValue ? trueLabel : falseLabel;
                }

                // Try to parse as integer (0 = false, 1 = true)
                if (int.TryParse(value.ToString(), out int intValue))
                {
                    return intValue != 0 ? trueLabel : falseLabel;
                }

                // Try to parse as string (case-insensitive)
                var str = value.ToString().Trim();
                if (str.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                    str.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                    str.Equals("yes", StringComparison.OrdinalIgnoreCase))
                {
                    return trueLabel;
                }
                if (str.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                    str.Equals("0", StringComparison.OrdinalIgnoreCase) ||
                    str.Equals("no", StringComparison.OrdinalIgnoreCase))
                {
                    return falseLabel;
                }

                // Fallback to original value if parsing fails
                return value.ToString();
            }
            catch
            {
                // Fallback to original value if any error occurs
                return value.ToString();
            }
        }

        /// <summary>
        /// Get a summary of JSON content instead of showing raw JSON
        /// </summary>
        protected virtual string GetJsonSummary(string jsonString, string fieldName = null)
        {
            try
            {
                if (string.IsNullOrEmpty(fieldName))
                {
                    return "[JSON data]";
                }

                // Handle specific JSON field types
                switch (fieldName.ToLower())
                {
                    case "structurejson":
                    case "structure":
                        return GetQuestionnaireStructureSummary(jsonString);

                    case "componentsjson":
                    case "components":
                        return GetComponentsSummary(jsonString);

                    case "configjson":
                    case "config":
                        return GetConfigSummary(jsonString);

                    case "actionconfig":
                        return GetActionConfigSummary(jsonString);

                    case "tagsjson":
                    case "tags":
                        return GetTagsSummary(jsonString);

                    case "defaultassignee":
                    case "assignee":
                        return GetAssigneeSummary(jsonString);

                    default:
                        return "[JSON data]";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate JSON summary for field {FieldName}", fieldName);
                return "[JSON data]";
            }
        }

        /// <summary>
        /// Get summary of questionnaire structure changes
        /// </summary>
        protected virtual string GetQuestionnaireStructureSummary(string structureJson)
        {
            try
            {
                var structure = JsonSerializer.Deserialize<JsonElement>(structureJson);

                if (structure.TryGetProperty("sections", out var sectionsElement) &&
                    sectionsElement.ValueKind == JsonValueKind.Array)
                {
                    var sections = sectionsElement.EnumerateArray().ToList();
                    var sectionNames = sections
                        .Where(s => s.TryGetProperty("name", out var nameElement))
                        .Select(s => s.GetProperty("name").GetString())
                        .Where(name => !string.IsNullOrEmpty(name) && 
                                      !name.Equals("Untitled Section", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    // Get detailed questions information
                    var afterQuestions = GetAllQuestionsDetailed(sections);
                    var totalQuestions = afterQuestions.Count;

                    var changes = new List<string>();

                    // Section count and names (only show if there are named sections other than default)
                    if (sectionNames.Any())
                    {
                        changes.Add($"sections: {string.Join(", ", sectionNames.Select(n => $"'{n}'"))}");
                    }
                    // Don't show section count if only default sections exist

                    // Question count
                    if (totalQuestions > 0)
                    {
                        changes.Add($"questions: {totalQuestions}");
                    }

                    // Show detailed question information (similar to update)
                    if (afterQuestions.Any())
                    {
                        var questionDetails = afterQuestions.Select(q => GetFormattedQuestionInfo(q)).Where(t => !string.IsNullOrEmpty(t)).ToList();
                        if (questionDetails.Any())
                        {
                            changes.Add($"added questions: {string.Join("; ", questionDetails)}");
                        }
                    }

                    return string.Join("; ", changes);
                }

                return "[Structure data]";
            }
            catch
            {
                return "[Structure data]";
            }
        }

        /// <summary>
        /// Get detailed structural changes for StructureJson field
        /// </summary>
        protected virtual string GetStructuralChangeDetails(string beforeJson, string afterJson)
        {
            try
            {
                var beforeStructure = JsonSerializer.Deserialize<JsonElement>(beforeJson);
                var afterStructure = JsonSerializer.Deserialize<JsonElement>(afterJson);

                var changes = new List<string>();

                // Compare sections
                if (beforeStructure.TryGetProperty("sections", out var beforeSections) &&
                    afterStructure.TryGetProperty("sections", out var afterSections) &&
                    beforeSections.ValueKind == JsonValueKind.Array &&
                    afterSections.ValueKind == JsonValueKind.Array)
                {
                    var beforeSectionsList = beforeSections.EnumerateArray().ToList();
                    var afterSectionsList = afterSections.EnumerateArray().ToList();

                    // Section count change
                    if (beforeSectionsList.Count != afterSectionsList.Count)
                    {
                        changes.Add($"sections changed from {beforeSectionsList.Count} to {afterSectionsList.Count}");
                    }

                    // Section name changes (skip default "Untitled Section")
                    var beforeSectionNames = beforeSectionsList
                        .Where(s => s.TryGetProperty("name", out var _))
                        .Select(s => s.GetProperty("name").GetString())
                        .Where(name => !string.IsNullOrEmpty(name) && 
                                      !name.Equals("Untitled Section", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    var afterSectionNames = afterSectionsList
                        .Where(s => s.TryGetProperty("name", out var _))
                        .Select(s => s.GetProperty("name").GetString())
                        .Where(name => !string.IsNullOrEmpty(name) && 
                                      !name.Equals("Untitled Section", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    // Find added sections
                    var addedSections = afterSectionNames.Except(beforeSectionNames).ToList();
                    if (addedSections.Any())
                    {
                        changes.Add($"added sections: {string.Join(", ", addedSections.Select(n => $"'{n}'"))}");
                    }

                    // Find removed sections
                    var removedSections = beforeSectionNames.Except(afterSectionNames).ToList();
                    if (removedSections.Any())
                    {
                        changes.Add($"removed sections: {string.Join(", ", removedSections.Select(n => $"'{n}'"))}");
                    }

                    // Compare questions with detailed analysis
                    var beforeQuestions = GetAllQuestionsDetailed(beforeSectionsList);
                    var afterQuestions = GetAllQuestionsDetailed(afterSectionsList);

                    var beforeQuestionCount = beforeQuestions.Count;
                    var afterQuestionCount = afterQuestions.Count;

                    if (beforeQuestionCount != afterQuestionCount)
                    {
                        changes.Add($"questions changed from {beforeQuestionCount} to {afterQuestionCount}");
                    }

                    // Get detailed question changes
                    var questionChanges = GetDetailedQuestionChanges(beforeQuestions, afterQuestions);
                    changes.AddRange(questionChanges);
                }

                if (changes.Any())
                {
                    // Format the structure changes in a more readable way
                    // Show all changes, but limit individual change descriptions if needed
                    if (changes.Count == 1)
                    {
                        return $"Structure modified: {changes[0]}";
                    }
                    else
                    {
                        // Join all changes - show everything
                        var allChanges = string.Join("; ", changes);
                        return $"Structure modified: {allChanges}";
                    }
                }

                return "Structure modified";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze structural changes");
                return "Structure modified";
            }
        }

        /// <summary>
        /// Get detailed components changes for ComponentsJson field
        /// </summary>
        protected virtual string GetComponentsChangeDetails(string beforeJson, string afterJson)
        {
            try
            {
                var beforeComponents = JsonSerializer.Deserialize<JsonElement>(beforeJson);
                var afterComponents = JsonSerializer.Deserialize<JsonElement>(afterJson);

                var changes = new List<string>();

                if (beforeComponents.ValueKind == JsonValueKind.Array && afterComponents.ValueKind == JsonValueKind.Array)
                {
                    var beforeArray = beforeComponents.EnumerateArray().ToList();
                    var afterArray = afterComponents.EnumerateArray().ToList();

                    if (beforeArray.Count != afterArray.Count)
                    {
                        changes.Add($"components changed from {beforeArray.Count} to {afterArray.Count}");
                    }

                    // Try to identify component types if available
                    var beforeTypes = beforeArray
                        .Where(c => c.TryGetProperty("type", out var _))
                        .Select(c => c.GetProperty("type").GetString())
                        .Where(type => !string.IsNullOrEmpty(type))
                        .GroupBy(t => t)
                        .ToDictionary(g => g.Key, g => g.Count());

                    var afterTypes = afterArray
                        .Where(c => c.TryGetProperty("type", out var _))
                        .Select(c => c.GetProperty("type").GetString())
                        .Where(type => !string.IsNullOrEmpty(type))
                        .GroupBy(t => t)
                        .ToDictionary(g => g.Key, g => g.Count());

                    foreach (var kvp in afterTypes)
                    {
                        var afterCount = kvp.Value;
                        var beforeCount = beforeTypes.GetValueOrDefault(kvp.Key, 0);

                        if (beforeCount != afterCount)
                        {
                            if (beforeCount == 0)
                            {
                                changes.Add($"added {afterCount} {kvp.Key} component(s)");
                            }
                            else if (afterCount == 0)
                            {
                                changes.Add($"removed {beforeCount} {kvp.Key} component(s)");
                            }
                            else
                            {
                                changes.Add($"{kvp.Key} components: {beforeCount} → {afterCount}");
                            }
                        }
                    }
                }

                if (changes.Any())
                {
                    return $"Components: {string.Join(", ", changes.Take(3))}";
                }

                return "Components modified";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze components changes");
                return "Components modified";
            }
        }

        /// <summary>
        /// Get summary of components for display in creation logs
        /// </summary>
        protected virtual string GetComponentsSummary(JsonElement componentsElement)
        {
            try
            {
                if (componentsElement.ValueKind != JsonValueKind.Array)
                {
                    return string.Empty;
                }

                var components = new List<string>();
                foreach (var component in componentsElement.EnumerateArray())
                {
                    if (!component.TryGetProperty("key", out var keyElement) &&
                        !component.TryGetProperty("Key", out keyElement))
                    {
                        continue;
                    }

                    var key = keyElement.GetString();
                    if (string.IsNullOrEmpty(key))
                    {
                        continue;
                    }

                    var componentDetails = new List<string>();

                    // Extract checklist names
                    if ((component.TryGetProperty("checklistNames", out var checklistNamesElement) ||
                         component.TryGetProperty("ChecklistNames", out checklistNamesElement)) &&
                        checklistNamesElement.ValueKind == JsonValueKind.Array)
                    {
                        var checklistNames = checklistNamesElement.EnumerateArray()
                            .Select(n => n.GetString())
                            .Where(n => !string.IsNullOrEmpty(n))
                            .ToList();
                        if (checklistNames.Any())
                        {
                            componentDetails.Add($"checklists: {string.Join(", ", checklistNames.Select(n => $"'{n}'"))}");
                        }
                    }

                    // Extract questionnaire names
                    if ((component.TryGetProperty("questionnaireNames", out var questionnaireNamesElement) ||
                         component.TryGetProperty("QuestionnaireNames", out questionnaireNamesElement)) &&
                        questionnaireNamesElement.ValueKind == JsonValueKind.Array)
                    {
                        var questionnaireNames = questionnaireNamesElement.EnumerateArray()
                            .Select(n => n.GetString())
                            .Where(n => !string.IsNullOrEmpty(n))
                            .ToList();
                        if (questionnaireNames.Any())
                        {
                            componentDetails.Add($"questionnaires: {string.Join(", ", questionnaireNames.Select(n => $"'{n}'"))}");
                        }
                    }

                    // Extract static fields
                    if ((component.TryGetProperty("staticFields", out var staticFieldsElement) ||
                         component.TryGetProperty("StaticFields", out staticFieldsElement)) &&
                        staticFieldsElement.ValueKind == JsonValueKind.Array)
                    {
                        var staticFields = staticFieldsElement.EnumerateArray()
                            .Select(f => f.GetString())
                            .Where(f => !string.IsNullOrEmpty(f))
                            .ToList();
                        if (staticFields.Any())
                        {
                            componentDetails.Add($"fields: {string.Join(", ", staticFields)}");
                        }
                    }

                    // Check if component is enabled
                    var isEnabled = true;
                    if (component.TryGetProperty("isEnabled", out var isEnabledElement) ||
                        component.TryGetProperty("IsEnabled", out isEnabledElement))
                    {
                        isEnabled = isEnabledElement.ValueKind == JsonValueKind.True;
                    }

                    var componentInfo = key;
                    if (componentDetails.Any())
                    {
                        componentInfo += $" ({string.Join("; ", componentDetails)})";
                    }
                    if (!isEnabled)
                    {
                        componentInfo += " [disabled]";
                    }

                    components.Add(componentInfo);
                }

                if (components.Any())
                {
                    return string.Join("; ", components);
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get components summary");
                return string.Empty;
            }
        }

        /// <summary>
        /// Get summary of teams for display in creation logs (async version)
        /// </summary>
        protected virtual async Task<string> GetTeamsSummaryAsync(JsonElement teamsElement)
        {
            try
            {
                var teamIds = new List<string>();

                // Handle different JSON value kinds
                if (teamsElement.ValueKind == JsonValueKind.Array)
                {
                    // Direct array
                    foreach (var teamIdElement in teamsElement.EnumerateArray())
                    {
                        var teamId = teamIdElement.GetString();
                        if (!string.IsNullOrEmpty(teamId))
                        {
                            teamIds.Add(teamId);
                        }
                    }
                }
                else if (teamsElement.ValueKind == JsonValueKind.String)
                {
                    // String that contains JSON array (double-encoded)
                    var teamsJson = teamsElement.GetString();
                    if (!string.IsNullOrEmpty(teamsJson))
                    {
                        // Parse the JSON string to get the actual array
                        var parsedTeams = ParseTeamList(teamsJson);
                        teamIds.AddRange(parsedTeams);
                    }
                }
                else if (teamsElement.ValueKind == JsonValueKind.Null)
                {
                    return string.Empty;
                }

                if (teamIds.Any())
                {
                    // Try to get team names if UserService is available
                    try
                    {
                        if (_userService != null)
                        {
                            var tenantId = _userContext?.TenantId ?? "999";
                            var teamNameMap = await _userService.GetTeamNamesByIdsAsync(teamIds, tenantId);
                            
                            if (teamNameMap != null && teamNameMap.Any())
                            {
                                var teamNames = teamIds
                                    .Select(id => teamNameMap.TryGetValue(id, out var name) && !string.IsNullOrEmpty(name) 
                                        ? name 
                                        : id)
                                    .ToList();
                                return string.Join(", ", teamNames.Select(n => $"'{n}'"));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to get team names for teams summary, using IDs as fallback");
                    }

                    // Fallback to IDs if team names are not available
                    return string.Join(", ", teamIds.Select(id => $"'{id}'"));
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get teams summary");
                return string.Empty;
            }
        }

        /// <summary>
        /// Get summary of teams for display in creation logs (synchronous version - for backward compatibility)
        /// </summary>
        protected virtual string GetTeamsSummary(JsonElement teamsElement)
        {
            try
            {
                if (teamsElement.ValueKind != JsonValueKind.Array)
                {
                    return string.Empty;
                }

                var teamIds = new List<string>();
                foreach (var teamIdElement in teamsElement.EnumerateArray())
                {
                    var teamId = teamIdElement.GetString();
                    if (!string.IsNullOrEmpty(teamId))
                    {
                        teamIds.Add(teamId);
                    }
                }

                if (teamIds.Any())
                {
                    // Try to get team names if UserService is available
                    try
                    {
                        var tenantId = _userContext?.TenantId ?? "999";
                        var teamNameMap = _userService?.GetTeamNamesByIdsAsync(teamIds, tenantId).GetAwaiter().GetResult();
                        
                        if (teamNameMap != null && teamNameMap.Any())
                        {
                            var teamNames = teamIds
                                .Select(id => teamNameMap.TryGetValue(id, out var name) && !string.IsNullOrEmpty(name) 
                                    ? name 
                                    : id)
                                .ToList();
                            return string.Join(", ", teamNames.Select(n => $"'{n}'"));
                        }
                    }
                    catch
                    {
                        // If team name lookup fails, use IDs
                    }

                    // Fallback to IDs if team names are not available
                    return string.Join(", ", teamIds.Select(id => $"'{id}'"));
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get teams summary");
                return string.Empty;
            }
        }

        /// <summary>
        /// Get summary of default assignees for display in creation logs
        /// </summary>
        protected virtual string GetDefaultAssigneeSummary(JsonElement assigneeElement)
        {
            try
            {
                if (assigneeElement.ValueKind != JsonValueKind.Array)
                {
                    return string.Empty;
                }

                var userIds = new List<string>();
                foreach (var userIdElement in assigneeElement.EnumerateArray())
                {
                    var userId = userIdElement.GetString();
                    if (!string.IsNullOrEmpty(userId))
                    {
                        userIds.Add(userId);
                    }
                }

                if (userIds.Any())
                {
                    // Try to get user names if UserService is available
                    try
                    {
                        var tenantId = _userContext?.TenantId ?? "999";
                        var userIdsLong = userIds.Where(id => long.TryParse(id, out _))
                            .Select(id => long.Parse(id))
                            .ToList();
                        
                        if (userIdsLong.Any() && _userService != null)
                        {
                            var users = _userService.GetUsersByIdsAsync(userIdsLong, tenantId).GetAwaiter().GetResult();
                            
                            if (users != null && users.Any())
                            {
                                // GetUsersByIdsAsync already sets display name to Username field with priority:
                                // FirstName + LastName > UserName (same as GetUserTreeAsync logic)
                                // So we can directly use Username field
                                var userMap = users.ToDictionary(u => u.Id.ToString(), u => 
                                    !string.IsNullOrEmpty(u.Username) ? u.Username : 
                                    !string.IsNullOrEmpty(u.Email) ? u.Email : 
                                    u.Id.ToString());
                                
                                var userNames = userIds
                                    .Select(id => userMap.TryGetValue(id, out var name) && !string.IsNullOrEmpty(name) 
                                        ? name 
                                        : id)
                                    .ToList();
                                return string.Join(", ", userNames.Select(n => $"'{n}'"));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get user names for default assignees");
                        // If user name lookup fails, use IDs
                    }

                    // Fallback to IDs if user names are not available
                    return string.Join(", ", userIds.Select(id => $"'{id}'"));
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get default assignee summary");
                return string.Empty;
            }
        }

        /// <summary>
        /// Helper class to represent a question for comparison
        /// </summary>
        protected class QuestionInfo
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Type { get; set; }
        }

        /// <summary>
        /// Enhanced helper class to represent a question with detailed information for comparison
        /// </summary>
        protected class DetailedQuestionInfo
        {
            public string Id { get; set; }
            public string TemporaryId { get; set; }
            public string Title { get; set; }
            public string Type { get; set; }
            public string Description { get; set; }
            public bool Required { get; set; }
            public List<string> Options { get; set; } = new List<string>();
            public List<OptionInfo> OptionDetails { get; set; } = new List<OptionInfo>();
            public List<OptionInfo> Rows { get; set; } = new List<OptionInfo>();
            public List<OptionInfo> Columns { get; set; } = new List<OptionInfo>();
            public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

            public override bool Equals(object obj)
            {
                if (obj is DetailedQuestionInfo other)
                {
                    // Match by ID first, then temporaryId, then title
                    if (!string.IsNullOrEmpty(Id) && !string.IsNullOrEmpty(other.Id))
                        return Id == other.Id;
                    if (!string.IsNullOrEmpty(TemporaryId) && !string.IsNullOrEmpty(other.TemporaryId))
                        return TemporaryId == other.TemporaryId;
                    return string.IsNullOrEmpty(Id) && string.IsNullOrEmpty(other.Id) && 
                           string.IsNullOrEmpty(TemporaryId) && string.IsNullOrEmpty(other.TemporaryId) && 
                           Title == other.Title;
                }
                return false;
            }

            public override int GetHashCode()
            {
                if (!string.IsNullOrEmpty(Id))
                    return Id.GetHashCode();
                if (!string.IsNullOrEmpty(TemporaryId))
                    return TemporaryId.GetHashCode();
                return Title?.GetHashCode() ?? 0;
            }
        }

        /// <summary>
        /// Helper class to represent an option with label and value
        /// </summary>
        protected class OptionInfo
        {
            public string Id { get; set; }
            public string Label { get; set; }
            public string Value { get; set; }
            public bool IsOther { get; set; }

            public override bool Equals(object obj)
            {
                if (obj is OptionInfo other)
                {
                    // Compare by ID first, then by value, then by label
                    if (!string.IsNullOrEmpty(Id) && !string.IsNullOrEmpty(other.Id))
                        return Id == other.Id;
                    if (!string.IsNullOrEmpty(Value) && !string.IsNullOrEmpty(other.Value))
                        return Value == other.Value;
                    return Label == other.Label;
                }
                return false;
            }

            public override int GetHashCode()
            {
                if (!string.IsNullOrEmpty(Id)) return Id.GetHashCode();
                if (!string.IsNullOrEmpty(Value)) return Value.GetHashCode();
                return Label?.GetHashCode() ?? 0;
            }

            public string GetDisplayText()
            {
                // If label is not empty, use it
                if (!string.IsNullOrEmpty(Label)) return Label;
                
                // If value is "other" (case-insensitive), display "Other"
                if (!string.IsNullOrEmpty(Value))
                {
                    if (Value.Equals("other", StringComparison.OrdinalIgnoreCase) || IsOther)
                        return "Other";
                    return Value;
                }
                
                return "Option";
            }
        }

        /// <summary>
        /// Extract all questions from sections list
        /// </summary>
        protected virtual List<QuestionInfo> GetAllQuestions(List<JsonElement> sections)
        {
            var questions = new List<QuestionInfo>();

            foreach (var section in sections)
            {
                if (section.TryGetProperty("questions", out var questionsElement) &&
                    questionsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var question in questionsElement.EnumerateArray())
                    {
                        var questionInfo = new QuestionInfo();

                        if (question.TryGetProperty("id", out var idElement))
                        {
                            questionInfo.Id = idElement.GetString() ?? string.Empty;
                        }

                        if (question.TryGetProperty("title", out var titleElement))
                        {
                            questionInfo.Title = titleElement.GetString() ?? string.Empty;
                        }
                        else if (question.TryGetProperty("text", out var textElement))
                        {
                            // Some questions might use "text" instead of "title"
                            questionInfo.Title = textElement.GetString() ?? string.Empty;
                        }
                        else if (question.TryGetProperty("question", out var questionElement))
                        {
                            // Some questions might use "question" instead of "title"
                            questionInfo.Title = questionElement.GetString() ?? string.Empty;
                        }

                        if (question.TryGetProperty("type", out var typeElement))
                        {
                            questionInfo.Type = typeElement.GetString() ?? string.Empty;
                        }

                        // Only add questions that have some identifying information
                        if (!string.IsNullOrEmpty(questionInfo.Title) || !string.IsNullOrEmpty(questionInfo.Id))
                        {
                            questions.Add(questionInfo);
                        }
                    }
                }
            }

            return questions;
        }

        /// <summary>
        /// Get detailed assignee changes for DefaultAssignee field
        /// </summary>
        protected virtual async Task<string> GetAssigneeChangeDetailsAsync(string beforeJson, string afterJson)
        {
            try
            {
                // Handle double-escaped JSON strings (when JSON arrays are serialized as string values)
                string normalizedBeforeJson = NormalizeJsonString(beforeJson);
                string normalizedAfterJson = NormalizeJsonString(afterJson);

                var beforeAssignees = JsonSerializer.Deserialize<JsonElement>(normalizedBeforeJson);
                var afterAssignees = JsonSerializer.Deserialize<JsonElement>(normalizedAfterJson);

                var changes = new List<string>();

                if (beforeAssignees.ValueKind == JsonValueKind.Array && afterAssignees.ValueKind == JsonValueKind.Array)
                {
                    var beforeList = beforeAssignees.EnumerateArray()
                        .Where(a => a.ValueKind == JsonValueKind.String)
                        .Select(a => a.GetString())
                        .Where(a => !string.IsNullOrEmpty(a))
                        .ToHashSet();

                    var afterList = afterAssignees.EnumerateArray()
                        .Where(a => a.ValueKind == JsonValueKind.String)
                        .Select(a => a.GetString())
                        .Where(a => !string.IsNullOrEmpty(a))
                        .ToHashSet();

                    // Find added and removed assignees
                    var addedAssignees = afterList.Except(beforeList).ToList();
                    var removedAssignees = beforeList.Except(afterList).ToList();

                    // Get user names for the changed assignees
                    var changedUserIds = addedAssignees.Concat(removedAssignees)
                        .Where(id => long.TryParse(id, out _))
                        .Select(long.Parse)
                        .ToList();

                    var userNameMap = new Dictionary<long, string>();
                    if (changedUserIds.Any())
                    {
                        try
                        {
                            // Get tenant ID from UserContext (works in background tasks)
                            var tenantId = _userContext?.TenantId ?? "999";
                            _logger.LogDebug("Using TenantId: {TenantId} for fetching user names", tenantId);

                            // Fixed: Properly await async operation instead of blocking
                            var users = await _userService.GetUsersByIdsAsync(changedUserIds, tenantId);

                            // Group by ID to handle potential duplicates (shouldn't happen after UserService fix, but defensive)
                            userNameMap = users
                                .GroupBy(u => u.Id)
                                .ToDictionary(
                                    g => g.Key,
                                    g =>
                                    {
                                        var user = g.First();
                                        return !string.IsNullOrEmpty(user.Username) ? user.Username :
                                               (!string.IsNullOrEmpty(user.Email) ? user.Email : $"User_{user.Id}");
                                    }
                                );

                            _logger.LogDebug("Fetched {Count} user names for assignee change details", userNameMap.Count);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to fetch user names for assignee change details. Using IDs instead.");
                        }
                    }

                    // Build change descriptions with user names
                    if (addedAssignees.Any())
                    {
                        var addedNames = addedAssignees
                            .Where(id => long.TryParse(id, out var userId))
                            .Select(id =>
                            {
                                var userId = long.Parse(id);
                                return userNameMap.GetValueOrDefault(userId, id);
                            })
                            .ToList(); // Show all names without limitation

                        if (addedNames.Any())
                        {
                            if (addedAssignees.Count == 1)
                            {
                                changes.Add($"added {addedNames[0]}");
                            }
                            else
                            {
                                changes.Add($"added {string.Join(", ", addedNames)}");
                            }
                        }
                    }

                    // Build removed descriptions with user names
                    if (removedAssignees.Any())
                    {
                        var removedNames = removedAssignees
                            .Where(id => long.TryParse(id, out var userId))
                            .Select(id =>
                            {
                                var userId = long.Parse(id);
                                return userNameMap.GetValueOrDefault(userId, id);
                            })
                            .ToList(); // Show all names without limitation

                        if (removedNames.Any())
                        {
                            if (removedAssignees.Count == 1)
                            {
                                changes.Add($"removed {removedNames[0]}");
                            }
                            else
                            {
                                changes.Add($"removed {string.Join(", ", removedNames)}");
                            }
                        }
                    }

                    // Show total count change if significant and no specific names shown
                    if (beforeList.Count != afterList.Count && Math.Abs(beforeList.Count - afterList.Count) > 3 && !changes.Any())
                    {
                        changes.Add($"assignees changed from {beforeList.Count} to {afterList.Count}");
                    }
                }

                if (changes.Any())
                {
                    return $"DefaultAssignee: {string.Join(", ", changes)}";
                }

                return "DefaultAssignee modified";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze assignee changes");
                return "DefaultAssignee modified";
            }
        }

        /// <summary>
        /// Get summary of components changes
        /// </summary>
        protected virtual string GetComponentsSummary(string componentsJson)
        {
            try
            {
                var components = JsonSerializer.Deserialize<JsonElement>(componentsJson);

                if (components.ValueKind == JsonValueKind.Array)
                {
                    var componentsArray = components.EnumerateArray().ToList();
                    return $"{componentsArray.Count} components";
                }
                else if (components.ValueKind == JsonValueKind.Object)
                {
                    var properties = components.EnumerateObject().ToList();
                    return $"{properties.Count} component properties";
                }

                return "[Components data]";
            }
            catch
            {
                return "[Components data]";
            }
        }

        /// <summary>
        /// Get summary of config changes
        /// </summary>
        protected virtual string GetConfigSummary(string configJson)
        {
            try
            {
                var config = JsonSerializer.Deserialize<JsonElement>(configJson);

                if (config.ValueKind == JsonValueKind.Object)
                {
                    var properties = config.EnumerateObject().ToList();
                    var keyNames = properties.Take(3).Select(p => p.Name).ToList();

                    if (keyNames.Any())
                    {
                        var summary = $"{properties.Count} config items";
                        if (keyNames.Count <= 3)
                        {
                            summary += $": {string.Join(", ", keyNames)}";
                        }
                        else
                        {
                            summary += $": {string.Join(", ", keyNames.Take(2))} and {properties.Count - 2} more";
                        }
                        return summary;
                    }

                    return $"{properties.Count} config items";
                }

                return "[Config data]";
            }
            catch
            {
                return "[Config data]";
            }
        }

        /// <summary>
        /// Get summary of action configuration JSON
        /// </summary>
        protected virtual string GetActionConfigSummary(string actionConfigJson)
        {
            try
            {
                var config = JsonSerializer.Deserialize<JsonElement>(actionConfigJson);

                // Try to extract sourceCode for Python actions
                if (config.TryGetProperty("sourceCode", out var sourceCodeElement))
                {
                    var sourceCode = sourceCodeElement.GetString();
                    if (!string.IsNullOrEmpty(sourceCode))
                    {
                        // Clean up the code by removing extra whitespace
                        var cleanedCode = sourceCode.Replace("\r\n", " ").Replace("\n", " ").Replace("  ", " ").Trim();

                        // Limit the length to avoid overly long descriptions
                        if (cleanedCode.Length > 150)
                        {
                            return $"[Python: {cleanedCode.Substring(0, 147)}...]";
                        }

                        return $"[Python: {cleanedCode}]";
                    }
                }

                // Try to extract other common config properties
                var properties = new List<string>();

                if (config.TryGetProperty("url", out var urlElement))
                {
                    properties.Add($"url: {urlElement.GetString()}");
                }

                if (config.TryGetProperty("method", out var methodElement))
                {
                    properties.Add($"method: {methodElement.GetString()}");
                }

                if (config.TryGetProperty("endpoint", out var endpointElement))
                {
                    properties.Add($"endpoint: {endpointElement.GetString()}");
                }

                if (properties.Any())
                {
                    return $"[{string.Join(", ", properties.Take(3))}]";
                }

                return "[Action config]";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse action config JSON");
                return "[Action config]";
            }
        }

        /// <summary>
        /// Get summary of tags changes
        /// </summary>
        protected virtual string GetTagsSummary(string tagsJson)
        {
            try
            {
                var tags = JsonSerializer.Deserialize<JsonElement>(tagsJson);

                if (tags.ValueKind == JsonValueKind.Array)
                {
                    var tagsArray = tags.EnumerateArray()
                        .Where(t => t.ValueKind == JsonValueKind.String)
                        .Select(t => t.GetString())
                        .Where(tag => !string.IsNullOrEmpty(tag))
                        .ToList();

                    if (tagsArray.Any())
                    {
                        if (tagsArray.Count <= 3)
                        {
                            return $"[{string.Join(", ", tagsArray)}]";
                        }
                        else
                        {
                            return $"[{string.Join(", ", tagsArray.Take(3))} and {tagsArray.Count - 3} more]";
                        }
                    }

                    return "[]";
                }

                return "[Tags data]";
            }
            catch
            {
                return "[Tags data]";
            }
        }

        /// <summary>
        /// Get summary of assignee changes
        /// </summary>
        protected virtual string GetAssigneeSummary(string assigneeJson)
        {
            try
            {
                var assignees = JsonSerializer.Deserialize<JsonElement>(assigneeJson);

                if (assignees.ValueKind == JsonValueKind.Array)
                {
                    var assigneeList = assignees.EnumerateArray()
                        .Where(a => a.ValueKind == JsonValueKind.String)
                        .Select(a => a.GetString())
                        .Where(a => !string.IsNullOrEmpty(a))
                        .ToList();

                    if (assigneeList.Any())
                    {
                        if (assigneeList.Count == 1)
                        {
                            return "1 assignee";
                        }
                        else
                        {
                            return $"{assigneeList.Count} assignees";
                        }
                    }

                    return "no assignees";
                }

                return "[Assignee data]";
            }
            catch
            {
                return "[Assignee data]";
            }
        }

        /// <summary>
        /// Generic helper method for logging independent operations
        /// (Extracted from child classes to eliminate code duplication)
        /// </summary>
        protected virtual async Task<bool> LogIndependentOperationAsync(
            OperationTypeEnum operationType,
            BusinessModuleEnum businessModule,
            long businessId,
            string entityName,
            string operationAction,
            string beforeData = null,
            string afterData = null,
            List<string> changedFields = null,
            string reason = null,
            string version = null,
            string description = null,
            long? relatedEntityId = null,
            string relatedEntityType = null,
            string extendedData = null,
            string customDescription = null)
        {
            try
            {
                var operationTitle = $"{businessModule} {operationAction}: {entityName}";

                // Use custom description if provided, otherwise use enhanced description method
                var operationDescription = !string.IsNullOrEmpty(customDescription)
                    ? customDescription
                    : await BuildEnhancedOperationDescriptionAsync(
                        businessModule,
                        entityName,
                        operationAction,
                        beforeData,
                        afterData,
                        changedFields,
                        relatedEntityId,
                        relatedEntityType,
                        reason);

                // Add module-specific additions
                if (!string.IsNullOrEmpty(description))
                {
                    operationDescription += $". Description: {description}";
                }

                if (!string.IsNullOrEmpty(version))
                {
                    operationDescription += $" as version {version}";
                }

                if (string.IsNullOrEmpty(extendedData))
                {
                    extendedData = BuildDefaultExtendedData(
                        businessModule, businessId, entityName, operationAction,
                        relatedEntityId, relatedEntityType, reason, version, description, changedFields);
                }

                var operationLog = BuildOperationLogEntity(
                    operationType,
                    businessModule,
                    businessId,
                    null, // No onboardingId for independent operations
                    null, // No stageId for independent operations
                    operationTitle,
                    operationDescription,
                    beforeData,
                    afterData,
                    changedFields,
                    extendedData
                );

                return await _operationChangeLogRepository.InsertOperationLogAsync(operationLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log independent {OperationType} operation for {BusinessModule} {BusinessId}",
                    operationType, businessModule, businessId);
                return false;
            }
        }

        /// <summary>
        /// Build default extended data for independent operations
        /// (Extracted from child classes to eliminate code duplication)
        /// </summary>
        protected virtual string BuildDefaultExtendedData(
            BusinessModuleEnum businessModule,
            long businessId,
            string entityName,
            string operationAction,
            long? relatedEntityId = null,
            string relatedEntityType = null,
            string reason = null,
            string version = null,
            string description = null,
            List<string> changedFields = null)
        {
            var currentTime = DateTimeOffset.UtcNow;
            var extendedDataObj = new Dictionary<string, object>
            {
                { $"{businessModule}Id", businessId },
                { $"{businessModule}Name", entityName },
                { $"{operationAction}At", FormatToUSTime(currentTime) }
            };

            if (relatedEntityId.HasValue && !string.IsNullOrEmpty(relatedEntityType))
            {
                extendedDataObj.Add($"{relatedEntityType}Id", relatedEntityId.Value);
            }

            if (!string.IsNullOrEmpty(reason))
            {
                extendedDataObj.Add("Reason", reason);
            }

            if (!string.IsNullOrEmpty(version))
            {
                extendedDataObj.Add("Version", version);
            }

            if (!string.IsNullOrEmpty(description))
            {
                extendedDataObj.Add("Description", description);
            }

            if (changedFields?.Any() == true)
            {
                extendedDataObj.Add("ChangedFieldsCount", changedFields.Count);
            }

            return JsonSerializer.Serialize(extendedDataObj);
        }

        /// <summary>
        /// Extract all questions from sections list with detailed information
        /// </summary>
        protected virtual List<DetailedQuestionInfo> GetAllQuestionsDetailed(List<JsonElement> sections)
        {
            var questions = new List<DetailedQuestionInfo>();
            var processedQuestions = new HashSet<string>(); // Track processed questions by title to avoid duplicates

            foreach (var section in sections)
            {
                // Process both 'questions' and 'items' arrays, but avoid duplicates
                var questionArrays = new List<(string arrayName, JsonElement array)>();

                if (section.TryGetProperty("questions", out var questionsElement) &&
                    questionsElement.ValueKind == JsonValueKind.Array)
                {
                    questionArrays.Add(("questions", questionsElement));
                }

                if (section.TryGetProperty("items", out var itemsElement) &&
                    itemsElement.ValueKind == JsonValueKind.Array)
                {
                    questionArrays.Add(("items", itemsElement));
                }

                // Process arrays in priority order (questions first, then items)
                foreach (var (arrayName, questionArray) in questionArrays.OrderBy(x => x.arrayName == "questions" ? 0 : 1))
                {
                    foreach (var question in questionArray.EnumerateArray())
                    {
                        var questionInfo = ExtractQuestionInfo(question);

                        if (questionInfo != null)
                        {
                            // Use title as the primary key for deduplication
                            var questionKey = GetQuestionDeduplicationKey(questionInfo);

                            if (!processedQuestions.Contains(questionKey))
                            {
                                questions.Add(questionInfo);
                                processedQuestions.Add(questionKey);
                            }
                        }
                    }
                }
            }

            return questions;
        }

        /// <summary>
        /// Extract question information from a JSON element
        /// </summary>
        protected virtual DetailedQuestionInfo ExtractQuestionInfo(JsonElement question)
        {
            var questionInfo = new DetailedQuestionInfo();

            // Extract basic properties
            if (question.TryGetProperty("id", out var idElement))
            {
                questionInfo.Id = idElement.GetString() ?? string.Empty;
            }

            if (question.TryGetProperty("temporaryId", out var temporaryIdElement))
            {
                questionInfo.TemporaryId = temporaryIdElement.GetString() ?? string.Empty;
            }

            if (question.TryGetProperty("title", out var titleElement))
            {
                questionInfo.Title = titleElement.GetString() ?? string.Empty;
            }
            else if (question.TryGetProperty("text", out var textElement))
            {
                questionInfo.Title = textElement.GetString() ?? string.Empty;
            }
            else if (question.TryGetProperty("question", out var questionElement))
            {
                questionInfo.Title = questionElement.GetString() ?? string.Empty;
            }

            if (question.TryGetProperty("type", out var typeElement))
            {
                questionInfo.Type = typeElement.GetString() ?? string.Empty;
            }

            if (question.TryGetProperty("description", out var descElement))
            {
                questionInfo.Description = descElement.GetString() ?? string.Empty;
            }

            if (question.TryGetProperty("required", out var requiredElement))
            {
                questionInfo.Required = requiredElement.ValueKind == JsonValueKind.True;
            }

            // Extract options for multiple choice questions
            if (question.TryGetProperty("options", out var optionsElement) &&
                optionsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var option in optionsElement.EnumerateArray())
                {
                    var optionInfo = new OptionInfo();
                    
                    // Extract option properties
                    if (option.TryGetProperty("id", out var optionId))
                        optionInfo.Id = optionId.GetString() ?? string.Empty;
                    
                    if (option.TryGetProperty("label", out var optionLabel))
                        optionInfo.Label = optionLabel.GetString() ?? string.Empty;
                    
                    if (option.TryGetProperty("value", out var optionValue))
                        optionInfo.Value = optionValue.GetString() ?? string.Empty;
                    
                    if (option.TryGetProperty("isOther", out var isOther))
                        optionInfo.IsOther = isOther.ValueKind == JsonValueKind.True;
                    
                    // For string options (backward compatibility)
                    if (option.ValueKind == JsonValueKind.String)
                    {
                        var strValue = option.GetString();
                        optionInfo.Value = strValue ?? string.Empty;
                        optionInfo.Label = strValue ?? string.Empty;
                    }
                    
                    // Add to both lists for backward compatibility
                    // Always add optionInfo to OptionDetails, even if displayText is empty
                    questionInfo.OptionDetails.Add(optionInfo);
                    
                    var displayText = optionInfo.GetDisplayText();
                    if (!string.IsNullOrEmpty(displayText))
                    {
                        questionInfo.Options.Add(displayText);
                    }
                    else
                    {
                        // Fallback: use value or label if displayText is empty
                        var fallbackText = !string.IsNullOrEmpty(optionInfo.Value) 
                            ? optionInfo.Value 
                            : (!string.IsNullOrEmpty(optionInfo.Label) ? optionInfo.Label : "Option");
                        questionInfo.Options.Add(fallbackText);
                    }
                }
            }

            // Extract rows for grid type questions
            if (question.TryGetProperty("rows", out var rowsElement) &&
                rowsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var row in rowsElement.EnumerateArray())
                {
                    var rowInfo = new OptionInfo();
                    
                    if (row.TryGetProperty("id", out var rowId))
                        rowInfo.Id = rowId.GetString() ?? string.Empty;
                    
                    if (row.TryGetProperty("label", out var rowLabel))
                        rowInfo.Label = rowLabel.GetString() ?? string.Empty;
                    
                    if (row.ValueKind == JsonValueKind.String)
                    {
                        var strValue = row.GetString();
                        rowInfo.Label = strValue ?? string.Empty;
                    }
                    
                    if (!string.IsNullOrEmpty(rowInfo.Label))
                    {
                        questionInfo.Rows.Add(rowInfo);
                    }
                }
            }

            // Extract columns for grid type questions
            if (question.TryGetProperty("columns", out var columnsElement) &&
                columnsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var column in columnsElement.EnumerateArray())
                {
                    var columnInfo = new OptionInfo();
                    
                    if (column.TryGetProperty("id", out var columnId))
                        columnInfo.Id = columnId.GetString() ?? string.Empty;
                    
                    if (column.TryGetProperty("label", out var columnLabel))
                        columnInfo.Label = columnLabel.GetString() ?? string.Empty;
                    
                    if (column.TryGetProperty("isOther", out var isOther))
                        columnInfo.IsOther = isOther.ValueKind == JsonValueKind.True;
                    
                    if (column.ValueKind == JsonValueKind.String)
                    {
                        var strValue = column.GetString();
                        columnInfo.Label = strValue ?? string.Empty;
                    }
                    
                    if (!string.IsNullOrEmpty(columnInfo.Label))
                    {
                        questionInfo.Columns.Add(columnInfo);
                    }
                }
            }

            // Extract additional properties for detailed comparison
            foreach (var property in question.EnumerateObject())
            {
                // Exclude basic properties but include important ones like 'question' for comparison
                if (!new[] { "id", "title", "text", "type", "description", "required", "options", "rows", "columns" }
                    .Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                {
                    try
                    {
                        questionInfo.Properties[property.Name] = property.Value.ToString();
                    }
                    catch
                    {
                        // Ignore properties that can't be converted to string
                    }
                }
            }

            // Only return questions that have some identifying information
            if (!string.IsNullOrEmpty(questionInfo.Title) || !string.IsNullOrEmpty(questionInfo.Id))
            {
                return questionInfo;
            }

            return null;
        }

        /// <summary>
        /// Get a deduplication key for questions (to avoid counting the same question multiple times)
        /// </summary>
        protected virtual string GetQuestionDeduplicationKey(DetailedQuestionInfo question)
        {
            // Use ID first, then temporaryId, then title as fallback
            if (!string.IsNullOrEmpty(question.Id))
                return question.Id.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(question.TemporaryId))
                return question.TemporaryId.Trim().ToLowerInvariant();
            return question.Title?.Trim().ToLowerInvariant() ?? string.Empty;
        }

        /// <summary>
        /// Get detailed question changes between before and after question lists
        /// </summary>
        protected virtual List<string> GetDetailedQuestionChanges(List<DetailedQuestionInfo> beforeQuestions, List<DetailedQuestionInfo> afterQuestions)
        {
            var changes = new List<string>();

            try
            {
                // Create dictionaries for efficient lookup
                var beforeDict = beforeQuestions.ToDictionary(q => GetQuestionKey(q), q => q);
                var afterDict = afterQuestions.ToDictionary(q => GetQuestionKey(q), q => q);

                // Find added questions
                var allAddedQuestions = afterDict.Keys.Except(beforeDict.Keys).ToList();
                if (allAddedQuestions.Any())
                {
                    // Show all added questions
                    var addedInfo = allAddedQuestions.Select(k => GetFormattedQuestionInfo(afterDict[k])).Where(t => !string.IsNullOrEmpty(t));
                    
                    if (allAddedQuestions.Count == 1)
                    {
                        changes.Add($"added question: {string.Join("; ", addedInfo)}");
                    }
                    else
                    {
                        changes.Add($"added questions: {string.Join("; ", addedInfo)}");
                    }
                }

                // Find removed questions
                var removedQuestions = beforeDict.Keys.Except(afterDict.Keys).ToList();
                if (removedQuestions.Any())
                {
                    var removedInfo = removedQuestions.Select(k => GetFormattedQuestionInfo(beforeDict[k])).Where(t => !string.IsNullOrEmpty(t));
                    if (removedQuestions.Count == 1)
                        changes.Add($"removed question: {string.Join("; ", removedInfo)}");
                    else
                        changes.Add($"removed questions: {string.Join("; ", removedInfo)}");
                }

                // Find modified questions
                var modifiedQuestions = new List<string>();
                foreach (var key in beforeDict.Keys.Intersect(afterDict.Keys))
                {
                    var beforeQ = beforeDict[key];
                    var afterQ = afterDict[key];

                    var questionChanges = GetQuestionSpecificChanges(beforeQ, afterQ);
                    if (questionChanges.Any())
                    {
                        var title = GetDisplayTitle(afterQ);
                        if (!string.IsNullOrEmpty(title))
                        {
                            modifiedQuestions.Add($"'{title}' ({string.Join(", ", questionChanges)})");
                        }
                    }
                }

                if (modifiedQuestions.Any())
                {
                    changes.Add($"modified questions: {string.Join(", ", modifiedQuestions)}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze detailed question changes");
                changes.Add("questions modified");
            }

            return changes;
        }

        /// <summary>
        /// Get a unique key for question identification
        /// </summary>
        protected virtual string GetQuestionKey(DetailedQuestionInfo question)
        {
            // Use title as primary key for consistency with deduplication logic
            return GetQuestionDeduplicationKey(question);
        }

        /// <summary>
        /// Get display title for a question
        /// </summary>
        protected virtual string GetDisplayTitle(DetailedQuestionInfo question)
        {
            if (!string.IsNullOrEmpty(question.Title))
                return question.Title.Length > 30 ? question.Title.Substring(0, 30) + "..." : question.Title;

            if (!string.IsNullOrEmpty(question.Id))
                return $"Question {question.Id}";

            return "Unknown Question";
        }

        /// <summary>
        /// Get formatted question info with type and options
        /// </summary>
        protected virtual string GetFormattedQuestionInfo(DetailedQuestionInfo question)
        {
            var title = GetDisplayTitle(question);
            var parts = new List<string> { $"'{title}'" };

            // Add question type
            if (!string.IsNullOrEmpty(question.Type))
            {
                parts.Add($"type: {question.Type}");
            }

            // Add description if available
            if (!string.IsNullOrEmpty(question.Description))
            {
                parts.Add($"description: '{question.Description}'");
            }

            // For grid type questions, show rows and columns instead of options
            var isGridType = question.Type != null && 
                (question.Type.Equals("multiple_choice_grid", StringComparison.OrdinalIgnoreCase) ||
                 question.Type.Equals("checkbox_grid", StringComparison.OrdinalIgnoreCase) ||
                 question.Type.Equals("short_answer_grid", StringComparison.OrdinalIgnoreCase));

            if (isGridType)
            {
                // Add rows if available
                if (question.Rows != null && question.Rows.Count > 0)
                {
                    var rowTexts = question.Rows.Select(r => r.GetDisplayText()).Where(t => !string.IsNullOrEmpty(t)).ToList();
                    if (rowTexts.Any())
                    {
                        var rowsStr = string.Join(", ", rowTexts.Select(r => $"'{r}'"));
                        parts.Add($"rows: {rowsStr}");
                    }
                }

                // Add columns if available
                if (question.Columns != null && question.Columns.Count > 0)
                {
                    var columnTexts = question.Columns.Select(c => c.GetDisplayText()).Where(t => !string.IsNullOrEmpty(t)).ToList();
                    if (columnTexts.Any())
                    {
                        var columnsStr = string.Join(", ", columnTexts.Select(c => $"'{c}'"));
                        parts.Add($"columns: {columnsStr}");
                    }
                }
            }
            else
            {
                // Add options if available (for non-grid questions)
                if (question.OptionDetails != null && question.OptionDetails.Count > 0)
                {
                    var optionTexts = question.OptionDetails.Select(o => o.GetDisplayText()).Where(t => !string.IsNullOrEmpty(t)).ToList();
                    if (optionTexts.Any())
                    {
                        var optionsStr = string.Join(", ", optionTexts.Select(o => $"'{o}'"));
                        parts.Add($"options: {optionsStr}");
                    }
                }
                else if (question.Options != null && question.Options.Count > 0)
                {
                    var optionsStr = string.Join(", ", question.Options.Select(o => $"'{o}'"));
                    parts.Add($"options: {optionsStr}");
                }
            }

            return string.Join(", ", parts);
        }

        /// <summary>
        /// Get detailed option changes between before and after option lists
        /// </summary>
        protected virtual List<string> GetDetailedOptionChanges(List<OptionInfo> beforeOptions, List<OptionInfo> afterOptions)
        {
            var changes = new List<string>();
            
            try
            {
                // Find added options
                var addedOptions = afterOptions.Except(beforeOptions).ToList();
                if (addedOptions.Any())
                {
                    var addedTexts = addedOptions.Select(o => $"'{o.GetDisplayText()}'").ToList();
                    if (addedOptions.Count == 1)
                        changes.Add($"added option: {addedTexts[0]}");
                    else
                        changes.Add($"added options: {string.Join(", ", addedTexts)}");
                }

                // Find removed options
                var removedOptions = beforeOptions.Except(afterOptions).ToList();
                if (removedOptions.Any())
                {
                    var removedTexts = removedOptions.Select(o => $"'{o.GetDisplayText()}'").ToList();
                    if (removedOptions.Count == 1)
                        changes.Add($"removed option: {removedTexts[0]}");
                    else
                        changes.Add($"removed options: {string.Join(", ", removedTexts)}");
                }

                // Find modified options (label or value changed)
                var modifiedOptions = new List<string>();
                foreach (var beforeOption in beforeOptions)
                {
                    var afterOption = afterOptions.FirstOrDefault(o => o.Equals(beforeOption));
                    if (afterOption != null && beforeOption.GetDisplayText() != afterOption.GetDisplayText())
                    {
                        modifiedOptions.Add($"'{beforeOption.GetDisplayText()}' → '{afterOption.GetDisplayText()}'");
                    }
                }
                
                if (modifiedOptions.Any())
                {
                    if (modifiedOptions.Count == 1)
                        changes.Add($"modified option: {modifiedOptions[0]}");
                    else
                        changes.Add($"modified options: {string.Join(", ", modifiedOptions)}");
                }

                // If no detailed changes found but count changed, report count change
                if (!changes.Any() && beforeOptions.Count != afterOptions.Count)
                {
                    changes.Add($"options: {beforeOptions.Count} → {afterOptions.Count}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze detailed option changes");
                if (beforeOptions.Count != afterOptions.Count)
                {
                    changes.Add($"options: {beforeOptions.Count} → {afterOptions.Count}");
                }
            }

            return changes;
        }

        /// <summary>
        /// Get detailed row/column changes between before and after lists
        /// </summary>
        protected virtual List<string> GetDetailedRowColumnChanges(List<OptionInfo> beforeItems, List<OptionInfo> afterItems, string itemType)
        {
            var changes = new List<string>();
            
            try
            {
                // Find added items
                var addedItems = afterItems.Except(beforeItems).ToList();
                if (addedItems.Any())
                {
                    var addedTexts = addedItems.Select(o => $"'{o.GetDisplayText()}'").ToList();
                    if (addedItems.Count == 1)
                        changes.Add($"added {itemType}: {addedTexts[0]}");
                    else
                        changes.Add($"added {itemType}s: {string.Join(", ", addedTexts)}");
                }

                // Find removed items
                var removedItems = beforeItems.Except(afterItems).ToList();
                if (removedItems.Any())
                {
                    var removedTexts = removedItems.Select(o => $"'{o.GetDisplayText()}'").ToList();
                    if (removedItems.Count == 1)
                        changes.Add($"removed {itemType}: {removedTexts[0]}");
                    else
                        changes.Add($"removed {itemType}s: {string.Join(", ", removedTexts)}");
                }

                // Find modified items (label or value changed)
                var modifiedItems = new List<string>();
                foreach (var beforeItem in beforeItems)
                {
                    var afterItem = afterItems.FirstOrDefault(o => o.Equals(beforeItem));
                    if (afterItem != null && beforeItem.GetDisplayText() != afterItem.GetDisplayText())
                    {
                        modifiedItems.Add($"'{beforeItem.GetDisplayText()}' → '{afterItem.GetDisplayText()}'");
                    }
                }
                
                if (modifiedItems.Any())
                {
                    if (modifiedItems.Count == 1)
                        changes.Add($"modified {itemType}: {modifiedItems[0]}");
                    else
                        changes.Add($"modified {itemType}s: {string.Join(", ", modifiedItems)}");
                }

                // If no detailed changes found but count changed, report count change
                if (!changes.Any() && beforeItems.Count != afterItems.Count)
                {
                    changes.Add($"{itemType}s: {beforeItems.Count} → {afterItems.Count}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze detailed {ItemType} changes", itemType);
                if (beforeItems.Count != afterItems.Count)
                {
                    changes.Add($"{itemType}s: {beforeItems.Count} → {afterItems.Count}");
                }
            }

            return changes;
        }

        /// <summary>
        /// Get specific changes within a question
        /// </summary>
        protected virtual List<string> GetQuestionSpecificChanges(DetailedQuestionInfo before, DetailedQuestionInfo after)
        {
            var changes = new List<string>();

            // Check title changes
            if (before.Title != after.Title)
            {
                changes.Add("title changed");
            }

            // Check type changes
            if (before.Type != after.Type)
            {
                changes.Add($"type: {before.Type} → {after.Type}");
            }

            // Check required status changes
            if (before.Required != after.Required)
            {
                changes.Add(after.Required ? "made required" : "made optional");
            }

            // Check description changes
            if (before.Description != after.Description)
            {
                var beforeDesc = string.IsNullOrEmpty(before.Description) ? "[empty]" : $"'{before.Description}'";
                var afterDesc = string.IsNullOrEmpty(after.Description) ? "[empty]" : $"'{after.Description}'";
                
                if (string.IsNullOrEmpty(before.Description))
                {
                    changes.Add($"description added: {afterDesc}");
                }
                else if (string.IsNullOrEmpty(after.Description))
                {
                    changes.Add($"description removed: {beforeDesc}");
                }
                else
                {
                    changes.Add($"description changed: {beforeDesc} → {afterDesc}");
                }
            }

            // Check options changes for multiple choice questions with detailed information
            if (before.OptionDetails.Count > 0 || after.OptionDetails.Count > 0)
            {
                var optionChanges = GetDetailedOptionChanges(before.OptionDetails, after.OptionDetails);
                if (optionChanges.Any())
                {
                    changes.AddRange(optionChanges);
                }
            }
            else if (before.Options.Count != after.Options.Count)
            {
                changes.Add($"options: {before.Options.Count} → {after.Options.Count}");
            }
            else if (!before.Options.SequenceEqual(after.Options))
            {
                changes.Add("options modified");
            }

            // Check rows changes for grid type questions
            if (before.Rows.Count > 0 || after.Rows.Count > 0)
            {
                var rowChanges = GetDetailedRowColumnChanges(before.Rows, after.Rows, "row");
                if (rowChanges.Any())
                {
                    changes.AddRange(rowChanges);
                }
            }

            // Check columns changes for grid type questions
            if (before.Columns.Count > 0 || after.Columns.Count > 0)
            {
                var columnChanges = GetDetailedRowColumnChanges(before.Columns, after.Columns, "column");
                if (columnChanges.Any())
                {
                    changes.AddRange(columnChanges);
                }
            }

            // Check specific important property changes
            if (before.Properties.TryGetValue("question", out var beforeQuestion) &&
                after.Properties.TryGetValue("question", out var afterQuestion) &&
                beforeQuestion?.ToString() != afterQuestion?.ToString())
            {
                changes.Add($"question text: '{beforeQuestion}' → '{afterQuestion}'");
            }

            // Check other property changes (excluding already checked properties)
            var importantProps = new[] { "question", "rows", "columns", "max", "min", "iconType" };
            var beforeProps = before.Properties.Keys.Where(k => !importantProps.Contains(k)).ToHashSet();
            var afterProps = after.Properties.Keys.Where(k => !importantProps.Contains(k)).ToHashSet();

            var addedProps = afterProps.Except(beforeProps).Count();
            var removedProps = beforeProps.Except(afterProps).Count();
            var modifiedProps = beforeProps.Intersect(afterProps)
                .Where(prop => before.Properties[prop]?.ToString() != after.Properties[prop]?.ToString())
                .Count();

            // Check important properties for changes
            var importantChanges = new List<string>();
            foreach (var prop in importantProps.Where(p => p != "question"))
            {
                if (before.Properties.TryGetValue(prop, out var beforeVal) &&
                    after.Properties.TryGetValue(prop, out var afterVal) &&
                    beforeVal?.ToString() != afterVal?.ToString())
                {
                    importantChanges.Add(prop);
                }
            }

            if (importantChanges.Any())
            {
                changes.Add($"properties changed: {string.Join(", ", importantChanges)}");
            }
            else if (addedProps > 0 || removedProps > 0 || modifiedProps > 0)
            {
                changes.Add("properties changed");
            }

            return changes;
        }

        #endregion

        #region Team Permission Helper Methods

        /// <summary>
        /// Parse team list from JSON string (handles double-encoded JSON)
        /// </summary>
        protected virtual List<string> ParseTeamList(string teamsJson)
        {
            if (string.IsNullOrWhiteSpace(teamsJson))
            {
                return new List<string>();
            }

            try
            {
                var trimmedData = teamsJson.Trim();

                // Handle double-encoded JSON string (e.g., "\"[\\\"123\\\",\\\"456\\\"]\"")
                // First, try to deserialize as a JSON string to get the actual JSON array string
                if (trimmedData.StartsWith("\"") && trimmedData.EndsWith("\""))
                {
                    try
                    {
                        var unescapedJson = JsonSerializer.Deserialize<string>(trimmedData);
                        if (!string.IsNullOrWhiteSpace(unescapedJson))
                        {
                            trimmedData = unescapedJson;
                            _logger.LogDebug("Unescaped double-encoded team JSON: {UnescapedJson}", trimmedData);
                        }
                    }
                    catch
                    {
                        // If deserialization fails, continue with original data
                        _logger.LogDebug("Failed to unescape as double-encoded JSON, using original data");
                    }
                }

                // Try to parse as JSON array
                if (trimmedData.StartsWith("["))
                {
                    var teams = JsonSerializer.Deserialize<List<string>>(trimmedData);
                    if (teams != null)
                    {
                        _logger.LogDebug("Successfully parsed {Count} teams from JSON array", teams.Count);
                        return teams;
                    }
                }

                // Fallback: treat as comma-separated string
                var teamList = trimmedData.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();

                _logger.LogDebug("Parsed {Count} teams from comma-separated string", teamList.Count);
                return teamList;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse team list: {TeamsJson}", teamsJson);
                return new List<string>();
            }
        }

        /// <summary>
        /// Get team changes description (async version with team name resolution)
        /// </summary>
        protected virtual async Task<string> GetTeamChangesAsync(List<string> beforeTeams, List<string> afterTeams, string permissionType)
        {
            var changes = new List<string>();

            var addedTeams = afterTeams.Except(beforeTeams).ToList();
            var removedTeams = beforeTeams.Except(afterTeams).ToList();

            // Get team names for all changed teams
            var allChangedTeams = addedTeams.Concat(removedTeams).Distinct().ToList();
            var teamNameMap = new Dictionary<string, string>();

            if (allChangedTeams.Any())
            {
                try
                {
                    // Get tenant ID from UserContext (works in background tasks)
                    var tenantId = _userContext?.TenantId ?? "999";
                    _logger.LogDebug("Using TenantId: {TenantId} for fetching team names", tenantId);

                    teamNameMap = await _userService.GetTeamNamesByIdsAsync(allChangedTeams, tenantId);
                    _logger.LogDebug("Fetched {Count} team names for team change details", teamNameMap.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch team names for team change details. Using IDs instead.");
                }
            }

            if (addedTeams.Any())
            {
                var addedNames = addedTeams
                    .Select(id => teamNameMap.GetValueOrDefault(id, id))
                    .ToList();

                if (addedNames.Count == 1)
                {
                    changes.Add($"added {addedNames[0]} to {permissionType} teams");
                }
                else
                {
                    changes.Add($"added {string.Join(", ", addedNames)} to {permissionType} teams");
                }
            }

            if (removedTeams.Any())
            {
                var removedNames = removedTeams
                    .Select(id => teamNameMap.GetValueOrDefault(id, id))
                    .ToList();

                if (removedNames.Count == 1)
                {
                    changes.Add($"removed {removedNames[0]} from {permissionType} teams");
                }
                else
                {
                    changes.Add($"removed {string.Join(", ", removedNames)} from {permissionType} teams");
                }
            }

            return string.Join(", ", changes);
        }

        #endregion
    }
}