using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Unis.CustomerAccounts
{
    /// <summary>
    /// Customer sorting
    /// </summary>
    public enum CustomerOrderByEnum
    {
        [Description("CustomerName")]
        CustomerName = 1,

        [Description("CreatedTime")]
        CreatedTime = 2
    }
}
