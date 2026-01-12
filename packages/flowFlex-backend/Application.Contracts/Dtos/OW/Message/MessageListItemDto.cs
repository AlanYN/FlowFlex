using FlowFlex.Domain.Shared.Enums;

namespace FlowFlex.Application.Contracts.Dtos.OW.Message;

/// <summary>
/// Message List Item DTO - Used for displaying messages in list view
/// </summary>
public class MessageListItemDto
{
    /// <summary>
    /// Message ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Message Subject
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Body Preview (first 200 characters)
    /// </summary>
    public string BodyPreview { get; set; } = string.Empty;

    /// <summary>
    /// Sender Name
    /// </summary>
    public string SenderName { get; set; } = string.Empty;

    /// <summary>
    /// Sender Email
    /// </summary>
    public string SenderEmail { get; set; } = string.Empty;

    /// <summary>
    /// Sender ID
    /// </summary>
    public long? SenderId { get; set; }

    /// <summary>
    /// Recipients
    /// </summary>
    public List<RecipientDto> Recipients { get; set; } = new();

    /// <summary>
    /// Message Type: Internal, Email, Portal
    /// </summary>
    public MessageType MessageType { get; set; } = MessageType.Internal;

    /// <summary>
    /// Labels: Internal, External, Important, Portal
    /// </summary>
    public List<MessageLabel> Labels { get; set; } = new();

    /// <summary>
    /// Related Entity Code (e.g., LEAD-001)
    /// </summary>
    public string? RelatedEntityCode { get; set; }

    /// <summary>
    /// Is Read
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// Is Starred
    /// </summary>
    public bool IsStarred { get; set; }

    /// <summary>
    /// Is Archived
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// Has Attachments
    /// </summary>
    public bool HasAttachments { get; set; }

    /// <summary>
    /// Received Date
    /// </summary>
    public DateTimeOffset? ReceivedDate { get; set; }

    /// <summary>
    /// Sent Date
    /// </summary>
    public DateTimeOffset? SentDate { get; set; }
}
