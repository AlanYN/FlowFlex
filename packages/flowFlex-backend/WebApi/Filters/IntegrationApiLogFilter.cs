using FlowFlex.Application.Services.Integration;
using FlowFlex.Domain.Repository.Integration;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;

namespace FlowFlex.WebApi.Filters;

/// <summary>
/// Action filter to log external integration API calls
/// </summary>
public class IntegrationApiLogFilter : IAsyncActionFilter
{
    private readonly IIntegrationApiLogService _logService;
    private readonly IEntityMappingRepository _entityMappingRepository;
    private readonly IIntegrationRepository _integrationRepository;
    private readonly ILogger<IntegrationApiLogFilter> _logger;

    public IntegrationApiLogFilter(
        IIntegrationApiLogService logService,
        IEntityMappingRepository entityMappingRepository,
        IIntegrationRepository integrationRepository,
        ILogger<IntegrationApiLogFilter> logger)
    {
        _logService = logService;
        _entityMappingRepository = entityMappingRepository;
        _integrationRepository = integrationRepository;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var startTime = DateTimeOffset.UtcNow;
        long logId = 0;
        long integrationId = 0;
        string systemId = string.Empty;

        try
        {
            // Try to get integration ID from systemId or systemName
            (integrationId, systemId) = await GetIntegrationInfoAsync(context);
            
            if (integrationId > 0)
            {
                // Get endpoint and method
                var endpoint = context.HttpContext.Request.Path.Value ?? string.Empty;
                var httpMethod = context.HttpContext.Request.Method;
                var callerIp = GetClientIp(context.HttpContext);
                var requestParams = GetRequestParams(context);

                // Start logging
                logId = await _logService.StartLogAsync(
                    integrationId,
                    systemId,
                    endpoint,
                    httpMethod,
                    requestParams,
                    callerIp);
                
                _logger.LogDebug("Started API log {LogId} for integration {IntegrationId}", logId, integrationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start API logging");
        }

        // Execute the action
        var resultContext = await next();

        // Complete logging
        if (logId > 0)
        {
            try
            {
                var statusCode = resultContext.HttpContext.Response.StatusCode;
                var isSuccess = statusCode >= 200 && statusCode < 300;
                string? errorMessage = null;

                if (resultContext.Exception != null)
                {
                    isSuccess = false;
                    errorMessage = resultContext.Exception.Message;
                    statusCode = 500;
                }

                await _logService.CompleteLogAsync(logId, statusCode, isSuccess, errorMessage);
                _logger.LogDebug("Completed API log {LogId} with status {StatusCode}", logId, statusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to complete API logging");
            }
        }
    }

    /// <summary>
    /// Get integration ID and system ID from request parameters
    /// </summary>
    private async Task<(long IntegrationId, string SystemId)> GetIntegrationInfoAsync(ActionExecutingContext context)
    {
        // Try systemId first
        var systemId = ExtractParameter(context, "systemId", "SystemId");
        if (!string.IsNullOrEmpty(systemId))
        {
            var entityMapping = await _entityMappingRepository.GetBySystemIdAsync(systemId);
            if (entityMapping != null)
            {
                return (entityMapping.IntegrationId, systemId);
            }
        }

        // Try systemName - look up integration by system name
        var systemName = ExtractParameter(context, "systemName", "SystemName");
        if (!string.IsNullOrEmpty(systemName))
        {
            var integration = await _integrationRepository.GetBySystemNameAsync(systemName);
            if (integration != null)
            {
                return (integration.Id, integration.SystemName ?? systemName);
            }
        }

        return (0, string.Empty);
    }


    /// <summary>
    /// Extract parameter from query string or action arguments
    /// </summary>
    private string ExtractParameter(ActionExecutingContext context, string lowerCaseName, string pascalCaseName)
    {
        // Try query parameters
        if (context.HttpContext.Request.Query.TryGetValue(pascalCaseName, out var valueFromQuery))
        {
            return valueFromQuery.ToString();
        }

        if (context.HttpContext.Request.Query.TryGetValue(lowerCaseName, out var valueFromQueryLower))
        {
            return valueFromQueryLower.ToString();
        }

        // Try action arguments
        if (context.ActionArguments.TryGetValue(lowerCaseName, out var argValue) && argValue != null)
        {
            return argValue.ToString() ?? string.Empty;
        }

        if (context.ActionArguments.TryGetValue(pascalCaseName, out var argValuePascal) && argValuePascal != null)
        {
            return argValuePascal.ToString() ?? string.Empty;
        }

        // Try request body for POST requests
        if (context.ActionArguments.TryGetValue("request", out var requestArg) && requestArg != null)
        {
            var prop = requestArg.GetType().GetProperty(pascalCaseName);
            if (prop != null)
            {
                var value = prop.GetValue(requestArg);
                if (value != null)
                {
                    return value.ToString() ?? string.Empty;
                }
            }
        }

        return string.Empty;
    }

    private string? GetClientIp(HttpContext context)
    {
        // Check X-Forwarded-For header first (for proxied requests)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').FirstOrDefault()?.Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    private string? GetRequestParams(ActionExecutingContext context)
    {
        try
        {
            var queryParams = context.HttpContext.Request.Query
                .ToDictionary(q => q.Key, q => q.Value.ToString());

            if (queryParams.Count > 0)
            {
                return JsonConvert.SerializeObject(queryParams);
            }
        }
        catch
        {
            // Ignore serialization errors
        }

        return null;
    }
}
