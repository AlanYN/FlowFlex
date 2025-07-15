using FlowFlex.Application.Contracts.Dtos.OW.User;

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
    }
}

