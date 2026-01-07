using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Message;

/// <summary>
/// Message Update DTO - Used for updating draft messages
/// </summary>
public class MessageUpdateDto
{
    /// <summary>
    /// Recipients
    /// </summary>
    public List<RecipientDto> Recipients { get; set; } = new();

    /// <summary>
    /// CC Recipients
    /// </summary>
    public List<RecipientDto> CcRecipients { get; set; } = new();

    /// <summary>
    /// BCC Recipients
    /// </summary>
    public List<RecipientDto> BccRecipients { get; set; } = new();

    /// <summary>
    /// Message Subject
    /// </summary>
    [StringLength(500)]
    public string? Subject { get; set; }

    /// <summary>
    /// Message Body
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Related Entity Type
    /// </summary>
    [StringLength(50)]
    public string? RelatedEntityType { get; set; }

    /// <summary>
    /// Related Entity ID
    /// </summary>
    public long? RelatedEntityId { get; set; }

    /// <summary>
    /// Portal ID
    /// </summary>
    public long? PortalId { get; set; }

    /// <summary>
    /// Attachment IDs
    /// </summary>
    public List<long> AttachmentIds { get; set; } = new();
}
