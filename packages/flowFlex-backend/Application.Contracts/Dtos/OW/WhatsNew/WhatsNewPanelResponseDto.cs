namespace FlowFlex.Application.Contracts.Dtos.OW.WhatsNew;

/// <summary>
/// Response DTO for the What's New panel endpoint
/// </summary>
public class WhatsNewPanelResponseDto
{
    /// <summary>
    /// Up to 10 most recent published updates, sorted by publish_time DESC
    /// </summary>
    public List<WhatsNewPanelItemDto> Items { get; set; } = new();

    /// <summary>
    /// Total number of unread published updates for the current user
    /// </summary>
    public int UnreadCount { get; set; }
}
