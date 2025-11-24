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
    /// Integration ID
    /// </summary>
    public long IntegrationId { get; set; }
    
    /// <summary>
    /// Action ID - associates this configuration with a specific action
    /// </summary>
    public long ActionId { get; set; }
    
    /// <summary>
    /// Attachment sharing configuration (JSON)
    /// </summary>
    public string AttachmentSharingConfig { get; set; } = "{}";
    
    /// <summary>
    /// Whether to automatically create entities if not exists
    /// </summary>
    public bool AutoCreateEntities { get; set; } = true;
    
    /// <summary>
    /// Validation rules (JSON)
    /// </summary>
    public string ValidationRules { get; set; } = "{}";
    
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

