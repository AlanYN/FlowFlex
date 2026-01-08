using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Services.Integration;

/// <summary>
/// Service for logging external integration API calls
/// </summary>
public interface IIntegrationApiLogService
{
    /// <summary>
    /// Log an API call
    /// </summary>
    Task<long> LogApiCallAsync(
        long integrationId,
        string systemId,
        string endpoint,
        string httpMethod,
        DateTimeOffset startedAt,
        DateTimeOffset? completedAt,
        long? durationMs,
        int statusCode,
        bool isSuccess,
        string? errorMessage = null,
        string? requestParams = null,
        string? callerIp = null);

    /// <summary>
    /// Log API call start and return log ID
    /// </summary>
    Task<long> StartLogAsync(
        long integrationId,
        string systemId,
        string endpoint,
        string httpMethod,
        string? requestParams = null,
        string? callerIp = null);

    /// <summary>
    /// Complete an API call log
    /// </summary>
    Task CompleteLogAsync(
        long logId,
        int statusCode,
        bool isSuccess,
        string? errorMessage = null);
}

/// <summary>
/// Implementation of integration API log service
/// </summary>
public class IntegrationApiLogService : IIntegrationApiLogService, IScopedService
{
    private readonly IIntegrationApiLogRepository _repository;
    private readonly UserContext _userContext;
    private readonly ILogger<IntegrationApiLogService> _logger;

    public IntegrationApiLogService(
        IIntegrationApiLogRepository repository,
        UserContext userContext,
        ILogger<IntegrationApiLogService> logger)
    {
        _repository = repository;
        _userContext = userContext;
        _logger = logger;
    }

    /// <summary>
    /// Log an API call
    /// </summary>
    public async Task<long> LogApiCallAsync(
        long integrationId,
        string systemId,
        string endpoint,
        string httpMethod,
        DateTimeOffset startedAt,
        DateTimeOffset? completedAt,
        long? durationMs,
        int statusCode,
        bool isSuccess,
        string? errorMessage = null,
        string? requestParams = null,
        string? callerIp = null)
    {
        try
        {
            var log = new IntegrationApiLog
            {
                IntegrationId = integrationId,
                SystemId = systemId ?? string.Empty,
                Endpoint = endpoint,
                HttpMethod = httpMethod,
                StartedAt = startedAt,
                CompletedAt = completedAt,
                DurationMs = durationMs,
                StatusCode = statusCode,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                RequestParams = requestParams,
                CallerUserId = ParseUserId(_userContext.UserId),
                CallerIp = callerIp
            };

            log.InitCreateInfo(_userContext);
            log.TenantId = _userContext.TenantId;

            return await _repository.InsertLogAsync(log);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log API call for integration {IntegrationId}", integrationId);
            return 0;
        }
    }

    /// <summary>
    /// Log API call start and return log ID
    /// </summary>
    public async Task<long> StartLogAsync(
        long integrationId,
        string systemId,
        string endpoint,
        string httpMethod,
        string? requestParams = null,
        string? callerIp = null)
    {
        try
        {
            var log = new IntegrationApiLog
            {
                IntegrationId = integrationId,
                SystemId = systemId ?? string.Empty,
                Endpoint = endpoint,
                HttpMethod = httpMethod,
                StartedAt = DateTimeOffset.UtcNow,
                StatusCode = 0,
                IsSuccess = false,
                RequestParams = requestParams,
                CallerUserId = ParseUserId(_userContext.UserId),
                CallerIp = callerIp
            };

            log.InitCreateInfo(_userContext);
            log.TenantId = _userContext.TenantId;

            return await _repository.InsertLogAsync(log);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start API log for integration {IntegrationId}", integrationId);
            return 0;
        }
    }

    /// <summary>
    /// Complete an API call log
    /// </summary>
    public async Task CompleteLogAsync(
        long logId,
        int statusCode,
        bool isSuccess,
        string? errorMessage = null)
    {
        if (logId == 0) return;

        try
        {
            var log = await _repository.GetByIdAsync(logId);
            if (log == null) return;

            log.CompletedAt = DateTimeOffset.UtcNow;
            log.DurationMs = (long)(log.CompletedAt.Value - log.StartedAt).TotalMilliseconds;
            log.StatusCode = statusCode;
            log.IsSuccess = isSuccess;
            log.ErrorMessage = errorMessage;
            log.InitModifyInfo(_userContext);

            await _repository.UpdateAsync(log);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to complete API log {LogId}", logId);
        }
    }

    /// <summary>
    /// Parse user ID string to long
    /// </summary>
    private static long? ParseUserId(string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        return long.TryParse(userId, out var result) ? result : null;
    }
}
