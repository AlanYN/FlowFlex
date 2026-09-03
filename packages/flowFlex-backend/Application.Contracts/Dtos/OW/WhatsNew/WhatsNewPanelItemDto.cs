using FlowFlex.Domain.Shared.JsonConverters;
using Newtonsoft.Json;

namespace FlowFlex.Application.Contracts.Dtos.OW.WhatsNew;

/// <summary>
/// What's New panel list item DTO (user-facing)
/// </summary>
public class WhatsNewPanelItemDto
{
    /// <summary>
    /// Unique identifier (serialized as string to preserve JS numeric precision)
    /// </summary>
    [JsonConverter(typeof(LongToStringConverter))]
    public long Id { get; set; }

    /// <summary>
    /// Update title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Short summary (up to 200 characters)
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Category: NewFeature / Improvement / BugFix / Announcement
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Actual publish timestamp (UTC)
    /// </summary>
    public DateTimeOffset? PublishTime { get; set; }

    /// <summary>
    /// Whether the current user has already read this item
    /// </summary>
    public bool IsRead { get; set; }
}
