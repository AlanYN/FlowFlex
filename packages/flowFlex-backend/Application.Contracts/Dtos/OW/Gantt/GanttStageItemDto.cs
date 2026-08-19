using System;
using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.OW.Gantt
{
    /// <summary>
    /// Per-stage data item returned in the Gantt chart response (Req 6.3)
    /// </summary>
    public class GanttStageItemDto
    {
        /// <summary>
        /// Stage ID (string to preserve JS snowflake precision)
        /// </summary>
        public string StageId { get; set; }

        /// <summary>
        /// Stage display name
        /// </summary>
        public string StageName { get; set; }

        /// <summary>
        /// Order index within the workflow (1-based, ascending)
        /// </summary>
        public int StageOrder { get; set; }

        /// <summary>
        /// Hex color assigned to the stage (e.g. "#1890FF")
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// Whether the stage is required to complete the workflow
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Gantt-chart-specific status. One of: NotStarted | Delayed | InProgress | Overdue | Completed.
        /// Derived at query time; independent of StageProgress.Status.
        /// </summary>
        public string GanttStatus { get; set; }

        /// <summary>
        /// Whether the stage is currently blocked
        /// </summary>
        public bool IsBlocked { get; set; }

        /// <summary>
        /// Primary assignees of the stage
        /// </summary>
        public List<GanttAssigneeDto> Assignee { get; set; }

        /// <summary>
        /// Co-assignees of the stage
        /// </summary>
        public List<GanttAssigneeDto> CoAssignees { get; set; }

        // ── Three sets of dates (ISO 8601 via serialiser) ────────────────────

        /// <summary>
        /// Planned start date — fixed at Case start
        /// </summary>
        public DateTimeOffset? PlannedStartDate { get; set; }

        /// <summary>
        /// Planned end date — fixed at Case start
        /// </summary>
        public DateTimeOffset? PlannedEndDate { get; set; }

        /// <summary>
        /// Projected start date — recalculated on each stage advance; null when blocked
        /// </summary>
        public DateTimeOffset? ProjectedStartDate { get; set; }

        /// <summary>
        /// Projected end date — recalculated on each stage advance; null when blocked
        /// </summary>
        public DateTimeOffset? ProjectedEndDate { get; set; }

        /// <summary>
        /// Actual start date (maps to StageProgress.StartTime)
        /// </summary>
        public DateTimeOffset? ActualStartDate { get; set; }

        /// <summary>
        /// Actual end date (maps to StageProgress.CompletionTime)
        /// </summary>
        public DateTimeOffset? ActualEndDate { get; set; }

        // ── Duration & progress ──────────────────────────────────────────────

        /// <summary>
        /// Estimated duration in days (Math.Round of Stage.EstimatedDuration)
        /// </summary>
        public int EstimatedDurationDays { get; set; }

        /// <summary>
        /// Weighted completion percentage (0–100), computed at query time
        /// </summary>
        public decimal CompletionPercentage { get; set; }

        /// <summary>
        /// Calendar days elapsed since actualStartDate (null if not started)
        /// </summary>
        public int? DaysElapsed { get; set; }

        // ── Variance analysis (populated for Completed stages only) ──────────

        /// <summary>
        /// Inherited delay days = actualStartDate - plannedStartDate (positive = late start)
        /// </summary>
        public int? InheritedDelayDays { get; set; }

        /// <summary>
        /// Own variance days = actualDuration - estimatedDuration (positive = took longer)
        /// </summary>
        public int? OwnVarianceDays { get; set; }

        /// <summary>
        /// Total variance days = actualEndDate - plannedEndDate (positive = late finish)
        /// </summary>
        public int? TotalVarianceDays { get; set; }

        // ── Blocker fields ───────────────────────────────────────────────────

        /// <summary>
        /// Total calendar days the stage has been blocked (cumulative across all blocker records)
        /// </summary>
        public int BlockedDays { get; set; }

        /// <summary>
        /// Reason for the current active blocker (null when not blocked)
        /// </summary>
        public string BlockReason { get; set; }

        /// <summary>
        /// Expected resolution date for the current blocker (null when not blocked)
        /// </summary>
        public DateTimeOffset? ExpectedResolutionDate { get; set; }

        // ── Component completion summary ──────────────────────────────────────

        /// <summary>
        /// Completion statistics for checklists, questionnaires, fields and files
        /// </summary>
        public GanttComponentsDto Components { get; set; }

        // ── Audit ────────────────────────────────────────────────────────────

        /// <summary>
        /// Name of the user who last saved this stage
        /// </summary>
        public string LastSavedBy { get; set; }

        /// <summary>
        /// Timestamp of the last save for this stage
        /// </summary>
        public DateTimeOffset? LastSavedAt { get; set; }
    }
}
