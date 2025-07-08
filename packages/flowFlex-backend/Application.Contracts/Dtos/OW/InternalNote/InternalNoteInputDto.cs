using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.InternalNote;

/// <summary>
/// Internal note input DTO
/// </summary>
public class InternalNoteInputDto
{
    /// <summary>
    /// Onboarding ID
    /// </summary>

    public long OnboardingId { get; set; }

    /// <summary>
    /// Stage ID (可选，如果为空则是整个Onboarding的备注)
    /// </summary>
    public long? StageId { get; set; }

    /// <summary>
    /// 备注标题 (可选)
    /// </summary>
    [StringLength(200)]
    public string? Title { get; set; }

    /// <summary>
    /// 备注内容
    /// </summary>

    [StringLength(4000)]
    public string Content { get; set; }

    /// <summary>
    /// 备注类型 (General, Important, Warning, Question, etc.)
    /// </summary>
    [StringLength(50)]
    public string NoteType { get; set; } = "General";

    /// <summary>
    /// 优先级 (Low, Normal, High, Critical)
    /// </summary>
    [StringLength(20)]
    public string Priority { get; set; } = "Normal";
}