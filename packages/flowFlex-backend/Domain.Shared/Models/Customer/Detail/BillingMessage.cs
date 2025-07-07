
using FlowFlex.Domain.Shared.Enums.Unis;

namespace FlowFlex.Domain.Shared.Models.Customer.Detail
{
    public class BillingMessage : DataMessageBase
    {
        /// <summary>
        /// String format. Only displayed for UF company.
        /// </summary>
        public string BillingCodePrefix { get; set; }

        /// <summary>
        /// Default invoice format for the customer.
        /// </summary>
        public string DefaultInvoiceFormat { get; set; }

        /// <summary>
        /// Revenue code. Only displayed for UT. Option field.
        /// </summary>
        public string RevenueCode { get; set; }

        /// <summary>
        /// Allows increments of 15 minutes. Only displayed for UF.
        /// </summary>
        public bool? AllowIncrementsOf15Min { get; set; }

        /// <summary>
        /// Invoice option. Only displayed for UT.
        /// </summary>
        public int? Invoice { get; set; }

        /// <summary>
        /// Invoice attachment file category. Only displayed for UT. Option field.
        /// </summary>
        public string InvoiceAttachmentFileCategory { get; set; }

        /// <summary>
        /// Option for sending invoices.
        /// </summary>
        public int? SendingOption { get; set; }

        /// <summary>
        /// Frequency of sending invoices.
        /// </summary>
        public int? SendingFrequency { get; set; }

        /// <summary>
        /// Send EDI option. When checked, a prompt should appear: "Send EDI checked, all invoices will be sent via EDI, except manual invoices. Otherwise, please use billing rule to set SendEDI!"
        /// </summary>
        public bool? SendEDI { get; set; }

        /// <summary>
        /// Specifies how invoices are generated.
        /// </summary>
        public int? InvoiceBy { get; set; }

        /// <summary>
        /// Supporting documentation option.
        /// </summary>
        public int? Supporting { get; set; }

        /// <summary>
        /// Field pushed by Client Portal. May be used in the future, field temporarily retained.
        /// </summary>
        public string SupportingImg { get; set; }

        /// <summary>
        /// Indicates whether invoice notes should be included in remittance information.
        /// </summary>
        public bool? InvoiceNotesInRemittanceInfo { get; set; }

        /// <summary>
        /// Invoice format enum.
        /// </summary>
        public CustomerBillingInvoiceFormatEnum? InvoiceFormat { get; set; }

        /// <summary>
        /// Name of the invoice format.
        /// </summary>
        public string InvoiceFormatName { get; set; }
    }
}
