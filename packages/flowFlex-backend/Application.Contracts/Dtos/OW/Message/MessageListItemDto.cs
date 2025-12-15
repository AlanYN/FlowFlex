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
    /// Message Type: Internal, Email, Portal
    /// </summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// Labels: Internal, External, Important, Portal
    /// </summary>
    public List<string> Labels { get; set; } = new();

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
