using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Unis
{
    public enum FulfillmentTypeEnum
    {
        [Description("D2C Fulfillment")]
        D2CFulfillment = 1,
        [Description("Prep")]
        Prep = 2,
        [Description("Wholesale Fulfillment")]
        WholesaleFulfillment = 3,
        [Description("Ecommerce Fulfillment")]
        EcommerceFulfillment = 4,
        [Description("Parcel")]
        Parcel = 5,
        [Description("3PL")]
        ThreePL = 6,
        [Description("Replenishment")]
        Replenishment = 7,
        [Description("Refrigerated Storage")]
        RefrigeratedStorage = 8
    }
}
