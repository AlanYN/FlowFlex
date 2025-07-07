using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Item
{
    /// <summary>
    ///
    /// </summary>
    public enum BuyingTimeframeEnum
    {
        [Description("Less than 1 month")]
        LessThan1Month = 1,

        [Description("1-3 months")]
        OneToThreeMonths = 2,

        [Description("4-6 months")]
        FourToSixMonths = 3
    }
}
