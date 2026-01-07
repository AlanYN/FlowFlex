using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Net;
using FlowFlex.Application.Contracts.Dtos.OW.Dashboard;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Filter;
using FlowFlex.Domain.Shared.Const;
using FlowFlex.Domain.Shared.Models;
using Item.Internal.StandardApi.Response;
using WebApi.Authorization;

namespace FlowFlex.WebApi.Controllers.OW
{
    /// <summary>
    /// Dashboard API - Provides aggregated data for dashboard UI
    /// </summary>
    [ApiController]
    [PortalAccess]
    [Route("ow/dashboard/v{version:apiVersion}")]
    [Display(Name = "Dashboard")]
    [Tags("OW-Dashboard", "Onboard Workflow", "Analytics")]
    [Authorize]
    public class DashboardController : Controllers.ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Get aggregated dashboard data with optional module selection
        /// </summary>
        /// <param name="modules">Optional comma-separated module names (statistics,stageDistribution,tasks,messages,achievements,deadlines)</param>
        /// <param name="workflowId">Optional workflow ID for stage distribution</param>
        /// <param name="team">Optional team filter</param>
        /// <param name="taskCategory">Optional task category filter (Sales, Account, Other)</param>
        /// <param name="taskPageIndex">Task list page index (default: 1)</param>
        /// <param name="taskPageSize">Task list page size (default: 10)</param>
        /// <param name="messageLimit">Message list limit (default: 5)</param>
        /// <param name="achievementLimit">Achievement list limit (default: 5)</param>
        /// <param name="deadlineDays">Deadline range in days (default: 7)</param>
        /// <returns>Aggregated dashboard data</returns>
        [HttpGet]
        [WFEAuthorize(PermissionConsts.Case.Read)]
        [ProducesResponseType<SuccessResponse<DashboardDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetDashboard(
            [FromQuery] string? modules = null,
            [FromQuery] long? workflowId = null,
            [FromQuery] string? team = null,
            [FromQuery] string? taskCategory = null,
            [FromQuery] int taskPageIndex = 1,
            [FromQuery] int taskPageSize = 10,
            [FromQuery] int messageLimit = 5,
            [FromQuery] int achievementLimit = 5,
            [FromQuery] int deadlineDays = 7)
        {
            var query = new DashboardQueryDto
            {
                Modules = string.IsNullOrEmpty(modules) 
                    ? null 
                    : modules.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                WorkflowId = workflowId,
                Team = team,
                TaskCategory = taskCategory,
                TaskPageIndex = taskPageIndex,
                TaskPageSize = taskPageSize,
                MessageLimit = messageLimit,
                AchievementLimit = achievementLimit,
                DeadlineDays = deadlineDays
            };

            var data = await _dashboardService.GetDashboardAsync(query);
            return Success(data);
        }


        /// <summary>
        /// Get statistics overview with month-over-month comparison
        /// </summary>
        /// <param name="team">Optional team filter</param>
        /// <returns>Statistics with comparison data</returns>
        [HttpGet("statistics")]
        [WFEAuthorize(PermissionConsts.Case.Read)]
        [ProducesResponseType<SuccessResponse<DashboardStatisticsDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetStatistics([FromQuery] string? team = null)
        {
            var data = await _dashboardService.GetStatisticsAsync(team);
            return Success(data);
        }

        /// <summary>
        /// Get cases overview with stage distribution
        /// </summary>
        /// <param name="workflowId">Optional workflow ID filter</param>
        /// <returns>Cases overview with stage distribution data</returns>
        [HttpGet("cases-overview")]
        [WFEAuthorize(PermissionConsts.Case.Read)]
        [ProducesResponseType<SuccessResponse<StageDistributionResultDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetCasesOverview([FromQuery] long? workflowId = null)
        {
            var data = await _dashboardService.GetStageDistributionAsync(workflowId);
            return Success(data);
        }

        /// <summary>
        /// Get pending tasks for current user with pagination
        /// </summary>
        /// <param name="category">Optional category filter (Sales, Account, Other)</param>
        /// <param name="pageIndex">Page index (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10)</param>
        /// <returns>Paged task list</returns>
        [HttpGet("tasks")]
        [WFEAuthorize(PermissionConsts.Case.Read)]
        [ProducesResponseType<SuccessResponse<PagedResult<DashboardTaskDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetTasks(
            [FromQuery] string? category = null,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new DashboardTaskQueryDto
            {
                Category = category,
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            var data = await _dashboardService.GetTasksAsync(query);
            return Success(data);
        }

        /// <summary>
        /// Get recent messages summary
        /// </summary>
        /// <param name="limit">Maximum number of messages (default: 5)</param>
        /// <returns>Message summary with unread count</returns>
        [HttpGet("messages")]
        [WFEAuthorize(PermissionConsts.Case.Read)]
        [ProducesResponseType<SuccessResponse<MessageSummaryDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetMessages([FromQuery] int limit = 5)
        {
            var data = await _dashboardService.GetMessageSummaryAsync(limit);
            return Success(data);
        }

        /// <summary>
        /// Get recent achievements/milestones
        /// </summary>
        /// <param name="limit">Maximum number of achievements (default: 5)</param>
        /// <param name="team">Optional team filter</param>
        /// <returns>List of recent achievements</returns>
        [HttpGet("achievements")]
        [WFEAuthorize(PermissionConsts.Case.Read)]
        [ProducesResponseType<SuccessResponse<List<AchievementDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAchievements(
            [FromQuery] int limit = 5,
            [FromQuery] string? team = null)
        {
            var data = await _dashboardService.GetAchievementsAsync(limit, team);
            return Success(data);
        }

        /// <summary>
        /// Get upcoming deadlines
        /// </summary>
        /// <param name="days">Number of days to look ahead (default: 7)</param>
        /// <returns>List of upcoming deadlines</returns>
        [HttpGet("deadlines")]
        [WFEAuthorize(PermissionConsts.Case.Read)]
        [ProducesResponseType<SuccessResponse<List<DeadlineDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetDeadlines([FromQuery] int days = 7)
        {
            var data = await _dashboardService.GetDeadlinesAsync(days);
            return Success(data);
        }
    }
}
