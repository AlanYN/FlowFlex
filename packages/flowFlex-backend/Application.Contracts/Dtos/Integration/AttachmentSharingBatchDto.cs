using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.Integration;

/// <summary>
/// DTO for batch saving attachment sharing configuration
/// Items not in the list will be automatically deleted
/// </summary>
public class AttachmentSharingBatchSaveDto
{
    /// <summary>
    /// Integration ID
    /// </summary>
    [Required]
    public long IntegrationId { get; set; }

    /// <summary>
    /// Items to save (create or update)
    /// Items with ID will be updated, items without ID will be created
    /// Existing items not in this list will be automatically deleted
    /// </summary>
    public List<AttachmentSharingBatchItemDto> Items { get; set; } = new();
}

/// <summary>
/// DTO for batch item in attachment sharing configuration
/// </summary>
public class AttachmentSharingBatchItemDto
{
    /// <summary>
    /// Item ID (null or empty for new items)
    /// </summary>
    public string? Id { get; set; }

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
/// Result DTO for batch save attachment sharing configuration
/// </summary>
public class AttachmentSharingBatchSaveResultDto
{
    /// <summary>
    /// Number of items created
    /// </summary>
    public int CreatedCount { get; set; }

    /// <summary>
    /// Number of items updated
    /// </summary>
    public int UpdatedCount { get; set; }

    /// <summary>
    /// Number of items deleted
    /// </summary>
    public int DeletedCount { get; set; }

    /// <summary>
    /// All items after batch save
    /// </summary>
    public List<InboundAttachmentItemDto> Items { get; set; } = new();
}
