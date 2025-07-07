using System.Collections.Generic;
using FlowFlex.Domain.Shared.Enums.Unis;

namespace FlowFlex.Domain.Shared.Models.Settings;

public class TagConfigModel
{
    public List<CustomerTagEnum> Tags { get; set; }
}
