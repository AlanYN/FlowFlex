using System;

namespace FlowFlex.Domain.Shared.Models
{
    /// <summary>
    /// Application context model for storing app and tenant information
    /// </summary>
    public class AppContext
    {
        /// <summary>
        /// Application code for application isolation
        /// </summary>
        public string AppCode { get; set; } = "DEFAULT";

        /// <summary>
        /// Tenant ID for tenant isolation
        /// </summary>
        public string TenantId { get; set; } = "DEFAULT";

        /// <summary>
        /// Request ID for tracing
        /// </summary>
        public string RequestId { get; set; } = Guid.NewGuid().ToString("N")[..8];

        /// <summary>
        /// Request timestamp
        /// </summary>
        public DateTimeOffset RequestTime { get; set; } = DateTimeOffset.Now;

        /// <summary>
        /// Client IP address
        /// </summary>
        public string ClientIp { get; set; } = string.Empty;

        /// <summary>
        /// User agent
        /// </summary>
        public string UserAgent { get; set; } = string.Empty;

        /// <summary>
        /// Check if context is valid
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(AppCode) && !string.IsNullOrEmpty(TenantId);
        }

        /// <summary>
        /// Get combined isolation key
        /// </summary>
        public string GetIsolationKey()
        {
            return $"{AppCode}:{TenantId}";
        }
    }
} 