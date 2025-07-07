namespace FlowFlex.Domain.Shared.Models.Customer;

public struct CustomerSimpleRedisModel
{
    public string CustomerName { get; set; }

    public string CustomerCode { get; set; }

    public long CustomerId { get; set; }

    public string BnpAccountId { get; set; }

    public int CustomerType { get; set; }
}
