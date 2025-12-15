using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Message;

/// <summary>
/// Message Forward DTO
/// </summary>
public class MessageForwardDto
{
    /// <summary>
    /// Forward Recipients
    /// </summary>
    [Required(ErrorMessage = "At least one recipient is required")]
    public List<RecipientDto> Recipients { get; set; } = new();

    /// <summary>
    /// Additional Body (prepended to original message)
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Attachment IDs
    /// </summary>
    public List<long> AttachmentIds { get; set; } = new();

    /// <summary>
    /// Is HTML body
    /// </summary>
    public bool IsHtml { get; set; } = true;
}
