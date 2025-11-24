using FlowFlex.Domain.Entities.Base;
using SqlSugar;

namespace FlowFlex.Domain.Entities.Integration;

/// <summary>
/// Outbound configuration - settings for sending data to external system
/// </summary>
[SugarTable("ff_outbound_configuration")]
public class OutboundConfiguration : EntityBase
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
    /// Sync mode - mapped from database column sync_mode
    /// </summary>
    [SugarColumn(ColumnName = "sync_mode")]
    public int SyncMode { get; set; }
    
    /// <summary>
    /// Webhook URL for notifications
    /// </summary>
    [SugarColumn(ColumnName = "webhook_url")]
    public string? WebhookUrl { get; set; }
    
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
    
    /// <summary>
    /// Field configurations
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public virtual ICollection<OutboundFieldConfig> FieldConfigs { get; set; } = new List<OutboundFieldConfig>();
}

