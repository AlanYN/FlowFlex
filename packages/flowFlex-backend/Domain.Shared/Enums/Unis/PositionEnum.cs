using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Unis
{
    public enum PositionEnum
    {
        [Description("LTL")]
        LTL = 1,

        [Description("OTR")]
        OTR = 2,

        [Description("Drayage")]
        Drayage = 3,

        [Description("Bulk up")]
        Bulk = 4,

        [Description("Owner Operator")]
        Owner = 5,

        [Description("Carriers (CA)")]
        Carriers = 6,

        [Description("POV")]
        POV = 7
    }
}
