using FlowFlex.Domain.Entities.Base;
using Domain.Shared.Enums;
using SqlSugar;

namespace FlowFlex.Domain.Entities.Integration;

/// <summary>
/// Entity key mapping - defines how to match external and internal records
/// </summary>
[SugarTable("ff_entity_key_mapping")]
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
    
    // Navigation Properties (ignored by SqlSugar)
    
    /// <summary>
    /// Parent entity mapping
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public virtual EntityMapping? EntityMapping { get; set; }
}

