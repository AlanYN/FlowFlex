using System.Collections.Generic;

namespace FlowFlex.Domain.Shared.Models.Customer.Detail
{
    /// <summary>
    /// Represents address information for a customer
    /// </summary>
    public class AddressMessage : DataMessageBase
    {
        /// <summary>
        /// List of address type identifiers
        /// </summary>
        public List<long> AddressType { get; set; }

        /// <summary>
        /// List of address type names
        /// </summary>
        public List<string> AddressTypeName { get; set; }

        /// <summary>
        /// Status of the address (e.g., active, inactive)
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Name of the address status
        /// </summary>
        public string StatusName { get; set; }

        /// <summary>
        /// Indicates if this is the default address
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Name associated with the address
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Phone number
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Phone extension
        /// </summary>
        public string PhoneExt { get; set; }

        /// <summary>
        /// Fax number
        /// </summary>
        public string Fax { get; set; }

        /// <summary>
        /// Email address
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Street address
        /// </summary>
        public string Street { get; set; }

        /// <summary>
        /// Unit or apartment number
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// City name
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// State or province
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// Zip or postal code
        /// </summary>
        public string ZipCode { get; set; }

        /// <summary>
        /// Country name
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// Additional notes or comments about the address
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Category of the address (optional)
        /// </summary>
        public int? Category { get; set; }

        /// <summary>
        /// Name of the address category
        /// </summary>
        public string CategoryName { get; set; }

        /// <summary>
        /// Prefix for the phone number (e.g., country code)
        /// </summary>
        public string PhoneNumberPrefix { get; set; }
    }
}
