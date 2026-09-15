using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.OW.TriggerGraph
{
    /// <summary>
    /// Full trigger graph output — graph metadata + all connections
    /// </summary>
    public class TriggerGraphDto
    {
        /// <summary>Graph primary key</summary>
        public long Id { get; set; }

        /// <summary>Owner workflow ID</summary>
        public long WorkflowId { get; set; }

        /// <summary>Graph name</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Card positions on canvas.
        /// Serialised JSON: {"workflowId": {"x": 100, "y": 200}, ...}
        /// </summary>
        public string CanvasLayout { get; set; } = "{}";

        /// <summary>All workflow IDs on the canvas (besides owner)</summary>
        public string CanvasWorkflowIds { get; set; } = "[]";

        /// <summary>All connections in this graph</summary>
        public List<TriggerConnectionDto> Connections { get; set; } = new();
    }
}
