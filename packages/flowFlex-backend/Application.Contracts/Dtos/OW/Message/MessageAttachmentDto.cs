namespace FlowFlex.Application.Contracts.Dtos.OW.Message;

/// <summary>
/// Message Attachment DTO
/// </summary>
public class MessageAttachmentDto
{
    /// <summary>
    /// Attachment ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// File Name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File Size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Content Type (MIME type)
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Content ID (for inline images)
    /// </summary>
    public string? ContentId { get; set; }

    /// <summary>
    /// Is Inline attachment
    /// </summary>
    public bool IsInline { get; set; }
}
