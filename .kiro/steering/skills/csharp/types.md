---
inclusion: manual
---

# Type Traps

- Struct is value type — assignment copies, modifications don't affect original
- Boxing allocates — `object o = 5;` creates heap allocation
- `readonly` field can still have mutable reference type — contents can change
- `default(int)` is 0, `default(bool)` is false — not always obvious
- Struct in interface variable — boxed, modifications lost
- `Equals` on struct — default uses reflection (slow), override for performance
- `==` on struct — not defined by default, must implement
- Nullable value type boxing — `int? x = null; object o = x;` results in null object
- `decimal` vs `double` — decimal is exact but slower, use for financial

## Snowflake ID → string（API 响应强制规则）

Snowflake IDs are `long` (Int64). JavaScript's `Number.MAX_SAFE_INTEGER` is 2^53-1, so any `long` value larger than that loses precision when parsed as a JS number.

**Rule: NEVER return a snowflake `long` as a JSON number. Always serialize to `string`.**

```csharp
// Option A: Per-property attribute
public class OrderDto
{
    [JsonConverter(typeof(LongToStringConverter))]
    public long Id { get; set; }

    [JsonConverter(typeof(LongToStringConverter))]
    public long UserId { get; set; }
}

// Option B: Global converter (recommended — covers all long fields automatically)
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.JsonSerializerOptions.Converters.Add(new LongToStringConverter());
});

// LongToStringConverter implementation
public class LongToStringConverter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o)
        => reader.TokenType == JsonTokenType.String
            ? long.Parse(reader.GetString()!)
            : reader.GetInt64();

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions o)
        => writer.WriteStringValue(value.ToString());
}
```

Frontend type definition must declare all ID fields as `string`, never `number`:

```ts
// ✅ CORRECT
export namespace Order {
    export interface Item {
        id: string       // snowflake id — received as string from backend
        userId: string
    }
}

// ❌ WRONG — precision loss for large snowflake values
export namespace Order {
    export interface Item {
        id: number
    }
}
```
