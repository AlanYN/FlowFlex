using Item.Common.Lib.Attr;
using Item.Common.Lib.EnumUtil;
using System;
using System.Collections.Concurrent;
using FlowFlex.Domain.Shared.Enums.DynamicData;

namespace FlowFlex.Domain.Shared.DynamicConverter.Interfaces;

public static class ConverterHelper
{
    private static readonly ConcurrentDictionary<Type, IDynamicDataConverter> ConverterMaps = [];

    public static IDynamicDataConverter GetConverter(DataType dataType)
    {
        var attr = dataType.GetAttribute<EnumValueAttribute>();
        if (attr != null && attr.ResourceType != null)
        {
            if (ConverterMaps.TryGetValue(attr.ResourceType, out var converter))
            {
                return converter;
            }

            converter = Activator.CreateInstance(attr.ResourceType) as IDynamicDataConverter;
            ConverterMaps.TryAdd(attr.ResourceType, converter);
            return converter;
        }

        throw new CRMException(ErrorCodeEnum.SystemError);
    }
}
