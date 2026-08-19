using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.Gantt;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Shared.Const;
using FlowFlex.WebApi.Filters;
using Item.Internal.StandardApi.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Authorization;

namespace FlowFlex.WebApi.Controllers.OW
{
    /// <summary>
    /// Gantt chart API — case timeline visualization, blocker management, and tour record
    /// </summary>
    [ApiController]
    [Route("ow/gantt/v1")]
    [Display(Name = "gantt")]
    [Authorize]
    public class GanttController : Controllers.ControllerBase
    {
        private readonly IGanttService _ganttService;
        private readonly IUserTourRecordService _userTourRecordService;

        private const string GanttTourKey = "gantt-case-tour";

        public GanttController(IGanttService ganttService, IUserTourRecordService userTourRecordService)
        {
            _ganttService = ganttService;
            _userTourRecordService = userTourRecordService;
        }

        /// <summary>
        /// Get Gantt chart data for a case
        /// Requires CASE:READ permission
        /// </summary>
        [HttpGet("{onboardingId:long}")]
        [WFEAuthorize(PermissionConsts.Case.Read)]
        [ProducesResponseType<SuccessResponse<GanttDataResponseDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetGanttData(long onboardingId)
        {
            var data = await _ganttService.GetGanttDataAsync(onboardingId);
            return Success(data);
        }

        /// <summary>
        /// Block a stage for a case
        /// Requires CASE:UPDATE permission
        /// </summary>
        [HttpPost("{onboardingId:long}/block")]
        [WFEAuthorize(PermissionConsts.Case.Update)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> BlockStage(long onboardingId, [FromBody] BlockStageInputDto input)
        {
            var result = await _ganttService.BlockStageAsync(onboardingId, input);
            return Success(result);
        }

        /// <summary>
        /// Unblock a stage for a case
        /// Requires CASE:UPDATE permission
        /// </summary>
        [HttpPost("{onboardingId:long}/unblock")]
        [WFEAuthorize(PermissionConsts.Case.Update)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> UnblockStage(long onboardingId, [FromBody] UnblockStageInputDto input)
        {
            var result = await _ganttService.UnblockStageAsync(onboardingId, input);
            return Success(result);
        }

        /// <summary>
        /// Check whether the current user has seen the Gantt tour
        /// </summary>
        [HttpGet("tour/seen")]
        [WFEAuthorize(PermissionConsts.Case.Read)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetTourSeen()
        {
            var seen = await _userTourRecordService.HasSeenAsync(GanttTourKey);
            return Success(seen);
        }

        /// <summary>
        /// Mark the Gantt tour as seen for the current user
        /// </summary>
        [HttpPost("tour/mark-seen")]
        [WFEAuthorize(PermissionConsts.Case.Read)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> MarkTourSeen()
        {
            await _userTourRecordService.MarkSeenAsync(GanttTourKey);
            return Success(true);
        }
    }
}
