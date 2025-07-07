using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.InternalNote;

/// <summary>
/// Internal note update DTO - 用于更新操作，所有字段都是可选的
/// 重要：StageId 是固定的，不允许在更新操作中修改，如需修改请使用专门的接口
/// 这样设计是为了防止意外修改关键的关联关系
/// </summary>
public class InternalNoteUpdateDto
{
    /// <summary>
    /// Onboarding ID (可选，如果提供则会验证并更新)
    /// </summary>
    public long? OnboardingId { get; set; }

    /// <summary>
    /// 备注标题 (可选)
    /// </summary>
    [StringLength(200)]
    public string? Title { get; set; }

    /// <summary>
    /// 备注内容 (可选)
    /// </summary>
    [StringLength(4000)]
    public string? Content { get; set; }

    /// <summary>
    /// 备注类型 (General, Important, Warning, Question, etc.)
    /// </summary>
    [StringLength(50)]
    public string? NoteType { get; set; }

    /// <summary>
    /// 优先级 (Low, Normal, High, Critical)
    /// </summary>
    [StringLength(20)]
    public string? Priority { get; set; }
}