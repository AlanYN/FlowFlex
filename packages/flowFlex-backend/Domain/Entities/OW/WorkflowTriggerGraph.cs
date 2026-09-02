using System.ComponentModel.DataAnnotations;
using FlowFlex.Domain.Entities.Base;
using SqlSugar;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// Workflow Trigger Graph — represents the trigger graph for a given workflow (source node).
    /// Each workflow can have at most one graph; the graph holds all outbound trigger connections.
    /// </summary>
    [SugarTable("ff_workflow_trigger_graph")]
    public class WorkflowTriggerGraph : EntityBaseCreateInfo
    {
        /// <summary>
        /// The workflow this graph belongs to (source/owner workflow)
        /// </summary>
        [SugarColumn(ColumnName = "workflow_id")]
        public long WorkflowId { get; set; }

        /// <summary>
        /// Human-readable name for this graph (defaults to workflow name)
        /// </summary>
        [StringLength(200)]
        [SugarColumn(ColumnName = "name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Serialised canvas layout (card positions keyed by workflowId).
        /// Format: {"workflowId": {"x": 100, "y": 200}, ...}
        /// </summary>
        [SugarColumn(ColumnName = "canvas_layout", ColumnDataType = "jsonb", IsJson = true)]
        public string CanvasLayout { get; set; } = "{}";

        /// <summary>
        /// All workflow IDs that have been added to this canvas (besides the owner)
        /// </summary>
        [SugarColumn(ColumnName = "canvas_workflow_ids", ColumnDataType = "jsonb", IsJson = true)]
        public string CanvasWorkflowIds { get; set; } = "[]";
    }
}
