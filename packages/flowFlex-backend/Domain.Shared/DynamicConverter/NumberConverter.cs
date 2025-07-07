using System;
using FlowFlex.Domain.Shared.DynamicConverter.Interfaces;
using FlowFlex.Domain.Shared.Models.DynamicData;
using FlowFlex.Domain.Shared.Extensions;

namespace FlowFlex.Domain.Shared.DynamicConverter;

public class NumberConverter : DynamicDataConverterBase<double?>
{
    protected override double? Converter(object value)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()) || value == DBNull.Value)
            return null;

        return value.GetChangeValue<double?>();
    }

    protected override double? Getter(IDynamicValue dynamicValue)
    {
        return dynamicValue.DoubleValue;
    }

    protected override void Setter(IDynamicValue dynamicValue, object value)
    {
        dynamicValue.DoubleValue = Converter(value);
    }
}
