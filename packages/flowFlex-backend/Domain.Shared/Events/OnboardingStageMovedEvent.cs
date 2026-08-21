using MediatR;

namespace FlowFlex.Domain.Shared.Events
{
    /// <summary>
    /// Event published when a Case's current stage is moved to a different stage (move-to-stage operation).
    /// Consumed by GanttProjectedTimeRecalcHandler to recalculate Projected times.
    /// </summary>
    public class OnboardingStageMovedEvent : INotification
    {
        /// <summary>
        /// Onboarding ID
        /// </summary>
        public long OnboardingId { get; set; }

        /// <summary>
        /// The stage being moved away from
        /// </summary>
        public long FromStageId { get; set; }

        /// <summary>
        /// The stage being moved to
        /// </summary>
        public long ToStageId { get; set; }

        /// <summary>
        /// Tenant ID
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// User ID who triggered the move
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// User name who triggered the move
        /// </summary>
        public string UserName { get; set; }
    }
}
