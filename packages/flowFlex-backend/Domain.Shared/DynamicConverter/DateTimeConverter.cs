#nullable enable

using Newtonsoft.Json.Linq;
using System;
using FlowFlex.Domain.Shared.DynamicConverter.Interfaces;
using FlowFlex.Domain.Shared.Models.DynamicData;

namespace FlowFlex.Domain.Shared.DynamicConverter;

public class DateTimeConverter : DynamicDataConverterBase<DateTimeOffset?>, ISingletonService
{
    protected override DateTimeOffset? Converter(object value)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()) || value == DBNull.Value)
            return null;

        if (value is string strValue)
        {
            return DateTimeOffset.Parse(strValue);
        }
        else if (value is DateTime dateTime)
        {
            return new DateTimeOffset(dateTime.ToUniversalTime(), TimeSpan.Zero);
        }
        else if (value is DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset;
        }

        return System.Convert.ToDateTime(value).ToUniversalTime();
    }

    protected override DateTimeOffset? Getter(IDynamicValue dynamicValue)
    {
        return dynamicValue.DateTimeValue;
    }

    protected override void Setter(IDynamicValue dynamicValue, object value)
    {
        if (value != null)
            dynamicValue.DateTimeValue = Converter(value);
        else
            dynamicValue.DateTimeValue = null;
    }
}
