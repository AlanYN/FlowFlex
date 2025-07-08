using Microsoft.Extensions.Logging;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Infrastructure.Services.Logging
{
    /// <summary>
    /// Unified application logging service implementation
    /// </summary>
    public class ApplicationLogger : IApplicationLogger
    {
        private readonly ILogger<ApplicationLogger> _logger;
        private readonly UserContext _userContext;

        public ApplicationLogger(ILogger<ApplicationLogger> logger, UserContext userContext)
        {
            _logger = logger;
            _userContext = userContext;
        }

        public void LogInformation(string message, params object[] args)
        {
            _logger.LogInformation(EnrichMessage(message), args);
        }

        public void LogError(Exception ex, string message, params object[] args)
        {
            _logger.LogError(ex, EnrichMessage(message), args);
        }

        public void LogWarning(string message, params object[] args)
        {
            _logger.LogWarning(EnrichMessage(message), args);
        }

        public void LogDebug(string message, params object[] args)
        {
            _logger.LogDebug(EnrichMessage(message), args);
        }

        public void LogTrace(string message, params object[] args)
        {
            _logger.LogTrace(EnrichMessage(message), args);
        }

        public void LogBusinessOperation(string operationType, string entityType, object entityId, string message, params object[] args)
        {
            var enrichedMessage = $"[BUSINESS] [{operationType}] [{entityType}:{entityId}] {EnrichMessage(message)}";
            _logger.LogInformation(enrichedMessage, args);
        }

        public void LogPerformance(string operationName, long elapsedMilliseconds, string additionalInfo = null)
        {
            var message = $"[PERFORMANCE] [{operationName}] Elapsed: {elapsedMilliseconds}ms";
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                message += $" - {additionalInfo}";
            }
            _logger.LogInformation(EnrichMessage(message));
        }

        public void LogSecurity(string eventType, string userId, string message, params object[] args)
        {
            var enrichedMessage = $"[SECURITY] [{eventType}] [User:{userId}] {EnrichMessage(message)}";
            _logger.LogWarning(enrichedMessage, args);
        }

        private string EnrichMessage(string message)
        {
            var tenantId = _userContext?.TenantId ?? "UNKNOWN";
            var userId = _userContext?.UserId ?? "UNKNOWN";
            return $"[Tenant:{tenantId}] [User:{userId}] {message}";
        }
    }
}