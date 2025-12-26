using FlowFlex.Domain.Entities;
using Newtonsoft.Json.Linq;
using SqlSugar;

namespace FlowFlex.Domain.Entities.DynamicData;

/// <summary>
/// Business data entity - main table for dynamic data
/// </summary>
[SugarTable("ff_business_data")]
public class BusinessData : OwEntityBase
{
    /// <summary>
    /// Module ID
    /// </summary>
    [SugarColumn(ColumnName = "module_id")]
    public int ModuleId { get; set; }

    /// <summary>
    /// Internal extension data (JSONB)
    /// </summary>
    [SugarColumn(ColumnName = "internal_data", ColumnDataType = "jsonb", IsJson = true)]
    public JObject? InternalData { get; set; }
}
