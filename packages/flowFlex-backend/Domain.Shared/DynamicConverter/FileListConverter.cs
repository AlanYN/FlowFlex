#nullable enable

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using FlowFlex.Domain.Shared.DynamicConverter.Interfaces;
using FlowFlex.Domain.Shared.Extensions;
using FlowFlex.Domain.Shared.Models.DynamicData;

namespace FlowFlex.Domain.Shared.DynamicConverter;

public class FileListConverter : DynamicDataConverterBase<List<FileModel>?>, ISingletonService
{
    protected override List<FileModel>? Converter(object value)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()) || value == DBNull.Value)
            return null;
        else if (value is string strValue)
            return JArray.Parse(strValue).ToObject<List<FileModel>>();
        else if (value is List<FileModel> model)
            return model;

        return JArray.FromObject(value).ToObject<List<FileModel>>();
    }

    protected override List<FileModel>? Getter(IDynamicValue dynamicValue)
    {
        if (dynamicValue.JsonValue == null) return null;

        return dynamicValue.JsonValue.ToObject<List<FileModel>>();
    }

    protected override void Setter(IDynamicValue dynamicValue, object value)
    {
        if (value != null)
            dynamicValue.JsonValue = JArray.FromObject(Converter(value)!);
        else
            dynamicValue.JsonValue = null;
    }
}
