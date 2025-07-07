using System;

namespace FlowFlex.Domain.Shared.Models
{
    public class CustomerAccountsIndividualModel
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
        /// CustomerCode
        /// </summary>
        public string CustomerCode { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Email
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Created time
        /// </summary>
        public DateTimeOffset CreatedTime { get; set; }

        /// <summary>
        /// Creator
        /// </summary>
        public string CreatedUserName { get; set; }

        /// <summary>
        /// Updated time
        /// </summary>
        public DateTimeOffset UpdatedTime { get; set; }

        /// <summary>
        /// Last updater
        /// </summary>
        public string LastUpdateUserName { get; set; }

        /// <summary>
        /// BNPAccountID
        /// </summary>
        public string BNPAccountID { get; set; }
    }
}
