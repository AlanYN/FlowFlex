using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Unis.OperationalAttributes
{
    public enum ShipCommodityTypeEnum
    {
        [Description("Electronics")]
        Electronics = 1,

        [Description("Perishable Goods")]
        PerishableGoods = 2,

        [Description("General Merchandise")]
        GeneralMerchandise = 3,

        [Description("Apparel")]
        Apparel = 4,

        [Description("Hazardous Materials")]
        HazardousMaterials = 5,

        [Description("Other")]
        Other = 6
    }
}
