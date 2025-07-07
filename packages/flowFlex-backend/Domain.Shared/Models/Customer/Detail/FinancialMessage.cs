using System;

namespace FlowFlex.Domain.Shared.Models.Customer.Detail
{
    /// <summary>
    /// Represents financial information for a customer
    /// </summary>
    public class FinancialMessage : DataMessageBase
    {
        /// <summary>
        /// The current balance of the customer's account
        /// </summary>
        public decimal? CurrentBalance { get; set; }

        /// <summary>
        /// The credit limit assigned to the customer
        /// </summary>
        public decimal? CreditLimit { get; set; }

        /// <summary>
        /// The expiration date of the grace period
        /// </summary>
        public DateTimeOffset? GracePeriodExpDate { get; set; }

        /// <summary>
        /// Indicates whether the customer's credit is on hold
        /// </summary>
        public bool? CreditHold { get; set; }

        /// <summary>
        /// The class or category of the customer
        /// </summary>
        public int? Class { get; set; }

        /// <summary>
        /// The name of the customer's class or category
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// The default Accounts Receivable account for the customer
        /// </summary>
        public int? DefaultARAccount { get; set; }

        /// <summary>
        /// The name of the default Accounts Receivable account
        /// </summary>
        public string DefaultARAccountName { get; set; }

        /// <summary>
        /// The company that the customer represents, if applicable
        /// </summary>
        public int? RepresentsCompany { get; set; }

        /// <summary>
        /// The name of the company that the customer represents
        /// </summary>
        public string RepresentsCompanyName { get; set; }

        /// <summary>
        /// Indicates whether the customer is exempt from credit application forms
        /// </summary>
        public int? CreditAppFormExempt { get; set; }

        /// <summary>
        /// The name or description of the credit application form exemption
        /// </summary>
        public string CreditAppFormExemptName { get; set; }

        /// <summary>
        /// The net payment term for the customer
        /// </summary>
        public int? NetTerm { get; set; }

        /// <summary>
        /// The name or description of the net payment term
        /// </summary>
        public string NetTermName { get; set; }

        /// <summary>
        /// The number of grace period days allowed for the customer
        /// </summary>
        public int? GracePeriodDays { get; set; }

        /// <summary>
        /// The minimum prepaid amount required from the customer
        /// </summary>
        public decimal? MinPrepaidAmount { get; set; }

        /// <summary>
        /// The allowance granted to the customer
        /// </summary>
        public string Allowance { get; set; }

        /// <summary>
        /// The tax identification number of the customer
        /// </summary>
        public string TaxID { get; set; }

        /// <summary>
        /// The currency code used for the customer's transactions
        /// </summary>
        public int? CurrencyCode { get; set; }

        /// <summary>
        /// The name of the currency used for the customer's transactions
        /// </summary>
        public string CurrencyCodeName { get; set; }

        /// <summary>
        /// The contract terms agreed with the customer
        /// </summary>
        public string ContractTerms { get; set; }

        /// <summary>
        /// The number of grace period days (possibly redundant with GracePeriodDays)
        /// </summary>
        public int? GracePeriodDay { get; set; }

        /// <summary>
        /// The ID of the user who placed the customer's credit on hold
        /// </summary>
        public int? CreditHoldUserId { get; set; }

        /// <summary>
        /// Indicates whether the customer is exempted from paperwork requirements
        /// </summary>
        public bool? PaperWorkExempted { get; set; }

        /// <summary>
        /// Indicates whether insurance is required for the customer
        /// </summary>
        public bool? InsuranceRequired { get; set; }
    }
}
