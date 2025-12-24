using System;

namespace FlowFlex.Application.Contracts.Dtos.OW.Dashboard
{
    /// <summary>
    /// Upcoming deadline item for dashboard
    /// </summary>
    public class DeadlineDto
    {
        /// <summary>
        /// Task/Milestone ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Task/Milestone name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Due date
        /// </summary>
        public DateTimeOffset DueDate { get; set; }

        /// <summary>
        /// Due date display text (e.g., "Due in 2 days", "Due tomorrow")
        /// </summary>
        public string DueDateDisplay { get; set; } = string.Empty;

        /// <summary>
        /// Urgency category:
        /// - overdue: Past due date
        /// - today: Due today
        /// - tomorrow: Due tomorrow
        /// - thisWeek: Due within this week
        /// - upcoming: Due later
        /// </summary>
        public string Urgency { get; set; } = "upcoming";

        /// <summary>
        /// Days until due (negative if overdue)
        /// </summary>
        public int DaysUntilDue { get; set; }

        /// <summary>
        /// Associated case code
        /// </summary>
        public string CaseCode { get; set; } = string.Empty;

        /// <summary>
        /// Associated case name
        /// </summary>
        public string CaseName { get; set; } = string.Empty;

        /// <summary>
        /// Associated onboarding ID
        /// </summary>
        public long OnboardingId { get; set; }

        /// <summary>
        /// Associated checklist ID
        /// </summary>
        public long ChecklistId { get; set; }

        /// <summary>
        /// Deadline type:
        /// - Task: Checklist task deadline
        /// - Milestone: Stage milestone deadline
        /// - StageEstimate: Stage estimated completion
        /// </summary>
        public string Type { get; set; } = "Task";

        /// <summary>
        /// Priority (Low, Medium, High, Critical)
        /// </summary>
        public string? Priority { get; set; }

        /// <summary>
        /// Assigned team
        /// </summary>
        public string? AssignedTeam { get; set; }

        /// <summary>
        /// Stage ID (for Stage type deadlines)
        /// </summary>
        public long? StageId { get; set; }

        /// <summary>
        /// Stage name (for Stage type deadlines)
        /// </summary>
        public string? StageName { get; set; }

        /// <summary>
        /// Stage order (for Stage type deadlines)
        /// </summary>
        public int? StageOrder { get; set; }
    }
}
