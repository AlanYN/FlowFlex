using FlowFlex.Application.Contracts.Dtos.OW.User;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// JWT Service Interface
    /// </summary>
    public interface IJwtService
    {
        /// <summary>
        /// Generate JWT Token
        /// </summary>
        /// <param name="user">User entity</param>
        /// <returns>JWT token</returns>
        string GenerateJwtToken(User user);

        /// <summary>
        /// Generate JWT Token
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="email">User email</param>
        /// <param name="username">Username</param>
        /// <returns>JWT Token</returns>
        string GenerateToken(long userId, string email, string username);

        /// <summary>
        /// Generate JWT Token for portal access
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="email">User email</param>
        /// <param name="username">Username</param>
        /// <returns>JWT Token</returns>
        string GenerateJwtToken(long userId, string email, string username);

        /// <summary>
        /// Validate JWT Token
        /// </summary>
        /// <param name="token">JWT Token</param>
        /// <returns>Validation result</returns>
        bool ValidateToken(string token);

        /// <summary>
        /// Get user ID from Token
        /// </summary>
        /// <param name="token">JWT Token</param>
        /// <returns>User ID</returns>
        long? GetUserIdFromToken(string token);

        /// <summary>
        /// Get user email from Token
        /// </summary>
        /// <param name="token">JWT Token</param>
        /// <returns>User email</returns>
        string GetEmailFromToken(string token);

        /// <summary>
        /// Parse JWT Token and return detailed information
        /// </summary>
        /// <param name="token">JWT Token</param>
        /// <returns>JWT Token information</returns>
        JwtTokenInfoDto ParseToken(string token);

        /// <summary>
        /// Parse third-party JWT token without signature validation
        /// </summary>
        /// <param name="token">Third-party JWT token to parse</param>
        /// <returns>JWT token information</returns>
        JwtTokenInfoDto ParseThirdPartyToken(string token);

        /// <summary>
        /// Refresh JWT Token
        /// </summary>
        /// <param name="token">Current JWT Token</param>
        /// <returns>New JWT Token</returns>
        string RefreshToken(string token);

        /// <summary>
        /// Get token expiry time in seconds
        /// </summary>
        /// <returns>Expiry time in seconds</returns>
        int GetTokenExpiryInSeconds();

        /// <summary>
        /// Generate JWT token with detailed information for token management
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="email">User email</param>
        /// <param name="username">Username</param>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="tokenType">Token type</param>
        /// <returns>Token details</returns>
        TokenDetailsDto GenerateTokenWithDetails(long userId, string email, string username, string tenantId = "DEFAULT", string tokenType = "login");

        /// <summary>
        /// Extract JTI from token
        /// </summary>
        /// <param name="token">JWT token</param>
        /// <returns>JTI value</returns>
        string GetJtiFromToken(string token);
    }
}

