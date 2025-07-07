using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums
{
    public enum ImportTypeEnum
    {
        [Description("Contact")]
        Contact = 1,

        [Description("Company")]
        Company = 2,

        [Description("Deal")]
        Deal = 3,

        [Description("CustomerCubework")]
        CustomerCubework = 101,

        [Description("RetailerMapping")]
        RetailerMapping = 102,

        [Description("TitleMapping")]
        TitleMapping = 103,
    }
}
