using System.Collections.Generic;

namespace FlowFlex.Domain.Shared.Models;

public class DTableDynamicFieldsResponse
{
    public long GroupId { get; set; }

    public string GroupName { get; set; }

    public List<DTableColumnConfig> Fields { get; set; } = [];
}
