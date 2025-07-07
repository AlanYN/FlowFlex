namespace FlowFlex.Domain.Shared.Models.Customer.Detail
{
    public class RelTitleMessage : DataMessageBase
    {
        /// <summary>
        /// Associated label ID
        /// </summary>
        public string TitleId { get; set; }

        /// <summary>
        /// Associated label name
        /// </summary>
        public string TitleName { get; set; }

        /// <summary>
        /// Specifies which Customer to bill to. Only used when the company is UF, 
        /// to record which CustomerId to bill to.
        /// </summary>
        public long? BillTo { get; set; }

        /// <summary>
        /// ID type: 1 - Regular (default), 2 - Reverse
        /// </summary>
        public int IdType { get; set; }

        /// <summary>
        /// Status of the relationship
        /// </summary>
        public int? Status { get; set; }

        /// <summary>
        /// Third-party ID
        /// </summary>
        public string ThirdPartyId { get; set; }

        /// <summary>
        /// Third-party name
        /// </summary>
        public string ThirdPartyName { get; set; }

        /// <summary>
        /// Program ID associated with this relationship
        /// </summary>
        public int? ProgramId { get; set; }
    }
}
