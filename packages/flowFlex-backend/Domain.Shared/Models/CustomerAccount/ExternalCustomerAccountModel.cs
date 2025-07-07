using System;

namespace FlowFlex.Domain.Shared.Models.CustomerAccount
{
    public class ExternalCustomerAccountModel
    {
        public long Id { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string CustomerName { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string CustomerFullName { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string CustomerCode { get; set; }

        /// <summary>
        ///
        /// </summary>
        public int Category { get; set; }

        /// <summary>
        ///
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string Memo { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        ///
        /// </summary>
        public int? PayTerm { get; set; }

        /// <summary>
        ///
        /// </summary>
        public DateTimeOffset CreatedTime { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string CreatedUserName { get; set; }

        /// <summary>
        ///
        /// </summary>
        public DateTimeOffset UpdatedTime { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string LastUpdateUserName { get; set; }

        public string Currency { get; set; }
        public string Country { get; set; }
        public long logoImageID { get; set; }
        public int? UsageScenario { get; set; }
        public long Tags { get; set; }
        public string BNPAccountID { get; set; }
    }
}
