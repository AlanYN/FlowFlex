using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Message;

/// <summary>
/// Message Create DTO - Used for creating/sending messages
/// </summary>
public class MessageCreateDto
{
    /// <summary>
    /// Message Type: Internal, Email, Portal
    /// </summary>
    [Required(ErrorMessage = "Message type is required")]
    [StringLength(20)]
    public string MessageType { get; set; } = "Internal";

    /// <summary>
    /// Recipients
    /// </summary>
    public List<RecipientDto> Recipients { get; set; } = new();

    /// <summary>
    /// CC Recipients (for Email type)
    /// </summary>
    public List<RecipientDto> CcRecipients { get; set; } = new();

    /// <summary>
    /// BCC Recipients (for Email type)
    /// </summary>
    public List<RecipientDto> BccRecipients { get; set; } = new();

    /// <summary>
    /// Message Subject
    /// </summary>
    [Required(ErrorMessage = "Subject is required")]
    [StringLength(500)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Message Body
    /// </summary>
    [Required(ErrorMessage = "Body is required")]
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Related Entity Type: Onboarding, Case
    /// </summary>
    [StringLength(50)]
    public string? RelatedEntityType { get; set; }

    /// <summary>
    /// Related Entity ID
    /// </summary>
    public long? RelatedEntityId { get; set; }

    /// <summary>
    /// Portal ID (for Portal messages)
    /// </summary>
    public long? PortalId { get; set; }

    /// <summary>
    /// Attachment IDs to associate with this message
    /// </summary>
    public List<long> AttachmentIds { get; set; } = new();

    /// <summary>
    /// Save as draft instead of sending
    /// </summary>
    public bool SaveAsDraft { get; set; } = false;

    /// <summary>
    /// Is HTML body
    /// </summary>
    public bool IsHtml { get; set; } = true;
}
