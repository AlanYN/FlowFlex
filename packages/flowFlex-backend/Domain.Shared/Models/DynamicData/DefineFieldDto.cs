using FlowFlex.Domain.Shared.Enums.DynamicData;
using Newtonsoft.Json.Linq;

namespace FlowFlex.Domain.Shared.Models.DynamicData;

/// <summary>
/// Field definition DTO
/// </summary>
public class DefineFieldDto
{
    /// <summary>
    /// Field ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Module ID
    /// </summary>
    public int ModuleId { get; set; }

    /// <summary>
    /// Group ID
    /// </summary>
    public long GroupId { get; set; }

    /// <summary>
    /// Display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Field name (identifier)
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Data type
    /// </summary>
    public DataType DataType { get; set; }

    /// <summary>
    /// Source type
    /// </summary>
    public int? SourceType { get; set; }

    /// <summary>
    /// Source name
    /// </summary>
    public string? SourceName { get; set; }

    /// <summary>
    /// Reference field ID
    /// </summary>
    public long? RefFieldId { get; set; }

    /// <summary>
    /// Whether is system defined
    /// </summary>
    public bool IsSystemDefine { get; set; }

    /// <summary>
    /// Whether is static field
    /// </summary>
    public bool IsStatic { get; set; }

    /// <summary>
    /// Whether is display field
    /// </summary>
    public bool IsDisplayField { get; set; }

    /// <summary>
    /// Whether must use
    /// </summary>
    public bool IsMustUse { get; set; }

    /// <summary>
    /// Whether is required
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Whether must show in table
    /// </summary>
    public bool IsTableMustShow { get; set; }

    /// <summary>
    /// Whether is hidden
    /// </summary>
    public bool IsHidden { get; set; }

    /// <summary>
    /// Whether is computed field
    /// </summary>
    public bool IsComputed { get; set; }

    /// <summary>
    /// Whether allow edit
    /// </summary>
    public bool AllowEdit { get; set; } = true;

    /// <summary>
    /// Whether allow edit item
    /// </summary>
    public bool AllowEditItem { get; set; } = true;

    /// <summary>
    /// Sort order
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// Dropdown items (for dropdown type)
    /// </summary>
    public List<DropdownItemDto>? DropdownItems { get; set; }

    /// <summary>
    /// Field format configuration
    /// </summary>
    public FieldTypeFormatDto? Format { get; set; }

    /// <summary>
    /// Field validation configuration
    /// </summary>
    public FieldValidateDto? FieldValidate { get; set; }

    /// <summary>
    /// Additional info (JSONB)
    /// </summary>
    public JObject? AdditionalInfo { get; set; }
}

/// <summary>
/// Dropdown item DTO
/// </summary>
public class DropdownItemDto
{
    /// <summary>
    /// Item ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Item value
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Item label
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Sort order
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// Whether is default
    /// </summary>
    public bool IsDefault { get; set; }
}

/// <summary>
/// Field type format DTO
/// </summary>
public class FieldTypeFormatDto
{
    /// <summary>
    /// Format ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Format type
    /// </summary>
    public int FormatType { get; set; }

    /// <summary>
    /// Format pattern
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// Decimal places (for number type)
    /// </summary>
    public int? DecimalPlaces { get; set; }

    /// <summary>
    /// Date format (for date type)
    /// </summary>
    public string? DateFormat { get; set; }
}

/// <summary>
/// Field validation DTO
/// </summary>
public class FieldValidateDto
{
    /// <summary>
    /// Validation ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Min length
    /// </summary>
    public int? MinLength { get; set; }

    /// <summary>
    /// Max length
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Min value
    /// </summary>
    public decimal? MinValue { get; set; }

    /// <summary>
    /// Max value
    /// </summary>
    public decimal? MaxValue { get; set; }

    /// <summary>
    /// Regex pattern
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// Custom validation message
    /// </summary>
    public string? Message { get; set; }
}
