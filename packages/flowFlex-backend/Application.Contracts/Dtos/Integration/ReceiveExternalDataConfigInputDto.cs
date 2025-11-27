using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.Integration;

/// <summary>
/// Input DTO for creating/updating Receive External Data Configuration
/// </summary>
public class ReceiveExternalDataConfigInputDto
{
    /// <summary>
    /// External entity name (user-defined)
    /// </summary>
    [Required(ErrorMessage = "Entity name is required")]
    [StringLength(200, ErrorMessage = "Entity name cannot exceed 200 characters")]
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// Workflow ID that will be triggered when this entity is received
    /// </summary>
    [Required(ErrorMessage = "Trigger workflow ID is required")]
    public long TriggerWorkflowId { get; set; }

    /// <summary>
    /// Field mappings for this entity
    /// </summary>
    public List<FieldMappingInputDto> FieldMappings { get; set; } = new();

    /// <summary>
    /// Whether this configuration is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Description or notes about this configuration
    /// </summary>
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }
}
