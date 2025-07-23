using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Net;
using FlowFlex.Application.Contracts.Dtos.OW;
using FlowFlex.Application.Contracts.IServices.OW;
using Item.Internal.StandardApi.Response;
using FlowFlex.WebApi.Model.Response;

namespace FlowFlex.WebApi.Controllers.OW
{
    /// <summary>
    /// User invitation management API
    /// </summary>
    [ApiController]
    [Route("ow/user-invitations/v{version:apiVersion}")]
    [Display(Name = "user-invitation")]
    [ApiVersion("1.0")]
    [Authorize]
    public class UserInvitationController : Controllers.ControllerBase
    {
        private readonly IUserInvitationService _userInvitationService;
        private readonly ILogger<UserInvitationController> _logger;

        public UserInvitationController(IUserInvitationService userInvitationService, ILogger<UserInvitationController> logger)
        {
            _userInvitationService = userInvitationService;
            _logger = logger;
        }

        /// <summary>
        /// Send invitations for onboarding portal access
        /// </summary>
        /// <param name="request">Invitation request</param>
        /// <returns>Invitation response</returns>
        [HttpPost("send")]
        [ProducesResponseType<SuccessResponse<UserInvitationResponseDto>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> SendInvitationsAsync([FromBody] UserInvitationRequestDto request)
        {
            var result = await _userInvitationService.SendInvitationsAsync(request);
            return Success(result);
        }

        /// <summary>
        /// Get portal users for onboarding
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>List of portal users</returns>
        [HttpGet("portal-users/{onboardingId}")]
        [ProducesResponseType<SuccessResponse<List<PortalUserDto>>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> GetPortalUsersAsync(long onboardingId)
        {
            var result = await _userInvitationService.GetPortalUsersAsync(onboardingId);
            return Success(result);
        }

        // Legacy endpoint - removed as we only use short URL now
        // /// <summary>
        // /// Verify portal access with invitation token
        // /// </summary>
        // /// <param name="request">Verification request</param>
        // /// <returns>Verification response</returns>
        // [HttpPost("verify-access")]
        // [AllowAnonymous]
        // [ProducesResponseType<SuccessResponse<PortalAccessVerificationResponseDto>>((int)HttpStatusCode.OK)]
        // [ProducesResponseType(typeof(ErrorResponse), 400)]
        // public async Task<IActionResult> VerifyPortalAccessAsync([FromBody] PortalAccessVerificationRequestDto request)
        // {
        //     throw new NotImplementedException("Legacy token verification is no longer supported. Use short URL verification instead.");
        // }

        /// <summary>
        /// Resend invitation
        /// </summary>
        /// <param name="request">Resend invitation request</param>
        /// <returns>Whether resend was successful</returns>
        [HttpPost("resend")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> ResendInvitationAsync([FromBody] ResendInvitationRequestDto request)
        {
            var result = await _userInvitationService.ResendInvitationAsync(request);
            return Success(result);
        }

        /// <summary>
        /// Remove portal access
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="email">Email address</param>
        /// <returns>Whether removal was successful</returns>
        [HttpDelete("remove-access/{onboardingId}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> RemovePortalAccessAsync(long onboardingId, [FromQuery] string email)
        {
            var result = await _userInvitationService.RemovePortalAccessAsync(onboardingId, email);
            return Success(result);
        }

        /// <summary>
        /// Toggle portal access status (Active/Inactive)
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="email">Email address</param>
        /// <param name="isActive">Whether to activate or deactivate</param>
        /// <returns>Whether status change was successful</returns>
        [HttpPut("toggle-status/{onboardingId}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> TogglePortalAccessStatusAsync(long onboardingId, [FromQuery] string email, [FromQuery] bool isActive)
        {
            var result = await _userInvitationService.TogglePortalAccessStatusAsync(onboardingId, email, isActive);
            return Success(result);
        }

        /// <summary>
        /// Get invitation link for a user
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="email">Email address</param>
        /// <returns>Invitation link information</returns>
        [HttpGet("invitation-link/{onboardingId}")]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> GetInvitationLinkAsync(long onboardingId, [FromQuery] string email)
        {
            var result = await _userInvitationService.GetInvitationLinkAsync(onboardingId, email);
            return Success(result);
        }

        /// <summary>
        /// Validate invitation token
        /// </summary>
        /// <param name="request">Token validation request</param>
        /// <returns>Token validation result</returns>
        [HttpPost("validate-token")]
        public async Task<IActionResult> ValidateTokenAsync([FromBody] TokenValidationRequestDto request)
        {
            try
            {
                var result = await _userInvitationService.ValidateTokenAsync(request.Token, request.OnboardingId);
                return Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate token for onboarding ID {OnboardingId}", request.OnboardingId);
                return StatusCode(500, new
                {
                    Code = 500,
                    Message = "Internal server error",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Verify portal access with short URL ID
        /// </summary>
        /// <param name="shortUrlId">Short URL identifier</param>
        /// <param name="request">Verification request with email</param>
        /// <returns>Verification response</returns>
        [HttpPost("verify-access-short/{shortUrlId}")]
        [AllowAnonymous]
        [ProducesResponseType<SuccessResponse<PortalAccessVerificationResponseDto>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> VerifyPortalAccessByShortUrlAsync(string shortUrlId, [FromBody] PortalAccessVerificationByShortUrlRequestDto request)
        {
            var result = await _userInvitationService.VerifyPortalAccessByShortUrlAsync(shortUrlId, request.Email);
            return Success(result);
        }
    }
}