namespace FlowFlex.Domain.Shared.Models;

public class CustomerMappingModel
{
    public long CustomerId { get; set; }

    public string CustomerName { get; set; }

    public string BnpAccountId { get; set; }

    public int? BnpCustomerId { get; set; }

    public string CustomerCode { get; set; }

    public long? SubCustomerOf { get; set; }

    public int? TmsId { get; set; }
}
