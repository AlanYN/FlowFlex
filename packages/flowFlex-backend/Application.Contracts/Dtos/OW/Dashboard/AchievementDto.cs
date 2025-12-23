using System;
using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.OW.Dashboard
{
    /// <summary>
    /// Achievement/milestone item for dashboard
    /// </summary>
    public class AchievementDto
    {
        /// <summary>
        /// Achievement ID (onboarding ID or milestone ID)
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Achievement title (e.g., "TechSolutions Inc. case completed")
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Achievement description (e.g., "Successfully completed all case stages in 42 days")
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Completion date
        /// </summary>
        public DateTimeOffset CompletionDate { get; set; }

        /// <summary>
        /// Completion date display text (e.g., "May 15, 2023")
        /// </summary>
        public string CompletionDateDisplay { get; set; } = string.Empty;

        /// <summary>
        /// Associated team(s)
        /// </summary>
        public List<string> Teams { get; set; } = new List<string>();

        /// <summary>
        /// Achievement type
        /// - CaseCompleted: Case fully completed
        /// - StageCompleted: Important stage milestone completed
        /// - MilestoneSigned: Document signed (e.g., MSA)
        /// - SystemIntegration: System integration completed
        /// </summary>
        public string Type { get; set; } = "CaseCompleted";

        /// <summary>
        /// Related case code
        /// </summary>
        public string? CaseCode { get; set; }

        /// <summary>
        /// Related case name
        /// </summary>
        public string? CaseName { get; set; }

        /// <summary>
        /// Days to complete (for case completion achievements)
        /// </summary>
        public int? DaysToComplete { get; set; }

        /// <summary>
        /// Stage ID (for stage completion achievements)
        /// </summary>
        public long? StageId { get; set; }

        /// <summary>
        /// Stage name (for stage completion achievements)
        /// </summary>
        public string? StageName { get; set; }

        /// <summary>
        /// Stage order (for stage completion achievements)
        /// </summary>
        public int? StageOrder { get; set; }

        /// <summary>
        /// Onboarding ID (for stage completion achievements)
        /// </summary>
        public long? OnboardingId { get; set; }

        /// <summary>
        /// Completed by user name
        /// </summary>
        public string? CompletedBy { get; set; }
    }
}
