using FlowFlex.Domain.Entities.Base;
using Domain.Shared.Enums;

namespace FlowFlex.Domain.Entities.Integration;

/// <summary>
/// Quick link - provides quick navigation from WFE to external system
/// </summary>
public class QuickLink : EntityBaseCreateInfo
{
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
    /// URL parameters configuration (JSON array)
    /// </summary>
    public string UrlParameters { get; set; } = "[]";
    
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
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Sort order for display
    /// </summary>
    public int SortOrder { get; set; }
    
    // Navigation Properties
    
    /// <summary>
    /// Parent integration
    /// </summary>
    public virtual Integration? Integration { get; set; }
}

