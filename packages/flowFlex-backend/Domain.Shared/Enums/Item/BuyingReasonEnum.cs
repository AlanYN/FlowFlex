using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Item
{
    /// <summary>
    /// Buying Reason options for company, contact, and transaction
    /// </summary>
    public enum BuyingReasonEnum
    {
        [Description("New Requirement")]
        NewRequirement = 1,

        [Description("Other")]
        Other = 2,

        [Description("Replace Existing")]
        ReplaceExisting = 3
    }
}
