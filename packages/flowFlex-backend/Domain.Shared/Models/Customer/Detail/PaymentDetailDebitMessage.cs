
using FlowFlex.Domain.Shared.Enums;

namespace FlowFlex.Domain.Shared.Models.Customer.Detail
{
    public class PaymentDetailDebitMessage : DataMessageBase
    {
        /// <summary>
        /// Name of the account holder
        /// </summary>
        public string AccountHolderName { get; set; }

        /// <summary>
        /// Bank routing number
        /// </summary>
        public string RoutingNumber { get; set; }

        /// <summary>
        /// Bank account number
        /// </summary>
        public string AccountNumber { get; set; }

        /// <summary>
        /// Card logo or icon
        /// </summary>
        public string CardLogo { get; set; } = string.Empty;

        /// <summary>
        /// Type of payment method
        /// </summary>
        public PaymentTypeEnum PaymentType { get; set; }

        /// <summary>
        /// Indicates if this is the default payment method
        /// </summary>
        public bool IsDefault { get; set; }
    }
}
