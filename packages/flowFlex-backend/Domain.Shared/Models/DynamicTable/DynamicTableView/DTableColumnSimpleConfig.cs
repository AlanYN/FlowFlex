using FlowFlex.Domain.Shared.Enums.DynamicData;

namespace FlowFlex.Domain.Shared.Models;


public class DTableColumnSimpleConfig
{
    public long DynamicFieldId { get; set; }

    public int DisplayOrder { get; set; }

    public string DisplayName { get; set; }

    public int Width { get; set; }

    public int MinWidth { get; set; }

    public bool IsSortable { get; set; }

    public string FixedPosition { get; set; }

    public ColumnType ColumnType { get; set; }

    public bool IsTableMustShow { get; set; }

    public DataType DataType { get; set; }

    public string DataTypeStr { get; set; }

    public string FieldName { get; set; }
}

public enum ColumnType
{
    Link = 1,

    Drawer = 2
}
