using FlowFlex.Domain.Entities.Base;

namespace FlowFlex.Domain.Entities.Integration;

/// <summary>
/// Outbound field configuration - defines which fields to share with external system
/// </summary>
public class OutboundFieldConfig : EntityBase
{
    /// <summary>
    /// Outbound configuration ID
    /// </summary>
    public long OutboundConfigurationId { get; set; }
    
    /// <summary>
    /// WFE field ID
    /// </summary>
    public string WfeFieldId { get; set; } = string.Empty;
    
    /// <summary>
    /// External system field name
    /// </summary>
    public string ExternalFieldName { get; set; } = string.Empty;
    
    /// <summary>
    /// Sort order for display
    /// </summary>
    public int SortOrder { get; set; }
    
    /// <summary>
    /// Whether this field is required
    /// </summary>
    public bool IsRequired { get; set; }
    
    // Navigation Properties
    
    /// <summary>
    /// Parent outbound configuration
    /// </summary>
    public virtual OutboundConfiguration? OutboundConfiguration { get; set; }
}

