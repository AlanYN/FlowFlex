namespace FlowFlex.Domain.Shared.Models.ExternalApi
{
    public class ExternalApiRateCustomerInfoModel
    {
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string Accountld { get; set; }
        public string BNPld { get; set; }
        /// <summary>
        /// Whether has subset
        /// </summary>
        public bool HasSubset { get; set; }
    }
}
