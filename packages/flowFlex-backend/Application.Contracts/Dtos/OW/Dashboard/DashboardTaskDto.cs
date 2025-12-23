using System;

namespace FlowFlex.Application.Contracts.Dtos.OW.Dashboard
{
    /// <summary>
    /// Dashboard task item for to-do list
    /// </summary>
    public class DashboardTaskDto
    {
        /// <summary>
        /// Task ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Task name/title
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Task description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Priority (Low, Medium, High, Critical)
        /// </summary>
        public string Priority { get; set; } = "Medium";

        /// <summary>
        /// Due date
        /// </summary>
        public DateTimeOffset? DueDate { get; set; }

        /// <summary>
        /// Is task overdue
        /// </summary>
        public bool IsOverdue { get; set; }

        /// <summary>
        /// Days until due (negative if overdue)
        /// </summary>
        public int? DaysUntilDue { get; set; }

        /// <summary>
        /// Due date display text (e.g., "Due: Today", "Due: Tomorrow", "Due in 2 days")
        /// </summary>
        public string? DueDateDisplay { get; set; }

        /// <summary>
        /// Associated case code
        /// </summary>
        public string CaseCode { get; set; } = string.Empty;

        /// <summary>
        /// Associated case name (lead name)
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
        /// Assigned team
        /// </summary>
        public string? AssignedTeam { get; set; }

        /// <summary>
        /// Assignee name
        /// </summary>
        public string? AssigneeName { get; set; }

        /// <summary>
        /// Task category (Sales, Account, Other)
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Task status (Pending, InProgress, Completed, Blocked, Cancelled)
        /// </summary>
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Is task required
        /// </summary>
        public bool IsRequired { get; set; }
    }
}
