using FlowFlex.Domain.Entities.Base;
using SqlSugar;

namespace FlowFlex.Domain.Entities.Integration;

/// <summary>
/// Inbound configuration - settings for receiving data from external system
/// </summary>
[SugarTable("ff_inbound_configuration")]
public class InboundConfiguration : EntityBase
{
    /// <summary>
    /// Tenant ID
    /// </summary>
    [SugarColumn(ColumnName = "tenant_id")]
    public string TenantId { get; set; } = "default";

    /// <summary>
    /// App code
    /// </summary>
    [SugarColumn(ColumnName = "app_code")]
    public string AppCode { get; set; } = "DEFAULT";

    /// <summary>
    /// Integration ID
    /// </summary>
    [SugarColumn(ColumnName = "integration_id")]
    public long IntegrationId { get; set; }
    
    /// <summary>
    /// Action ID - associates this configuration with a specific action
    /// </summary>
    [SugarColumn(ColumnName = "action_id")]
    public long? ActionId { get; set; }
    
    /// <summary>
    /// Entity types (JSON array) - mapped from database column entity_types
    /// </summary>
    [SugarColumn(ColumnName = "entity_types")]
    public string EntityTypes { get; set; } = "[]";
    
    /// <summary>
    /// Field mappings (JSON array) - mapped from database column field_mappings
    /// </summary>
    [SugarColumn(ColumnName = "field_mappings")]
    public string FieldMappings { get; set; } = "[]";
    
    /// <summary>
    /// Attachment settings (JSON) - mapped from database column attachment_settings
    /// </summary>
    [SugarColumn(ColumnName = "attachment_settings")]
    public string AttachmentSettings { get; set; } = "{}";
    
    /// <summary>
    /// Whether to automatically sync
    /// </summary>
    [SugarColumn(ColumnName = "auto_sync")]
    public bool AutoSync { get; set; }
    
    /// <summary>
    /// Sync interval in minutes
    /// </summary>
    [SugarColumn(ColumnName = "sync_interval")]
    public int SyncInterval { get; set; }
    
    /// <summary>
    /// Last sync date
    /// </summary>
    [SugarColumn(ColumnName = "last_sync_date")]
    public DateTimeOffset? LastSyncDate { get; set; }
    
    // Navigation Properties (ignored by SqlSugar)
    
    /// <summary>
    /// Parent integration
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public virtual Integration? Integration { get; set; }
    
    /// <summary>
    /// Associated action
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public virtual IntegrationAction? Action { get; set; }
}

