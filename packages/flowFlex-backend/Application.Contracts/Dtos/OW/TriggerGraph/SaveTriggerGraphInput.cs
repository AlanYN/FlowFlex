using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.TriggerGraph
{
    /// <summary>
    /// Input for creating or updating a trigger graph (full-replace save).
    /// All connections are replaced atomically.
    /// </summary>
    public class SaveTriggerGraphInput
    {
        /// <summary>Owner workflow ID</summary>
        [Required]
        public long WorkflowId { get; set; }

        /// <summary>Graph name (optional, defaults to workflow name)</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Card positions serialised as JSON.
        /// Format: {"workflowId": {"x": 100, "y": 200}, ...}
        /// </summary>
        public string CanvasLayout { get; set; } = "{}";

        /// <summary>All workflow IDs currently on the canvas</summary>
        public string CanvasWorkflowIds { get; set; } = "[]";

        /// <summary>All connections to persist (replaces existing ones)</summary>
        public List<TriggerConnectionDto> Connections { get; set; } = new();
    }
}
