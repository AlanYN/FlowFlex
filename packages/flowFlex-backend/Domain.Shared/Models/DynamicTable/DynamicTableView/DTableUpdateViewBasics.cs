using System.ComponentModel.DataAnnotations;

using FlowFlex.Domain.Shared.Enums;

namespace FlowFlex.Domain.Shared.Models;

public class DTableUpdateViewBasics
{

    public string Name { get; set; }


    public ViewShareTypeEnum ShareType { get; set; }
}
