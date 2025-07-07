using System.ComponentModel.DataAnnotations;
using SqlSugar;
using FlowFlex.Domain.Entities.Base;

namespace FlowFlex.Domain.Entities.OW;

/// <summary>
/// Checklist Task Entity - Task List Item
/// </summary>
    [SugarTable("ff_checklist_task")]
public class ChecklistTask : EntityBaseCreateInfo
{
    /// <summary>
    /// Associated Checklist Primary Key ID
    /// </summary>
    [SugarColumn(ColumnName = "checklist_id")]
    public long ChecklistId { get; set; }

    /// <summary>
    /// Task Name
    /// </summary>
    
    [StringLength(200)]
    public string Name { get; set; }

    /// <summary>
    /// Task Description
    /// </summary>
    [StringLength(1000)]
    public string Description { get; set; }

    /// <summary>
    /// Task Type (Manual/Automatic/Document/Approval)
    /// </summary>
    [StringLength(20)]
    [SugarColumn(ColumnName = "task_type")]
    public string TaskType { get; set; } = "Manual";

    /// <summary>
    /// Is Completed
    /// </summary>
    public bool IsCompleted { get; set; } = false;

    /// <summary>
    /// Is Required
    /// </summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// Assignee ID
    /// </summary>
    [SugarColumn(ColumnName = "assignee_id")]
    public long? AssigneeId { get; set; }

    /// <summary>
    /// Assignee Name
    /// </summary>
    [StringLength(100)]
    [SugarColumn(ColumnName = "assignee_name")]
    public string AssigneeName { get; set; }

    /// <summary>
    /// Assigned Team
    /// </summary>
    [StringLength(100)]
    public string AssignedTeam { get; set; }

    /// <summary>
    /// Task Priority (Low/Medium/High/Critical)
    /// </summary>
    [StringLength(20)]
    public string Priority { get; set; } = "Medium";

    /// <summary>
    /// Sort Order
    /// </summary>
    [SugarColumn(ColumnName = "order_index")]
    public int Order { get; set; }

    /// <summary>
    /// Estimated Completion Time (hours)
    /// </summary>
    public int EstimatedHours { get; set; } = 0;

    /// <summary>
    /// Actual Completion Time (hours)
    /// </summary>
    public int ActualHours { get; set; } = 0;

    /// <summary>
    /// Due Date
    /// </summary>
    [SugarColumn(ColumnName = "due_date")]
    public DateTimeOffset? DueDate { get; set; }

    /// <summary>
    /// Completion Date
    /// </summary>
    [SugarColumn(ColumnName = "completed_date")]
    public DateTimeOffset? CompletedDate { get; set; }

    /// <summary>
    /// Completion Notes
    /// </summary>
    [StringLength(500)]
    public string CompletionNotes { get; set; }

    /// <summary>
    /// Dependent Task ID (prerequisite task)
    /// </summary>
    [SugarColumn(ColumnName = "depends_on_task_id")]
    public long? DependsOnTaskId { get; set; }

    /// <summary>
    /// Attachment URL List (JSON)
    /// </summary>
    [SugarColumn(ColumnName = "attachments_json")]
    public string AttachmentsJson { get; set; }

    /// <summary>
    /// Task Status (Pending/InProgress/Completed/Blocked/Cancelled)
    /// </summary>
    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Is Active
    /// </summary>
    public bool IsActive { get; set; } = true;
}
