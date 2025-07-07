using System;
using FlowFlex.Domain.Shared.Enums.DynamicData;

namespace FlowFlex.Domain.Shared.Attr;

public class FieldTypeAttribute : Attribute
{
    public FieldTypeAttribute(DataType fieldType)
    {
        FieldType = fieldType;
    }

    public DataType FieldType { get; set; }

    public string DisplayName { get; set; }

    public string FieldName { get; set; }

    public bool IsDisplayField { get; set; }

    public bool IsRequired { get; set; }
}
