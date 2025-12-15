using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Message;

/// <summary>
/// Message Reply DTO
/// </summary>
public class MessageReplyDto
{
    /// <summary>
    /// Reply Body
    /// </summary>
    [Required(ErrorMessage = "Reply body is required")]
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Attachment IDs
    /// </summary>
    public List<long> AttachmentIds { get; set; } = new();

    /// <summary>
    /// Is HTML body
    /// </summary>
    public bool IsHtml { get; set; } = true;
}
