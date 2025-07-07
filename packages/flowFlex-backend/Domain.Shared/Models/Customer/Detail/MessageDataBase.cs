using System;

namespace FlowFlex.Domain.Shared.Models.Customer.Detail
{
    /// <summary>
    /// Base class for message data in the CRM system
    /// </summary>
    public class MessageDataBase
    {
        /// <summary>
        /// The unique identifier of the company associated with this message
        /// </summary>
        public string CompanyId { get; set; }
    }
}
