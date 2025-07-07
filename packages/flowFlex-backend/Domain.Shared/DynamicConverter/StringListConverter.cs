#nullable enable

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using FlowFlex.Domain.Shared.DynamicConverter.Interfaces;
using FlowFlex.Domain.Shared.Models.DynamicData;

namespace FlowFlex.Domain.Shared.DynamicConverter;

public class StringListConverter : DynamicDataConverterBase<List<string>?>, ISingletonService
{
    protected override List<string>? Converter(object value)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()) || value == DBNull.Value)
            return null;
        else if (value is string strValue)
            return JArray.Parse(strValue).ToObject<List<string>>();
        else if (value is List<string> strings)
            return strings;

        return JArray.FromObject(value).ToObject<List<string>>();
    }

    protected override List<string>? Getter(IDynamicValue dynamicValue)
    {
        if (dynamicValue.JsonValue != null)
            return dynamicValue.JsonValue.ToObject<List<string>>();
        return null;
    }

    protected override void Setter(IDynamicValue dynamicValue, object value)
    {
        if (value != null)
            dynamicValue.JsonValue = JArray.FromObject(value);
        else
            dynamicValue.JsonValue = null;
    }
}
