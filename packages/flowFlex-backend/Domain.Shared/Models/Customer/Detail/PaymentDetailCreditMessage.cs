using System;
using FlowFlex.Domain.Shared.Enums;

namespace FlowFlex.Domain.Shared.Models.Customer.Detail
{
    public class PaymentDetailCreditMessage : DataMessageBase
    {
        /// <summary>
        /// Credit card number validation rule
        /// </summary>
        public string CardNumber { get; set; }

        /// <summary>
        /// Name of the card holder
        /// </summary>
        public string CardHolder { get; set; }

        /// <summary>
        /// Card Verification Value (CVV) or Card Security Code (CSC)
        /// </summary>
        public string CVV { get; set; }

        /// <summary>
        /// Expiration date of the credit card
        /// </summary>
        public DateTimeOffset? Expires { get; set; }

        /// <summary>
        /// Zip code associated with the billing address
        /// </summary>
        public string Zip { get; set; }

        /// <summary>
        /// Primary billing address line
        /// </summary>
        public string Address1 { get; set; }

        /// <summary>
        /// Secondary billing address line (if applicable)
        /// </summary>
        public string Address2 { get; set; }

        /// <summary>
        /// Country of the billing address
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// State or province of the billing address
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// City of the billing address
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Logo or icon representing the credit card type
        /// </summary>
        public string CardLogo { get; set; }

        /// <summary>
        /// Type of payment method (e.g., Credit Card, Debit Card)
        /// </summary>
        public PaymentTypeEnum PaymentType { get; set; }

        /// <summary>
        /// Indicates whether this is the default payment method
        /// </summary>
        public bool IsDefault { get; set; }
    }
}
