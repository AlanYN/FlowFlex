namespace FlowFlex.Domain.Shared.Models.RuleEngine;

public class RuleHandlerResultDto
{
    public bool Status { get; set; }

    public string Message { get; set; }

    public object Data { get; set; }
}
