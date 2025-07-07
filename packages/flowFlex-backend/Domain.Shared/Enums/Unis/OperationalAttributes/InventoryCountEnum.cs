using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Unis
{
    public enum InventoryCountEnum
    {
        /// <summary>
        /// ״̬
        /// </summary>
        [Description("Cycle Count")]
        CycleCount = 1,

        [Description("Full Inventory Count")]
        FullInventoryCount = 2,

        [Description("Both")]
        Both = 3
    }
}
