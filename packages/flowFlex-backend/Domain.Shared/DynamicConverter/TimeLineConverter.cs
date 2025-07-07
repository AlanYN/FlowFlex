#nullable enable

using Newtonsoft.Json.Linq;
using System;
using FlowFlex.Domain.Shared.DynamicConverter.Interfaces;
using FlowFlex.Domain.Shared.Extensions;
using FlowFlex.Domain.Shared.Models.DynamicData;

namespace FlowFlex.Domain.Shared.DynamicConverter;

public class TimeLineConverter : DynamicDataConverterBase<TimeLineModel?>, ISingletonService
{
    protected override TimeLineModel? Converter(object value)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()) || value == DBNull.Value)
            return null;
        else if (value is string strValue)
            return JObject.Parse(strValue).ToObject<TimeLineModel>();
        else if (value is TimeLineModel model)
            return model;

        return JObject.FromObject(value).ToObject<TimeLineModel>();
    }

    protected override TimeLineModel? Getter(IDynamicValue dynamicValue)
    {
        if (dynamicValue.JsonValue != null)
            return dynamicValue.JsonValue.ToObject<TimeLineModel>();
        else return null;
    }

    protected override void Setter(IDynamicValue dynamicValue, object value)
    {
        if (value != null)
            dynamicValue.JsonValue = JObject.FromObject(value);
        else
            dynamicValue.JsonValue = null;
    }
}
