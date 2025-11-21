namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// Quick link DTO
    /// </summary>
    public class QuickLinkDto
    {
        public long Id { get; set; }
        public long IntegrationId { get; set; }
        public string Label { get; set; }
        public string UrlTemplate { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
        public int DisplayOrder { get; set; }
        public bool OpenInNewTab { get; set; }
        public bool RequireConfirmation { get; set; }
        public string ConfirmationMessage { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
    }

    /// <summary>
    /// Create quick link DTO
    /// </summary>
    public class CreateQuickLinkDto
    {
        public long IntegrationId { get; set; }
        public string Label { get; set; }
        public string UrlTemplate { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
        public int DisplayOrder { get; set; }
        public bool OpenInNewTab { get; set; }
        public bool RequireConfirmation { get; set; }
        public string ConfirmationMessage { get; set; }
    }

    /// <summary>
    /// Update quick link DTO
    /// </summary>
    public class UpdateQuickLinkDto
    {
        public string Label { get; set; }
        public string UrlTemplate { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
        public int DisplayOrder { get; set; }
        public bool OpenInNewTab { get; set; }
        public bool RequireConfirmation { get; set; }
        public string ConfirmationMessage { get; set; }
    }
}
