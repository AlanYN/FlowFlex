using FlowFlex.Domain.Entities.Base;
using Domain.Shared.Enums;
using SqlSugar;

namespace FlowFlex.Domain.Entities.Integration;

/// <summary>
/// Integration action - automated actions associated with integration
/// </summary>
[SugarTable("ff_integration_action")]
public class IntegrationAction : EntityBase
{
    /// <summary>
    /// Tenant ID
    /// </summary>
    [SugarColumn(ColumnName = "tenant_id")]
    public string TenantId { get; set; } = "default";

    /// <summary>
    /// App code
    /// </summary>
    [SugarColumn(ColumnName = "app_code")]
    public string AppCode { get; set; } = "DEFAULT";

    /// <summary>
    /// Integration ID
    /// </summary>
    [SugarColumn(ColumnName = "integration_id")]
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
    
    // Navigation Properties (ignored by SqlSugar)
    
    /// <summary>
    /// Parent integration
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public virtual Integration? Integration { get; set; }
}

