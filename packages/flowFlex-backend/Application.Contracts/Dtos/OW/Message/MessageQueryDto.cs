namespace FlowFlex.Application.Contracts.Dtos.OW.Message;

/// <summary>
/// Message Query DTO - Used for filtering and pagination
/// </summary>
public class MessageQueryDto
{
    /// <summary>
    /// Page Index (1-based)
    /// </summary>
    public int PageIndex { get; set; } = 1;

    /// <summary>
    /// Page Size
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Folder: Inbox, Sent, Starred, Archive, Trash
    /// </summary>
    public string? Folder { get; set; }

    /// <summary>
    /// Label: Internal, External, Important, Portal
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Message Type: Internal, Email, Portal
    /// </summary>
    public string? MessageType { get; set; }

    /// <summary>
    /// Search Term (searches in subject and body)
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Related Entity ID
    /// </summary>
    public long? RelatedEntityId { get; set; }

    /// <summary>
    /// Sort Field: ReceivedDate, SentDate, Subject
    /// </summary>
    public string SortField { get; set; } = "ReceivedDate";

    /// <summary>
    /// Sort Direction: asc, desc
    /// </summary>
    public string SortDirection { get; set; } = "desc";

    /// <summary>
    /// Include Outlook email sync
    /// </summary>
    public bool IncludeOutlookSync { get; set; } = true;
}
