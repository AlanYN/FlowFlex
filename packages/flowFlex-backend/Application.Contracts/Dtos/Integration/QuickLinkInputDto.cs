using System.ComponentModel.DataAnnotations;
using Domain.Shared.Enums;

namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// Quick link create/update input DTO
    /// </summary>
    public class QuickLinkInputDto
    {
        /// <summary>
        /// Integration ID
        /// </summary>
        [Required]
        public long IntegrationId { get; set; }

        /// <summary>
        /// Link display name
        /// </summary>
        [Required]
        [StringLength(100)]
        public string LinkName { get; set; } = string.Empty;

        /// <summary>
        /// Link description
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Target URL with parameter placeholders
        /// </summary>
        [Required]
        [StringLength(1000)]
        public string TargetUrl { get; set; } = string.Empty;

        /// <summary>
        /// URL parameters configuration
        /// </summary>
        public List<UrlParameterDto> UrlParameters { get; set; } = new();

        /// <summary>
        /// Display icon name
        /// </summary>
        [StringLength(50)]
        public string DisplayIcon { get; set; } = "ExternalLink";

        /// <summary>
        /// Redirect type
        /// </summary>
        public RedirectType RedirectType { get; set; } = RedirectType.Direct;

        /// <summary>
        /// Whether this link is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Sort order for display
        /// </summary>
        public int SortOrder { get; set; }
    }

    /// <summary>
    /// URL parameter configuration DTO
    /// URL参数配置DTO
    /// </summary>
    public class UrlParameterDto
    {
        /// <summary>
        /// Parameter name (e.g., orderNo, userId)
        /// 参数名称
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Value source type
        /// 值来源类型
        /// </summary>
        [Required]
        public UrlParameterValueSource ValueSource { get; set; } = UrlParameterValueSource.PageParameter;

        /// <summary>
        /// Value detail (specific field or value based on ValueSource)
        /// 值详情（根据ValueSource指定的具体字段或值）
        /// - PageParameter: Customer ID, Order No, etc.
        /// - LoginUserInfo: UserId, UserName, Email, etc.
        /// - FixedValue: The actual fixed value
        /// - SystemVariable: CurrentDate, CurrentTime, etc.
        /// </summary>
        [StringLength(500)]
        public string? ValueDetail { get; set; }
    }
}

