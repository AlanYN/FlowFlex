using FlowFlex.Domain.Entities;
using FlowFlex.Domain.Shared.Enums.DynamicData;
using Newtonsoft.Json.Linq;
using SqlSugar;

namespace FlowFlex.Domain.Entities.DynamicData;

/// <summary>
/// Field definition entity
/// </summary>
[SugarTable("ff_define_field")]
public class DefineField : OwEntityBase
{
    /// <summary>
    /// Module ID
    /// </summary>
    [SugarColumn(ColumnName = "module_id")]
    public int ModuleId { get; set; }

    /// <summary>
    /// Display name
    /// </summary>
    [SugarColumn(ColumnName = "display_name", Length = 200)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Field name (identifier)
    /// </summary>
    [SugarColumn(ColumnName = "field_name", Length = 100)]
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    [SugarColumn(ColumnName = "description", ColumnDataType = "text", IsNullable = true)]
    public string? Description { get; set; }

    /// <summary>
    /// Data type
    /// </summary>
    [SugarColumn(ColumnName = "data_type")]
    public DataType DataType { get; set; }

    /// <summary>
    /// Source type
    /// </summary>
    [SugarColumn(ColumnName = "source_type", IsNullable = true)]
    public int? SourceType { get; set; }

    /// <summary>
    /// Source name
    /// </summary>
    [SugarColumn(ColumnName = "source_name", Length = 100, IsNullable = true)]
    public string? SourceName { get; set; }

    /// <summary>
    /// Format ID
    /// </summary>
    [SugarColumn(ColumnName = "format_id", IsNullable = true)]
    public long? FormatId { get; set; }

    /// <summary>
    /// Validate ID
    /// </summary>
    [SugarColumn(ColumnName = "validate_id", IsNullable = true)]
    public long? ValidateId { get; set; }

    /// <summary>
    /// Reference field ID
    /// </summary>
    [SugarColumn(ColumnName = "ref_field_id", IsNullable = true)]
    public long? RefFieldId { get; set; }

    /// <summary>
    /// Whether is system defined
    /// </summary>
    [SugarColumn(ColumnName = "is_system_define")]
    public bool IsSystemDefine { get; set; }

    /// <summary>
    /// Whether is static field
    /// </summary>
    [SugarColumn(ColumnName = "is_static")]
    public bool IsStatic { get; set; }

    /// <summary>
    /// Whether is display field
    /// </summary>
    [SugarColumn(ColumnName = "is_display_field")]
    public bool IsDisplayField { get; set; }

    /// <summary>
    /// Whether must use
    /// </summary>
    [SugarColumn(ColumnName = "is_must_use")]
    public bool IsMustUse { get; set; }

    /// <summary>
    /// Whether is required
    /// </summary>
    [SugarColumn(ColumnName = "is_required")]
    public bool IsRequired { get; set; }

    /// <summary>
    /// Whether must show in table
    /// </summary>
    [SugarColumn(ColumnName = "is_table_must_show")]
    public bool IsTableMustShow { get; set; }

    /// <summary>
    /// Whether is hidden
    /// </summary>
    [SugarColumn(ColumnName = "is_hidden")]
    public bool IsHidden { get; set; }

    /// <summary>
    /// Whether is computed field
    /// </summary>
    [SugarColumn(ColumnName = "is_computed")]
    public bool IsComputed { get; set; }

    /// <summary>
    /// Whether allow edit
    /// </summary>
    [SugarColumn(ColumnName = "allow_edit")]
    public bool AllowEdit { get; set; } = true;

    /// <summary>
    /// Whether allow edit item
    /// </summary>
    [SugarColumn(ColumnName = "allow_edit_item")]
    public bool AllowEditItem { get; set; } = true;

    /// <summary>
    /// Sort order
    /// </summary>
    [SugarColumn(ColumnName = "sort")]
    public int Sort { get; set; }

    /// <summary>
    /// Additional info (JSONB)
    /// </summary>
    [SugarColumn(ColumnName = "additional_info", ColumnDataType = "jsonb", IsJson = true, IsNullable = true)]
    public JObject? AdditionalInfo { get; set; }
}
