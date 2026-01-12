namespace FlowFlex.Application.Contracts.Dtos.OW.Dashboard
{
    /// <summary>
    /// Statistics overview with month-over-month comparison
    /// </summary>
    public class DashboardStatisticsDto
    {
        /// <summary>
        /// Active cases count with trend
        /// </summary>
        public StatisticItemDto ActiveCases { get; set; } = new StatisticItemDto();

        /// <summary>
        /// Completed this month count with trend
        /// </summary>
        public StatisticItemDto CompletedThisMonth { get; set; } = new StatisticItemDto();

        /// <summary>
        /// Overdue tasks count with trend
        /// </summary>
        public StatisticItemDto OverdueTasks { get; set; } = new StatisticItemDto();

        /// <summary>
        /// Average completion time in days with trend
        /// </summary>
        public StatisticItemDto AvgCompletionTime { get; set; } = new StatisticItemDto();
    }

    /// <summary>
    /// Single statistic item with trend information
    /// </summary>
    public class StatisticItemDto
    {
        /// <summary>
        /// Current value
        /// </summary>
        public decimal Value { get; set; }

        /// <summary>
        /// Difference compared to last month (positive or negative)
        /// </summary>
        public decimal Difference { get; set; }

        /// <summary>
        /// Trend direction: "up", "down", or "neutral"
        /// </summary>
        public string Trend { get; set; } = "neutral";

        /// <summary>
        /// Whether the trend is positive (improvement)
        /// For overdue tasks, decrease is positive
        /// For completion time, decrease is positive
        /// For active cases and completed, increase is positive
        /// </summary>
        public bool IsPositive { get; set; }

        /// <summary>
        /// Display suffix (e.g., "days" for completion time)
        /// </summary>
        public string? Suffix { get; set; }
    }
}
