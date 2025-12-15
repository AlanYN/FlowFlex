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
