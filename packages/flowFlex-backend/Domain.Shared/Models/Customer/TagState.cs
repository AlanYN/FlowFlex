using FlowFlex.Domain.Shared.Enums.Unis;

namespace FlowFlex.Domain.Shared.Models.Customer;

public class TagState
{
    public CustomerStatusEnum Title { get; set; }

    public CustomerStatusEnum Retailer { get; set; }

    public CustomerStatusEnum Supplier { get; set; }

    public CustomerStatusEnum Brand { get; set; }

    public CustomerStatusEnum Manufacturer { get; set; }
}
