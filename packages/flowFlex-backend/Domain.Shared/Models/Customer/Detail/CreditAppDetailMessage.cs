using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowFlex.Domain.Shared.Models.Customer.Detail
{
    /// <summary>
    /// Represents details of a credit application
    /// </summary>
    public class CreditAppDetailMessage : DataMessageBase
    {
        /// <summary>
        /// The ID of the associated credit application header
        /// </summary>
        public long CreditAppHeadId { get; set; }

        /// <summary>
        /// The requested credit limit
        /// </summary>
        public decimal CreditLimit { get; set; }

        /// <summary>
        /// The ID of the user who requested the credit application
        /// </summary>
        public long RequestedBy { get; set; }

        /// <summary>
        /// The date and time when the credit application was requested
        /// </summary>
        public DateTimeOffset? RequestedDate { get; set; }

        /// <summary>
        /// The ID of the user who approved the credit application
        /// </summary>
        public long ApprovedBy { get; set; }

        /// <summary>
        /// The name of the user who approved the credit application
        /// </summary>
        public string ApprovedUserName { get; set; }

        /// <summary>
        /// The date and time when the credit application was approved
        /// </summary>
        public DateTimeOffset? ApprovedDate { get; set; }

        /// <summary>
        /// The ID of the user who rejected the credit application
        /// </summary>
        public long RejectedBy { get; set; }

        /// <summary>
        /// The name of the user who rejected the credit application
        /// </summary>
        public string RejectedUserName { get; set; }

        /// <summary>
        /// The date and time when the credit application was rejected
        /// </summary>
        public DateTimeOffset? RejectedDate { get; set; }

        /// <summary>
        /// The reason for rejecting the credit application
        /// </summary>
        public string RejectedReason { get; set; }

        /// <summary>
        /// The current status of the credit application
        /// </summary>
        public int Status { get; set; }
    }
}
