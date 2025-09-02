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

                // Initialize unique snowflake ID
                operationLog.InitNewId();

                _logger.LogDebug("Attempting to insert operation log with ID {LogId} for {BusinessModule} {BusinessId}", 
                    operationLog.Id, businessModule, businessId);

                bool result = await InsertWithRetryAsync(operationLog);

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

        /// <summary>
        /// Build enhanced operation description with specific change values
        /// </summary>
        protected virtual string BuildEnhancedOperationDescription(
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
                var changeDetails = GetChangeDetails(beforeData, afterData, changedFields);
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
                description += $" with reason: {reason}";
            }

            return description;
        }

        /// <summary>
        /// Get specific change details from before and after data
        /// </summary>
        protected virtual string GetChangeDetails(string beforeData, string afterData, List<string> changedFields)
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
                                var beforeStr = GetDisplayValue(beforeValue, field);
                                var afterStr = GetDisplayValue(afterValue, field);
                                changeList.Add($"{field} from '{beforeStr}' to '{afterStr}'");
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
                                var assigneeChange = GetAssigneeChangeDetails(beforeJsonStr, afterJsonStr);
                                changeList.Add(assigneeChange);
                            }
                            else
                            {
                                var beforeStr = GetDisplayValue(beforeValue, field);
                                var afterStr = GetDisplayValue(afterValue, field);
                                changeList.Add($"{field} from '{beforeStr}' to '{afterStr}'");
                            }
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

            // Truncate very long non-JSON values
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
                    
                    // Compare questions count and specific question changes
                    var beforeQuestions = GetAllQuestions(beforeSectionsList);
                    var afterQuestions = GetAllQuestions(afterSectionsList);
                    
                    var beforeQuestionCount = beforeQuestions.Count;
                    var afterQuestionCount = afterQuestions.Count;
                    
                    if (beforeQuestionCount != afterQuestionCount)
                    {
                        changes.Add($"questions changed from {beforeQuestionCount} to {afterQuestionCount}");
                    }
                    
                    // Find specific question changes
                    var beforeQuestionTitles = beforeQuestions.Select(q => q.Title).Where(t => !string.IsNullOrEmpty(t)).ToHashSet();
                    var afterQuestionTitles = afterQuestions.Select(q => q.Title).Where(t => !string.IsNullOrEmpty(t)).ToHashSet();
                    
                    // Find added questions
                    var addedQuestions = afterQuestionTitles.Except(beforeQuestionTitles).Take(2).ToList();
                    if (addedQuestions.Any())
                    {
                        changes.Add($"added questions: {string.Join(", ", addedQuestions.Select(q => $"'{q}'"))}");
                    }
                    
                    // Find removed questions  
                    var removedQuestions = beforeQuestionTitles.Except(afterQuestionTitles).Take(2).ToList();
                    if (removedQuestions.Any())
                    {
                        changes.Add($"removed questions: {string.Join(", ", removedQuestions.Select(q => $"'{q}'"))}");
                    }
                }
                
                if (changes.Any())
                {
                    return $"Structure: {string.Join(", ", changes.Take(5))}";
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
        protected virtual string GetAssigneeChangeDetails(string beforeJson, string afterJson)
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
                            // Get user names - this is a fire-and-forget async operation wrapped in sync
                            var users = Task.Run(async () => await _userService.GetUsersByIdsAsync(changedUserIds)).Result;
                            userNameMap = users.ToDictionary(
                                u => u.Id, 
                                u => !string.IsNullOrEmpty(u.Username) ? u.Username : 
                                     (!string.IsNullOrEmpty(u.Email) ? u.Email : $"User_{u.Id}")
                            );
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
                            .Take(3) // Limit to first 3 names to avoid overly long descriptions
                            .ToList();
                        
                        if (addedNames.Any())
                        {
                            if (addedAssignees.Count == 1)
                            {
                                changes.Add($"added {addedNames[0]}");
                            }
                            else if (addedAssignees.Count <= 3)
                            {
                                changes.Add($"added {string.Join(", ", addedNames)}");
                            }
                            else
                            {
                                changes.Add($"added {string.Join(", ", addedNames)} and {addedAssignees.Count - 3} more");
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
                            .Take(3) // Limit to first 3 names
                            .ToList();
                        
                        if (removedNames.Any())
                        {
                            if (removedAssignees.Count == 1)
                            {
                                changes.Add($"removed {removedNames[0]}");
                            }
                            else if (removedAssignees.Count <= 3)
                            {
                                changes.Add($"removed {string.Join(", ", removedNames)}");
                            }
                            else
                            {
                                changes.Add($"removed {string.Join(", ", removedNames)} and {removedAssignees.Count - 3} more");
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

        #endregion
    }
}