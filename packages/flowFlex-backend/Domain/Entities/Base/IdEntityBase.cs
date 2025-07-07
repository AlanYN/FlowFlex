using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Attr;
using FlowFlex.Domain.Shared.Models.DynamicData;
using FlowFlex.Domain.Shared.Extensions;
using Item.Common.Lib.JsonConverts;
using SqlSugar;
using ValueToStringConverter = Item.Common.Lib.JsonConverts.ValueToStringConverter;
using FlowFlex.Domain.Shared.JsonConverters;

namespace FlowFlex.Domain.Entities.Base;

public class IdEntityBase : ICloneable
{
    /// <summary>
    /// id
    /// </summary>
    [SugarColumn(IsPrimaryKey = true)]
    [JsonConverter(typeof(LongToStringConverter))]
    [IgnoreDisplay]
    public virtual long Id { get; set; }

    public long InitNewId()
    {
        Id = SnowFlakeSingle.Instance.NextId();
        return Id;
    }

    public object Clone()
    {
        var type = GetType();
        var objNew = Activator.CreateInstance(type);

        foreach (var item in type.GetProperties())
        {
            if (item.SetMethod != null)
            {
                item.SetValue(objNew, item.GetValue(this));
            }
        }

        return objNew;
    }

    [NotMapped]
    private readonly List<IDomainEvent> _domainEvents = [];

    [NotMapped]
    private readonly Dictionary<string, object> _extendData = [];

    public object Get(string key)
    {
        if (_extendData.TryGetValue(key, out object value))
            return value;
        return null;
    }

    public T Get<T>(string key)
    {
        object value = Get(key);
        if (value is IDynamicValue dataValue)
        {
            value = dataValue.GetValue();
        }

        T result;
        if (value is T tValue)
            result = tValue;
        else
            result = ChangeValue<T>(value);

        return result;
    }

    public void Set(string key, object value)
    {
        if (!_extendData.TryAdd(key, value))
            _extendData[key] = value;
    }

    private static T ChangeValue<T>(object value)
    {
        if (value == null || value is DBNull)
        {
            return default;
        }

        var targetType = typeof(T);

        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            targetType = Nullable.GetUnderlyingType(targetType);
        }

        return (T)Convert.ChangeType(value, targetType);
    }

    public void AddDomainEvent(IDomainEvent eventItem)
    {
        _domainEvents.Add(eventItem);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public IEnumerable<IDomainEvent> GetDomainEvents()
    {
        return _domainEvents;
    }
}
