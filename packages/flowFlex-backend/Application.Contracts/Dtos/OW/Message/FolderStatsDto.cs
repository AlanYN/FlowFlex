namespace FlowFlex.Application.Contracts.Dtos.OW.Message;

/// <summary>
/// Folder Statistics DTO
/// </summary>
public class FolderStatsDto
{
    /// <summary>
    /// Folder Name: Inbox, Sent, Starred, Archive, Trash
    /// </summary>
    public string Folder { get; set; } = string.Empty;

    /// <summary>
    /// Total Message Count
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Unread Message Count
    /// </summary>
    public int UnreadCount { get; set; }

    /// <summary>
    /// Internal Message Count
    /// </summary>
    public int InternalCount { get; set; }

    /// <summary>
    /// Email Message Count
    /// </summary>
    public int EmailCount { get; set; }

    /// <summary>
    /// Portal Message Count
    /// </summary>
    public int PortalCount { get; set; }
}
