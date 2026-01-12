namespace FlowFlex.Application.Contracts.Dtos.OW.EmailBinding;

/// <summary>
/// Email binding status DTO
/// </summary>
public class EmailBindingDto
{
    public long Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string SyncStatus { get; set; } = string.Empty;
    public DateTimeOffset? LastSyncTime { get; set; }
    public string? LastSyncError { get; set; }
    public bool AutoSyncEnabled { get; set; }
    public int SyncIntervalMinutes { get; set; }
    public bool IsTokenValid { get; set; }
    public DateTimeOffset? TokenExpireTime { get; set; }
}

/// <summary>
/// OAuth authorization URL response
/// </summary>
public class AuthorizeUrlDto
{
    public string AuthorizationUrl { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

/// <summary>
/// OAuth callback request
/// </summary>
public class OAuthCallbackDto
{
    public string Code { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string? Error { get; set; }
    public string? ErrorDescription { get; set; }
}

/// <summary>
/// Update email binding settings
/// </summary>
public class EmailBindingUpdateDto
{
    public bool? AutoSyncEnabled { get; set; }
    public int? SyncIntervalMinutes { get; set; }
}

/// <summary>
/// Sync result DTO
/// </summary>
public class SyncResultDto
{
    public int SyncedCount { get; set; }
    public DateTimeOffset SyncTime { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Full sync request DTO
/// </summary>
public class FullSyncRequestDto
{
    /// <summary>
    /// Maximum number of emails to sync (default: 500, max: 2000)
    /// </summary>
    public int MaxCount { get; set; } = 500;

    /// <summary>
    /// Folders to sync (default: inbox, sentitems)
    /// Available: inbox, sentitems, drafts, deleteditems
    /// </summary>
    public List<string>? Folders { get; set; }

    /// <summary>
    /// Whether to clear existing emails before sync (default: false)
    /// </summary>
    public bool ClearExisting { get; set; } = false;
}

/// <summary>
/// Full sync result DTO
/// </summary>
public class FullSyncResultDto
{
    public int TotalSyncedCount { get; set; }
    public Dictionary<string, int> SyncedCountByFolder { get; set; } = new();
    public DateTimeOffset SyncTime { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsComplete { get; set; }
}

/// <summary>
/// Incremental sync request DTO
/// </summary>
public class IncrementalSyncRequestDto
{
    /// <summary>
    /// Folders to sync (default: inbox)
    /// </summary>
    public List<string>? Folders { get; set; }

    /// <summary>
    /// Maximum number of recent emails to check per folder (default: 100)
    /// </summary>
    public int MaxCount { get; set; } = 100;
}
