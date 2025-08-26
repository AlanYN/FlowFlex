using System;

namespace FlowFlex.Application.Contracts.Dtos.OW.Checklist;

/// <summary>
/// Checklist task completion output DTO
/// </summary>
public class ChecklistTaskCompletionOutputDto
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Onboarding ID
    /// </summary>
    public long OnboardingId { get; set; }

    /// <summary>
    /// Lead ID
    /// </summary>
    public string LeadId { get; set; }

    /// <summary>
    /// Checklist ID
    /// </summary>
    public long ChecklistId { get; set; }

    /// <summary>
    /// Task ID
    /// </summary>
    public long TaskId { get; set; }

    /// <summary>
    /// Stage ID
    /// </summary>
    public long? StageId { get; set; }

    /// <summary>
    /// 是否已完成
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTimeOffset? CompletedTime { get; set; }

    /// <summary>
    /// 完成备注
    /// </summary>
    public string CompletionNotes { get; set; }

    /// <summary>
    /// 相关文件JSON（文件信息数组的JSON字符串）
    /// </summary>
    public string FilesJson { get; set; }

    /// <summary>
    /// 提交来源
    /// </summary>
    public string Source { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    public string CreateBy { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifyDate { get; set; }

    /// <summary>
    /// 修改人
    /// </summary>
    public string ModifyBy { get; set; }
}