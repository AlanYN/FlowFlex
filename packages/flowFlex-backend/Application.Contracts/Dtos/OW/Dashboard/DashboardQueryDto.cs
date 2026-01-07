using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.OW.Dashboard
{
    /// <summary>
    /// Dashboard query parameters
    /// </summary>
    public class DashboardQueryDto
    {
        /// <summary>
        /// Modules to include (statistics, stageDistribution, tasks, messages, achievements, deadlines)
        /// If empty or null, returns all modules
        /// </summary>
        public List<string>? Modules { get; set; }

        /// <summary>
        /// Team filter for statistics and achievements
        /// </summary>
        public string? Team { get; set; }

        /// <summary>
        /// Workflow ID filter for stage distribution
        /// </summary>
        public long? WorkflowId { get; set; }

        /// <summary>
        /// Days range for deadlines (default: 7)
        /// </summary>
        public int DeadlineDays { get; set; } = 7;

        /// <summary>
        /// Limit for messages (default: 5)
        /// </summary>
        public int MessageLimit { get; set; } = 5;

        /// <summary>
        /// Limit for achievements (default: 5)
        /// </summary>
        public int AchievementLimit { get; set; } = 5;

        /// <summary>
        /// Task category filter (All, Sales, Account, Other)
        /// </summary>
        public string? TaskCategory { get; set; }

        /// <summary>
        /// Task page index (default: 1)
        /// </summary>
        public int TaskPageIndex { get; set; } = 1;

        /// <summary>
        /// Task page size (default: 10)
        /// </summary>
        public int TaskPageSize { get; set; } = 10;
    }

    /// <summary>
    /// Task query parameters for standalone task endpoint
    /// </summary>
    public class DashboardTaskQueryDto
    {
        /// <summary>
        /// Category filter (All, Sales, Account, Other)
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Page index (default: 1)
        /// </summary>
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// Page size (default: 10)
        /// </summary>
        public int PageSize { get; set; } = 10;
    }
}
