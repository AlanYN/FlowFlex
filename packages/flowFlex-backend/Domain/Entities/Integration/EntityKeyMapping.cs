using FlowFlex.Domain.Entities.Base;
using Domain.Shared.Enums;

namespace FlowFlex.Domain.Entities.Integration;

/// <summary>
/// Entity key mapping - defines how to match external and internal records
/// </summary>
public class EntityKeyMapping : EntityBase
{
    /// <summary>
    /// Entity mapping ID
    /// </summary>
    public long EntityMappingId { get; set; }
    
    /// <summary>
    /// Key field display name
    /// </summary>
    public string KeyFieldName { get; set; } = string.Empty;
    
    /// <summary>
    /// Key field type
    /// </summary>
    public EntityKeyType KeyFieldType { get; set; }
    
    /// <summary>
    /// External system key field name
    /// </summary>
    public string ExternalKeyField { get; set; } = string.Empty;
    
    /// <summary>
    /// WFE key field name
    /// </summary>
    public string WfeKeyField { get; set; } = string.Empty;
    
    /// <summary>
    /// Mapping rules (JSON)
    /// </summary>
    public string MappingRules { get; set; } = "{}";
    
    // Navigation Properties
    
    /// <summary>
    /// Parent entity mapping
    /// </summary>
    public virtual EntityMapping? EntityMapping { get; set; }
}

