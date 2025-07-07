using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Unis.OperationalAttributes
{
    public enum SelectOptionEnum
    {
        #region UF

        [Description("FulfillmentType")]
        FulfillmentType = 1,

        [Description("AverageStack")]
        AverageStack = 2,

        #endregion UF

        #region UT

        [Description("ShipCommodityType")]
        ShipCommodityType = 3,

        [Description("AvgShipmentVolume")]
        AvgShipmentVolume = 4,

        [Description("FreightPackaged")]
        FreightPackaged = 5,

        [Description("AdditionalService")]
        AdditionalService = 6

        #endregion UT

    }
}
