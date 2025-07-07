namespace FlowFlex.Domain.Shared.Models.ExternalApi
{
    public class ExternalApiCustomerInfoModel
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
        /// CustomerCode
        /// </summary>
        public string PrintName { get; set; }

        /// <summary>
        /// CustomerCode
        /// </summary>
        public bool? BillToOnly { get; set; }

        /// <summary>
        /// Customer type
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// Tenant ID
        /// </summary>
        public string TelnetId { get; set; }

        /// <summary>
        /// Tenant company name
        /// </summary>
        public string Company { get; set; }
    }
}
