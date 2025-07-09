using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlowFlex.WebApi.Converters
{
    /// <summary>
    /// Nullable long integer JSON converter, supports empty string conversion to null
    /// </summary>
    public class NullableLongConverter : JsonConverter<long?>
    {
        public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();

                // If empty string, return null
                if (string.IsNullOrEmpty(stringValue))
                {
                    return null;
                }

                // Try to parse as long integer
                if (long.TryParse(stringValue, out var result))
                {
                    return result;
                }

                // If parsing fails, return null
                return null;
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetInt64();
            }

            // Return null for other cases
            return null;
        }

        public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteNumberValue(value.Value);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
