namespace FlowFlex.Application.Contracts.Dtos.OW.Message;

/// <summary>
/// Message Detail DTO - Full message information
/// </summary>
public class MessageDetailDto : MessageListItemDto
{
    /// <summary>
    /// Full Message Body (HTML or plain text)
    /// </summary>
    public string Body { get; set; } = string.Empty;

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
    /// Related Entity Type: Onboarding, Case
    /// </summary>
    public string? RelatedEntityType { get; set; }

    /// <summary>
    /// Related Entity ID
    /// </summary>
    public long? RelatedEntityId { get; set; }

    /// <summary>
    /// Parent Message ID (for reply thread)
    /// </summary>
    public long? ParentMessageId { get; set; }

    /// <summary>
    /// Conversation ID
    /// </summary>
    public string? ConversationId { get; set; }

    /// <summary>
    /// Folder
    /// </summary>
    public string Folder { get; set; } = string.Empty;

    /// <summary>
    /// Is Draft
    /// </summary>
    public bool IsDraft { get; set; }

    /// <summary>
    /// Attachments
    /// </summary>
    public List<MessageAttachmentDto> Attachments { get; set; } = new();
}
