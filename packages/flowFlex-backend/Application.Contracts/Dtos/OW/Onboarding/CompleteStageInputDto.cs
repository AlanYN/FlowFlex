using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding;

/// <summary>
/// Complete stage input DTO
/// </summary>
public class CompleteStageInputDto
{
    /// <summary>
    /// 完成备注
    /// </summary>
    [StringLength(1000)]
    public string CompletionNotes { get; set; } = string.Empty;

    /// <summary>
    /// 是否自动移动到下一阶段
    /// </summary>
    public bool AutoMoveToNext { get; set; } = true;

    /// <summary>
    /// 完成评分 (1-5)
    /// </summary>
    public int? Rating { get; set; }

    /// <summary>
    /// 反馈信息
    /// </summary>
    [StringLength(2000)]
    public string Feedback { get; set; } = string.Empty;

    /// <summary>
    /// 附件URL列表 (JSON)
    /// </summary>
    public string AttachmentsJson { get; set; } = string.Empty;

    /// <summary>
    /// 完成人姓名
    /// </summary>
    [StringLength(100)]
    public string CompletedBy { get; set; } = "System";

    /// <summary>
    /// 完成人ID
    /// </summary>
    public long? CompletedById { get; set; }
}