using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using FlowFlex.Domain.Shared.JsonConverters;

namespace FlowFlex.Application.Contracts.Dtos.OW.StageCondition
{
    /// <summary>
    /// Stage Condition Output DTO - For query responses
    /// </summary>
    public class StageConditionOutputDto
    {
        /// <summary>
        /// Condition ID
        /// </summary>
        [JsonConverter(typeof(LongToStringConverter))]
        public long Id { get; set; }

        /// <summary>
        /// Associated Stage ID
        /// </summary>
        [JsonConverter(typeof(LongToStringConverter))]
        public long StageId { get; set; }

        /// <summary>
        /// Associated Workflow ID
        /// </summary>
        [JsonConverter(typeof(LongToStringConverter))]
        public long WorkflowId { get; set; }

        /// <summary>
        /// Condition Name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Condition Description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// RulesEngine Workflow JSON
        /// </summary>
        public string RulesJson { get; set; } = string.Empty;

        /// <summary>
        /// Actions JSON array
        /// </summary>
        public string ActionsJson { get; set; } = string.Empty;

        /// <summary>
        /// Fallback Stage ID
        /// </summary>
        [JsonConverter(typeof(LongToStringConverter))]
        public long? FallbackStageId { get; set; }

        /// <summary>
        /// Is Active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Condition Status (Valid, Invalid, Draft)
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Create Date
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }

        /// <summary>
        /// Modify Date
        /// </summary>
        public DateTimeOffset ModifyDate { get; set; }

        /// <summary>
        /// Parsed Rules (for convenience)
        /// </summary>
        public List<ConditionRuleDto>? Rules { get; set; }

        /// <summary>
        /// Parsed Actions (for convenience)
        /// </summary>
        public List<ConditionActionDto>? Actions { get; set; }
    }

    /// <summary>
    /// Condition Rule DTO
    /// </summary>
    public class ConditionRuleDto
    {
        /// <summary>
        /// Rule Name
        /// </summary>
        public string RuleName { get; set; } = string.Empty;

        /// <summary>
        /// Rule Expression
        /// </summary>
        public string Expression { get; set; } = string.Empty;
    }

    /// <summary>
    /// Condition Action DTO
    /// </summary>
    public class ConditionActionDto
    {
        /// <summary>
        /// Action Type
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Execution Order
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Target Stage ID (for GoToStage action)
        /// </summary>
        [JsonConverter(typeof(LongToStringConverter))]
        public long? TargetStageId { get; set; }

        /// <summary>
        /// Action Definition ID (for TriggerAction)
        /// </summary>
        [JsonConverter(typeof(LongToStringConverter))]
        public long? ActionDefinitionId { get; set; }

        /// <summary>
        /// Additional Parameters
        /// </summary>
        public Dictionary<string, object>? Parameters { get; set; }
    }
}
