using FlowFlex.Domain.Entities.Base;
using SqlSugar;

namespace FlowFlex.Domain.Entities.Integration;

/// <summary>
/// Dynamic field definition for Field Mapping - stores WFE field definitions that can be used in field mappings
/// </summary>
[SugarTable("ff_dynamic_field")]
public class DynamicField : EntityBaseCreateInfo
{
    /// <summary>
    /// Field ID (API Name) - unique identifier for the field, corresponds to vIfKey in static-field.json
    /// </summary>
    [SugarColumn(ColumnName = "field_id", Length = 100)]
    public string FieldId { get; set; } = string.Empty;

    /// <summary>
    /// Field label (display name)
    /// </summary>
    [SugarColumn(ColumnName = "field_label", Length = 200)]
    public string FieldLabel { get; set; } = string.Empty;

    /// <summary>
    /// Form property name (corresponds to formProp in static-field.json)
    /// </summary>
    [SugarColumn(ColumnName = "form_prop", Length = 100)]
    public string FormProp { get; set; } = string.Empty;

    /// <summary>
    /// Field category (e.g., "Basic Info", "Lead", "Application", "Business Details", "System")
    /// </summary>
    [SugarColumn(ColumnName = "category", Length = 100)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Field data type (Text, Number, Date, Boolean, Lookup)
    /// </summary>
    [SugarColumn(ColumnName = "field_type")]
    public int FieldType { get; set; } = 0; // 0 = Text by default

    /// <summary>
    /// Sort order for display
    /// </summary>
    [SugarColumn(ColumnName = "sort_order")]
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Whether this field is required
    /// </summary>
    [SugarColumn(ColumnName = "is_required")]
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// Whether this is a system field (cannot be deleted)
    /// </summary>
    [SugarColumn(ColumnName = "is_system")]
    public bool IsSystem { get; set; } = false;
}

