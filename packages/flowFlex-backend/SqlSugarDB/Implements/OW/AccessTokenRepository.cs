using SqlSugar;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.SqlSugarDB.Context;
using Microsoft.Extensions.Logging;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// Access Token Repository Implementation
    /// </summary>
    public class AccessTokenRepository : OwBaseRepository<AccessToken>, IAccessTokenRepository, IScopedService
    {
        private readonly ILogger<AccessTokenRepository> _logger;

        public AccessTokenRepository(ISqlSugarContext context, ILogger<AccessTokenRepository> logger) : base(context)
        {
            _logger = logger;
        }

        /// <summary>
        /// Create a new access token record
        /// </summary>
        public async Task<long> CreateAsync(AccessToken token)
        {
            try
            {
                // Ensure table exists
                await EnsureTableExistsAsync();

                // Use InsertAsync method instead which is more reliable
                var result = await _db.Insertable(token).ExecuteReturnEntityAsync();
                return result.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating access token");
                throw;
            }
        }

        /// <summary>
        /// Ensure the access token table exists
        /// </summary>
        private async Task EnsureTableExistsAsync()
        {
            try
            {
                // Check if table exists
                var tableExists = _db.DbMaintenance.IsAnyTable("ff_access_tokens", false);

                if (!tableExists)
                {
                    // Create table using SqlSugar code first
                    _db.CodeFirst.SetStringDefaultLength(200).InitTables<AccessToken>();
                    _logger.LogInformation("AccessToken table created successfully using CodeFirst");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error ensuring AccessToken table exists");
                // Don't throw here, let the insert try anyway
            }
        }

        /// <summary>
        /// Get access token by JTI
        /// </summary>
        public async Task<AccessToken?> GetByJtiAsync(string jti)
        {
            return await _db.Queryable<AccessToken>()
                .Where(x => x.Jti == jti && x.IsValid)
                .FirstAsync();
        }

        /// <summary>
        /// Get all active tokens for a user
        /// </summary>
        public async Task<List<AccessToken>> GetActiveTokensByUserIdAsync(long userId)
        {
            return await _db.Queryable<AccessToken>()
                .Where(x => x.UserId == userId && x.IsActive && x.IsValid && x.ExpiresAt > DateTimeOffset.UtcNow)
                .OrderByDescending(x => x.IssuedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Revoke all active tokens for a user except the specified one
        /// </summary>
        public async Task<int> RevokeUserTokensAsync(long userId, string? exceptJti = null, string reason = "new_login")
        {
            var updateBuilder = _db.Updateable<AccessToken>()
                .SetColumns(x => new AccessToken
                {
                    IsActive = false,
                    RevokedAt = DateTimeOffset.UtcNow,
                    RevokeReason = reason,
                    ModifyDate = DateTimeOffset.UtcNow
                })
                .Where(x => x.UserId == userId && x.IsActive && x.IsValid);

            if (!string.IsNullOrEmpty(exceptJti))
            {
                updateBuilder = updateBuilder.Where(x => x.Jti != exceptJti);
            }

            return await updateBuilder.ExecuteCommandAsync();
        }

        /// <summary>
        /// Revoke a specific token
        /// </summary>
        public async Task<bool> RevokeTokenAsync(string jti, string reason = "logout")
        {
            var rowsAffected = await _db.Updateable<AccessToken>()
                .SetColumns(x => new AccessToken
                {
                    IsActive = false,
                    RevokedAt = DateTimeOffset.UtcNow,
                    RevokeReason = reason,
                    ModifyDate = DateTimeOffset.UtcNow
                })
                .Where(x => x.Jti == jti && x.IsActive && x.IsValid)
                .ExecuteCommandAsync();

            return rowsAffected > 0;
        }

        /// <summary>
        /// Update token last used time
        /// </summary>
        public async Task<bool> UpdateLastUsedAsync(string jti)
        {
            var rowsAffected = await _db.Updateable<AccessToken>()
                .SetColumns(x => new AccessToken
                {
                    LastUsedAt = DateTimeOffset.UtcNow,
                    ModifyDate = DateTimeOffset.UtcNow
                })
                .Where(x => x.Jti == jti && x.IsActive && x.IsValid)
                .ExecuteCommandAsync();

            return rowsAffected > 0;
        }

        /// <summary>
        /// Check if token is active and valid
        /// </summary>
        public async Task<bool> IsTokenActiveAsync(string jti)
        {
            var count = await _db.Queryable<AccessToken>()
                .Where(x => x.Jti == jti && x.IsActive && x.IsValid && x.ExpiresAt > DateTimeOffset.UtcNow)
                .CountAsync();

            return count > 0;
        }

        /// <summary>
        /// Clean up expired tokens
        /// </summary>
        public async Task<int> CleanupExpiredTokensAsync()
        {
            var expiredTime = DateTimeOffset.UtcNow.AddDays(-7); // Keep expired tokens for 7 days for audit

            return await _db.Deleteable<AccessToken>()
                .Where(x => x.ExpiresAt < expiredTime || (!x.IsActive && x.RevokedAt < expiredTime))
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// Get token statistics for a user
        /// </summary>
        public async Task<TokenStatistics> GetUserTokenStatisticsAsync(long userId)
        {
            var tokens = await _db.Queryable<AccessToken>()
                .Where(x => x.UserId == userId && x.IsValid)
                .ToListAsync();

            var now = DateTimeOffset.UtcNow;
            var activeTokens = tokens.Where(x => x.IsActive && x.ExpiresAt > now).ToList();
            var expiredTokens = tokens.Where(x => x.ExpiresAt <= now).ToList();
            var revokedTokens = tokens.Where(x => !x.IsActive).ToList();

            var lastLogin = tokens.OrderByDescending(x => x.IssuedAt)
                .FirstOrDefault();

            return new TokenStatistics
            {
                ActiveTokens = activeTokens.Count,
                ExpiredTokens = expiredTokens.Count,
                RevokedTokens = revokedTokens.Count,
                LastLoginTime = lastLogin?.IssuedAt,
                LastLoginIp = lastLogin?.IssuedIp ?? string.Empty
            };
        }
    }
}