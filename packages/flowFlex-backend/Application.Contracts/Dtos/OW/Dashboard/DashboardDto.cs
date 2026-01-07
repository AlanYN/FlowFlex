using System.Collections.Generic;
using System.Text.Json.Serialization;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.Dtos.OW.Dashboard
{
    /// <summary>
    /// Aggregated dashboard response containing all modules
    /// </summary>
    public class DashboardDto
    {
        /// <summary>
        /// Statistics overview with trends
        /// </summary>
        public DashboardStatisticsDto? Statistics { get; set; }

        /// <summary>
        /// Case distribution by stage (renamed from StageDistribution)
        /// </summary>
        [JsonPropertyName("casesOverview")]
        public StageDistributionResultDto? CasesOverview { get; set; }

        /// <summary>
        /// Pending tasks for current user
        /// </summary>
        public PagedResult<DashboardTaskDto>? Tasks { get; set; }

        /// <summary>
        /// Recent messages summary
        /// </summary>
        public MessageSummaryDto? Messages { get; set; }

        /// <summary>
        /// Recent achievements
        /// </summary>
        public List<AchievementDto>? Achievements { get; set; }

        /// <summary>
        /// Upcoming deadlines
        /// </summary>
        public List<DeadlineDto>? Deadlines { get; set; }
    }

    /// <summary>
    /// Stage distribution result with overall progress
    /// </summary>
    public class StageDistributionResultDto
    {
        /// <summary>
        /// List of stages with case counts
        /// </summary>
        public List<StageDistributionDto> Stages { get; set; } = new List<StageDistributionDto>();

        /// <summary>
        /// Overall progress percentage (0-100)
        /// </summary>
        public decimal OverallProgress { get; set; }
    }
}
