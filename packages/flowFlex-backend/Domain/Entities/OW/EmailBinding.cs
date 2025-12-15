using System.ComponentModel.DataAnnotations;
using SqlSugar;

namespace FlowFlex.Domain.Entities.OW;

/// <summary>
/// Email Binding Entity - Stores user's Outlook account binding information
/// </summary>
[SugarTable("ff_email_bindings")]
public class EmailBinding : OwEntityBase
{
    /// <summary>
    /// User ID who owns this binding
    /// </summary>
    [SugarColumn(ColumnName = "user_id")]
    public long UserId { get; set; }

    /// <summary>
    /// Bound email address
    /// </summary>
    [MaxLength(255)]
    [SugarColumn(ColumnName = "email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Email provider type (Outlook, Gmail, etc.)
    /// </summary>
    [MaxLength(50)]
    [SugarColumn(ColumnName = "provider")]
    public string Provider { get; set; } = "Outlook";

    /// <summary>
    /// OAuth access token
    /// </summary>
    [SugarColumn(ColumnName = "access_token", ColumnDataType = "text")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// OAuth refresh token
    /// </summary>
    [SugarColumn(ColumnName = "refresh_token", ColumnDataType = "text")]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time
    /// </summary>
    [SugarColumn(ColumnName = "token_expire_time")]
    public DateTimeOffset TokenExpireTime { get; set; }

    /// <summary>
    /// Last sync time
    /// </summary>
    [SugarColumn(ColumnName = "last_sync_time")]
    public DateTimeOffset? LastSyncTime { get; set; }

    /// <summary>
    /// Sync status (Active, Error, Disabled)
    /// </summary>
    [MaxLength(20)]
    [SugarColumn(ColumnName = "sync_status")]
    public string SyncStatus { get; set; } = "Active";

    /// <summary>
    /// Last sync error message
    /// </summary>
    [MaxLength(500)]
    [SugarColumn(ColumnName = "last_sync_error")]
    public string? LastSyncError { get; set; }

    /// <summary>
    /// Whether auto sync is enabled
    /// </summary>
    [SugarColumn(ColumnName = "auto_sync_enabled")]
    public bool AutoSyncEnabled { get; set; } = true;

    /// <summary>
    /// Sync interval in minutes
    /// </summary>
    [SugarColumn(ColumnName = "sync_interval_minutes")]
    public int SyncIntervalMinutes { get; set; } = 15;
}
