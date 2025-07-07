#nullable enable

using System;
using FlowFlex.Domain.Shared.DynamicConverter.Interfaces;
using FlowFlex.Domain.Shared.Models.DynamicData;

namespace FlowFlex.Domain.Shared.DynamicConverter;

public class BooleanConverter : DynamicDataConverterBase<bool?>, ISingletonService
{
    protected override bool? Converter(object value)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()) || value == DBNull.Value)
            return null;

        return System.Convert.ToBoolean(value);
    }

    protected override bool? Getter(IDynamicValue dynamicValue)
    {
        return dynamicValue.BoolValue;
    }

    protected override void Setter(IDynamicValue dynamicValue, object value)
    {
        if (value != null)
            dynamicValue.BoolValue = Converter(value);
        else
            dynamicValue.BoolValue = null;
    }
}
