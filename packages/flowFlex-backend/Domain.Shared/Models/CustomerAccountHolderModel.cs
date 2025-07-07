namespace FlowFlex.Domain.Shared.Models
{
    public class CustomerAccountHolderModel
    {
        /// <summary>
        /// ID
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Customer ID
        /// </summary>
        public long CustomerId { get; set; }
        /// <summary>
        /// List provided by Wise interface - Facility
        /// </summary>
        public string Facility { get; set; }
        /// <summary>
        /// Category ID
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// User ID under data group
        /// </summary>
        public string AssigneeId { get; set; }
        /// <summary>
        /// User name under data group (firstName + , + lastName)
        /// </summary>
        public string AssigneeName { get; set; }
        /// <summary>
        /// Email
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// Status
        /// </summary>
        public bool Status { get; set; }

        /// <summary>
        /// Phone number
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// Notes
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Phone area code identifier
        /// </summary>
        public long? PhoneNumberPrefixesId { get; set; }
    }
}
