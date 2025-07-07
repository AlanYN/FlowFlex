using System;
using System.Collections.Generic;

namespace FlowFlex.Domain.Shared.Models.Customer.Detail
{
    public class CustomerBasicMessage
    {
        public string AccountId { get; set; } = "";
        public long? CrmPrimaryId { get; set; }
        public string CustomerCode { get; set; }
        public int Source { get; set; }
        public string SourceName { get; set; }
        public string CustomerName { get; set; }
        public string CustomerFullName { get; set; }
        public int CustomerType { get; set; }
        public string CustomerTypeName { get; set; }
        public int Category { get; set; }
        public string CategoryName { get; set; }
        public string Subsidiary { get; set; }
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
        public string Memo { get; set; }
        public string Website { get; set; }
        public string DunAndBradstreet { get; set; }
        public string InsuranceDetails { get; set; }
        public bool IsValid { get; set; }
        public DateTimeOffset CreateDate { get; set; }
        public DateTimeOffset ModifyDate { get; set; }
        public bool BillToOnly { get; set; } = false;
        public long? SubCustomerOf { get; set; }

        /// <summary>
        /// The date when the user was first approved and became a Customer
        /// </summary>
        public DateTimeOffset? CustomerFirstApprovedDate { get; set; }

        /// <summary>
        /// Creator
        /// </summary>
        public virtual string CreateBy { get; set; }

        /// <summary>
        /// Modifier
        /// </summary>
        public virtual string ModifyBy { get; set; }

        /// <summary>
        /// Creator's ID
        /// </summary>
        public virtual long CreateUserId { get; set; }

        /// <summary>
        /// Modifier's ID
        /// </summary>
        public virtual long ModifyUserId { get; set; }

        /// <summary>
        /// List of tags associated with the customer
        /// </summary>
        public List<int> Tags { get; set; }

        /// <summary>
        /// Individual customer's first name
        /// </summary>
        public string IndividualFirstName { get; set; }

        /// <summary>
        /// Individual customer's middle name
        /// </summary>
        public string IndividualMiddleName { get; set; }

        /// <summary>
        /// Individual customer's last name
        /// </summary>
        public string IndividualLastName { get; set; }

        /// <summary>
        /// Individual customer's birthday
        /// </summary>
        public DateTimeOffset? IndividualBirthday { get; set; }

        /// <summary>
        /// Individual customer's fax number
        /// </summary>
        public string IndividualFax { get; set; }

        /// <summary>
        /// Individual customer's job title
        /// </summary>
        public string IndividualTitle { get; set; }

        /// <summary>
        /// Individual customer's company name
        /// </summary>
        public string IndividualCompany { get; set; }

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

        /// <summary>
        /// Indicates if this is the default customer
        /// </summary>
        public bool IsDefaultCustomer { get; set; } = false;

        /// <summary>
        /// The registered address of the customer
        /// </summary>
        public string RegisteredAddress { get; set; }

        /// <summary>
        /// The registere city of the customer
        /// </summary>
        public string RegisteredCity { get; set; }
        /// <summary>
        /// The registere state of the customer
        /// </summary>

        public string RegisteredState { get; set; }

        /// <summary>
        /// The registere zip of the customer
        /// </summary>
        public string RegisteredZipCode { get; set; }

        /// <summary>
        /// The registere country of the customer
        /// </summary>
        public string RegisteredCountry { get; set; }

        /// <summary>
        /// The corporate address of the customer
        /// </summary>
        public string CorporateAddress { get; set; }

        /// <summary>
        /// The city of the corporate address
        /// </summary>
        public string CorporateCity { get; set; }

        /// <summary>
        /// The state of the corporate address
        /// </summary>
        public string CorporateState { get; set; }

        /// <summary>
        /// The zip code of the corporate address
        /// </summary>
        public string CorporateZipCode { get; set; }

        /// <summary>
        /// The country of the corporate address
        /// </summary>
        public string CorporateCountry { get; set; }
        public string Notes { get; set; }

        public string MID { get; set; }
    }
}
