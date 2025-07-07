using System;
using FlowFlex.Domain.Shared.Models.Customer;

namespace FlowFlex.Domain.Shared.Models
{
    public class AccountsModel : BaseModel
    {
        /// <summary>
        /// Account name
        /// </summary>
        public string AccountName { get; set; }

        /// <summary>
        /// Code
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Tags
        /// </summary>
        public long Tags { get; set; }

        public TagState TagStatus { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        public int? Status { get; set; }


        /// <summary>
        /// Creator ID
        /// </summary>
        public long CreateUserId { get; set; }

        /// <summary>
        /// Creator
        /// </summary>
        public string CreateBy { get; set; }

        /// <summary>
        /// Creation time
        /// </summary>
        public DateTimeOffset CreatedTime { get; set; }

        public string TenantId { get; set; }

        public bool OnHold { get; set; }
    }
}
