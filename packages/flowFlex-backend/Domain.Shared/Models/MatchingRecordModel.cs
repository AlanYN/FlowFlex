using System;
using FlowFlex.Domain.Shared.Enums.Unis;
using FlowFlex.Domain.Shared.Enums;

namespace FlowFlex.Domain.Shared.Models;

public class MatchingRecordModel
{
    public long Id { get; set; }

    public CustomerTagEnum MatchingSourceTag { get; set; }

    public string OriginalName { get; set; }

    public DateTime ReceivedTime { get; set; }

    public string CustomerName { get; set; }

    public long? MatchedId { get; set; }

    public string MatchingMasterName { get; set; }

    public MatchingStatusEnum Status { get; set; }

    public DateTime? EmailLastSendingTime { get; set; }

    public int EmailSendingCount { get; set; }
}
