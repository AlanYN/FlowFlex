namespace Domain.Shared.Enums;

/// <summary>
/// Data sync status
/// </summary>
public enum SyncStatus
{
    /// <summary>
    /// Sync completed successfully
    /// </summary>
    Success = 0,

    /// <summary>
    /// Sync failed
    /// </summary>
    Failed = 1,

    /// <summary>
    /// Sync is pending/in progress
    /// </summary>
    Pending = 2,

    /// <summary>
    /// Sync is in progress
    /// </summary>
    InProgress = 3,

    /// <summary>
    /// Partial success (some fields synced, some failed)
    /// </summary>
    PartialSuccess = 4
}

