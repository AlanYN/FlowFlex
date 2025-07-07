#nullable enable

using Item.Common.Lib.DateTimeUtil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using FlowFlex.Domain.Shared.Const;
using FlowFlex.Domain.Shared.Enums.DynamicData;
using FlowFlex.Domain.Shared.Extensions;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Domain.Shared.Utils;

public class FilterExpressionFieldValueConverter : CustomJsonConverter<FilterExpression>
{
    StringComparison sc = StringComparison.OrdinalIgnoreCase;

    public override void CustomWriteJson(CustomJsonWriter writer, FilterExpression? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();

        // Serialize FieldName
        if (value.FieldName != null)
        {
            writer.WritePropertyName("fieldName");
            serializer.Serialize(writer.Obj, value.FieldName);
        }

        // Serialize FieldType
        if (value.FieldType.HasValue)
        {
            writer.WritePropertyName("fieldType");
            serializer.Serialize(writer.Obj, value.FieldType);
        }

        // Custom FieldValue serialization logic
        if (value.FieldValue != null)
        {
            writer.WritePropertyName("fieldValue");

            // Different serialization handling based on FieldType
            switch (value.FieldType)
            {
                case DataType.DateTime:
                    // If it's a date type, ensure using standard format
                    if (value.FieldValue is DateTime dateTime)
                    {
                        serializer.Serialize(writer.Obj, dateTime.ToString("yyyy-MM-dd HH:mm:ss"));
                    }
                    else if (value.FieldValue is DateTimeOffset dateTimeOffset)
                    {
                        var datetimeStr = UtcTimeUtility.ConvertUtcToTimeZoneInUtcForm(dateTimeOffset, "UTC").ToString(TimeCosts.DateTimeFormat);
                        serializer.Serialize(writer.Obj, datetimeStr);
                    }
                    else
                    {
                        serializer.Serialize(writer.Obj, value.FieldValue);
                    }
                    break;

                case DataType.Number:
                    // If it's a number type, ensure using correct number format
                    if (decimal.TryParse(value.FieldValue.ToString(), out decimal number))
                    {
                        serializer.Serialize(writer.Obj, number);
                    }
                    else
                    {
                        serializer.Serialize(writer.Obj, 0);
                    }
                    break;
                case DataType.TimeLine:
                    {
                        if (value.FieldValue is JObject jObject)
                        {
                            var startDate = jObject.GetValue(nameof(TimeLineModel.StartDate), sc)?.ToObject<DateTimeOffset>();
                            var endDate = jObject.GetValue(nameof(TimeLineModel.EndDate), sc)?.ToObject<DateTimeOffset>();

                            var timeLineModel = new TimeLineModel()
                            {
                                StartDate = (startDate ?? DateTimeOffset.MinValue).UtcDateTime,
                                EndDate = (endDate ?? DateTimeOffset.MinValue).UtcDateTime,
                            };

                            serializer.Serialize(writer.Obj, timeLineModel);
                        }
                        else
                        {
                            serializer.Serialize(writer.Obj, value.FieldValue);
                        }
                        break;
                    }
                default:
                    // Other types serialize directly
                    serializer.Serialize(writer.Obj, value.FieldValue);
                    break;
            }
        }

        // Serialize Logic
        writer.WritePropertyName("logic");
        serializer.Serialize(writer.Obj, value.Logic);

        // Serialize Condition
        if (value.Condition != null)
        {
            writer.WritePropertyName("condition");
            serializer.Serialize(writer.Obj, value.Condition);
        }

        writer.WriteEndObject();
    }

    public override FilterExpression? CustomReadJson(CustomJsonReader reader, Type objectType, FilterExpression? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        // Read JSON object

        JObject jsonObject = JObject.Load(reader.Obj);
        var filterExpression = new FilterExpression
        {
            FieldName = jsonObject.GetValue(nameof(FilterExpression.FieldName), sc)?.ToString(),
            FieldType = jsonObject.GetValue(nameof(FilterExpression.FieldType), sc)?.ToObject<DataType?>(),
            Logic = jsonObject.GetValue(nameof(FilterExpression.Logic), sc)?.ToObject<LogicEnum>() ?? LogicEnum.And
        };

        // Handle FieldValue
        if (jsonObject.GetValue(nameof(FilterExpression.FieldValue), sc) != null)
        {
            filterExpression.FieldValue = jsonObject.GetValue(nameof(FilterExpression.FieldValue), sc);
        }

        // Handle Condition array
        if (jsonObject.GetValue(nameof(FilterExpression.Condition), sc) != null)
        {
            var conditions = jsonObject.GetValue(nameof(FilterExpression.Condition), sc)!.ToObject<JArray>();
            if (conditions != null)
            {
                filterExpression.Condition = new FilterExpression[conditions.Count];
                for (int i = 0; i < conditions.Count; i++)
                {
                    // Recursively handle each condition
                    using var subReader = conditions[i].CreateReader();
                    filterExpression.Condition[i] = ReadJson(subReader, typeof(FilterExpression), null, false, serializer);
                }
            }
        }

        return filterExpression;
    }
}
