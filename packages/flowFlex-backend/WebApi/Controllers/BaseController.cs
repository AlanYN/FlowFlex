using Microsoft.AspNetCore.Mvc;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.WebApi.Model.Response;

namespace FlowFlex.WebApi.Controllers
{
    /// <summary>
    /// Base controller
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseController : ControllerBase
    {
        protected readonly IUserContextService _userContextService;

        public BaseController(IUserContextService userContextService)
        {
            _userContextService = userContextService;
        }

        /// <summary>
        /// Success response
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="data">Data</param>
        /// <param name="message">Message</param>
        /// <returns>API response</returns>
        protected IActionResult Success<T>(T data, string message = "Operation successful")
        {
            return Ok(ApiResponse<T>.Success(data, message));
        }

        /// <summary>
        /// Failure response
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="errorCode">Error code</param>
        /// <returns>API response</returns>
        protected IActionResult Fail(string message = "Operation failed", int errorCode = 400)
        {
            return BadRequest(ApiResponse<object>.Fail(message, errorCode));
        }

        /// <summary>
        /// Get current user ID
        /// </summary>
        /// <returns>User ID</returns>
        protected long GetCurrentUserId()
        {
            return _userContextService.GetCurrentUserId();
        }

        /// <summary>
        /// Get current user email
        /// </summary>
        /// <returns>User email</returns>
        protected string GetCurrentUserEmail()
        {
            return _userContextService.GetCurrentUserEmail();
        }

        /// <summary>
        /// Get current username
        /// </summary>
        /// <returns>Username</returns>
        protected string GetCurrentUsername()
        {
            return _userContextService.GetCurrentUsername();
        }

        /// <summary>
        /// Get current tenant ID
        /// </summary>
        /// <returns>Tenant ID</returns>
        protected string GetCurrentTenantId()
        {
            return _userContextService.GetCurrentTenantId();
        }

        /// <summary>
        /// Check if authenticated
        /// </summary>
        /// <returns>Whether authenticated</returns>
        protected bool IsAuthenticated()
        {
            return _userContextService.IsAuthenticated();
        }
    }
}
