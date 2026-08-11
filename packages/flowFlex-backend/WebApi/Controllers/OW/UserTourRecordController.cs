using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.WebApi.Controllers;
using Item.Internal.StandardApi.Response;
using System.Net;

namespace FlowFlex.WebApi.Controllers.OW
{
    /// <summary>
    /// Manages per-user guided-tour "seen" state (account-level persistence).
    /// </summary>
    [Route("ow/tour-records/v{version:apiVersion}")]
    [ApiController]
    [Asp.Versioning.ApiVersion("1.0")]
    [Authorize]
    public class UserTourRecordController : Controllers.ControllerBase
    {
        private readonly IUserTourRecordService _service;

        public UserTourRecordController(IUserTourRecordService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Check whether the current user has already seen a specific tour.
        /// </summary>
        /// <param name="tourKey">Tour identifier — must match the frontend persistKey.</param>
        /// <returns>true if seen, false otherwise.</returns>
        [HttpGet("seen")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> HasSeen([FromQuery] string tourKey)
        {
            var result = await _service.HasSeenAsync(tourKey);
            return Success(result);
        }

        /// <summary>
        /// Mark a tour as seen for the current user.
        /// Idempotent — safe to call multiple times.
        /// </summary>
        /// <param name="tourKey">Tour identifier.</param>
        [HttpPost("seen")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> MarkSeen([FromQuery] string tourKey)
        {
            await _service.MarkSeenAsync(tourKey);
            return Success(true);
        }
    }
}
