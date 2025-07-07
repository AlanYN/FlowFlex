using System.Collections.Generic;
using System;

namespace FlowFlex.Domain.Shared.Models.Customer.CustomerSimple
{
    public class BasicModel
    {
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public string CustomerFullName { get; set; }
        public int CustomerType { get; set; }
        public string CustomerTypeName { get; set; }
        public int Category { get; set; }
        public string CategoryName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public int Status { get; set; }
        public string StatusName { get; set; }
        public string Group { get; set; }
        public string GroupName { get; set; }
        public DateTimeOffset? CurrentContactPeriod { get; set; }
        public int? TypeOfBusiness { get; set; }
        public string TypeOfBusinessName { get; set; }
        public string LineOfBusiness { get; set; }
        public string LineOfBusinessName { get; set; }
        public string DunBradstreetNo { get; set; }
        public decimal? AnnualSpending { get; set; }
        public string MonthlyCreditRequired { get; set; }
        public string ParentCompany { get; set; }
        public decimal? NumberOfEmployees { get; set; }
        public DateTimeOffset? DateEstablished { get; set; }
        public string Website { get; set; }
        public string DunAndBradstreet { get; set; }
        public string InsuranceDetails { get; set; }
        /// <summary>
        /// The date when the user was first approved and became a Customer
        /// </summary>
        public DateTimeOffset? CustomerFirstApprovedDate { get; set; }
        /// <summary>
        /// List of tags associated with the customer
        /// </summary>
        public List<int> Tags { get; set; }

        /// <summary>
        /// ID of the customer's logo image
        /// </summary>
        public long? LogoImageID { get; set; }

        /// <summary>
        /// Zebra code for the customer's logo
        /// </summary>
        public string LogoZebraCode { get; set; }

        /// <summary>
        /// Customer's preferred currency
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// Customer's country
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// Phone number prefix for the customer's country
        /// </summary>
        public string PhoneNumberPrefix { get; set; }
        public string Notes { get; set; }
        public string TaxID { get; set; }
        public string Subsidiary { get; set; }
        public bool OnHold { get; set; } = false;
    }
}
