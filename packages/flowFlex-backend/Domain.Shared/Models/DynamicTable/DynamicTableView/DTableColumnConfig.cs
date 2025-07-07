using System.Collections.Generic;
using FlowFlex.Domain.Shared.Enums.DynamicData;
using FlowFlex.Domain.Shared.Models.DynamicData;

namespace FlowFlex.Domain.Shared.Models;

/// <summary>
/// Dynamic table column configuration
/// </summary>
public class DTableColumnConfig
{
    /// <summary>
    /// Dynamic field ID
    /// </summary> 
    public long DynamicFieldId { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Column width
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Minimum width
    /// </summary>
    public int MinWidth { get; set; }

    /// <summary>
    /// Fixed position (left/right)
    /// </summary>
    public string FixedPosition { get; set; }

    /// <summary>
    /// Whether sortable
    /// </summary>
    public bool IsSortable { get; set; }

    public ColumnType ColumnType { get; set; }

    public DataType DataType { get; set; }

    public string DataTypeStr { get; set; }

    public string FieldName { get; set; }

    public string FilterValue { get; set; }

    public string DisplayName { get; set; }

    public bool IsTableMustShow { get; set; }

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
