using System.Collections.Generic;

namespace FlowFlex.Domain.Shared.Models.Customer.Detail
{
    public class CustomerMessage : MessageDataBase
    {
        /// <summary>
        /// Basic customer information
        /// </summary>
        public CustomerBasicMessage Basic { get; set; }

        /// <summary>
        /// List of customer addresses
        /// </summary>
        public List<AddressMessage> Addresses { get; set; } = new();

        /// <summary>
        /// List of customer contacts
        /// </summary>
        public List<ContactMessage> Contacts { get; set; } = new();

        /// <summary>
        /// List of account holders associated with the customer
        /// </summary>
        public List<AccountHolderMessage> AccountHolders { get; set; } = new();

        /// <summary>
        /// Customer notes
        /// </summary>
        public NoteMessage Note { get; set; }

        /// <summary>
        /// Financial information of the customer
        /// </summary>
        public FinancialMessage Financial { get; set; }

        /// <summary>
        /// Additional information to be carried in JSON format
        /// </summary>
        public string OtherMessageJson { get; set; }

        /// <summary>
        /// List of debit payment details
        /// </summary>
        public List<PaymentDetailDebitMessage> PaymentDetailDebits { get; set; } = new();

        /// <summary>
        /// List of credit payment details
        /// </summary>
        public List<PaymentDetailCreditMessage> PaymentDetailCredits { get; set; } = new();

        /// <summary>
        /// List of program attributes associated with the customer
        /// </summary>
        public List<ProgramAttributeMessage> ProgramAttributes { get; set; } = new();

        /// <summary>
        /// List of credit application headers
        /// </summary>
        public List<CreditAppHeadMessage> CreditAppHeads { get; set; } = new();

        /// <summary>
        /// List of credit application details
        /// </summary>
        public List<CreditAppDetailMessage> CreditAppDetails { get; set; } = new();

        /// <summary>
        /// List of files associated with the customer
        /// </summary>
        public List<FileMessage> Files { get; set; } = new();

        /// <summary>
        /// Billing information of the customer
        /// </summary>
        public BillingMessage Billing { get; set; }

        /// <summary>
        /// List of bill-to mappings for the customer
        /// </summary>
        public List<BillToMappingMessage> BillToMappings { get; set; } = new();

        /// <summary>
        /// List of contracts associated with the customer
        /// </summary>
        public List<ContractMessage> Contracts { get; set; } = new();

        /// <summary>
        /// List of relationships the customer has with other entities
        /// </summary>
        public List<RelationshipMessage> Relationships { get; set; } = new();

        /// <summary>
        /// List of facilities associated with the customer
        /// </summary>
        public List<CustomerFacilityMessage> CustomerFacilities { get; set; } = new();
    }
}
