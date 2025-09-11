using MediatR;

namespace FlowFlex.Domain.Shared.Events
{
    /// <summary>
    /// Event to request stage completion (avoids circular dependency)
    /// </summary>
    public class CompleteStageRequestEvent : INotification
    {
        public long OnboardingId { get; set; }
        public long? StageId { get; set; }
        public string CompletionNotes { get; set; } = string.Empty;
        public string CompletedBy { get; set; } = string.Empty;
        public bool AutoMoveToNext { get; set; } = true;
        public string Source { get; set; } = string.Empty;
        public long? TriggerTaskId { get; set; }
        public string TriggerTaskName { get; set; } = string.Empty;
        public long? TriggerActionId { get; set; }
        public long? UserId { get; set; }

        public CompleteStageRequestEvent(
            long onboardingId,
            long? stageId,
            string completionNotes,
            string completedBy,
            bool autoMoveToNext = true,
            string source = "",
            long? triggerTaskId = null,
            string triggerTaskName = "",
            long? triggerActionId = null,
            long? userId = null)
        {
            OnboardingId = onboardingId;
            StageId = stageId;
            CompletionNotes = completionNotes;
            CompletedBy = completedBy;
            AutoMoveToNext = autoMoveToNext;
            Source = source;
            TriggerTaskId = triggerTaskId;
            TriggerTaskName = triggerTaskName;
            TriggerActionId = triggerActionId;
            UserId = userId;
        }
    }
}
