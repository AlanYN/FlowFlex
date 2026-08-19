using System;

namespace FlowFlex.Application.Contracts.Dtos.OW.Gantt
{
    /// <summary>
    /// Case-level summary displayed in the Gantt chart header (Req 6.2)
    /// </summary>
    public class GanttCaseSummaryDto
    {
        /// <summary>
        /// Onboarding/Case ID (string to preserve JS snowflake precision)
        /// </summary>
        public string OnboardingId { get; set; }

        /// <summary>
        /// Case / lead name
        /// </summary>
        public string CaseName { get; set; }

        /// <summary>
        /// Case code (e.g. "CASE-0001")
        /// </summary>
        public string CaseCode { get; set; }

        /// <summary>
        /// Name of the associated workflow
        /// </summary>
        public string WorkflowName { get; set; }

        /// <summary>
        /// Current case status
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Case priority
        /// </summary>
        public string Priority { get; set; }

        // ── Timeline dates (ISO 8601 via serialiser) ──────────────────────────

        /// <summary>
        /// Planned start date — set at Case start, never mutated afterwards
        /// </summary>
        public DateTimeOffset? PlannedStartDate { get; set; }

        /// <summary>
        /// Planned end date — computed from the last stage's plannedEndDate
        /// </summary>
        public DateTimeOffset? PlannedEndDate { get; set; }

        /// <summary>
        /// Projected end date — current forecast based on latest stage progress
        /// </summary>
        public DateTimeOffset? ProjectedEndDate { get; set; }

        /// <summary>
        /// Actual start date of the case
        /// </summary>
        public DateTimeOffset? ActualStartDate { get; set; }

        /// <summary>
        /// Actual end date of the case (null while in progress)
        /// </summary>
        public DateTimeOffset? ActualEndDate { get; set; }

        // ── Aggregate statistics ──────────────────────────────────────────────

        /// <summary>
        /// Weighted average completion percentage across all stages (0–100)
        /// </summary>
        public decimal OverallCompletionPercentage { get; set; }

        /// <summary>
        /// Total number of stages in the workflow
        /// </summary>
        public int TotalStages { get; set; }

        /// <summary>
        /// Number of completed stages
        /// </summary>
        public int CompletedStages { get; set; }

        /// <summary>
        /// Number of overdue stages (started but past plannedEndDate)
        /// </summary>
        public int OverdueStages { get; set; }

        /// <summary>
        /// Number of delayed stages (not started but past plannedStartDate)
        /// </summary>
        public int DelayedStages { get; set; }

        /// <summary>
        /// Number of currently blocked stages
        /// </summary>
        public int BlockedStages { get; set; }

        /// <summary>
        /// Name of the current active stage
        /// </summary>
        public string CurrentStageName { get; set; }

        /// <summary>
        /// Order index of the current active stage
        /// </summary>
        public int CurrentStageOrder { get; set; }
    }
}
