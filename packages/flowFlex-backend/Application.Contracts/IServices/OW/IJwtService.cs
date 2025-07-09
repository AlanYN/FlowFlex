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
    }
}

