using FlowFlex.Domain.Repository.OW;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Access Token Service Interface
    /// </summary>
    public interface IAccessTokenService
    {
        /// <summary>
        /// Record a new token when user logs in or refreshes
        /// </summary>
        /// <param name="jti">JWT ID</param>
        /// <param name="userId">User ID</param>
        /// <param name="userEmail">User email</param>
        /// <param name="tokenHash">Token hash</param>
        /// <param name="expiresAt">Token expiration time</param>
        /// <param name="tokenType">Token type (login, refresh, portal-access)</param>
        /// <param name="ipAddress">IP address</param>
        /// <param name="userAgent">User agent</param>
        /// <param name="revokeOtherTokens">Whether to revoke other user tokens</param>
        /// <returns>Token ID</returns>
        Task<long> RecordTokenAsync(string jti, long userId, string userEmail, string tokenHash, 
            DateTimeOffset expiresAt, string tokenType = "login", string ipAddress = "", string userAgent = "", 
            bool revokeOtherTokens = true);

        /// <summary>
        /// Validate if a token is active and valid
        /// </summary>
        /// <param name="jti">JWT ID</param>
        /// <returns>True if token is valid</returns>
        Task<bool> ValidateTokenAsync(string jti);

        /// <summary>
        /// Update token last used time
        /// </summary>
        /// <param name="jti">JWT ID</param>
        /// <returns>True if updated</returns>
        Task<bool> UpdateTokenUsageAsync(string jti);

        /// <summary>
        /// Revoke a specific token (logout)
        /// </summary>
        /// <param name="jti">JWT ID</param>
        /// <param name="reason">Revocation reason</param>
        /// <returns>True if revoked</returns>
        Task<bool> RevokeTokenAsync(string jti, string reason = "logout");

        /// <summary>
        /// Revoke all tokens for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="reason">Revocation reason</param>
        /// <returns>Number of tokens revoked</returns>
        Task<int> RevokeAllUserTokensAsync(long userId, string reason = "logout_all");

        /// <summary>
        /// Get user token statistics
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Token statistics</returns>
        Task<TokenStatistics> GetUserTokenStatisticsAsync(long userId);

        /// <summary>
        /// Clean up expired tokens (background job)
        /// </summary>
        /// <returns>Number of tokens cleaned up</returns>
        Task<int> CleanupExpiredTokensAsync();
    }
} 