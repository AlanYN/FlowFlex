using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Unis
{
    /// <summary>
    /// Query condition type distinction
    /// </summary>
    public enum ConditionsTypeEnum
    {
        [Description("PayTermList")]
        PayTermList = 1,

        [Description("ApprovalFlowList")]
        ApprovalFlowList = 2,
    }
}
