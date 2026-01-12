using FlowFlex.Domain.Entities;
using SqlSugar;

namespace FlowFlex.Domain.Entities.DynamicData;

/// <summary>
/// Field group entity
/// </summary>
[SugarTable("ff_field_group")]
public class FieldGroup : OwEntityBase
{
    /// <summary>
    /// Module ID
    /// </summary>
    [SugarColumn(ColumnName = "module_id")]
    public int ModuleId { get; set; }

    /// <summary>
    /// Group name
    /// </summary>
    [SugarColumn(ColumnName = "group_name", Length = 200)]
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// Sort order
    /// </summary>
    [SugarColumn(ColumnName = "sort")]
    public int Sort { get; set; }

    /// <summary>
    /// Whether is system defined
    /// </summary>
    [SugarColumn(ColumnName = "is_system_define")]
    public bool IsSystemDefine { get; set; }

    /// <summary>
    /// Whether is default group
    /// </summary>
    [SugarColumn(ColumnName = "is_default")]
    public bool IsDefault { get; set; }

    /// <summary>
    /// Field IDs in this group (PostgreSQL array)
    /// </summary>
    [SugarColumn(ColumnName = "fields", ColumnDataType = "bigint[]", IsArray = true, IsNullable = true)]
    public long[]? Fields { get; set; }
}
