using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Unis
{
    public enum CustomerBillingInvoiceFormatEnum
    {
        /// <summary>
        /// Regular Invoice
        /// </summary>
        [Description("Regular Invoice")]
        RegularInvoice = 1,
        /// <summary>
        /// Master Invoice
        /// </summary>
        [Description("Master Invoice")]
        MasterInvoice = 2
    }
}
