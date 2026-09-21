using System.ComponentModel.DataAnnotations;
using FlowFlex.Domain.Entities.Base;
using Newtonsoft.Json;
using SqlSugar;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// Records the outcome of each trigger evaluation when a Case completes.
    /// Corresponds to table ff_workflow_trigger_log.
    /// </summary>
    [SugarTable("ff_workflow_trigger_log")]
    public class WorkflowTriggerLog : EntityBaseCreateInfo
    {
        /// <summary>The connection that was evaluated</summary>
        [SugarColumn(ColumnName = "connection_id")]
        public long ConnectionId { get; set; }

        /// <summary>The source Workflow</summary>
        [SugarColumn(ColumnName = "source_workflow_id")]
        public long SourceWorkflowId { get; set; }

        /// <summary>The target Workflow</summary>
        [SugarColumn(ColumnName = "target_workflow_id")]
        public long TargetWorkflowId { get; set; }

        /// <summary>The source Case that completed</summary>
        [SugarColumn(ColumnName = "source_onboarding_id")]
        public long SourceOnboardingId { get; set; }

        /// <summary>The newly created target Case (null when Skipped/Failed)</summary>
        [SugarColumn(ColumnName = "target_onboarding_id")]
        public long? TargetOnboardingId { get; set; }

        /// <summary>
        /// Trigger result: "Triggered" / "Skipped" / "Failed"
        /// </summary>
        [StringLength(20)]
        [SugarColumn(ColumnName = "status")]
        public string Status { get; set; } = "Pending";

        /// <summary>Human-readable reason (for Skipped: condition details; for Failed: error message)</summary>
        [StringLength(1000)]
        [SugarColumn(ColumnName = "reason")]
        public string Reason { get; set; } = string.Empty;

        /// <summary>How the source case finished: "Completed" / "ForceCompleted"</summary>
        [StringLength(30)]
        [SugarColumn(ColumnName = "completion_type")]
        public string CompletionType { get; set; } = "Completed";

        /// <summary>Snapshot of evaluated conditions (for debugging)</summary>
        [SugarColumn(ColumnName = "conditions_snapshot", ColumnDataType = "jsonb", IsJson = true)]
        public string ConditionsSnapshot { get; set; } = "[]";

        /// <summary>Snapshot of applied mappings</summary>
        [SugarColumn(ColumnName = "mappings_snapshot", ColumnDataType = "jsonb", IsJson = true)]
        public string MappingsSnapshot { get; set; } = "[]";
    }
}
