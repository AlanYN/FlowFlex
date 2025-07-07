namespace FlowFlex.Domain.Shared.Models.Customer.Detail
{
    public class RelSupplierMessage : DataMessageBase
    {
        /// <summary>
        /// Associated supplier ID
        /// </summary>
        public long RelationshipSupplierId { get; set; }

        /// <summary>
        /// Associated supplier name
        /// </summary>
        public string RelationshipSupplierName { get; set; }

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

        /// <summary>
        /// Manually entered associated supplier ID
        /// </summary>
        public string SupplierId { get; set; }

        /// <summary>
        /// Manually entered associated supplier name
        /// </summary>
        public string SupplierName { get; set; }
    }
}
