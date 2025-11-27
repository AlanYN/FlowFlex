using FlowFlex.Domain.Entities.Base;
using Domain.Shared.Enums;
using SqlSugar;

namespace FlowFlex.Domain.Entities.Integration;

/// <summary>
/// Integration sync log - records data synchronization history
/// </summary>
[SugarTable("ff_integration_sync_log")]
public class IntegrationSyncLog : EntityBaseCreateInfo
{
    /// <summary>
    /// Integration ID
    /// </summary>
    [SugarColumn(ColumnName = "integration_id")]
    public long IntegrationId { get; set; }

    /// <summary>
    /// Sync direction
    /// </summary>
    [SugarColumn(ColumnName = "sync_direction")]
    public SyncDirection SyncDirection { get; set; }

    /// <summary>
    /// Entity type being synced
    /// </summary>
    [SugarColumn(ColumnName = "entity_type")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// External system record ID
    /// </summary>
    [SugarColumn(ColumnName = "external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Internal WFE record ID
    /// </summary>
    [SugarColumn(ColumnName = "internal_id")]
    public string? InternalId { get; set; }

    /// <summary>
    /// Sync status
    /// </summary>
    [SugarColumn(ColumnName = "sync_status")]
    public SyncStatus SyncStatus { get; set; }

    /// <summary>
    /// Error message if sync failed
    /// </summary>
    [SugarColumn(ColumnName = "error_message")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Request payload (JSON)
    /// </summary>
    [SugarColumn(ColumnName = "request_payload")]
    public string? RequestPayload { get; set; }

    /// <summary>
    /// Response payload (JSON)
    /// </summary>
    [SugarColumn(ColumnName = "response_payload")]
    public string? ResponsePayload { get; set; }

    /// <summary>
    /// Sync timestamp
    /// </summary>
    [SugarColumn(ColumnName = "synced_at")]
    public DateTime SyncedAt { get; set; }

    /// <summary>
    /// Duration of sync operation in milliseconds
    /// </summary>
    [SugarColumn(ColumnName = "duration_ms")]
    public long? DurationMs { get; set; }

    // Navigation Properties (ignored by SqlSugar)

    /// <summary>
    /// Parent integration
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public virtual Integration? Integration { get; set; }
}

