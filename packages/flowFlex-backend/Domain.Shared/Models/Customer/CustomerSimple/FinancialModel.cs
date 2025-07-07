using System;

namespace FlowFlex.Domain.Shared.Models.Customer.CustomerSimple
{
    public class FinancialModel
    {

        /// <summary>
        /// The expiration date of the grace period
        /// </summary>
        public DateTimeOffset? GracePeriodExpDate { get; set; }

        /// <summary>
        /// Indicates whether the customer's credit is on hold
        /// </summary>
        public bool? CreditHold { get; set; }
    }
}
