using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Unis
{
    public enum OutboundMethodsEnum
    {
        [Description("CPU")]
        CPU = 1,
        [Description("Routed")]
        Routed = 2,
        [Description("Both")]
        Both = 3
    }
}
