using System;
using System.Collections.Generic;
using FlowFlex.Domain.Shared.Enums.DynamicData;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Models.DynamicData;

namespace FlowFlex.Domain.Shared.Models.DynamicData;

public class DefineFieldDto : IField
{
    /// <summary>
    /// Unique identifier for the field
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// ID of the module this field belongs to
    /// </summary>
    public int ModuleId { get; set; }

    /// <summary>
    /// ID of the group this field belongs to
    /// </summary>
    public int GroupId { get; set; }

    /// <summary>
    /// Display name of the field
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Internal name of the field
    /// </summary>
    public string FieldName { get; set; }

    /// <summary>
    /// Description of the field
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Data type of the field
    /// </summary>
    public DataType DataType { get; set; }

    public DataType FieldType => DataType;

    public SourceType SourceType { get; set; }

    public string SourceName { get; set; }

    public int Sort { get; set; }

    /// <summary>
    /// Sort type for the field
    /// </summary>
    public SortType SortType { get; set; }

    /// <summary>
    /// reference field id
    /// </summary>
    public long? RefFieldId { get; set; }

    /// <summary>
    /// Indicates if the field is system-defined
    /// </summary>
    public bool IsSystemDefine { get; set; }

    /// <summary>
    /// Whether the field is a static field
    /// </summary>
    public bool IsStatic { get; set; }

    /// <summary>
    /// ID of the format settings for this field
    /// </summary>
    public long? FormatId { get; set; }

    /// <summary>
    /// ID of the validation settings for this field
    /// </summary>
    public long? ValidateId { get; set; }

    /// <summary>
    /// Format settings for the field
    /// </summary>
    public FieldTypeFormatDto Format { get; set; }

    /// <summary>
    /// List of dropdown items if the field is a dropdown type
    /// </summary>
    public List<DropdownItemDto> DropdownItems { get; set; }

    public List<ConnectionDto> ConnectionItems { get; set; }

    /// <summary>
    /// Validation settings for the field
    /// </summary>
    public FieldValidateDto FieldValidate { get; set; }

    public string CreateBy { get; set; }

    public DateTimeOffset CreateDate { get; set; }

    public string ModifyBy { get; set; }

    public DateTimeOffset ModifyDate { get; set; }

    public long CreateUserId { get; set; }

    public long ModifyUserId { get; set; }

    /// <summary>
    /// Whether editing is allowed
    /// </summary>
    public bool AllowEdit { get; set; } = true;

    /// <summary>
    /// Whether editing options is allowed
    /// </summary>
    public bool AllowEditItem { get; set; } = true;

    /// <summary>
    /// Whether this is a display field name
    /// </summary>
    public bool IsDisplayField { get; set; }

    /// <summary>
    /// Whether the field is required
    /// </summary>
    public bool IsRequired { get; set; }

    public bool IsTableMustShow { get; set; }

    /// <summary>
    /// Hidden field, will not be displayed on the frontend page
    /// </summary>
    public bool IsHidden { get; set; }

    /// <summary>
    /// Whether it is a computed property
    /// </summary>
    public bool IsComputed { get; set; }

    public AdditionalInfo AdditionalInfo { get; set; } = new();
}
