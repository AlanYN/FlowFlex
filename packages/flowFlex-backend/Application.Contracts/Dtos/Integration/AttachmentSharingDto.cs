using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.Integration;

/// <summary>
/// DTO for creating attachment sharing configuration
/// </summary>
public class AttachmentSharingCreateDto
{
    /// <summary>
    /// Integration ID
    /// </summary>
    [Required]
    public long IntegrationId { get; set; }

    /// <summary>
    /// Module name
    /// </summary>
    [Required]
    [StringLength(200)]
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>
    /// Workflow ID
    /// </summary>
    public long WorkflowId { get; set; }

    /// <summary>
    /// Action ID
    /// </summary>
    public long ActionId { get; set; }
}

/// <summary>
/// DTO for updating attachment sharing configuration
/// </summary>
public class AttachmentSharingUpdateDto
{
    /// <summary>
    /// Integration ID
    /// </summary>
    [Required]
    public long IntegrationId { get; set; }

    /// <summary>
    /// Module name
    /// </summary>
    [Required]
    [StringLength(200)]
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>
    /// Workflow ID
    /// </summary>
    public long WorkflowId { get; set; }

    /// <summary>
    /// Action ID
    /// </summary>
    public long ActionId { get; set; }
}
