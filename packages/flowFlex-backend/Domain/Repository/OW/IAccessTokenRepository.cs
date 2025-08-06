using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// Access Token Repository Interface
    /// </summary>
    public interface IAccessTokenRepository
    {
        /// <summary>
        /// Create a new access token record
        /// </summary>
        /// <param name="token">Access token entity</param>
        /// <returns>Created token ID</returns>
        Task<long> CreateAsync(AccessToken token);

        /// <summary>
        /// Get access token by JTI
        /// </summary>
        /// <param name="jti">JWT ID</param>
        /// <returns>Access token entity</returns>
        Task<AccessToken?> GetByJtiAsync(string jti);

        /// <summary>
        /// Get all active tokens for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of active tokens</returns>
        Task<List<AccessToken>> GetActiveTokensByUserIdAsync(long userId);

        /// <summary>
        /// Revoke all active tokens for a user except the specified one
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="exceptJti">JTI to keep active (optional)</param>
        /// <param name="reason">Revocation reason</param>
        /// <returns>Number of tokens revoked</returns>
        Task<int> RevokeUserTokensAsync(long userId, string? exceptJti = null, string reason = "new_login");

        /// <summary>
        /// Revoke a specific token
        /// </summary>
        /// <param name="jti">JWT ID</param>
        /// <param name="reason">Revocation reason</param>
        /// <returns>True if token was revoked</returns>
        Task<bool> RevokeTokenAsync(string jti, string reason = "logout");

        /// <summary>
        /// Update token last used time
        /// </summary>
        /// <param name="jti">JWT ID</param>
        /// <returns>True if updated</returns>
        Task<bool> UpdateLastUsedAsync(string jti);

        /// <summary>
        /// Check if token is active and valid
        /// </summary>
        /// <param name="jti">JWT ID</param>
        /// <returns>True if token is active</returns>
        Task<bool> IsTokenActiveAsync(string jti);

        /// <summary>
        /// Clean up expired tokens
        /// </summary>
        /// <returns>Number of tokens cleaned up</returns>
        Task<int> CleanupExpiredTokensAsync();

        /// <summary>
        /// Get token statistics for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Token statistics</returns>
        Task<TokenStatistics> GetUserTokenStatisticsAsync(long userId);
    }

    /// <summary>
    /// Token statistics model
    /// </summary>
    public class TokenStatistics
    {
        public int ActiveTokens { get; set; }
        public int ExpiredTokens { get; set; }
        public int RevokedTokens { get; set; }
        public DateTimeOffset? LastLoginTime { get; set; }
        public string LastLoginIp { get; set; } = string.Empty;
    }
} 