using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Unis
{
    public enum MatchingRuleOptionEnum
    {
        [Description("FuzzyRule")]
        FuzzyRule = 1,

        [Description("FieldsType")]
        FieldsType = 2
    }
}
