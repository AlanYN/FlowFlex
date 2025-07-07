using System.Collections.Generic;

using FlowFlex.Domain.Shared.Enums;
using FlowFlex.Domain.Shared.Enums.DynamicData;

namespace FlowFlex.Domain.Shared.Models;

public class DTableQueryResponse
{
    public long DTableId { get; set; }

    public string DTableName { get; set; }

    public string DTableDescription { get; set; }

    public int DTableModuleType { get; set; }

    public List<DynamicTableColumnModel> Columns { get; set; } = [];

    public List<DynamicTableFilterModel> Filters { get; set; } = [];
}

public class DynamicTableColumnModel
{
    public long DTableColumnId { get; set; }

    public long DynamicFieldId { get; set; }

    public string DynamicFieldName { get; set; }

    public long? DynamicFieldGroupId { get; set; }

    public string DynamicFieldDisplayName { get; set; }

    public DataType DynamicFieldDataType { get; set; }

    public string DynamicFieldDataTypeStr { get; set; }

    public bool? DynamicFieldIsStatic { get; set; }

    public int DisplayOrder { get; set; }

    public int? Width { get; set; }

    public int? MinWidth { get; set; }

    public bool IsFixed { get; set; }

    public string FixedPosition { get; set; }

    public bool IsSortable { get; set; }

    public bool IsLink { get; set; }
}

public class DynamicTableFilterModel
{
    public bool DynamicFieldIsStatic { get; set; }

    public long DynamicFieldId { get; set; }

    public string FieldName { get; set; }

    public string DisplayName { get; set; }

    public DataType DataType { get; set; }

    public string DataTypeStr { get; set; }

    public int DisplayOrder { get; set; }

    public List<DTableExampleResponse> ExampleItems { get; set; } = [];

    public List<DTableOptionsResponse> Options { get; set; } = [];
}

public class DTableExampleResponse
{
    public object FieldId { get; set; }

    public object FieldName { get; set; }

    public int FieldCount { get; set; }
}

public class DTableOptionsResponse
{
    public long FieldId { get; set; }

    public string FieldName { get; set; }
}
