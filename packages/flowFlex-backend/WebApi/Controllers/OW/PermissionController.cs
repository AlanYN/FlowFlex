using FlowFlex.Application.Contracts.Dtos.OW.Permission;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Shared.Models;
using Item.Internal.StandardApi.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowFlex.WebApi.Controllers.OW
{
    /// <summary>
    /// Permission management controller
    /// </summary>
    [ApiController]
    [Route("ow/permissions/v1")]
    [Authorize]
    public class PermissionController : ControllerBase
    {
        private readonly IPermissionService _permissionService;
        private readonly UserContext _userContext;
        private readonly ILogger<PermissionController> _logger;

        public PermissionController(
            IPermissionService permissionService,
            UserContext userContext,
            ILogger<PermissionController> logger)
        {
            _permissionService = permissionService;
            _userContext = userContext;
            _logger = logger;
        }

        /// <summary>
        /// Check if current user can operate on a resource
        /// </summary>
        /// <param name="request">Permission check request</param>
        /// <returns>Permission check response</returns>
        [HttpPost("check")]
        public async Task<IActionResult> CheckPermission([FromBody] CheckPermissionRequest request)
        {
            _logger.LogInformation(
                "CheckPermission API called - UserId: {UserId}, ResourceId: {ResourceId}, ResourceType: {ResourceType}",
                _userContext?.UserId, request.ResourceId, request.ResourceType);

            // Validate input
            if (request.ResourceId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    errorCode = "INVALID_INPUT",
                    message = "Invalid resource ID"
                });
            }

            // Get current user ID
            if (string.IsNullOrEmpty(_userContext?.UserId))
            {
                return Unauthorized(new
                {
                    success = false,
                    errorCode = "UNAUTHORIZED",
                    message = "User not authenticated"
                });
            }

            if (!long.TryParse(_userContext.UserId, out var userId))
            {
                _logger.LogError("Failed to parse UserId: {UserId}", _userContext.UserId);
                return BadRequest(new
                {
                    success = false,
                    errorCode = "INVALID_USER_ID",
                    message = "Invalid user ID format"
                });
            }

            // Check permission
            var result = await _permissionService.CheckResourcePermissionAsync(
                userId,
                request.ResourceId,
                request.ResourceType);

            _logger.LogInformation(
                "CheckPermission API result - UserId: {UserId}, ResourceId: {ResourceId}, ResourceType: {ResourceType}, " +
                "CanView: {CanView}, CanOperate: {CanOperate}, Reason: {Reason}",
                userId, request.ResourceId, request.ResourceType,
                result.CanView, result.CanOperate, result.GrantReason ?? result.ErrorMessage);

            // Return result
            return Ok(FlowFlex.WebApi.Model.Response.SuccessResponse.Create(result));
        }
    }
}

