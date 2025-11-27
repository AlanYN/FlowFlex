using System.ComponentModel.DataAnnotations;
using FlowFlex.Domain.Entities.Base;
using SqlSugar;
using Newtonsoft.Json;
using FlowFlex.Domain.Shared.JsonConverters;
using Newtonsoft.Json.Linq;

namespace FlowFlex.Domain.Entities.Action
{
    /// <summary>
    /// Action Trigger Mapping Entity - Maps Actions to trigger sources (Stage/Task/Question) for reusability
    /// </summary>
    [SugarTable("ff_action_trigger_mappings")]
    public class ActionTriggerMapping : EntityBaseCreateInfo
    {
        /// <summary>
        /// Associated ActionDefinition ID
        /// </summary>
        [Required]
        [SugarColumn(ColumnName = "action_definition_id")]
        [JsonConverter(typeof(LongToStringConverter))]
        public long ActionDefinitionId { get; set; }

        /// <summary>
        /// Trigger type (Stage, Task, Question)
        /// </summary>
        [Required]
        [StringLength(50)]
        [SugarColumn(ColumnName = "trigger_type")]
        public string TriggerType { get; set; } = string.Empty;

        /// <summary>
        /// Trigger source entity ID (StageId, TaskId, QuestionId)
        /// </summary>
        [Required]
        [SugarColumn(ColumnName = "trigger_source_id")]
        [JsonConverter(typeof(LongToStringConverter))]
        public long TriggerSourceId { get; set; }

        /// <summary>
        /// Work flow ID
        /// </summary>
        [SugarColumn(ColumnName = "work_flow_id")]
        [JsonConverter(typeof(LongToStringConverter))]
        public long? WorkFlowId { get; set; }

        /// <summary>
        /// Stage ID
        /// </summary>
        [SugarColumn(ColumnName = "stage_id")]
        [JsonConverter(typeof(LongToStringConverter))]
        public long? StageId { get; set; }

        /// <summary>
        /// Trigger event (Completed, Created, Updated, Answered)
        /// </summary>
        [Required]
        [StringLength(50)]
        [SugarColumn(ColumnName = "trigger_event")]
        public string TriggerEvent { get; set; } = "Completed";

        /// <summary>
        /// Whether this mapping is enabled
        /// </summary>
        [SugarColumn(ColumnName = "is_enabled")]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Execution order when multiple actions on same trigger (smaller number executes first)
        /// </summary>
        [SugarColumn(ColumnName = "execution_order")]
        public int ExecutionOrder { get; set; } = 0;

        /// <summary>
        /// Trigger conditions (JSON format) - defines when this mapping should be triggered
        /// </summary>
        [SugarColumn(ColumnName = "trigger_conditions", ColumnDataType = "jsonb", IsJson = true)]
        public JToken TriggerConditions { get; set; } = new JObject();

        /// <summary>
        /// Custom parameters for this specific mapping (JSON format)
        /// </summary>
        [SugarColumn(ColumnName = "mapping_config", ColumnDataType = "jsonb", IsJson = true)]
        public JToken MappingConfig { get; set; } = new JObject();

        /// <summary>
        /// Description of this mapping
        /// </summary>
        [StringLength(500)]
        [SugarColumn(ColumnName = "description")]
        public string Description { get; set; } = string.Empty;
    }
}