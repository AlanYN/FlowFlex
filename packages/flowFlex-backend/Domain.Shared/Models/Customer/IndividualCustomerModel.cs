using System;
using FlowFlex.Domain.Shared.Enums.Unis;

namespace FlowFlex.Domain.Shared.Models.Customer;

public class IndividualCustomerModel : CustomerBasicModelBase
{
    public string Phone { get; set; }

    public string FirstName { get; set; }

    public string MiddleName { get; set; }

    public string LastName { get; set; }

    public string Fax { get; set; }

    public string Title { get; set; }

    public string Company { get; set; }

    public string Notes { get; set; }

    public override string CustomerName { get; set; }

    public DateTimeOffset? Brithday { get; set; }

    public override CustomerTypeEnum CustomerType => CustomerTypeEnum.Individual;
}
