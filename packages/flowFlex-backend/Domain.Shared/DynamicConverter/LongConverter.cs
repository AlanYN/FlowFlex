using System;
using FlowFlex.Domain.Shared.DynamicConverter.Interfaces;
using FlowFlex.Domain.Shared.Models.DynamicData;

namespace FlowFlex.Domain.Shared.DynamicConverter;

public class LongConverter : DynamicDataConverterBase<long?>, ISingletonService
{
    protected override long? Converter(object value)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()) || value == DBNull.Value)
            return null;

        return System.Convert.ToInt64(value);
    }

    protected override long? Getter(IDynamicValue dynamicValue)
    {
        return dynamicValue.LongValue;
    }

    protected override void Setter(IDynamicValue dynamicValue, object value)
    {
        dynamicValue.LongValue = Converter(value);
    }
}
