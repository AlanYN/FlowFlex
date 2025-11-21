using FlowFlex.Domain.Entities.Base;
using Domain.Shared.Enums;

namespace FlowFlex.Domain.Entities.Integration;

/// <summary>
/// Field mapping - maps external system fields to WFE fields
/// </summary>
public class FieldMapping : EntityBaseCreateInfo
{
    /// <summary>
    /// Integration ID
    /// </summary>
    public long IntegrationId { get; set; }
    
    /// <summary>
    /// Entity mapping ID
    /// </summary>
    public long EntityMappingId { get; set; }
    
    /// <summary>
    /// External system field name
    /// </summary>
    public string ExternalFieldName { get; set; } = string.Empty;
    
    /// <summary>
    /// WFE field ID
    /// </summary>
    public string WfeFieldId { get; set; } = string.Empty;
    
    /// <summary>
    /// Field data type
    /// </summary>
    public FieldType FieldType { get; set; }
    
    /// <summary>
    /// Sync direction
    /// </summary>
    public SyncDirection SyncDirection { get; set; }
    
    /// <summary>
    /// Associated workflow IDs (JSON array)
    /// </summary>
    public string WorkflowIds { get; set; } = "[]";
    
    /// <summary>
    /// Transformation rules (JSON)
    /// </summary>
    public string TransformRules { get; set; } = "{}";
    
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
    /// Parent integration
    /// </summary>
    public virtual Integration? Integration { get; set; }
    
    /// <summary>
    /// Parent entity mapping
    /// </summary>
    public virtual EntityMapping? EntityMapping { get; set; }
}

