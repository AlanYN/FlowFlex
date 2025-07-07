using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using FlowFlex.Domain.Shared.Enums;

namespace FlowFlex.Domain.Shared.Models;

public class DTableCreateRequest
{
    /// <summary>
    /// Table name
    /// </summary>
    
    public string TableName { get; set; }

    /// <summary>
    /// Table description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Module type
    /// </summary>
    
    public int ModuleType { get; set; }

    /// <summary>
    /// Columns included in the table (field name list)
    /// </summary>
    
    public List<string> Columns { get; set; }
}
