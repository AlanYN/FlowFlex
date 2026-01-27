using Newtonsoft.Json;
using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.OW.StageCondition
{
    /// <summary>
    /// Frontend rule configuration model for stage condition rules
    /// Used to parse frontend custom rule format before converting to RulesEngine format
    /// </summary>
    public class FrontendRuleConfig
    {
        /// <summary>
        /// Logic type for combining rules: "AND" or "OR"
        /// </summary>
        [JsonProperty("logic")]
        public string Logic { get; set; }

        /// <summary>
        /// List of individual rules
        /// </summary>
        [JsonProperty("rules")]
        public List<FrontendRule> Rules { get; set; }
    }

    /// <summary>
    /// Frontend individual rule model
    /// Represents a single condition rule from the frontend
    /// </summary>
    public class FrontendRule
    {
        /// <summary>
        /// Source stage ID where the component is located
        /// </summary>
        [JsonProperty("sourceStageId")]
        public string SourceStageId { get; set; }

        /// <summary>
        /// Component type: "checklist", "questionnaire", "field", "attachment"
        /// </summary>
        [JsonProperty("componentType")]
        public string ComponentType { get; set; }

        /// <summary>
        /// Component ID (checklist ID, questionnaire ID, etc.)
        /// </summary>
        [JsonProperty("componentId")]
        public string ComponentId { get; set; }

        /// <summary>
        /// Field path for accessing the value in input data
        /// Example: "input.checklist.tasks[\"123\"][\"456\"].isCompleted"
        /// </summary>
        [JsonProperty("fieldPath")]
        public string FieldPath { get; set; }

        /// <summary>
        /// Comparison operator: "==", "!=", ">", "<", ">=", "<=", "contains", etc.
        /// </summary>
        [JsonProperty("operator")]
        public string Operator { get; set; }

        /// <summary>
        /// Value to compare against
        /// </summary>
        [JsonProperty("value")]
        public object Value { get; set; }
    }
}
