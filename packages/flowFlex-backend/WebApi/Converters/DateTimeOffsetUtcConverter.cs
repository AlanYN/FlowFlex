using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FlowFlex.WebApi.Converters
{
    /// <summary>
    /// Custom JSON converter for DateTimeOffset that always serializes to UTC
    /// This ensures consistent timezone handling across all API responses
    /// </summary>
    public class DateTimeOffsetUtcConverter : DateTimeConverterBase
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                if (objectType == typeof(DateTimeOffset?))
                {
                    return null;
                }
                return DateTimeOffset.MinValue;
            }

            if (reader.TokenType == JsonToken.Date)
            {
                if (reader.Value is DateTimeOffset dto)
                {
                    return dto.ToUniversalTime();
                }
                if (reader.Value is DateTime dt)
                {
                    return new DateTimeOffset(dt.ToUniversalTime(), TimeSpan.Zero);
                }
            }

            if (reader.TokenType == JsonToken.String)
            {
                var dateString = reader.Value?.ToString();
                if (string.IsNullOrEmpty(dateString))
                {
                    if (objectType == typeof(DateTimeOffset?))
                    {
                        return null;
                    }
                    return DateTimeOffset.MinValue;
                }

                if (DateTimeOffset.TryParse(dateString, out var result))
                {
                    return result.ToUniversalTime();
                }
            }

            throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing DateTimeOffset.");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var dateTimeOffset = (DateTimeOffset)value;
            // Always convert to UTC before serializing
            var utcValue = dateTimeOffset.ToUniversalTime();
            writer.WriteValue(utcValue.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz"));
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTimeOffset) || objectType == typeof(DateTimeOffset?);
        }
    }
}
