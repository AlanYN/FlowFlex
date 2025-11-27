namespace FlowFlex.Domain.Shared.Models.RuleEngine;

public class RuleConditionDto
{
    public long Id { get; set; }

    public long RuleEngineRuleId { get; set; }

    public long RuleEngineConditionGroupId { get; set; }

    public string Field { get; set; }

    public string Operator { get; set; }

    public string Value { get; set; }

    public int Index { get; set; }

    public string PreviousLogicalOperator { get; set; }

    public string RuleEngineActionId { get; set; }

    public string RuleEngineActionTypeName { get; set; }

    public string RuleEngineActionHandlerName { get; set; }

    public string RuleEngineActionParameters { get; set; }
}

