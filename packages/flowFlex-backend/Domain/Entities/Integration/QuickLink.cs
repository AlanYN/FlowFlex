using FlowFlex.Domain.Entities.Base;
using Domain.Shared.Enums;
using SqlSugar;

namespace FlowFlex.Domain.Entities.Integration;

/// <summary>
/// Quick link - provides quick navigation from WFE to external system
/// </summary>
[SugarTable("ff_quick_link")]
public class QuickLink : EntityBaseCreateInfo
{
    /// <summary>
    /// Integration ID
    /// </summary>
    public long IntegrationId { get; set; }

    /// <summary>
    /// Link display name
    /// </summary>
    [SugarColumn(ColumnName = "name")]
    public string LinkName { get; set; } = string.Empty;

    /// <summary>
    /// Link description
    /// </summary>
    [SugarColumn(ColumnName = "description")]
    public string? Description { get; set; }

    /// <summary>
    /// Target URL with parameter placeholders
    /// </summary>
    [SugarColumn(ColumnName = "target_url")]
    public string TargetUrl { get; set; } = string.Empty;

    /// <summary>
    /// URL parameters configuration (JSON array)
    /// </summary>
    [SugarColumn(ColumnName = "parameters")]
    public string UrlParameters { get; set; } = "[]";

    /// <summary>
    /// Display icon name
    /// </summary>
    [SugarColumn(ColumnName = "icon")]
    public string DisplayIcon { get; set; } = "ExternalLink";

    /// <summary>
    /// Redirect type
    /// </summary>
    [SugarColumn(ColumnName = "redirect_type")]
    public RedirectType RedirectType { get; set; }

    /// <summary>
    /// Whether this link is active
    /// </summary>
    [SugarColumn(ColumnName = "is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Sort order for display
    /// </summary>
    [SugarColumn(ColumnName = "display_order")]
    public int SortOrder { get; set; }

    // Navigation Properties (ignored by SqlSugar)

    /// <summary>
    /// Parent integration
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public virtual Integration? Integration { get; set; }
}

