using SqlSugar;
using System.Text.Json;

namespace FlowFlex.WebApi.Extensions;

/// <summary>
/// Custom JSON serializer for SqlSugar to prevent double serialization of JSON strings
/// </summary>
public class SqlSugarJsonSerializer : ISerializeService
{
    /// <summary>
    /// Serialize object to JSON string
    /// If the value is already a valid JSON string (starts with [ or {), return it directly
    /// </summary>
    public string SerializeObject(object value)
    {
        if (value == null)
            return "null";

        // If value is already a string, check if it's valid JSON
        if (value is string strValue)
        {
            var trimmed = strValue.Trim();
            // Check if it's already a JSON array or object
            if ((trimmed.StartsWith("[") && trimmed.EndsWith("]")) ||
                (trimmed.StartsWith("{") && trimmed.EndsWith("}")))
            {
                // Already valid JSON, return as-is to prevent double serialization
                return strValue;
            }
        }

        // Otherwise, serialize normally
        return JsonSerializer.Serialize(value);
    }

    /// <summary>
    /// SqlSugar specific serialization method
    /// </summary>
    public string SugarSerializeObject(object value)
    {
        return SerializeObject(value);
    }

    /// <summary>
    /// Deserialize JSON string to object
    /// </summary>
    public T DeserializeObject<T>(string value)
    {
        if (string.IsNullOrEmpty(value))
            return default!;

        return JsonSerializer.Deserialize<T>(value)!;
    }
}
