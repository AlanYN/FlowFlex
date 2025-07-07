using System.ComponentModel.DataAnnotations;

using FlowFlex.Domain.Shared.Enums;

namespace FlowFlex.Domain.Shared.Models;
public class DTableViewCreateDto
{
    
    public string Name { get; set; }

    
    public ViewShareTypeEnum ShareType { get; set; }

    
    public int ModuleType { get; set; }
}

