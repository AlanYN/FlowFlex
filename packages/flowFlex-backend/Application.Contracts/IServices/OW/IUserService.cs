using FlowFlex.Application.Contracts.Dtos.OW.User;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// User Service Interface
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Register User
        /// </summary>
        /// <param name="request">Registration Request</param>
        /// <returns>User DTO</returns>
        Task<UserDto> RegisterAsync(RegisterRequestDto request);

        /// <summary>
        /// Send Verification Code
        /// </summary>
        /// <param name="request">Send Verification Code Request</param>
        /// <returns>Whether sending was successful</returns>
        Task<bool> SendVerificationCodeAsync(SendVerificationCodeRequestDto request);

        /// <summary>
        /// Verify Email
        /// </summary>
        /// <param name="request">Verify Email Request</param>
        /// <returns>Whether verification was successful</returns>
        Task<bool> VerifyEmailAsync(VerifyEmailRequestDto request);

        /// <summary>
        /// User Login
        /// </summary>
        /// <param name="request">Login Request</param>
        /// <returns>Login Response</returns>
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);

        /// <summary>
        /// Login with Verification Code
        /// </summary>
        /// <param name="request">Login with Code Request</param>
        /// <returns>Login Response</returns>
        Task<LoginResponseDto> LoginWithCodeAsync(LoginWithCodeRequestDto request);

        /// <summary>
        /// Check if Email Exists
        /// </summary>
        /// <param name="email">Email Address</param>
        /// <returns>Whether email exists</returns>
        Task<bool> CheckEmailExistsAsync(string email);

        /// <summary>
        /// Get Current User Information
        /// </summary>
        /// <returns>User DTO</returns>
        Task<UserDto> GetCurrentUserAsync();

        /// <summary>
        /// Get Current User Email
        /// </summary>
        /// <returns>User Email</returns>
        string GetCurrentUserEmail();

        /// <summary>
        /// Change Password
        /// </summary>
        /// <param name="request">Change Password Request</param>
        /// <returns>Whether password change was successful</returns>
        Task<bool> ChangePasswordAsync(ChangePasswordRequestDto request);

        /// <summary>
        /// Create Test User (for testing environment only)
        /// </summary>
        /// <param name="email">Email</param>
        /// <param name="password">Password</param>
        /// <returns>User DTO</returns>
        Task<UserDto> CreateTestUserAsync(string email, string password);

        /// <summary>
        /// Refresh Access Token
        /// </summary>
        /// <param name="request">Refresh Token Request</param>
        /// <returns>Login Response with new token</returns>
        Task<LoginResponseDto> RefreshAccessTokenAsync(RefreshTokenRequestDto request);

        /// <summary>
        /// Logout user and revoke token
        /// </summary>
        /// <param name="token">Access token to revoke</param>
        /// <returns>True if logout successful</returns>
        Task<bool> LogoutAsync(string token);

        /// <summary>
        /// Logout from all devices (revoke all user tokens)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Number of tokens revoked</returns>
        Task<int> LogoutFromAllDevicesAsync(long userId);

        /// <summary>
        /// Third-party login with automatic registration
        /// </summary>
        /// <param name="request">Third-party login request</param>
        /// <returns>Login response with system token</returns>
        Task<LoginResponseDto> ThirdPartyLoginAsync(ThirdPartyLoginRequestDto request);

        /// <summary>
        /// Get User List with Pagination and Search
        /// </summary>
        /// <param name="request">User list request</param>
        /// <returns>User list response</returns>
        Task<UserListResponseDto> GetUserListAsync(UserListRequestDto request);

        /// <summary>
        /// Assign Random Teams to Users Without Team
        /// </summary>
        /// <returns>Number of users assigned teams</returns>
        Task<int> AssignRandomTeamsToUsersAsync();

        /// <summary>
        /// Get User Tree Structure grouped by teams
        /// </summary>
        /// <returns>Tree structure with teams and users</returns>
        Task<List<UserTreeNodeDto>> GetUserTreeAsync();

        Task<UserDto> GetUserByEmail(string email);
    }
}
