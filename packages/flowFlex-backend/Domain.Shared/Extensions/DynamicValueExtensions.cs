using FlowFlex.Domain.Shared.DynamicConverter.Interfaces;
using FlowFlex.Domain.Shared.Enums.DynamicData;
using FlowFlex.Domain.Shared.Models.DynamicData;

namespace FlowFlex.Domain.Shared.Extensions;

public static class DynamicValueExtensions
{
    public static void SetValue(this IDynamicValue data, object value)
    {
        var converter = ConverterHelper.GetConverter(data.DataType);
        converter.Set(data, value);
    }

    public static object GetValue(this IDynamicValue data)
    {
        var converter = ConverterHelper.GetConverter(data.DataType);
        return converter.Get(data);
    }

    public static object ConvertValue(this DataType dataType, object value)
    {
        var converter = ConverterHelper.GetConverter(dataType);
        return converter.Convert(value);
    }

    public static object ConvertValue(this DataType? dataType, object value)
    {
        if (dataType.HasValue)
        {
            var converter = ConverterHelper.GetConverter(dataType.Value);
            return converter.Convert(value);
        }

        return value;
    }
}
