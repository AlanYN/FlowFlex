using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlowFlex.WebApi.Converters
{
    /// <summary>
    /// 可空长整型JSON转换器，支持空字符串转换为null
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
                
                // 如果是空字符串，返回null
                if (string.IsNullOrEmpty(stringValue))
                {
                    return null;
                }

                // 尝试解析为长整型
                if (long.TryParse(stringValue, out var result))
                {
                    return result;
                }

                // 如果解析失败，返回null
                return null;
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetInt64();
            }

            // 其他情况返回null
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
