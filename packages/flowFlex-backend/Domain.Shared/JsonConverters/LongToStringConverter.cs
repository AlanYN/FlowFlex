using Newtonsoft.Json;
using System;

namespace FlowFlex.Domain.Shared.JsonConverters;

/// <summary>
/// 将long类型转换为string类型的JSON转换�?
/// </summary>
public class LongToStringConverter : JsonConverter<long>
{
    public override void WriteJson(JsonWriter writer, long value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString());
    }

    public override long ReadJson(JsonReader reader, Type objectType, long existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.Value == null)
            return 0;

        if (reader.Value is string stringValue)
        {
            // Handle empty string as 0
            if (string.IsNullOrWhiteSpace(stringValue))
                return 0;
            
            if (long.TryParse(stringValue, out long result))
                return result;
            
            // If parsing fails, return 0 instead of throwing
            return 0;
        }
        else if (reader.Value is long longValue)
        {
            return longValue;
        }
        else if (reader.Value is int intValue)
        {
            return intValue;
        }

        try
        {
            return Convert.ToInt64(reader.Value);
        }
        catch (FormatException)
        {
            return 0;
        }
    }
}

/// <summary>
/// 将可空long类型转换为string类型的JSON转换�?
/// </summary>
public class NullableLongToStringConverter : JsonConverter<long?>
{
    public override void WriteJson(JsonWriter writer, long? value, JsonSerializer serializer)
    {
        if (value.HasValue)
            writer.WriteValue(value.Value.ToString());
        else
            writer.WriteNull();
    }

    public override long? ReadJson(JsonReader reader, Type objectType, long? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.Value == null)
            return null;

        if (reader.Value is string stringValue)
        {
            // Handle empty string as null for nullable type
            if (string.IsNullOrWhiteSpace(stringValue))
                return null;
            
            if (long.TryParse(stringValue, out long result))
                return result;
            
            // If parsing fails, return null instead of throwing
            return null;
        }
        else if (reader.Value is long longValue)
        {
            return longValue;
        }
        else if (reader.Value is int intValue)
        {
            return intValue;
        }

        try
        {
            return Convert.ToInt64(reader.Value);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
