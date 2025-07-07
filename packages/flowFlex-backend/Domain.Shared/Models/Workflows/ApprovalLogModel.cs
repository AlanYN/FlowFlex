using System;
using FlowFlex.Domain.Shared.Enums;

namespace FlowFlex.Domain.Shared.Models.Workflows;

public class ApprovalStageModel
{
    public long StageId { get; set; }

    public string TaskId { get; set; }

    public string ApproveUserName { get; set; }

    public long ApproveUserId { get; set; }

    public ApproveStatus Status { get; set; }

    public DateTimeOffset ApproveDate { get; set; }

    public string Comment { get; set; }
}
