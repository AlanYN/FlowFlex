using System;
using MediatR;

namespace FlowFlex.Domain.Shared.Events
{
    /// <summary>
    /// Published when a Case transitions from Inactive to Active via the /start endpoint.
    /// The GanttPlannedTimeInitHandler listens to this event to write plannedStartDate
    /// and plannedEndDate for all Stages in the same transactional context.
    /// </summary>
    public class OnboardingStartedEvent : INotification
    {
        /// <summary>
        /// Event ID
        /// </summary>
        public string EventId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Event timestamp
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Onboarding (Case) ID
        /// </summary>
        public long OnboardingId { get; set; }

        /// <summary>
        /// The date the Case was started — used as plannedStartDate for Stage 1
        /// </summary>
        public DateTimeOffset StartDate { get; set; }

        /// <summary>
        /// Optional ETA for the Case — used as fallback for distributing planned durations
        /// when individual Stage EstimatedDuration values are absent
        /// </summary>
        public DateTimeOffset? EstimatedCompletionDate { get; set; }

        /// <summary>
        /// Tenant ID
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// ID of the user who triggered the start action
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// Name of the user who triggered the start action
        /// </summary>
        public string UserName { get; set; }
    }
}
