using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Unis.OperationalAttributes
{
    public enum StockRotationSelectOptionEnum
    {
        [Description("InventoryReportedBy")]
        InventoryReportedBy = 1,

        [Description("InventoryMethod")]
        InventoryMethod = 2,

        [Description("InventoryCount")]
        InventoryCount = 3,
        [Description("NotificationType")]
        NotificationType = 4
    }
}
