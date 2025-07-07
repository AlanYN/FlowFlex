using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Unis.OperationalAttributes
{
    public enum AdditionalServiceEnum
    {
        [Description("Collect on Delivery (COD)")]
        COD = 1,

        [Description("Construction Site Delivery")]
        ConstructionSiteDelivery = 2,

        [Description("Customers or In-Bond")]
        CustomersOrInBond = 3,

        [Description("Extra Labor for Pickup or Delivery")]
        ELPD = 4,

        [Description("Hazardous Material")]
        AdditionalService = 5,

        [Description("Inside Pickup or Delivery")]
        IPD = 6,

        [Description("Liftgate")]
        Liftgate = 7,

        [Description("Protective Service")]
        ProtectiveService = 8,

        [Description("Residential Delivery")]
        ResidentialDelivery = 9,

        [Description("Sorting or Segregating")]
        SortingSegregating = 10,

        [Description("Weekend or Holiday Pickup or Delivery")]
        WHPUOD = 11,
    }
}
