using System;
using System.Collections.Generic;

namespace FlowFlex.Domain.Shared.Models.Workflows
{
    public class WorkflowModel
    {
        public long WorkflowId { get; set; }

        public bool IsEnd { get; set; }

        public bool IsCurrentUserApprove { get; set; }

        public bool IsApprove => IsEnd == false && IsCurrentUserApprove;

        public List<PaddingModel> Padding { get; set; }

        public List<ApproveModel> Approve { get; set; }
    }

    public class PaddingModel
    {
        public long RoleId { get; set; }
    }

    public class ApproveModel
    {
        public long UserId { get; set; }

        public string UserName { get; set; }

        public DateTimeOffset ApproveDate { get; set; }
    }
}
