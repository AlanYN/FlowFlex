
namespace FlowFlex.Domain.Shared.Models.RuleEngine;

public class RuleQueryRequest : QueryPageModel
{
    public bool IsAsc { get; set; } = true;

    public string OrderBy { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public string CreateBy { get; set; }
}
