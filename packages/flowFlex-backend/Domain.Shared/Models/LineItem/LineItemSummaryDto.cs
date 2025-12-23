namespace FlowFlex.Domain.Shared.Models.LineItem;

using System;
using System.Collections.Generic;

public class LineItemSummaryDto
{
    public decimal Subtotal { get; set; }
    public decimal DueNow { get; set; }
    public List<FuturePaymentDto> FuturePayments { get; set; } = new List<FuturePaymentDto>();
    public decimal TotalContractValue { get; set; }
    public int ContractTermMonths { get; set; }
}

public class FuturePaymentDto
{
    public decimal Amount { get; set; }
    public string Frequency { get; set; }
    public DateTimeOffset StartDate { get; set; }
}
