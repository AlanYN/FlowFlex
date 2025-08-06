using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlowFlex.Application.Contracts.Dtos.OW.User;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.WebApi.Controllers;
using FlowFlex.WebApi.Model.Response;
using Item.Internal.StandardApi.Response;
using System.Net;
using System.Linq;

namespace FlowFlex.WebApi.Controllers.OW
{
    /// <summary>
    /// User controller
    /// </summary>
    [Route("ow/users")]
    [ApiController]
    public class UserController : Controllers.ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUserContextService _userContextService;
        private readonly IJwtService _jwtService;

        public UserController(IUserService userService, IUserContextService userContextService, IJwtService jwtService)
        {
            _userService = userService;
            _userContextService = userContextService;
            _jwtService = jwtService;
        }

        /// <summary>
        /// Register user
        /// </summary>
        /// <param name="request">Registration request</param>
        /// <returns>User DTO</returns>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType<SuccessResponse<UserDto>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            var user = await _userService.RegisterAsync(request);
            return Success(user);
        }

        /// <summary>
        /// Send verification code
        /// </summary>
        /// <param name="request">Send verification code request</param>
        /// <returns>Whether sending was successful</returns>
        [HttpPost("send-verification-code")]
        [AllowAnonymous]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> SendVerificationCode([FromBody] SendVerificationCodeRequestDto request)
        {
            var result = await _userService.SendVerificationCodeAsync(request);
            return Success(result);
        }

        /// <summary>
        /// Verify email
        /// </summary>
        /// <param name="request">Verify email request</param>
        /// <returns>Whether verification was successful</returns>
        [HttpPost("verify-email")]
        [AllowAnonymous]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequestDto request)
        {
            var result = await _userService.VerifyEmailAsync(request);
            return Success(result);
        }

        /// <summary>
        /// User login
        /// </summary>
        /// <param name="request">Login request</param>
        /// <returns>Login response</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType<SuccessResponse<LoginResponseDto>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var response = await _userService.LoginAsync(request);
            return Success(response);
        }

        /// <summary>
        /// Login with verification code
        /// </summary>
        /// <param name="request">Login with code request</param>
        /// <returns>Login response</returns>
        [HttpPost("login-with-code")]
        [AllowAnonymous]
        [ProducesResponseType<SuccessResponse<LoginResponseDto>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> LoginWithCode([FromBody] LoginWithCodeRequestDto request)
        {
            var response = await _userService.LoginWithCodeAsync(request);
            return Success(response);
        }

        /// <summary>
        /// Check if email exists
        /// </summary>
        /// <param name="email">Email</param>
        /// <returns>Whether email exists</returns>
        [HttpGet("check-email")]
        [AllowAnonymous]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> CheckEmailExists([FromQuery] string email)
        {
            var exists = await _userService.CheckEmailExistsAsync(email);
            return Success(exists);
        }

        /// <summary>
        /// Get current user information
        /// </summary>
        /// <returns>User DTO</returns>
        [HttpGet("current")]
        [Authorize]
        [ProducesResponseType<SuccessResponse<UserDto>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> GetCurrentUser()
        {
            var user = await _userService.GetCurrentUserAsync();
            return Success(user);
        }

        /// <summary>
        /// Get current user email
        /// </summary>
        /// <returns>User email</returns>
        [HttpGet("current-email")]
        [Authorize]
        [ProducesResponseType<SuccessResponse<string>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public IActionResult GetCurrentUserEmail()
        {
            var email = _userService.GetCurrentUserEmail();
            return Success(email);
        }

        /// <summary>
        /// Change password
        /// </summary>
        /// <param name="request">Change password request</param>
        /// <returns>Whether password change was successful</returns>
        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
        {
            var result = await _userService.ChangePasswordAsync(request);
            return Success(result);
        }

        /// <summary>
        /// Create test user (for testing environment only)
        /// </summary>
        /// <param name="email">Email</param>
        /// <param name="password">Password</param>
        /// <returns>User DTO</returns>
        [HttpPost("create-test-user")]
        [AllowAnonymous]
        [ProducesResponseType<SuccessResponse<UserDto>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> CreateTestUser([FromQuery] string email, [FromQuery] string password)
        {
            var user = await _userService.CreateTestUserAsync(email, password);
            return Success(user);
        }

        /// <summary>
        /// Refresh access token
        /// </summary>
        /// <param name="request">Refresh token request</param>
        /// <returns>Login response with new token</returns>
        [HttpPost("refresh-access-token")]
        [AllowAnonymous]
        [ProducesResponseType<SuccessResponse<LoginResponseDto>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> RefreshAccessToken([FromBody] RefreshTokenRequestDto request)
        {
            var response = await _userService.RefreshAccessTokenAsync(request);
            return Success(response);
        }

        /// <summary>
        /// Logout user and revoke current token
        /// </summary>
        /// <returns>Logout result</returns>
        [HttpPost("logout")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> Logout()
        {
            // Extract token from Authorization header
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return BadRequest("Authorization header with Bearer token is required");
            }

            var token = authHeader.Substring("Bearer ".Length);
            var result = await _userService.LogoutAsync(token);
            return Success(result);
        }

        /// <summary>
        /// Logout from all devices (revoke all user tokens)
        /// </summary>
        /// <returns>Number of tokens revoked</returns>
        [HttpPost("logout-all-devices")]
        [ProducesResponseType<SuccessResponse<int>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> LogoutFromAllDevices()
        {
            var userId = _userContextService.GetCurrentUserId();
            if (userId <= 0)
            {
                return BadRequest("Unable to determine current user");
            }

            var revokedCount = await _userService.LogoutFromAllDevicesAsync(userId);
            return Success(revokedCount);
        }

        /// <summary>
        /// Parse JWT token and return detailed information
        /// </summary>
        /// <param name="request">Parse token request</param>
        /// <returns>JWT Token information</returns>
        [HttpPost("parse-jwt-token")]
        [AllowAnonymous]
        [ProducesResponseType<SuccessResponse<JwtTokenInfoDto>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public IActionResult ParseJwtToken([FromBody] ParseTokenRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Token))
            {
                // 尝试从Authorization Header获取token
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    var token = authHeader.Substring("Bearer ".Length);
                    var tokenInfo = _jwtService.ParseToken(token);
                    return Success(tokenInfo);
                }

                return BadRequest("Token is required either in request body or Authorization header");
            }

            var result = _jwtService.ParseToken(request.Token);
            return Success(result);
        }

        /// <summary>
        /// Parse JWT token from query parameter
        /// </summary>
        /// <param name="token">JWT Token</param>
        /// <returns>JWT Token information</returns>
        [HttpGet("parse-jwt-token")]
        [AllowAnonymous]
        [ProducesResponseType<SuccessResponse<JwtTokenInfoDto>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public IActionResult ParseJwtTokenFromQuery([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest("Token parameter is required");
            }

            var tokenInfo = _jwtService.ParseToken(token);
            return Success(tokenInfo);
        }

        /// <summary>
        /// Third-party login with automatic registration
        /// </summary>
        /// <param name="request">Third-party login request</param>
        /// <returns>Login response with system token</returns>
        [HttpPost("third-party-login")]
        [AllowAnonymous]
        [ProducesResponseType<SuccessResponse<LoginResponseDto>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> ThirdPartyLogin([FromBody] ThirdPartyLoginRequestDto request)
        {
            var response = await _userService.ThirdPartyLoginAsync(request);
            return Success(response);
        }
    }
}
