using FlowFlex.Domain.Shared.Enums.DynamicData;
using Newtonsoft.Json.Linq;
using SqlSugar;

namespace FlowFlex.Domain.Entities.DynamicData;

/// <summary>
/// Data value entity - EAV pattern for storing dynamic field values
/// </summary>
[SugarTable("ff_data_value")]
public class DataValue : OwEntityBase
{
    /// <summary>
    /// Module ID
    /// </summary>
    [SugarColumn(ColumnName = "module_id")]
    public int ModuleId { get; set; }

    /// <summary>
    /// Business data ID (FK to business_data.id)
    /// </summary>
    [SugarColumn(ColumnName = "business_id")]
    public long BusinessId { get; set; }

    /// <summary>
    /// Field definition ID (FK to define_field.id)
    /// </summary>
    [SugarColumn(ColumnName = "field_id")]
    public long FieldId { get; set; }

    /// <summary>
    /// Field name
    /// </summary>
    [SugarColumn(ColumnName = "field_name", Length = 100)]
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Data type
    /// </summary>
    [SugarColumn(ColumnName = "data_type")]
    public DataType DataType { get; set; }

    /// <summary>
    /// Long value storage
    /// </summary>
    [SugarColumn(ColumnName = "long_value", IsNullable = true)]
    public long? LongValue { get; set; }

    /// <summary>
    /// Int value storage
    /// </summary>
    [SugarColumn(ColumnName = "int_value", IsNullable = true)]
    public int? IntValue { get; set; }

    /// <summary>
    /// Double value storage
    /// </summary>
    [SugarColumn(ColumnName = "double_value", IsNullable = true)]
    public double? DoubleValue { get; set; }

    /// <summary>
    /// Text value storage (unlimited)
    /// </summary>
    [SugarColumn(ColumnName = "text_value", ColumnDataType = "text", IsNullable = true)]
    public string? TextValue { get; set; }

    /// <summary>
    /// Short varchar storage (100 chars)
    /// </summary>
    [SugarColumn(ColumnName = "varchar100_value", Length = 100, IsNullable = true)]
    public string? Varchar100Value { get; set; }

    /// <summary>
    /// Medium varchar storage (500 chars)
    /// </summary>
    [SugarColumn(ColumnName = "varchar500_value", Length = 500, IsNullable = true)]
    public string? Varchar500Value { get; set; }

    /// <summary>
    /// Long varchar storage (5000 chars)
    /// </summary>
    [SugarColumn(ColumnName = "varchar_value", Length = 5000, IsNullable = true)]
    public string? VarcharValue { get; set; }

    /// <summary>
    /// Boolean value storage
    /// </summary>
    [SugarColumn(ColumnName = "bool_value", IsNullable = true)]
    public bool? BoolValue { get; set; }

    /// <summary>
    /// DateTime value storage
    /// </summary>
    [SugarColumn(ColumnName = "date_time_value", IsNullable = true)]
    public DateTimeOffset? DateTimeValue { get; set; }

    /// <summary>
    /// JSON array/object storage
    /// </summary>
    [SugarColumn(ColumnName = "string_list_value", ColumnDataType = "jsonb", IsJson = true, IsNullable = true)]
    public JToken? StringListValue { get; set; }
}
