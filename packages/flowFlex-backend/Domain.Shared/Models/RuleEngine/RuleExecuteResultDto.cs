namespace FlowFlex.Domain.Shared.Models.RuleEngine;

public class RuleExecuteResultDto
{
    public bool IsSuccess { get; set; }

    public object HandlerData { get; set; }
}
