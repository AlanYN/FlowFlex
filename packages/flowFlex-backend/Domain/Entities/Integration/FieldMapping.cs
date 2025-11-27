using FlowFlex.Domain.Entities.Base;
using Domain.Shared.Enums;
using SqlSugar;

namespace FlowFlex.Domain.Entities.Integration;

/// <summary>
/// Inbound field mapping - maps external system fields to WFE fields for inbound data
/// </summary>
[SugarTable("ff_inbound_field_mapping")]
public class InboundFieldMapping : EntityBaseCreateInfo
{
    /// <summary>
    /// Integration ID
    /// </summary>
    public long IntegrationId { get; set; }
    
    /// <summary>
    /// Action ID - associates this field mapping with a specific action
    /// </summary>
    public long? ActionId { get; set; }
    
    /// <summary>
    /// External system field name
    /// </summary>
    public string ExternalFieldName { get; set; } = string.Empty;
    
    /// <summary>
    /// WFE field ID
    /// </summary>
    public string WfeFieldId { get; set; } = string.Empty;
    
    /// <summary>
    /// Field data type (Text, Number, Date, Boolean, Lookup)
    /// </summary>
    public FieldType FieldType { get; set; }
    
    /// <summary>
    /// Sync direction (ViewOnly, Editable, OutboundOnly)
    /// </summary>
    public SyncDirection SyncDirection { get; set; }
    
    /// <summary>
    /// Sort order for display
    /// </summary>
    public int SortOrder { get; set; }
    
    /// <summary>
    /// Whether this field is required
    /// </summary>
    public bool IsRequired { get; set; }
    
    /// <summary>
    /// Default value for this field
    /// </summary>
    public string? DefaultValue { get; set; }
    
    // Navigation Properties (ignored by SqlSugar)
    
    /// <summary>
    /// Parent integration
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public virtual Integration? Integration { get; set; }
}

/// <summary>
/// Backward compatibility alias for FieldMapping
/// </summary>
[Obsolete("Use InboundFieldMapping instead")]
public class FieldMapping : InboundFieldMapping { }

