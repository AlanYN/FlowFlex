using Newtonsoft.Json;
using FlowFlex.Domain.Shared.Enums.DynamicData;
using FlowFlex.Domain.Shared.Utils;

namespace FlowFlex.Domain.Shared.Models;

[JsonConverter(typeof(FilterExpressionFieldValueConverter))]
public class FilterExpression
{
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string FieldName { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public DataType? FieldType { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public object FieldValue { get; set; }

    public LogicEnum Logic { get; set; }

    public FilterExpression[] Condition { get; set; }
}
