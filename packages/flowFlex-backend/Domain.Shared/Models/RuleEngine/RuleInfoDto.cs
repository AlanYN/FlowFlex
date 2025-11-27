namespace FlowFlex.Domain.Shared.Models.RuleEngine;

using System;
using System.Collections.Generic;

using FlowFlex.Domain.Shared.Enums;

public class RuleInfoDto
{
    public long Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public List<RuleEngineConditionGroupDto> ConditionGroups { get; set; }

    public RuleEngineActionDto Action { get; set; }

    public string CreateBy { get; set; }

    public DateTimeOffset CreateDate { get; set; }
}

public class RuleEngineConditionGroupDto
{
    public long RuleEngineRuleId { get; set; }

    public int Index { get; set; }

    public string PreviousLogicalOperator { get; set; }

    public List<RuleEngineConditionDto> Conditions { get; set; }
}

public class RuleEngineConditionDto
{
    public long RuleEngineRuleId { get; set; }

    public long Id { get; set; }

    public long RuleEngineConditionGroupId { get; set; }

    public string Field { get; set; }

    public string Operator { get; set; }

    public string Value { get; set; }

    public int Index { get; set; }

    public string PreviousLogicalOperator { get; set; }
}

public class RuleEngineActionDto
{
    public long RuleEngineRuleId { get; set; }

    public RuleEngineActionTypeEnum ActionType { get; set; }

    public string ActionTypeName { get; set; }

    public string HandlerName { get; set; }

    public string Parameters { get; set; }
}


