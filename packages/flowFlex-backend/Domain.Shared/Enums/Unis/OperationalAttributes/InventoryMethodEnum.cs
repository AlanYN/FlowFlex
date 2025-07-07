using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Unis
{
    public enum InventoryMethodEnum
    {
        [Description("FIFO")]
        FIFO = 1,

        [Description("LIFO")]
        LIFO = 2,

        [Description("Other")]
        Other = 3
    }
}
