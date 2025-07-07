namespace FlowFlex.Domain.Shared.Models.RuleEngine;

using Newtonsoft.Json;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using FlowFlex.Domain.Shared.Enums;
using FlowFlex.Domain.Shared.Enums.RuleEngine;

public class CreateRuleDto
{
    
    public string Name { get; set; }

    
    public List<CreateRuleConditionGroup> ConditionGroups { get; set; }

    
    public CreateRuleAction Action { get; set; }
}

public class CreateRuleConditionGroup
{
    
    public int Index { get; set; }

    
    public string PreviousLogicalOperator { get; set; }

    
    public List<CreateRuleCondition> Conditions { get; set; }
}

public class CreateRuleCondition
{
    
    public string Field { get; set; }

    
    public RuleEngineOperatorEnum Operator { get; set; }

    
    public string Value { get; set; }

    
    public int Index { get; set; }

    
    public string PreviousLogicalOperator { get; set; }
}

public class CreateRuleAction
{
    
    public RuleEngineActionTypeEnum ActionType { get; set; }

    
    public string HandlerName { get; set; }

    public string Parameters { get; set; }
}
