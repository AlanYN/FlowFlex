using System.ComponentModel.DataAnnotations;
using SqlSugar;
using FlowFlex.Domain.Entities.Base;

namespace FlowFlex.Domain.Entities.OW;

/// <summary>
/// Checklist Entity - Task List
/// </summary>
[SugarTable("ff_checklist")]
public class Checklist : EntityBaseCreateInfo
{
    /// <summary>
    /// Checklist Name
    /// </summary>

    [StringLength(100)]
    public string Name { get; set; }

    /// <summary>
    /// Checklist Description
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; }

    /// <summary>
    /// Assigned Team (Role)
    /// </summary>
    [StringLength(100)]
    public string Team { get; set; }

    /// <summary>
    /// Checklist Type (Template/Instance)
    /// </summary>
    [StringLength(20)]
    public string Type { get; set; } = "Template";

    /// <summary>
    /// Checklist Status (Active/Inactive)
    /// </summary>
    [StringLength(20)]
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Is Template
    /// </summary>
    public bool IsTemplate { get; set; } = true;

    /// <summary>
    /// Template Source ID (if created from template instance)
    /// </summary>
    [SugarColumn(ColumnName = "template_id")]
    public long? TemplateId { get; set; }

    /// <summary>
    /// Completion Rate (0-100)
    /// </summary>
    [SugarColumn(ColumnName = "completion_rate")]
    public decimal CompletionRate { get; set; } = 0;

    /// <summary>
    /// Total Task Count
    /// </summary>
    public int TotalTasks { get; set; } = 0;

    /// <summary>
    /// Completed Task Count
    /// </summary>
    public int CompletedTasks { get; set; } = 0;

    /// <summary>
    /// Estimated Completion Time (hours)
    /// </summary>
    public int EstimatedHours { get; set; } = 0;

    /// <summary>
    /// Is Active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Associated Workflow ID (optional)
    /// </summary>
    [SugarColumn(ColumnName = "workflow_id")]
    public long? WorkflowId { get; set; }

    /// <summary>
    /// Associated Stage ID (optional)
    /// </summary>
    [SugarColumn(ColumnName = "stage_id")]
    public long? StageId { get; set; }

    /// <summary>
    /// Task Items Collection
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public List<ChecklistTask> Tasks { get; set; } = new List<ChecklistTask>();
}
