using FlowFlex.Domain.Shared.JsonConverters;
using Newtonsoft.Json;

namespace FlowFlex.Application.Contracts.Dtos.OW.WhatsNew;

/// <summary>
/// What's New list item DTO for the admin management page
/// </summary>
public class WhatsNewAdminItemDto
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
    /// Publication status: 0 = Draft, 1 = Published
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Actual publish timestamp (null when still a draft)
    /// </summary>
    public DateTimeOffset? PublishTime { get; set; }

    /// <summary>
    /// Number of distinct users who have read this update
    /// </summary>
    public int ReadCount { get; set; }
}
