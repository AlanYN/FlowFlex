using Newtonsoft.Json;
using System;
using System.Numerics;

namespace FlowFlex.Domain.Shared.Utils;

public class CustomJsonWriter(JsonWriter jsonWriter)
{
    public JsonWriter Obj => jsonWriter;

    public void WriteNull() => jsonWriter.WriteNull();

    public void WriteStartObject() => jsonWriter.WriteStartObject();

    public void WriteEndObject() => jsonWriter.WriteEndObject();

    public void WriteStartArray() => jsonWriter.WriteStartArray();

    public void WriteEndArray() => jsonWriter.WriteEndArray();

    public void WritePropertyName(string name) => jsonWriter.WritePropertyName(name);

    public void WriteValue(object value) => jsonWriter.WriteValue(value);

    public void WriteValue(string value) => jsonWriter.WriteValue(value);

    public void WriteValue(int value) => jsonWriter.WriteValue(value);

    public void WriteValue(long value) => jsonWriter.WriteValue(value);

    public void WriteValue(double value) => jsonWriter.WriteValue(value);

    public void WriteValue(decimal value) => jsonWriter.WriteValue(value);

    public void WriteValue(bool value) => jsonWriter.WriteValue(value);

    public void WriteValue(DateTime value) => jsonWriter.WriteValue(value);

    public void WriteValue(DateTimeOffset value) => jsonWriter.WriteValue(value);

    public void WriteValue(Guid value) => jsonWriter.WriteValue(value);

    public void WriteValue(TimeSpan value) => jsonWriter.WriteValue(value);

    public void WriteValue(Uri value) => jsonWriter.WriteValue(value);

    public void WriteValue(byte[] value) => jsonWriter.WriteValue(value);

    public void WriteValue(byte value) => jsonWriter.WriteValue(value);

    public void WriteValue(sbyte value) => jsonWriter.WriteValue(value);

    public void WriteValue(short value) => jsonWriter.WriteValue(value);

    public void WriteValue(ushort value) => jsonWriter.WriteValue(value);

    public void WriteValue(uint value) => jsonWriter.WriteValue(value);

    public void WriteValue(ulong value) => jsonWriter.WriteValue(value);

    public void WriteValue(float value) => jsonWriter.WriteValue(value);

    public void WriteValue(char value) => jsonWriter.WriteValue(value);

    public void WriteValue(Enum value) => jsonWriter.WriteValue(value);

    public void WriteValue(Version value) => jsonWriter.WriteValue(value);

    public void WriteValue(BigInteger value) => jsonWriter.WriteValue(value);

    public void WriteValue(BigInteger? value) => jsonWriter.WriteValue(value);

    public void WriteValue(DateTime? value) => jsonWriter.WriteValue(value);

    public void WriteValue(DateTimeOffset? value) => jsonWriter.WriteValue(value);

    public void WriteValue(Guid? value) => jsonWriter.WriteValue(value);

    public void WriteValue(TimeSpan? value) => jsonWriter.WriteValue(value);

    public void WriteValue(byte? value) => jsonWriter.WriteValue(value);

    public void WriteValue(sbyte? value) => jsonWriter.WriteValue(value);

    public void WriteValue(short? value) => jsonWriter.WriteValue(value);

    public void WriteValue(ushort? value) => jsonWriter.WriteValue(value);

    public void WriteValue(uint? value) => jsonWriter.WriteValue(value);

    public void WriteValue(ulong? value) => jsonWriter.WriteValue(value);

    public void WriteValue(float? value) => jsonWriter.WriteValue(value);

    public void WriteValue(double? value) => jsonWriter.WriteValue(value);

    public void WriteValue(decimal? value) => jsonWriter.WriteValue(value);

    public void WriteValue(char? value) => jsonWriter.WriteValue(value);

    public void WriteValue(int? value) => jsonWriter.WriteValue(value);

    public void WriteValue(long? value) => jsonWriter.WriteValue(value);

    public void WriteValue(bool? value) => jsonWriter.WriteValue(value);

    public void WriteComment(string text) => jsonWriter.WriteComment(text);

    public void WriteWhitespace(string ws) => jsonWriter.WriteWhitespace(ws);

    public void WriteRaw(string json) => jsonWriter.WriteRaw(json);

    public void WriteRawValue(string json) => jsonWriter.WriteRawValue(json);

    public void WriteUndefined() => jsonWriter.WriteUndefined();

    public void WriteToken(JsonToken token) => jsonWriter.WriteToken(token);

    public void WriteToken(JsonToken token, object value) => jsonWriter.WriteToken(token, value);

    public void WriteToken(JsonReader reader) => jsonWriter.WriteToken(reader);

    public void WriteToken(JsonReader reader, bool writeChildren) => jsonWriter.WriteToken(reader, writeChildren);
}
