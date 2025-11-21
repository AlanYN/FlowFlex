using FlowFlex.Domain.Entities.Base;

namespace FlowFlex.Domain.Entities.Integration;

/// <summary>
/// Inbound configuration - settings for receiving data from external system
/// </summary>
public class InboundConfiguration : EntityBase
{
    /// <summary>
    /// Integration ID
    /// </summary>
    public long IntegrationId { get; set; }
    
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
    
    // Navigation Properties
    
    /// <summary>
    /// Parent integration
    /// </summary>
    public virtual Integration? Integration { get; set; }
}

