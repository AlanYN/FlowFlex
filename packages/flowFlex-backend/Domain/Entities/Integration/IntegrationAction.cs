using FlowFlex.Domain.Entities.Base;
using Domain.Shared.Enums;

namespace FlowFlex.Domain.Entities.Integration;

/// <summary>
/// Integration action - automated actions associated with integration
/// </summary>
public class IntegrationAction : EntityBase
{
    /// <summary>
    /// Integration ID
    /// </summary>
    public long IntegrationId { get; set; }
    
    /// <summary>
    /// Action code (e.g., "ACT-101")
    /// </summary>
    public string ActionCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Action name
    /// </summary>
    public string ActionName { get; set; } = string.Empty;
    
    /// <summary>
    /// Action type
    /// </summary>
    public ActionType ActionType { get; set; }
    
    /// <summary>
    /// Action status
    /// </summary>
    public ActionStatus Status { get; set; }
    
    /// <summary>
    /// Assigned workflow IDs (JSON array)
    /// </summary>
    public string AssignedWorkflowIds { get; set; } = "[]";
    
    /// <summary>
    /// Action configuration (JSON)
    /// </summary>
    public string Configuration { get; set; } = "{}";
    
    // Navigation Properties
    
    /// <summary>
    /// Parent integration
    /// </summary>
    public virtual Integration? Integration { get; set; }
}

