using System.ComponentModel.DataAnnotations;
using FlowFlex.Domain.Entities.Base;
using FlowFlex.Domain.Shared.JsonConverters;
using Newtonsoft.Json;
using SqlSugar;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// Workflow Trigger Connection — a directed edge in the trigger graph.
    /// Records the trigger conditions and data-mapping config between two workflows.
    /// </summary>
    [SugarTable("ff_workflow_trigger_connection")]
    public class WorkflowTriggerConnection : EntityBaseCreateInfo
    {
        /// <summary>
        /// Owner graph ID
        /// </summary>
        [SugarColumn(ColumnName = "graph_id")]
        [JsonConverter(typeof(LongToStringConverter))]
        public long GraphId { get; set; }

        /// <summary>
        /// Source workflow ID (trigger fires from here)
        /// </summary>
        [SugarColumn(ColumnName = "source_workflow_id")]
        [JsonConverter(typeof(LongToStringConverter))]
        public long SourceWorkflowId { get; set; }

        /// <summary>
        /// Target workflow ID (trigger starts this workflow)
        /// </summary>
        [SugarColumn(ColumnName = "target_workflow_id")]
        [JsonConverter(typeof(LongToStringConverter))]
        public long TargetWorkflowId { get; set; }

        /// <summary>
        /// User-defined name/label for this connection rule
        /// </summary>
        [StringLength(200)]
        [SugarColumn(ColumnName = "rule_name")]
        public string RuleName { get; set; } = string.Empty;

        /// <summary>
        /// Short human-readable summary of the trigger condition (for canvas label)
        /// </summary>
        [StringLength(500)]
        [SugarColumn(ColumnName = "condition_summary")]
        public string ConditionSummary { get; set; } = string.Empty;

        /// <summary>
        /// Full trigger configuration JSON — contains:
        ///   - conditions: TriggerCondition[]
        ///   - mappings: DataMapping[]
        ///   - autoMap: bool
        /// Stored as jsonb for flexible future extension.
        /// </summary>
        [SugarColumn(ColumnName = "config_json", ColumnDataType = "jsonb", IsJson = true)]
        public string ConfigJson { get; set; } = "{}";

        /// <summary>
        /// Whether this connection is active
        /// </summary>
        [SugarColumn(ColumnName = "is_enabled")]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Execution order when multiple connections fire simultaneously
        /// </summary>
        [SugarColumn(ColumnName = "execution_order")]
        public int ExecutionOrder { get; set; } = 0;
    }
}
