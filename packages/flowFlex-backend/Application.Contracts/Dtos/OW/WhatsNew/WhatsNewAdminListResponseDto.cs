namespace FlowFlex.Application.Contracts.Dtos.OW.WhatsNew;

/// <summary>
/// Response DTO for the What's New admin list endpoint.
/// Bundles the item list together with aggregate counts so the frontend
/// can populate the statistics cards without an extra round-trip.
/// </summary>
public class WhatsNewAdminListResponseDto
{
    /// <summary>
    /// All active (is_valid = true) What's New entries matching the requested filter
    /// </summary>
    public List<WhatsNewAdminItemDto> Items { get; set; } = new();

    /// <summary>
    /// Total number of published entries (status = 1)
    /// </summary>
    public int PublishedCount { get; set; }

    /// <summary>
    /// Total number of draft entries (status = 0)
    /// </summary>
    public int DraftCount { get; set; }
}
