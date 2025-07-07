using System.Collections.Generic;

namespace FlowFlex.Domain.Shared.Models.DynamicData;

public class PropertyGroupDto
{
    public long GroupId { get; set; }

    public string GroupName { get; set; }

    public List<DefineFieldDto> Fields { get; set; }
}
