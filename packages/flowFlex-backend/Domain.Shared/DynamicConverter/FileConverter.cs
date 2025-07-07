#nullable enable

using Newtonsoft.Json.Linq;
using System;
using FlowFlex.Domain.Shared.DynamicConverter.Interfaces;
using FlowFlex.Domain.Shared.Extensions;
using FlowFlex.Domain.Shared.Models.DynamicData;

namespace FlowFlex.Domain.Shared.DynamicConverter;

public class FileConverter : DynamicDataConverterBase<FileModel?>, ISingletonService
{
    protected override FileModel? Converter(object value)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()) || value == DBNull.Value)
            return null;
        else if (value is string strValue)
            return JObject.Parse(strValue).ToObject<FileModel>();
        else if (value is FileModel model)
            return model;

        return JObject.FromObject(value).ToObject<FileModel>();
    }

    protected override FileModel? Getter(IDynamicValue dynamicValue)
    {
        if (dynamicValue.JsonValue == null) return null;

        return dynamicValue.JsonValue.ToObject<FileModel>();
    }

    protected override void Setter(IDynamicValue dynamicValue, object value)
    {
        if (value != null)
            dynamicValue.JsonValue = JObject.FromObject(value);
        else
            dynamicValue.JsonValue = null;
    }
}
