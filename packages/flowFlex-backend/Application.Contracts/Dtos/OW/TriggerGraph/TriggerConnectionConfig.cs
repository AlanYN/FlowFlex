using System.Collections.Generic;
using Newtonsoft.Json;

namespace FlowFlex.Application.Contracts.Dtos.OW.TriggerGraph
{
    /// <summary>
    /// Deserialized representation of WorkflowTriggerConnection.ConfigJson.
    /// This mirrors the structure saved by the frontend ConnectionPanel.
    /// </summary>
    public class TriggerConnectionConfig
    {
        /// <summary>Trigger conditions (AND/OR logic)</summary>
        [JsonProperty("conditions")]
        public List<TriggerConditionConfig> Conditions { get; set; } = new();

        /// <summary>Manual field mappings (Dynamic field / Questionnaire / Static)</summary>
        [JsonProperty("mappings")]
        public List<TriggerDataMappingConfig> Mappings { get; set; } = new();

        /// <summary>Whether auto-map dynamic fields is enabled</summary>
        [JsonProperty("autoMap")]
        public bool AutoMap { get; set; } = true;

        /// <summary>Auto-map matching states (enabled flag per target field)</summary>
        [JsonProperty("autoMappedStates")]
        public List<AutoMappedStateConfig> AutoMappedStates { get; set; } = new();

        /// <summary>Case Info field mapping states (CaseName / ContactEmail etc.)</summary>
        [JsonProperty("caseInfoStates")]
        public List<CaseInfoStateConfig> CaseInfoStates { get; set; } = new();
    }

    /// <summary>
    /// A single trigger condition rule.
    /// Mirrors the ConditionRow interface saved by ConnectionPanel.
    /// </summary>
    public class TriggerConditionConfig
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>"AND" or "OR"</summary>
        [JsonProperty("logic")]
        public string Logic { get; set; } = "AND";

        [JsonProperty("stageId")]
        public string StageId { get; set; }

        [JsonProperty("stageName")]
        public string StageName { get; set; }

        /// <summary>
        /// componentKey format: "field_{fieldId}" / "checklist_{id}" / "questionnaire_{id}"
        /// </summary>
        [JsonProperty("componentKey")]
        public string ComponentKey { get; set; }

        /// <summary>"fields" / "checklist" / "questionnaires"</summary>
        [JsonProperty("componentType")]
        public string ComponentType { get; set; }

        [JsonProperty("componentId")]
        public string ComponentId { get; set; }

        [JsonProperty("componentName")]
        public string ComponentName { get; set; }

        /// <summary>
        /// For checklist tasks and questionnaire questions: the selected resource ID
        /// </summary>
        [JsonProperty("resourceId")]
        public string ResourceId { get; set; }

        [JsonProperty("resourceName")]
        public string ResourceName { get; set; }

        /// <summary>Comparison operator: "==" / "!=" / "&gt;" / "&gt;=" / "&lt;" / "&lt;=" / "contains" / "CompleteTask" / "AllCompleted"</summary>
        [JsonProperty("operator")]
        public string Operator { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }

    /// <summary>
    /// A single manual data mapping entry (FIELD MAPPINGS section).
    /// sourceId uses fieldPath format: "input.fields.{id}" or questionnaire path.
    /// </summary>
    public class TriggerDataMappingConfig
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>"dynamic_field" / "questionnaire" / "static"</summary>
        [JsonProperty("sourceType")]
        public string SourceType { get; set; }

        /// <summary>fieldPath of the source field, e.g. "input.fields.{fieldId}"</summary>
        [JsonProperty("sourceId")]
        public string SourceId { get; set; }

        [JsonProperty("sourceName")]
        public string SourceName { get; set; }

        /// <summary>fieldPath of the target field</summary>
        [JsonProperty("targetFieldId")]
        public string TargetFieldId { get; set; }

        [JsonProperty("targetFieldName")]
        public string TargetFieldName { get; set; }

        /// <summary>Used when sourceType == "static"</summary>
        [JsonProperty("staticValue")]
        public string StaticValue { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;
    }

    /// <summary>Auto-map enabled state for a single dynamic field pair.</summary>
    public class AutoMappedStateConfig
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        /// <summary>Source field id (e.g. "input.fields.{fieldId}" or "case.contactEmail")</summary>
        [JsonProperty("sourceId")]
        public string? SourceId { get; set; }

        [JsonProperty("sourceName")]
        public string? SourceName { get; set; }
    }

    /// <summary>
    /// Case Info field mapping state.
    /// sourceId uses "case.{fieldKey}" format (e.g. "case.caseName").
    /// targetId uses "case_info_{fieldKey}" format (e.g. "case_info_caseName").
    /// </summary>
    public class CaseInfoStateConfig
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        /// <summary>"case.caseName" / "case.contactEmail" etc.</summary>
        [JsonProperty("sourceId")]
        public string SourceId { get; set; }

        [JsonProperty("sourceName")]
        public string SourceName { get; set; }
    }
}
