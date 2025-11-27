namespace FlowFlex.Application.Contracts.Dtos.Integration;

/// <summary>
/// Overview DTO for Outbound Configuration - displays summary of all actions
/// </summary>
public class OutboundConfigurationOverviewDto
{
    /// <summary>
    /// Action ID
    /// </summary>
    public long ActionId { get; set; }

    /// <summary>
    /// Action name
    /// </summary>
    public string ActionName { get; set; } = string.Empty;

    /// <summary>
    /// Whether master data sharing is enabled
    /// </summary>
    public bool MasterDataEnabled { get; set; }

    /// <summary>
    /// Number of fields configured for sharing
    /// </summary>
    public int FieldCount { get; set; }

    /// <summary>
    /// Whether attachment sharing is enabled
    /// </summary>
    public bool AttachmentsEnabled { get; set; }

    /// <summary>
    /// Whether real-time sync is enabled
    /// </summary>
    public bool RealTimeSyncEnabled { get; set; }

    /// <summary>
    /// Webhook URL if configured
    /// </summary>
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// Configuration status
    /// </summary>
    public string Status { get; set; } = "Not Configured";
}

