using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Unis
{
    public enum ShiftEnum
    {
        [Description("Day")]
        Day = 1,

        [Description("Night")]
        Night = 2,

        [Description("Both")]
        Both = 3
    }
}
