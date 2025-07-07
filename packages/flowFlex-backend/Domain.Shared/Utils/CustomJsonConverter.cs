#nullable enable

using Newtonsoft.Json;
using System;

namespace FlowFlex.Domain.Shared.Utils;

public abstract class CustomJsonConverter<T> : JsonConverter<T>
{
    public override T? ReadJson(JsonReader reader, Type objectType, T? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return CustomReadJson(new CustomJsonReader(reader), objectType, existingValue, hasExistingValue, serializer);
    }

    public override void WriteJson(JsonWriter writer, T? value, JsonSerializer serializer)
    {
        CustomWriteJson(new CustomJsonWriter(writer), value, serializer);
    }

    public abstract T? CustomReadJson(CustomJsonReader reader, Type objectType, T? existingValue, bool hasExistingValue, JsonSerializer serializer);

    public abstract void CustomWriteJson(CustomJsonWriter writer, T? value, JsonSerializer serializer);
}
