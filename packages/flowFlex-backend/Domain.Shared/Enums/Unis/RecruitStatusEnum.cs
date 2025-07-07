using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Unis
{
    public enum RecruitStatusEnum
    {
        [Description("Ready for Terminal Manager")]
        Ready = 1,

        [Description("Offer Letter")]
        Offer = 2,

        [Description("Hired")]
        Hired = 3,

        [Description("Closed")]
        Closed = 4
    }
}
