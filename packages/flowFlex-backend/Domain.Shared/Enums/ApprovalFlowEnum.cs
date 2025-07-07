using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowFlex.Domain.Shared.Enums
{
    /// <summary>
    /// Approval flow
    /// </summary>
    public enum ApprovalFlowEnum
    {
        [Description("None Approval")]
        NoneApproval = 9999,
        [Description("L1")]
        L1 = 1,
        [Description("L2")]
        L2 = 2,
        [Description("L3")]
        L3 = 3,
        [Description("L4")]
        L4 = 4,
        [Description("L5")]
        L5 = 5,
        [Description("L6")]
        L6 = 6,
        [Description("L7")]
        L7 = 7,
        [Description("L8")]
        L8 = 8,
        [Description("L9")]
        L9 = 9,
        [Description("L10")]
        L10 = 10
    }
}
