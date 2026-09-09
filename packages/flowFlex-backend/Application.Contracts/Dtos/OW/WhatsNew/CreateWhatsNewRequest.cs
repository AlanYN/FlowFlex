using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.WhatsNew;

/// <summary>
/// Request DTO for creating a new What's New entry (admin-only)
/// </summary>
public class CreateWhatsNewRequest
{
    /// <summary>
    /// Update title (required, max 100 characters)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Title { get; set; }

    /// <summary>
    /// Short summary (required, max 200 characters)
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Summary { get; set; }

    /// <summary>
    /// Full rich-text HTML body (required; XSS filtering is applied server-side before storage)
    /// </summary>
    [Required]
    public string Content { get; set; }

    /// <summary>
    /// Category: NewFeature / Improvement / BugFix / Announcement (required)
    /// </summary>
    [Required]
    public string Category { get; set; }

    /// <summary>
    /// Publication status: 0 = Draft (default), 1 = Published
    /// </summary>
    public int Status { get; set; } = 0;
}
