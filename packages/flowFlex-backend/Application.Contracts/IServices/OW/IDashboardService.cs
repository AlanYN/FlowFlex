using System.Collections.Generic;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.Dashboard;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Dashboard service interface - provides aggregated data for dashboard UI
    /// </summary>
    public interface IDashboardService : IScopedService
    {
        /// <summary>
        /// Get aggregated dashboard data with optional module selection
        /// </summary>
        /// <param name="query">Query parameters including module selection and filters</param>
        /// <returns>Aggregated dashboard data</returns>
        Task<DashboardDto> GetDashboardAsync(DashboardQueryDto query);

        /// <summary>
        /// Get statistics overview with month-over-month comparison
        /// </summary>
        /// <param name="team">Optional team filter</param>
        /// <returns>Statistics with trends</returns>
        Task<DashboardStatisticsDto> GetStatisticsAsync(string? team = null);

        /// <summary>
        /// Get case distribution by stage
        /// </summary>
        /// <param name="workflowId">Optional workflow ID filter</param>
        /// <returns>Stage distribution with case counts</returns>
        Task<StageDistributionResultDto> GetStageDistributionAsync(long? workflowId = null);

        /// <summary>
        /// Get pending tasks for current user
        /// </summary>
        /// <param name="query">Task query parameters</param>
        /// <returns>Paged list of tasks</returns>
        Task<PagedResult<DashboardTaskDto>> GetTasksAsync(DashboardTaskQueryDto query);

        /// <summary>
        /// Get recent messages summary
        /// </summary>
        /// <param name="limit">Maximum number of messages to return (default: 5)</param>
        /// <returns>Message summary with unread count</returns>
        Task<MessageSummaryDto> GetMessageSummaryAsync(int limit = 5);

        /// <summary>
        /// Get recent achievements/milestones
        /// </summary>
        /// <param name="limit">Maximum number of achievements to return (default: 5)</param>
        /// <param name="team">Optional team filter</param>
        /// <returns>List of recent achievements</returns>
        Task<List<AchievementDto>> GetAchievementsAsync(int limit = 5, string? team = null);

        /// <summary>
        /// Get upcoming deadlines
        /// </summary>
        /// <param name="days">Number of days to look ahead (default: 7)</param>
        /// <returns>List of upcoming deadlines</returns>
        Task<List<DeadlineDto>> GetDeadlinesAsync(int days = 7);
    }
}
