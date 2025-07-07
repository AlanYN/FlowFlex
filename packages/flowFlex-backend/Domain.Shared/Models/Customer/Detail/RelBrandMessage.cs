namespace FlowFlex.Domain.Shared.Models.Customer.Detail
{
    public class RelBrandMessage : DataMessageBase
    {
        /// <summary>
        /// Associated brand ID
        /// </summary>
        public long RelationshipBrandId { get; set; }

        /// <summary>
        /// Associated brand name
        /// </summary>
        public string RelationshipBrandName { get; set; }

        /// <summary>
        /// The customer ID to bill to. Only used when the company is UF, to record which CustomerId to bill to.
        /// </summary>
        public long? BillTo { get; set; }

        /// <summary>
        /// ID type: 1 - Regular (default), 2 - Reverse
        /// </summary>
        public int IdType { get; set; }

        /// <summary>
        /// Manually entered associated brand ID
        /// </summary>
        public string BrandId { get; set; }

        /// <summary>
        /// Manually entered associated brand name
        /// </summary>
        public string BrandName { get; set; }

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
