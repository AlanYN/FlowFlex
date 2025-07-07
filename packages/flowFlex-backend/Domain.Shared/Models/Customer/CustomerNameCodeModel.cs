namespace FlowFlex.Domain.Shared.Models.Customer
{
    public class CustomerNameCodeModel
    {
        public long Id { get; set; }
        public string CustomerName { get; set; }
        public string CustomerCode { get; set; }
        public long? SubCustomerOf { get; set; }
    }
}
