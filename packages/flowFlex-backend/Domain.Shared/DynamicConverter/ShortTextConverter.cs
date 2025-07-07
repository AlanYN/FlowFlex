#nullable enable

using System;
using FlowFlex.Domain.Shared.DynamicConverter.Interfaces;
using FlowFlex.Domain.Shared.Models.DynamicData;

namespace FlowFlex.Domain.Shared.DynamicConverter;

public class ShortTextConverter : DynamicDataConverterBase<string?>, ISingletonService
{
    protected override string? Converter(object value)
    {
        if (value == null || value == DBNull.Value)
            return null;

        return value.ToString();
    }

    protected override string? Getter(IDynamicValue dynamicValue)
    {
        return dynamicValue.Varchar100Value;
    }

    protected override void Setter(IDynamicValue dynamicValue, object value)
    {
        dynamicValue.Varchar100Value = Converter(value);
    }
}
