namespace FlowFlex.Application.Contracts.Dtos.OW.Dashboard
{
    /// <summary>
    /// Stage distribution item showing case count per stage
    /// </summary>
    public class StageDistributionDto
    {
        /// <summary>
        /// Stage ID
        /// </summary>
        public long StageId { get; set; }

        /// <summary>
        /// Stage name
        /// </summary>
        public string StageName { get; set; } = string.Empty;

        /// <summary>
        /// Case count in this stage
        /// </summary>
        public int CaseCount { get; set; }

        /// <summary>
        /// Stage order in workflow
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Stage color for UI display
        /// </summary>
        public string? Color { get; set; }

        /// <summary>
        /// Percentage of total cases (0-100)
        /// </summary>
        public decimal Percentage { get; set; }
    }
}
