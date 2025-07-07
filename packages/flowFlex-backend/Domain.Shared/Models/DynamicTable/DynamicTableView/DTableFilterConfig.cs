using System.Collections.Generic;

using FlowFlex.Domain.Shared.Enums.DynamicData;
using FlowFlex.Domain.Shared.Models.DynamicData;

namespace FlowFlex.Domain.Shared.Models;

/// <summary>
/// Dynamic table filter configuration
/// </summary>
public class DTableFilterConfig
{
    /// <summary>
    /// Dynamic field ID
    /// </summary>
    public long DynamicFieldId { get; set; }

    /// <summary>
    /// Filter key
    /// </summary>
    public string FieldName { get; set; }

    /// <summary>
    /// Filter value
    /// </summary>
    public string FilterValue { get; set; }

    /// <summary>
    /// Label name
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Data type
    /// </summary>
    public DataType DataType { get; set; }

    /// <summary>
    /// Data type string
    /// </summary>
    public string DataTypeStr { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int DisplayOrder { get; set; }

    public bool IsTableMustShow { get; set; }

    /// <summary>
    /// Examples
    /// </summary>
    public List<DTableExampleResponse> ExampleItems { get; set; } = [];

    /// <summary>
    /// Options
    /// </summary>
    public List<DTableOptionsResponse> Options { get; set; } = [];

    public int ModuleId { get; set; }

    public int GroupId { get; set; }

    public string Description { get; set; }

    public SourceType SourceType { get; set; }

    public string SourceName { get; set; }

    public int Sort { get; set; }

    public SortType SortType { get; set; }

    public long? RefFieldId { get; set; }

    public bool IsSystemDefine { get; set; }

    public long? FormatId { get; set; }

    public long? ValidateId { get; set; }

    public FieldTypeFormatDto Format { get; set; }

    public List<DropdownItemDto> DropdownItems { get; set; } = [];

    public List<object> ConnectionItems { get; set; } = [];

    public FieldValidateDto FieldValidate { get; set; }

    public bool AllowEdit { get; set; } = true;

    public bool AllowEditItem { get; set; } = true;

    public bool IsDisplayField { get; set; }

    public bool IsRequired { get; set; }
}
