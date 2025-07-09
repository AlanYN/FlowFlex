using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding;

/// <summary>
/// Reject onboarding input DTO
/// </summary>
public class RejectOnboardingInputDto
{
    /// <summary>
    /// 拒绝原因
    /// </summary>

    [StringLength(1000)]
    public string RejectionReason { get; set; }

    /// <summary>
    /// 是否终止整个工作流
    /// </summary>
    public bool TerminateWorkflow { get; set; } = true;

    /// <summary>
    /// 拒绝人姓名
    /// </summary>
    [StringLength(100)]
    public string RejectedBy { get; set; } = "System";

    /// <summary>
    /// 拒绝人ID
    /// </summary>
    public long? RejectedById { get; set; }

    /// <summary>
    /// 附加备注
    /// </summary>
    [StringLength(2000)]
    public string AdditionalNotes { get; set; }

    /// <summary>
    /// 是否发送通知
    /// </summary>
    public bool SendNotification { get; set; } = true;
}