using FlowFlex.Domain.Entities.Base;
using Domain.Shared.Enums;

namespace FlowFlex.Domain.Entities.Integration;

/// <summary>
/// Integration sync log - records data synchronization history
/// </summary>
public class IntegrationSyncLog : EntityBaseCreateInfo
{
    /// <summary>
    /// Integration ID
    /// </summary>
    public long IntegrationId { get; set; }
    
    /// <summary>
    /// Sync direction
    /// </summary>
    public SyncDirection SyncDirection { get; set; }
    
    /// <summary>
    /// Entity type being synced
    /// </summary>
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// External system record ID
    /// </summary>
    public string? ExternalId { get; set; }
    
    /// <summary>
    /// Internal WFE record ID
    /// </summary>
    public string? InternalId { get; set; }
    
    /// <summary>
    /// Sync status
    /// </summary>
    public SyncStatus SyncStatus { get; set; }
    
    /// <summary>
    /// Error message if sync failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Request payload (JSON)
    /// </summary>
    public string? RequestPayload { get; set; }
    
    /// <summary>
    /// Response payload (JSON)
    /// </summary>
    public string? ResponsePayload { get; set; }
    
    /// <summary>
    /// Sync timestamp
    /// </summary>
    public DateTime SyncedAt { get; set; }
    
    /// <summary>
    /// Duration of sync operation in milliseconds
    /// </summary>
    public long? DurationMs { get; set; }
    
    // Navigation Properties
    
    /// <summary>
    /// Parent integration
    /// </summary>
    public virtual Integration? Integration { get; set; }
}

