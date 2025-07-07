using FlowFlex.Domain.Shared.Enums.DynamicData;

namespace FlowFlex.Domain.Shared.Models;

public class DTableFilterSimpleConfig
{
    public long DynamicFieldId { get; set; }

    public string FieldName { get; set; }

    public string FilterValue { get; set; }

    public DataType DataType { get; set; }

    public string DataTypeStr { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsTableMustShow { get; set; }
}
