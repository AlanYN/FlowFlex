using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Item
{
    public enum LifeCycleStageEnum
    {
        [Description("Leads")]
        Leads = 1,

        [Description("Opportunity")]
        Opportunity = 2,

        [Description("Customer")]
        Customer = 3,

        [Description("Other")]
        Other = 4
    }
}
