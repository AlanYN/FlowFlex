namespace FlowFlex.Domain.Shared.Models.Customer.Detail
{
    public class AccountHolderMessage : DataMessageBase
    {
        /// <summary>
        /// Group ID
        /// </summary>
        public long Category { get; set; }

        /// <summary>
        /// Name of the category
        /// </summary>
        public string CategoryName { get; set; }

        /// <summary>
        /// Facility list provided by Wise API
        /// </summary>
        public string Facility { get; set; }

        /// <summary>
        /// User ID within the data group
        /// </summary>
        public long AssigneeId { get; set; }

        /// <summary>
        /// User name in the data group (firstName + "," + lastName)
        /// </summary>
        public string AssigneeName { get; set; }

        /// <summary>
        /// Status of the account holder
        /// </summary>
        public bool Status { get; set; }
    }
}
