using FlowFlex.Domain.Shared.Enums.Unis;

namespace FlowFlex.Domain.Shared.Models
{
    public class ExternalMatchingRuleMappingDataModel
    {

        public long MasterId { get; set; }
        public string MasterName { get; set; }
        public MatchingRuleFieldsTypeEnum FieldsType { get; set; }

    }
}
