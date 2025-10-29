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

        protected BaseOperationLogService(
            IOperationChangeLogRepository operationChangeLogRepository,
            ILogger logger,
            UserContext userContext,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ILogCacheService logCacheService,
            IUserService userService)
        {
            _operationChangeLogRepository = operationChangeLogRepository;
            _logger = logger;
            _userContext = userContext;
            _httpContextAccessor = httpContextAccessor;
            _mapper = mapper;
            _logCacheService = logCacheService;
            _userService = userService;
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
        /// Get operator display name
        /// </summary>
        protected virtual string GetOperatorDisplayName()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            var user = httpContext?.User;
            
            // Check if this is a Portal token (has scope=portal claim)
            var scope = user?.Claims.FirstOrDefault(c => c.Type == "scope")?.Value;
            var tokenType = user?.Claims.FirstOrDefault(c => c.Type == "token_type")?.Value;
            bool isPortalToken = scope == "portal" && tokenType == "portal-access";
            
            if (isPortalToken)
            {
                // For Portal tokens, prioritize email/username claims over other claims
                // to ensure we get the user's email address, not their ID
                string[] portalClaimTypes = new[]
                {
                    System.Security.Claims.ClaimTypes.Email,
                    "email",
                    "username",
                    System.Security.Claims.ClaimTypes.NameIdentifier
                };
                
                foreach (var ct in portalClaimTypes)
                {
                    var v = user?.Claims.FirstOrDefault(c => c.Type == ct)?.Value;
                    if (!string.IsNullOrWhiteSpace(v))
                    {
                        // Validate that it's an email address, not an ID
                        if (v.Contains("@"))
                        {
                            return v;
                        }
                    }
                }
            }
            
            // For ItemIAM and IdentityHub tokens, use UserContext
            if (!string.IsNullOrWhiteSpace(_userContext?.UserName))
            {
                return _userContext.UserName;
            }
            
            // Fallback to UserContext.Email
            if (!string.IsNullOrWhiteSpace(_userContext?.Email))
            {
                return _userContext.Email;
            }
            
            return "System";
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
                var changeDetails = await GetChangeDetailsAsync(beforeData, afterData, changedFields);
                if (!string.IsNullOrEmpty(changeDetails))
                {
                    description += $". {changeDetails}";
                }
            }
            else if (changedFields?.Any() == true)
            {
                // Fallback to field names if no before/after data
                description += $". Changed fields: {string.Join(", ", changedFields)}";
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
        protected virtual async Task<string> GetChangeDetailsAsync(string beforeData, string afterData, List<string> changedFields)
        {
            try
            {
                var beforeJson = JsonSerializer.Deserialize<Dictionary<string, object>>(beforeData);
                var afterJson = JsonSerializer.Deserialize<Dictionary<string, object>>(afterData);

                var changeList = new List<string>();

                foreach (var field in changedFields.Take(3)) // Limit to first 3 changes to avoid overly long descriptions
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
                            var beforeStr = beforeValue?.ToString() ?? "Public";
                            var afterStr = afterValue?.ToString() ?? "Public";
                            changeList.Add($"view permission mode from {beforeStr} to {afterStr}");
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
                    if (changedFields.Count > 3)
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
                        .Where(name => !string.IsNullOrEmpty(name))
                        .Take(3)
                        .ToList();

                    // Count total questions across all sections
                    var totalQuestions = sections
                        .Where(s => s.TryGetProperty("questions", out var questionsElement) && questionsElement.ValueKind == JsonValueKind.Array)
                        .Sum(s => s.GetProperty("questions").EnumerateArray().Count());

                    if (sectionNames.Any())
                    {
                        var summary = $"{sections.Count} sections";
                        if (sectionNames.Count <= 3)
                        {
                            summary += $": {string.Join(", ", sectionNames.Select(n => $"'{n}'"))}";
                        }
                        else
                        {
                            summary += $": '{sectionNames[0]}', '{sectionNames[1]}', '{sectionNames[2]}' and {sections.Count - 3} more";
                        }

                        if (totalQuestions > 0)
                        {
                            summary += $" ({totalQuestions} questions)";
                        }

                        return summary;
                    }

                    var result = $"{sections.Count} sections";
                    if (totalQuestions > 0)
                    {
                        result += $" ({totalQuestions} questions)";
                    }
                    return result;
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

                    // Section name changes
                    var beforeSectionNames = beforeSectionsList
                        .Where(s => s.TryGetProperty("name", out var _))
                        .Select(s => s.GetProperty("name").GetString())
                        .Where(name => !string.IsNullOrEmpty(name))
                        .ToList();

                    var afterSectionNames = afterSectionsList
                        .Where(s => s.TryGetProperty("name", out var _))
                        .Select(s => s.GetProperty("name").GetString())
                        .Where(name => !string.IsNullOrEmpty(name))
                        .ToList();

                    // Find added sections
                    var addedSections = afterSectionNames.Except(beforeSectionNames).Take(2).ToList();
                    if (addedSections.Any())
                    {
                        changes.Add($"added sections: {string.Join(", ", addedSections.Select(n => $"'{n}'"))}");
                    }

                    // Find removed sections
                    var removedSections = beforeSectionNames.Except(afterSectionNames).Take(2).ToList();
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
                    var formattedChanges = changes.Take(5).ToList();
                    if (formattedChanges.Count == 1)
                    {
                        return $"Structure modified: {formattedChanges[0]}";
                    }
                    else
                    {
                        return $"Structure modified: {string.Join("; ", formattedChanges)}";
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
            public string Title { get; set; }
            public string Type { get; set; }
            public string Description { get; set; }
            public bool Required { get; set; }
            public List<string> Options { get; set; } = new List<string>();
            public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

            public override bool Equals(object obj)
            {
                if (obj is DetailedQuestionInfo other)
                {
                    return Id == other.Id || (string.IsNullOrEmpty(Id) && string.IsNullOrEmpty(other.Id) && Title == other.Title);
                }
                return false;
            }

            public override int GetHashCode()
            {
                return string.IsNullOrEmpty(Id) ? Title?.GetHashCode() ?? 0 : Id.GetHashCode();
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
            string extendedData = null)
        {
            try
            {
                var operationTitle = $"{businessModule} {operationAction}: {entityName}";

                // Use enhanced description method that can handle beforeData and afterData
                var operationDescription = await BuildEnhancedOperationDescriptionAsync(
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
                questionInfo.Options = optionsElement.EnumerateArray()
                    .Where(o => o.ValueKind == JsonValueKind.String)
                    .Select(o => o.GetString())
                    .Where(o => !string.IsNullOrEmpty(o))
                    .ToList();
            }

            // Extract additional properties for detailed comparison
            foreach (var property in question.EnumerateObject())
            {
                // Exclude basic properties but include important ones like 'question' for comparison
                if (!new[] { "id", "title", "text", "type", "description", "required", "options" }
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
            // Use title as primary key, fall back to ID if title is empty
            var key = !string.IsNullOrEmpty(question.Title) ? question.Title.Trim() : question.Id?.Trim();
            return key?.ToLowerInvariant() ?? string.Empty;
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
                var addedQuestions = afterDict.Keys.Except(beforeDict.Keys).Take(2).ToList();
                if (addedQuestions.Any())
                {
                    var addedTitles = addedQuestions.Select(k => GetDisplayTitle(afterDict[k])).Where(t => !string.IsNullOrEmpty(t));
                    changes.Add($"added questions: {string.Join(", ", addedTitles.Select(t => $"'{t}'"))}");
                }

                // Find removed questions
                var removedQuestions = beforeDict.Keys.Except(afterDict.Keys).Take(2).ToList();
                if (removedQuestions.Any())
                {
                    var removedTitles = removedQuestions.Select(k => GetDisplayTitle(beforeDict[k])).Where(t => !string.IsNullOrEmpty(t));
                    changes.Add($"removed questions: {string.Join(", ", removedTitles.Select(t => $"'{t}'"))}");
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

                    if (modifiedQuestions.Count >= 2) break; // Limit to 2 modified questions for readability
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
                changes.Add("description changed");
            }

            // Check options changes for multiple choice questions
            if (before.Options.Count != after.Options.Count)
            {
                changes.Add($"options: {before.Options.Count} → {after.Options.Count}");
            }
            else if (!before.Options.SequenceEqual(after.Options))
            {
                changes.Add("options modified");
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