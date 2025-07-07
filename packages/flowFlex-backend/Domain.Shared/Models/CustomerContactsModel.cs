using System.Collections.Generic;

namespace FlowFlex.Domain.Shared.Models
{
    public class CustomerContactsModel
    {
        /// <summary>
        /// Primary key ID
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Customer ID
        /// </summary>
        public long CustomerId { get; set; }
        /// <summary>
        /// Status
        /// </summary>
        public int Status { get; set; }
        /// <summary>
        /// Contact type
        /// </summary>
        public List<int> ContactType { get; set; }
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Email
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// Title
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Facility
        /// </summary>
        public List<string> Facility { get; set; }
        /// <summary>
        /// Work phone
        /// </summary>
        public string WorkPhone { get; set; }
        /// <summary>
        /// Work phone extension
        /// </summary>
        public string WorkPhoneExt { get; set; }
        /// <summary>
        /// Cell phone
        /// </summary>
        public string CellPhone { get; set; }
        /// <summary>
        /// Fax
        /// </summary>
        public string Fax { get; set; }
        ///// <summary>
        ///// Bill to - requirement confirmed to remove this field
        ///// </summary>
        //public int? BillTo { get; set; }
        ///// <summary>
        ///// Reference - requirement confirmed to remove this field
        ///// </summary>
        //public string Reference { get; set; }
        /// <summary>
        /// Notes
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Work phone number prefix ID
        /// </summary>
        public long? WorkPhoneNumberPrefixesId { get; set; }

        /// <summary>
        /// Cell phone number prefix ID
        /// </summary>
        public long? CellPhoneNumberPrefixesId { get; set; }

        /// <summary>
        /// Whether the contact should be CC'd
        /// </summary>
        public bool Iscc { get; set; }
    }
}
