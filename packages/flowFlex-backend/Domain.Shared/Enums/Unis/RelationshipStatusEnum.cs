using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Unis
{
    public enum RelationshipStatusEnum
    {
        [Description("Active")]
        Active = 1,

        [Description("Inactive")]
        Inactive = 2,

        [Description("Expection")]
        Expection = 3
    }
}
