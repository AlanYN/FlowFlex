using Domain.Shared.Enums;

namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// Quick link output DTO
    /// </summary>
    public class QuickLinkOutputDto
    {
        /// <summary>
        /// Quick link ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Integration ID
        /// </summary>
        public long IntegrationId { get; set; }

        /// <summary>
        /// Link display name
        /// </summary>
        public string LinkName { get; set; } = string.Empty;

        /// <summary>
        /// Link description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Target URL with parameter placeholders
        /// </summary>
        public string TargetUrl { get; set; } = string.Empty;

        /// <summary>
        /// URL parameters configuration
        /// </summary>
        public List<UrlParameterDto> UrlParameters { get; set; } = new();

        /// <summary>
        /// Display icon name
        /// </summary>
        public string DisplayIcon { get; set; } = "ExternalLink";

        /// <summary>
        /// Redirect type
        /// </summary>
        public RedirectType RedirectType { get; set; }

        /// <summary>
        /// Whether this link is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Sort order for display
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Create date
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }

        /// <summary>
        /// Modify date
        /// </summary>
        public DateTimeOffset ModifyDate { get; set; }
    }
}

