using System;
using System.Collections.Generic;

namespace FlowFlex.Domain.Shared.Models.Customer.Detail
{
    public class ContactMessage : DataMessageBase
    {
        /// <summary>
        /// Status of the contact
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// List of contact type identifiers
        /// </summary>
        public List<int> ContactType { get; set; }

        /// <summary>
        /// List of contact type names
        /// </summary>
        public List<String> ContactTypeName { get; set; }

        /// <summary>
        /// Name of the contact
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Title or position of the contact
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// List of facility identifiers associated with the contact
        /// </summary>
        public List<int> Facility { get; set; }

        /// <summary>
        /// List of facility names associated with the contact
        /// </summary>
        public List<string> FacilityName { get; set; }

        /// <summary>
        /// Email address of the contact
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Work phone number of the contact
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// Extension for the work phone number
        /// </summary>
        public string WorkPhoneExt { get; set; }

        /// <summary>
        /// Cell phone number of the contact
        /// </summary>
        public string CellPhone { get; set; }

        /// <summary>
        /// Fax number of the contact
        /// </summary>
        public string Fax { get; set; }

        /// <summary>
        /// Reference information for the contact
        /// </summary>
        public string Reference { get; set; }

        /// <summary>
        /// Additional notes about the contact
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Prefix for the cell phone number
        /// </summary>
        public string CellPhoneNumberPrefix { get; set; }

        /// <summary>
        /// Prefix for the work phone number
        /// </summary>
        public string WorkPhoneNumberPrefix { get; set; }
    }
}
