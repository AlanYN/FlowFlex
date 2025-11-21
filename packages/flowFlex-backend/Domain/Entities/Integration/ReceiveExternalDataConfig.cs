using FlowFlex.Domain.Entities.Base;

namespace FlowFlex.Domain.Entities.Integration;

/// <summary>
/// Receive External Data Configuration - configures which workflows can be triggered by external entities
/// </summary>
public class ReceiveExternalDataConfig : EntityBaseCreateInfo
{
    /// <summary>
    /// Integration ID
    /// </summary>
    public long IntegrationId { get; set; }
    
    /// <summary>
    /// External entity name (user-defined)
    /// </summary>
    public string EntityName { get; set; } = string.Empty;
    
    /// <summary>
    /// Workflow ID that will be triggered when this entity is received
    /// </summary>
    public long TriggerWorkflowId { get; set; }
    
    /// <summary>
    /// Field mapping configuration (JSON)
    /// Stores the mapping between external fields and WFE fields for this entity
    /// </summary>
    public string FieldMappingConfig { get; set; } = "[]";
    
    /// <summary>
    /// Whether this configuration is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Description or notes about this configuration
    /// </summary>
    public string? Description { get; set; }
    
    // Navigation Properties
    
    /// <summary>
    /// Parent integration
    /// </summary>
    public virtual Integration? Integration { get; set; }
}

