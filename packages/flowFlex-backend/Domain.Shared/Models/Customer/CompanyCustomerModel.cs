using FlowFlex.Domain.Shared.Enums.Unis;

namespace FlowFlex.Domain.Shared.Models.Customer;

public class CompanyCustomerModel : CustomerBasicModelBase
{
    public string CustomerFullName { get; set; }

    public override string CustomerName { get; set; }

    public string Phone { get; set; }

    public string Subsidiary { get; set; }

    public CustomerAccountsCategoryEnum Category { get; set; }

    public int? LineOfBusiness { get; set; }

    public override CustomerTypeEnum CustomerType => CustomerTypeEnum.Company;
}
