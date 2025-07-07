using System;
using System.Collections.Generic;

namespace FlowFlex.Domain.Shared.Models.LineItem;

public class BillingItemDetail
{
    public List<LineItemDto> LineItems { get; set; }

    public DateTimeOffset StartDate { get; set; }

    public DateTimeOffset EndDate { get; set; }

    public string CustomerCode { get; set; }

    public string Frequency { get; set; }

    public string CreateBy { get; set; }

    public DateTimeOffset CreateDate { get; set; }

    public string ModifyBy { get; set; }

    public DateTimeOffset ModifyDate { get; set; }
}
