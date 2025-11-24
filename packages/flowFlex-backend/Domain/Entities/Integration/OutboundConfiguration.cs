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
    /// Integration ID
    /// </summary>
    public long IntegrationId { get; set; }
    
    /// <summary>
    /// Action ID - associates this configuration with a specific action
    /// </summary>
    public long ActionId { get; set; }
    
    /// <summary>
    /// Shared master data types (JSON array)
    /// </summary>
    public string SharedMasterDataTypes { get; set; } = "[]";
    
    /// <summary>
    /// Attachment workflow IDs (JSON array)
    /// </summary>
    public string AttachmentWorkflowIds { get; set; } = "[]";
    
    /// <summary>
    /// Enable real-time sync
    /// </summary>
    public bool EnableRealTimeSync { get; set; }
    
    /// <summary>
    /// Webhook URL for notifications
    /// </summary>
    public string? WebhookUrl { get; set; }
    
    /// <summary>
    /// Number of retry attempts on failure
    /// </summary>
    public int RetryAttempts { get; set; } = 3;
    
    /// <summary>
    /// Delay between retries in seconds
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 60;
    
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

