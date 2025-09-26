using System;

namespace FlowFlex.Application.Contracts.Options
{
    /// <summary>
    /// Configuration options for Stages Progress Sync Service
    /// Provides emergency controls and safety settings
    /// </summary>
    public class StagesProgressSyncOptions
    {
        /// <summary>
        /// Master switch to enable/disable all stages progress synchronization
        /// Set to false in emergency situations to prevent data loss
        /// Default: true
        /// </summary>
        public bool EnableSync { get; set; } = true;

        /// <summary>
        /// Enable synchronization after stage updates
        /// Default: true
        /// </summary>
        public bool EnableSyncAfterStageUpdate { get; set; } = true;

        /// <summary>
        /// Enable synchronization after stage deletion
        /// Default: true
        /// </summary>
        public bool EnableSyncAfterStageDelete { get; set; } = true;

        /// <summary>
        /// Enable synchronization after stage sorting/reordering
        /// Default: true
        /// </summary>
        public bool EnableSyncAfterStageSort { get; set; } = true;

        /// <summary>
        /// Enable data integrity validation before sync operations
        /// Default: true
        /// </summary>
        public bool EnableDataIntegrityValidation { get; set; } = true;

        /// <summary>
        /// Prevent clearing of existing stages progress data
        /// When true, operations that would result in empty stages progress are blocked
        /// Default: true
        /// </summary>
        public bool PreventDataClearing { get; set; } = true;

        /// <summary>
        /// Maximum number of onboardings to sync in a single batch operation
        /// Default: 100
        /// </summary>
        public int MaxBatchSize { get; set; } = 100;

        /// <summary>
        /// Timeout for sync operations in seconds
        /// Default: 300 (5 minutes)
        /// </summary>
        public int SyncTimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// Enable detailed logging for sync operations
        /// Default: false (to avoid log spam in production)
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;

        /// <summary>
        /// Emergency mode - when enabled, all sync operations are disabled
        /// This is a safety switch for critical situations
        /// Default: false
        /// </summary>
        public bool EmergencyMode { get; set; } = false;
    }
}
