using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Domain.Shared.Models;

public class DTableInitDto
{

    public string TableName { get; set; }

    public string Description { get; set; }
}
