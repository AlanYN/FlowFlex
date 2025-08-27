using MediatR;

namespace FlowFlex.Domain.Shared.Events.Action
{
    /// <summary>
    /// Unified action trigger event for all business scenarios
    /// </summary>
    public class ActionTriggerEvent : INotification
    {
        /// <summary>
        /// Trigger source type (Stage, Task, Question, etc.)
        /// </summary>
        public string TriggerSourceType { get; set; }

        /// <summary>
        /// Trigger source ID (StageId, TaskId, QuestionId, etc.)
        /// </summary>
        public long TriggerSourceId { get; set; }

        /// <summary>
        /// Trigger event type (Completed, Created, Updated, etc.)
        /// </summary>
        public string TriggerEventType { get; set; }

        /// <summary>
        /// Additional context data for action execution
        /// </summary>
        public object ContextData { get; set; }

        /// <summary>
        /// User ID who triggered the event
        /// </summary>
        public long? UserId { get; set; }

        public long? WorkflowId { get; set; }

        public long? StageId { get; set; }

        /// <summary>
        /// Timestamp when the event occurred
        /// </summary>
        public DateTime TriggeredAt { get; set; }

        public ActionTriggerEvent(
            string triggerSourceType,
            long triggerSourceId,
            string triggerEventType,
            object contextData = null,
            long? userId = null,
            long? workflowId = null,
            long? stageId = null)
        {
            TriggerSourceType = triggerSourceType;
            TriggerSourceId = triggerSourceId;
            TriggerEventType = triggerEventType;
            ContextData = contextData;
            UserId = userId;
            TriggeredAt = DateTime.UtcNow;
            WorkflowId = workflowId;
            StageId = stageId;
        }
    }
}