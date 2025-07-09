using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using FlowFlex.Domain.Shared.Enums;

namespace FlowFlex.Domain.Shared.Models;

public class DTableAndViewCreateDto
{

    public string TableName { get; set; }

    public string Description { get; set; }


    public List<long> DynamicFieldIds { get; set; }


    public int ModuleType { get; set; }


    public string ViewName { get; set; }


    public ViewShareTypeEnum? ViewShareType { get; set; }
}
