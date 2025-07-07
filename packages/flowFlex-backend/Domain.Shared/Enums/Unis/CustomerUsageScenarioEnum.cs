using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Unis
{
    /// <summary>
    /// The usage scenario to filter by:1. Customer 2. DefaultCustomer 3. Merchant ( Default value is 1 Customer).
    /// </summary>
    public enum CustomerUsageScenarioEnum
    {
        [Description("Customer")]
        Customer = 1,
        [Description("DefaultCustomer")]
        DefaultCustomer = 2,
        [Description("Merchant")]
        Merchant = 3
    }
}
