using System.ComponentModel.DataAnnotations;
using SqlSugar;
using FlowFlex.Domain.Entities.Base;

namespace FlowFlex.Domain.Entities.OW;

/// <summary>
/// Checklist Task Note Entity - Task Notes and Comments
/// </summary>
[SugarTable("ff_checklist_task_note")]
public class ChecklistTaskNote : EntityBaseCreateInfo
{
    /// <summary>
    /// Task ID
    /// </summary>
    [SugarColumn(ColumnName = "task_id")]
    public long TaskId { get; set; }

    /// <summary>
    /// Onboarding ID
    /// </summary>
    [SugarColumn(ColumnName = "onboarding_id")]
    public long OnboardingId { get; set; }

    /// <summary>
    /// Note content
    /// </summary>
    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Note type (General, Security, Progress, Issue, etc.)
    /// </summary>
    [StringLength(50)]
    [SugarColumn(ColumnName = "note_type")]
    public string NoteType { get; set; } = "General";

    /// <summary>
    /// Priority level (High, Medium, Low)
    /// </summary>
    [StringLength(20)]
    public string Priority { get; set; } = "Medium";

    /// <summary>
    /// Created by user ID
    /// </summary>
    [StringLength(100)]
    [SugarColumn(ColumnName = "created_by_id")]
    public string CreatedById { get; set; } = string.Empty;

    /// <summary>
    /// Created by user name
    /// </summary>
    [StringLength(100)]
    [SugarColumn(ColumnName = "created_by_name")]
    public string CreatedByName { get; set; } = string.Empty;

    /// <summary>
    /// Last modified by user ID
    /// </summary>
    [StringLength(100)]
    [SugarColumn(ColumnName = "modified_by_id", IsNullable = true)]
    public string? ModifiedById { get; set; }

    /// <summary>
    /// Last modified by user name
    /// </summary>
    [StringLength(100)]
    [SugarColumn(ColumnName = "modified_by_name", IsNullable = true)]
    public string? ModifiedByName { get; set; }

    /// <summary>
    /// Whether the note is deleted (soft delete)
    /// </summary>
    [SugarColumn(ColumnName = "is_deleted")]
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Whether the note is pinned
    /// </summary>
    [SugarColumn(ColumnName = "is_pinned")]
    public bool IsPinned { get; set; } = false;
}