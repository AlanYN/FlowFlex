using System.Collections.Generic;

namespace FlowFlex.Domain.Shared.Models.Customer.Detail
{
    /// <summary>
    /// Represents a message containing customer facility information.
    /// This class inherits from DataMessageBase and is used to transfer facility-related data for a specific customer.
    /// </summary>
    public class CustomerFacilityMessage : DataMessageBase
    {
        /// <summary>
        /// Gets or sets the facility name or identifier associated with the customer.
        /// </summary>
        public string Facility { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the customer.
        /// </summary>
        public long CustomerId { get; set; }
    }
}
