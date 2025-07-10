namespace FlowFlex.Application.Contracts.Dtos.OW.Event
{
    /// <summary>
    /// Event statistics DTO
    /// </summary>
    public class EventStatisticsDto
    {
        /// <summary>
        /// Total events count
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Published events count
        /// </summary>
        public int PublishedCount { get; set; }

        /// <summary>
        /// Processed events count
        /// </summary>
        public int ProcessedCount { get; set; }

        /// <summary>
        /// Failed events count
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// Events requiring retry count
        /// </summary>
        public int RetryRequiredCount { get; set; }

        /// <summary>
        /// Events by type statistics
        /// </summary>
        public Dictionary<string, int> EventTypeStatistics { get; set; } = new();

        /// <summary>
        /// Events by source statistics
        /// </summary>
        public Dictionary<string, int> EventSourceStatistics { get; set; } = new();

        /// <summary>
        /// Events by status statistics
        /// </summary>
        public Dictionary<string, int> EventStatusStatistics { get; set; } = new();

        /// <summary>
        /// Daily event counts (last N days)
        /// </summary>
        public Dictionary<string, int> DailyEventCounts { get; set; } = new();

        /// <summary>
        /// Average processing time (in seconds)
        /// </summary>
        public double AverageProcessingTime { get; set; }

        /// <summary>
        /// Success rate (percentage)
        /// </summary>
        public double SuccessRate { get; set; }

        /// <summary>
        /// Statistics period (in days)
        /// </summary>
        public int StatisticsPeriod { get; set; }

        /// <summary>
        /// Statistics generation time
        /// </summary>
        public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    }
} 