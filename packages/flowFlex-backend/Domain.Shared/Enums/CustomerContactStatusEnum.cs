using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums
{
    public enum CustomerContactStatusEnum
    {
        [Description("Active")]
        Active = 1,
        [Description("Inactive")]
        Inactive = 2
    }
}
