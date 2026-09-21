namespace FlowFlex.Application.Contracts.Dtos.OW.TriggerGraph
{
    /// <summary>
    /// A directed edge (trigger connection) in the graph — used for both input and output
    /// </summary>
    public class TriggerConnectionDto
    {
        /// <summary>Primary key (0 = new)</summary>
        public long Id { get; set; }

        /// <summary>Owner graph ID</summary>
        public long GraphId { get; set; }

        /// <summary>Source workflow ID (where the trigger fires)</summary>
        public long SourceWorkflowId { get; set; }

        /// <summary>Target workflow ID (what gets triggered)</summary>
        public long TargetWorkflowId { get; set; }

        /// <summary>User-defined rule name</summary>
        public string RuleName { get; set; } = string.Empty;

        /// <summary>Short label shown on canvas edge</summary>
        public string ConditionSummary { get; set; } = string.Empty;

        /// <summary>Full config JSON (conditions + mappings + autoMap)</summary>
        public string ConfigJson { get; set; } = "{}";

        /// <summary>Whether this connection is active</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>Execution order</summary>
        public int ExecutionOrder { get; set; } = 0;
    }
}
