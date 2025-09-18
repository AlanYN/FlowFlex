using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Options
{
    /// <summary>
    /// Cache configuration options
    /// </summary>
    public class CacheOptions
    {
        public const string SectionName = "Cache";

        /// <summary>
        /// Enable or disable caching globally
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Default cache expiry time in minutes
        /// </summary>
        [Range(1, 1440)]
        public int DefaultExpiryMinutes { get; set; } = 10;

        /// <summary>
        /// Enable graceful degradation when cache fails
        /// </summary>
        public bool EnableGracefulDegradation { get; set; } = true;

        /// <summary>
        /// Log cache operations for debugging
        /// </summary>
        public bool EnableDebugLogging { get; set; } = false;

        /// <summary>
        /// Maximum retry attempts for cache operations
        /// </summary>
        [Range(0, 5)]
        public int MaxRetryAttempts { get; set; } = 0;

        /// <summary>
        /// Retry delay in milliseconds
        /// </summary>
        [Range(100, 5000)]
        public int RetryDelayMs { get; set; } = 1000;
    }
}