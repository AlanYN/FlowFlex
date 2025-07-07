using System;
using FlowFlex.Domain.Shared.Enums.Unis;
using FlowFlex.Domain.Shared.Models.Customer;

namespace FlowFlex.Domain.Shared.Models
{
    /// <summary>
    /// Customer account
    /// </summary>
    public class CustomerAccountsModel
    {
        /// <summary>
        /// ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Account name
        /// </summary>
        public string CustomerName { get; set; }
        /// <summary>
        /// Print name
        /// </summary>
        public string PrintName { get; set; }
        /// <summary>
        /// Account ID
        /// </summary>
        public string CustomerCode { get; set; }
        /// <summary>
        /// Category
        /// </summary>
        public int Category { get; set; }
        /// <summary>
        /// Status
        /// </summary>
        public int Status { get; set; }

        public bool OnHold { get; set; }

        public TagState TagStatus { get; set; }

        /// <summary>
        /// Memo
        /// </summary>
        public string Memo { get; set; }
        /// <summary>
        /// Email
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// Payment term
        /// </summary>
        public int? PayTerm { get; set; }
        /// <summary>
        /// Created time
        /// </summary>
        public DateTimeOffset CreatedTime { get; set; }
        /// <summary>
        /// Created by
        /// </summary>
        public string CreatedUserName { get; set; }
        /// <summary>
        /// Updated time
        /// </summary>
        public DateTimeOffset UpdatedTime { get; set; }
        /// <summary>
        /// Last updated by
        /// </summary>
        public string LastUpdateUserName { get; set; }

        public string BNPAccountID { get; set; }
        public long Tags { get; set; }

        /// <summary>
        /// Has sub data
        /// </summary>
        public bool HasSubData { get; set; }


        public long? SubCustomerOf { get; set; }

        public string CustomerFullName { get; set; }
    }
}
