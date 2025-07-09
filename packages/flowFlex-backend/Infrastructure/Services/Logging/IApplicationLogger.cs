using Microsoft.Extensions.Logging;

namespace FlowFlex.Infrastructure.Services.Logging
{
    /// <summary>
    /// Unified application logging interface
    /// </summary>
    public interface IApplicationLogger
    {
        /// <summary>
        /// Log information message
        /// </summary>
        void LogInformation(string message, params object[] args);

        /// <summary>
        /// 记录错误日志
        /// </summary>
        void LogError(Exception ex, string message, params object[] args);

        /// <summary>
        /// 记录警告日志
        /// </summary>
        void LogWarning(string message, params object[] args);

        /// <summary>
        /// 记录调试日志
        /// </summary>
        void LogDebug(string message, params object[] args);

        /// <summary>
        /// 记录跟踪日志
        /// </summary>
        void LogTrace(string message, params object[] args);

        /// <summary>
        /// 记录业务操作日志
        /// </summary>
        void LogBusinessOperation(string operationType, string entityType, object entityId, string message, params object[] args);

        /// <summary>
        /// 记录性能日志
        /// </summary>
        void LogPerformance(string operationName, long elapsedMilliseconds, string additionalInfo = null);

        /// <summary>
        /// 记录安全相关日志
        /// </summary>
        void LogSecurity(string eventType, string userId, string message, params object[] args);
    }
}