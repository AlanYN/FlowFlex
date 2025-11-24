using FlowFlex.Domain.Entities.Base;
using SqlSugar;

namespace FlowFlex.Domain.Entities.Integration;

/// <summary>
/// Outbound field configuration - defines which fields to share with external system
/// </summary>
[SugarTable("ff_outbound_field_config")]
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
    
    // Navigation Properties (ignored by SqlSugar)
    
    /// <summary>
    /// Parent outbound configuration
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public virtual OutboundConfiguration? OutboundConfiguration { get; set; }
}

