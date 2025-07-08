using System.ComponentModel.DataAnnotations;
using SqlSugar;
using FlowFlex.Domain.Entities.Base;

namespace FlowFlex.Domain.Entities.OW;

/// <summary>
/// Checklist Task Completion Entity - Task Completion Status
/// </summary>
[SugarTable("ff_checklist_task_completion")]
public class ChecklistTaskCompletion : EntityBaseCreateInfo
{
    /// <summary>
    /// Onboarding ID
    /// </summary>

    [SugarColumn(ColumnName = "onboarding_id")]
    public long OnboardingId { get; set; }

    /// <summary>
    /// Lead ID
    /// </summary>

    [StringLength(100)]
    [SugarColumn(ColumnName = "lead_id")]
    public string LeadId { get; set; }

    /// <summary>
    /// Checklist ID
    /// </summary>

    [SugarColumn(ColumnName = "checklist_id")]
    public long ChecklistId { get; set; }

    /// <summary>
    /// Task ID
    /// </summary>

    [SugarColumn(ColumnName = "task_id")]
    public long TaskId { get; set; }

    /// <summary>
    /// Is Completed
    /// </summary>
    [SugarColumn(ColumnName = "is_completed")]
    public bool IsCompleted { get; set; } = false;

    /// <summary>
    /// Completion Time
    /// </summary>
    [SugarColumn(ColumnName = "completed_time")]
    public DateTimeOffset? CompletedTime { get; set; }

    /// <summary>
    /// Completion Notes
    /// </summary>
    [StringLength(500)]
    [SugarColumn(ColumnName = "completion_notes")]
    public string CompletionNotes { get; set; } = string.Empty;

    /// <summary>
    /// Submission Source
    /// </summary>
    [StringLength(50)]
    [SugarColumn(ColumnName = "source")]
    public string Source { get; set; } = "customer_portal";

    /// <summary>
    /// IP Address
    /// </summary>
    [StringLength(50)]
    [SugarColumn(ColumnName = "ip_address")]
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// User Agent
    /// </summary>
    [StringLength(500)]
    [SugarColumn(ColumnName = "user_agent")]
    public string UserAgent { get; set; } = string.Empty;
}
