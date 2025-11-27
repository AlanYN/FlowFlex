using FlowFlex.Domain.Entities.Base;
using SqlSugar;

namespace FlowFlex.Domain.Entities.Integration;

/// <summary>
/// Entity mapping - maps external system entity types to WFE entity types
/// </summary>
[SugarTable("ff_entity_mapping")]
public class EntityMapping : EntityBaseCreateInfo
{
    /// <summary>
    /// Integration ID
    /// </summary>
    public long IntegrationId { get; set; }
    
    /// <summary>
    /// External system ID (unique identifier in external system)
    /// </summary>
    public string? SystemId { get; set; }
    
    /// <summary>
    /// External entity display name (e.g., "Customers", "Leads")
    /// </summary>
    public string ExternalEntityName { get; set; } = string.Empty;
    
    /// <summary>
    /// External entity technical identifier (e.g., "customer", "lead")
    /// </summary>
    public string ExternalEntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// WFE entity type (e.g., "Cases", "Contacts")
    /// </summary>
    public string WfeEntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// Associated workflow IDs (JSON array)
    /// </summary>
    public string WorkflowIds { get; set; } = "[]";
    
    /// <summary>
    /// Whether this mapping is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    // Navigation Properties (ignored by SqlSugar)
    
    /// <summary>
    /// Parent integration
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public virtual Integration? Integration { get; set; }
    
    /// <summary>
    /// Entity key mappings
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public virtual ICollection<EntityKeyMapping> KeyMappings { get; set; } = new List<EntityKeyMapping>();
    
    /// <summary>
    /// Field mappings
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public virtual ICollection<InboundFieldMapping> FieldMappings { get; set; } = new List<InboundFieldMapping>();
}

