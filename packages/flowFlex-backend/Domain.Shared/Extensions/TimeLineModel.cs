using System;

namespace FlowFlex.Domain.Shared.Extensions;

public struct TimeLineModel
{
    private DateTimeOffset _startDate;
    private DateTimeOffset _endDate;

    public DateTimeOffset StartDate { readonly get => _startDate.ToUniversalTime(); set => _startDate = value; }

    public DateTimeOffset EndDate { readonly get => _endDate.ToUniversalTime(); set => _endDate = value; }
}
