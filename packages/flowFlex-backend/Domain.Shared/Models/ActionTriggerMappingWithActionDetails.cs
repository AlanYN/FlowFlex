namespace FlowFlex.Domain.Shared.Models
{
    /// <summary>
    /// Action trigger mapping with related entity details and action information
    /// </summary>
    public class ActionTriggerMappingWithActionDetails
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
        /// Action code
        /// </summary>
        public string ActionCode { get; set; } = string.Empty;

        /// <summary>
        /// Action name
        /// </summary>
        public string ActionName { get; set; } = string.Empty;

        /// <summary>
        /// Action type (Python, HttpApi, SendEmail)
        /// </summary>
        public string ActionType { get; set; } = string.Empty;

        /// <summary>
        /// Action description
        /// </summary>
        public string ActionDescription { get; set; } = string.Empty;

        /// <summary>
        /// Whether the action is enabled
        /// </summary>
        public bool ActionIsEnabled { get; set; } = true;

        /// <summary>
        /// Trigger type (Stage, Task, Question)
        /// </summary>
        public string TriggerType { get; set; } = string.Empty;

        /// <summary>
        /// Trigger source ID
        /// </summary>
        public long TriggerSourceId { get; set; }

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

        /// <summary>
        /// Last applied
        /// </summary>
        public DateTimeOffset? LastApplied { get; set; }
    }
}