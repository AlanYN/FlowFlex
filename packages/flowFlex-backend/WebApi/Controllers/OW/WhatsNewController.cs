using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.WhatsNew;
using FlowFlex.Application.Contracts.IServices.OW;
using WebApi.Authorization;

using Item.Internal.StandardApi.Response;

namespace FlowFlex.WebApi.Controllers.OW
{
    /// <summary>
    /// What's New notification API — user-facing queries and admin CRUD
    /// </summary>
    [ApiController]
    [Route("ow/whats-new/v{version:apiVersion}")]
    [Display(Name = "whats-new")]
    [Authorize]
    public class WhatsNewController : Controllers.ControllerBase
    {
        private readonly IWhatsNewService _whatsNewService;

        public WhatsNewController(IWhatsNewService whatsNewService)
        {
            _whatsNewService = whatsNewService;
        }

        #region User-facing endpoints

        /// <summary>
        /// Get the number of unread published updates for the current user.
        /// Redis cache (TTL 10 min) is checked first; DB is queried on a miss.
        /// </summary>
        [HttpGet("unread-count")]
        [ProducesResponseType<SuccessResponse<int>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetUnreadCount()
        {
            var count = await _whatsNewService.GetUnreadCountAsync();
            return Success(count);
        }

        /// <summary>
        /// Get the What's New panel: up to 10 most recent published updates with isRead flags.
        /// Does NOT trigger any read-marking.
        /// </summary>
        [HttpGet("panel")]
        [ProducesResponseType<SuccessResponse<WhatsNewPanelResponseDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetPanel()
        {
            var data = await _whatsNewService.GetPanelAsync();
            return Success(data);
        }

        /// <summary>
        /// Get full details for a single What's New entry, including rich-text content.
        /// </summary>
        [HttpGet("{id:long}")]
        [ProducesResponseType<SuccessResponse<WhatsNewDetailDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetDetail(long id)
        {
            var data = await _whatsNewService.GetDetailAsync(id);
            return Success(data);
        }

        /// <summary>
        /// Mark a specific update as read for the current user (idempotent).
        /// Also invalidates the Redis unread-count cache.
        /// </summary>
        [HttpPost("{id:long}/read")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> MarkRead(long id)
        {
            await _whatsNewService.MarkReadAsync(id);
            return Success(true);
        }

        /// <summary>
        /// Mark all published updates as read for the current user.
        /// Also invalidates the Redis unread-count cache.
        /// </summary>
        [HttpPost("read-all")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> MarkAllRead()
        {
            await _whatsNewService.MarkAllReadAsync();
            return Success(true);
        }

        #endregion

        #region Admin-facing endpoints (System Admin only)

        /// <summary>
        /// Get all active What's New entries with read counts and status statistics.
        /// Supports optional status filter: 0 = Draft, 1 = Published.
        /// Requires System Admin (userType = 1).
        /// </summary>
        [HttpGet("admin")]
        [WFEAuthorize]
        [ProducesResponseType<SuccessResponse<WhatsNewAdminListResponseDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAdminList([FromQuery] int? status = null)
        {
            var data = await _whatsNewService.GetAdminListAsync(status);
            return Success(data);
        }

        /// <summary>
        /// Create a new What's New entry. HTML content is XSS-filtered before storage.
        /// When status = 1 (Published), publish_time is set to now.
        /// Requires System Admin (userType = 1).
        /// </summary>
        [HttpPost("admin")]
        [WFEAuthorize]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Create([FromBody] CreateWhatsNewRequest request)
        {
            var id = await _whatsNewService.CreateAsync(request);
            return Success(id);
        }

        /// <summary>
        /// Update an existing What's New entry. HTML content is XSS-filtered before storage.
        /// Transitioning from Draft to Published automatically sets publish_time = now.
        /// Requires System Admin (userType = 1).
        /// </summary>
        [HttpPut("admin/{id:long}")]
        [WFEAuthorize]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateWhatsNewRequest request)
        {
            var result = await _whatsNewService.UpdateAsync(id, request);
            return Success(result);
        }

        /// <summary>
        /// Soft-delete a What's New entry (is_valid = false).
        /// Read-status history is preserved.
        /// Requires System Admin (userType = 1).
        /// </summary>
        [HttpDelete("admin/{id:long}")]
        [WFEAuthorize]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _whatsNewService.DeleteAsync(id);
            return Success(result);
        }

        #endregion
    }
}
