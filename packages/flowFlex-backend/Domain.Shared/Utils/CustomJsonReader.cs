using Newtonsoft.Json;
using System;

namespace FlowFlex.Domain.Shared.Utils;

public class CustomJsonReader(JsonReader jsonReader)
{
    public JsonReader Obj => jsonReader;

    public JsonToken TokenType => jsonReader.TokenType;

    public object Value => jsonReader.Value;

    public Type ValueType => jsonReader.ValueType;

    public int Depth => jsonReader.Depth;

    public string Path => jsonReader.Path;

    public bool CloseInput => jsonReader.CloseInput;

    public bool SupportMultipleContent => jsonReader.SupportMultipleContent;

    public bool Read() => jsonReader.Read();

    public bool? ReadAsBoolean() => jsonReader.ReadAsBoolean();

    public DateTimeOffset? ReadAsDateTimeOffset() => jsonReader.ReadAsDateTimeOffset();

    public decimal? ReadAsDecimal() => jsonReader.ReadAsDecimal();

    public double? ReadAsDouble() => jsonReader.ReadAsDouble();

    public int? ReadAsInt32() => jsonReader.ReadAsInt32();

    public string ReadAsString() => jsonReader.ReadAsString();

    public void Skip() => jsonReader.Skip();

    public void Close() => jsonReader.Close();
}
