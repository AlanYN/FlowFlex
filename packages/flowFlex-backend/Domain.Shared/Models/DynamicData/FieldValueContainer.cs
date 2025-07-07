using System.Collections.Generic;
using FlowFlex.Domain.Shared.Enums.DynamicData;
using FlowFlex.Domain.Shared.Extensions;
// using FlowFlex.Infrastructure;

namespace FlowFlex.Domain.Shared.Models.DynamicData;

public class FieldValueContainer : ValueContainer, IDynamicValue
{
    private readonly Dictionary<string, object> _dict = [];

    public object this[string key]
    {
        get
        {
            if (_dict.TryGetValue(key, out object value))
                return value;
            return null;
        }
        set
        {
            if (!_dict.TryAdd(key, value))
                _dict[key] = value;
        }
    }

    public bool IsValueChange { get; private set; }

    public long? RefFieldId { get; set; }

    public required string FieldName { get; set; }

    public required string DisplayName { get; set; }

    public required DataType DataType { get; set; }

    public required string Description { get; set; }

    public required bool IsDisplayField { get; set; }

    public long? GroupId { get; set; }

    public bool IsComputed { get; set; }

    public required int Sort { get; set; }

    public int ModuleId { get; set; }

    public AdditionalInfo AdditionalInfo { get; set; } = new();

    public bool SetChangeValue(object value)
    {
        var oldValue = this.GetValue();
        if (!oldValue.IsValueEqual(value))
        {
            this.SetValue(value);
            IsValueChange = true;
        }

        return IsValueChange;
    }
}
