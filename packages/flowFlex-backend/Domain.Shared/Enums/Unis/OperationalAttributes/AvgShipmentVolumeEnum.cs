using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Unis.OperationalAttributes
{
    public enum AvgShipmentVolumeEnum
    {
        [Description("1-15 Packages/month")]
        PackagesMonth1_15 = 1,

        [Description("16-50 Packages/month")]
        PackagesMonth16_50 = 2,

        [Description("51-200 Packages/month ")]
        PackagesMonth51_200 = 3,

        [Description("200+ Packages/month")]
        PackagesMonth200Plus = 4
    }
}
