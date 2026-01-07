using Newtonsoft.Json.Linq;

namespace FlowFlex.Domain.Shared.Models.DynamicData;

/// <summary>
/// Dynamic data object - represents a business data record with dynamic fields
/// </summary>
public class DynamicDataObject : List<FieldDataItem>
{
    /// <summary>
    /// Business data ID
    /// </summary>
    public long BusinessId { get; set; }

    /// <summary>
    /// Module ID
    /// </summary>
    public int ModuleId { get; private set; }

    /// <summary>
    /// Internal extension data (JSONB)
    /// </summary>
    public JObject? InternalData { get; set; }

    /// <summary>
    /// Create date
    /// </summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>
    /// Modify date
    /// </summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>
    /// Create by
    /// </summary>
    public string? CreateBy { get; set; }

    /// <summary>
    /// Modify by
    /// </summary>
    public string? ModifyBy { get; set; }

    /// <summary>
    /// Create user ID
    /// </summary>
    public long CreateUserId { get; set; }

    /// <summary>
    /// Modify user ID
    /// </summary>
    public long ModifyUserId { get; set; }

    public DynamicDataObject()
    {
    }

    public DynamicDataObject(int moduleId)
    {
        ModuleId = moduleId;
    }

    /// <summary>
    /// Set module ID
    /// </summary>
    public void SetModuleId(int moduleId)
    {
        ModuleId = moduleId;
    }

    /// <summary>
    /// Indexer to access field value by name
    /// </summary>
    public object? this[string name]
    {
        get
        {
            var item = this.FirstOrDefault(x => 
                string.Equals(x.FieldName, name, StringComparison.OrdinalIgnoreCase));
            return item?.Value;
        }
        set
        {
            var item = this.FirstOrDefault(x => 
                string.Equals(x.FieldName, name, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                item.Value = value;
            }
            else
            {
                Add(new FieldDataItem { FieldName = name, Value = value });
            }
        }
    }

    /// <summary>
    /// Get value with type safety
    /// </summary>
    public T? GetValueOrDefault<T>(string name)
    {
        return GetValueOrDefault(name, default(T));
    }

    /// <summary>
    /// Get value with type safety and default value
    /// </summary>
    public T? GetValueOrDefault<T>(string name, T? defaultValue)
    {
        var value = this[name];
        if (value == null)
            return defaultValue;

        try
        {
            if (value is T typedValue)
                return typedValue;

            // Handle JToken conversion
            if (value is JToken jToken)
                return jToken.ToObject<T>();

            // Handle type conversion
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Check if field exists
    /// </summary>
    public bool ContainsFieldName(string name)
    {
        return this.Any(x => 
            string.Equals(x.FieldName, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get field item by name
    /// </summary>
    public FieldDataItem? GetFieldItem(string name)
    {
        return this.FirstOrDefault(x => 
            string.Equals(x.FieldName, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Remove field by name
    /// </summary>
    public bool RemoveField(string name)
    {
        var item = GetFieldItem(name);
        if (item != null)
        {
            Remove(item);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Convert to dictionary
    /// </summary>
    public Dictionary<string, object?> ToDictionary()
    {
        return this.ToDictionary(x => x.FieldName, x => x.Value);
    }
}
