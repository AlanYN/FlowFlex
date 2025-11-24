namespace FlowFlex.Application.Contracts.Dtos.Integration;

/// <summary>
/// Output DTO for Receive External Data Configuration
/// </summary>
public class ReceiveExternalDataConfigOutputDto
{
    /// <summary>
    /// Configuration ID
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// Integration ID
    /// </summary>
    public long IntegrationId { get; set; }
    
    /// <summary>
    /// External entity name
    /// </summary>
    public string EntityName { get; set; } = string.Empty;
    
    /// <summary>
    /// Trigger workflow ID
    /// </summary>
    public long TriggerWorkflowId { get; set; }
    
    /// <summary>
    /// Trigger workflow name
    /// </summary>
    public string TriggerWorkflowName { get; set; } = string.Empty;
    
    /// <summary>
    /// Field mappings
    /// </summary>
    public List<FieldMappingOutputDto> FieldMappings { get; set; } = new();
    
    /// <summary>
    /// Whether this configuration is active
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Create date
    /// </summary>
    public DateTimeOffset CreateDate { get; set; }
    
    /// <summary>
    /// Created by
    /// </summary>
    public string? CreateBy { get; set; }
}

