using System;
using System.Collections.Generic;
using FlowFlex.Domain.Shared.Models.DynamicData;

namespace FlowFlex.Domain.Shared.Models.Models;

public abstract class DynamicModelBase : IDynamicDataModel
{
    private readonly Dictionary<string, object> _dic = [];

    public long Id { get; set; }

    public abstract int ModuleId { get; }

    public object this[string field]
    {
        get
        {
            if (_dic.TryGetValue(field, out object value))
            {
                if (value is FieldDataItem dataItem)
                    return dataItem.Value;

                return value;
            }
            return null;
        }
        set
        {
            if (value == null)
                return;

            if (!_dic.TryAdd(field, value))
                _dic[field] = value;
        }
    }

    public List<string> GetFieldNameList() => [.. _dic.Keys];

    public FieldDataItem GetFieldData(string fieldName) => _dic[fieldName] as FieldDataItem;

    bool IDynamicDataModel.DynamicDataFullFinish { get; set; }

    IServiceProvider IDynamicDataModel.Service { get; set; }

    void IDynamicDataModel.DynamicFullFinish()
    {
        (this as IDynamicDataModel).DynamicDataFullFinish = true;
    }
}
