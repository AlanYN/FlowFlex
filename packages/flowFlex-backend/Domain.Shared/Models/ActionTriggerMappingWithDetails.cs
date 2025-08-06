namespace FlowFlex.Domain.Shared.Models
{
    /// <summary>
    /// Action trigger mapping with related entity details
    /// </summary>
    public class ActionTriggerMappingWithDetails
    {
        /// <summary>
        /// Mapping ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Action definition ID
        /// </summary>
        public long ActionDefinitionId { get; set; }

        /// <summary>
        /// Trigger type (Stage, Task, Question)
        /// </summary>
        public string TriggerType { get; set; } = string.Empty;

        /// <summary>
        /// Trigger source ID
        /// </summary>
        public long TriggerSourceId { get; set; }

        /// <summary>
        /// Trigger source name
        /// </summary>
        public string TriggerSourceName { get; set; } = string.Empty;

        /// <summary>
        /// Workflow ID
        /// </summary>
        public long? WorkFlowId { get; set; }

        /// <summary>
        /// Workflow name
        /// </summary>
        public string WorkFlowName { get; set; } = string.Empty;

        /// <summary>
        /// Stage ID
        /// </summary>
        public long? StageId { get; set; }

        /// <summary>
        /// Stage name
        /// </summary>
        public string StageName { get; set; } = string.Empty;

        /// <summary>
        /// Trigger event
        /// </summary>
        public string TriggerEvent { get; set; } = string.Empty;

        /// <summary>
        /// Whether the mapping is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Execution order
        /// </summary>
        public int ExecutionOrder { get; set; } = 0;

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }
}