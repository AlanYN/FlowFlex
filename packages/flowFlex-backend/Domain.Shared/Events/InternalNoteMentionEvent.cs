using System;
using MediatR;

namespace FlowFlex.Domain.Shared.Events
{
    /// <summary>
    /// Internal note @mention event, published when a note containing mentions is created or updated
    /// </summary>
    public class InternalNoteMentionEvent : INotification
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
        /// Tenant ID
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Internal note ID
        /// </summary>
        public long NoteId { get; set; }

        /// <summary>
        /// Current note content (contains mention markers)
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Previous note content (null for new notes, used for diff calculation on edits)
        /// </summary>
        public string PreviousContent { get; set; }

        /// <summary>
        /// Associated onboarding ID
        /// </summary>
        public long OnboardingId { get; set; }

        /// <summary>
        /// Name of the user who triggered the mention
        /// </summary>
        public string SenderName { get; set; }

        /// <summary>
        /// Onboarding case name
        /// </summary>
        public string CaseName { get; set; }

        /// <summary>
        /// Onboarding case code
        /// </summary>
        public string CaseCode { get; set; }

        /// <summary>
        /// Current stage name
        /// </summary>
        public string StageName { get; set; }
    }
}
