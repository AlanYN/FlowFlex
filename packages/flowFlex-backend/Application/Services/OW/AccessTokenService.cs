using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Helpers;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Access Token Service Implementation
    /// </summary>
    public class AccessTokenService : IAccessTokenService, IScopedService
    {
        private readonly IAccessTokenRepository _accessTokenRepository;
        private readonly ILogger<AccessTokenService> _logger;
        private readonly UserContext _userContext;

        public AccessTokenService(
            IAccessTokenRepository accessTokenRepository,
            ILogger<AccessTokenService> logger,
            UserContext userContext)
        {
            _accessTokenRepository = accessTokenRepository;
            _logger = logger;
            _userContext = userContext;
        }

        /// <summary>
        /// Record a new token when user logs in or refreshes
        /// </summary>
        public async Task<long> RecordTokenAsync(string jti, long userId, string userEmail, string tokenHash,
            DateTimeOffset expiresAt, string tokenType = "login", string ipAddress = "", string userAgent = "",
            bool revokeOtherTokens = true)
        {
            try
            {
                // Revoke other active tokens if requested
                if (revokeOtherTokens)
                {
                    var revokedCount = await _accessTokenRepository.RevokeUserTokensAsync(userId, jti, "new_login");
                    _logger.LogInformation("Revoked {Count} existing tokens for user {UserId}", revokedCount, userId);
                }

                // Create new token record
                var accessToken = new AccessToken
                {
                    Jti = jti,
                    UserId = userId,
                    UserEmail = userEmail,
                    TokenHash = HashToken(tokenHash),
                    IssuedAt = DateTimeOffset.UtcNow,
                    ExpiresAt = expiresAt,
                    IsActive = true,
                    TokenType = tokenType,
                    IssuedIp = ipAddress,
                    UserAgent = userAgent,
                    TenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext),
                    AppCode = TenantContextHelper.GetAppCodeOrDefault(_userContext)
                };

                // Initialize create info which should set the ID
                accessToken.InitCreateInfo(_userContext);

                // If ID is still 0, manually set it using timestamp
                if (accessToken.Id == 0)
                {
                    accessToken.Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                }

                var tokenId = await _accessTokenRepository.CreateAsync(accessToken);

                _logger.LogInformation("Created new access token {TokenId} for user {UserId} with JTI {Jti}",
                    tokenId, userId, jti);

                return tokenId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record access token for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Validate if a token is active and valid
        /// </summary>
        public async Task<bool> ValidateTokenAsync(string jti)
        {
            try
            {
                return await _accessTokenRepository.IsTokenActiveAsync(jti);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate token {Jti}", jti);
                return false;
            }
        }

        /// <summary>
        /// Update token last used time
        /// </summary>
        public async Task<bool> UpdateTokenUsageAsync(string jti)
        {
            try
            {
                return await _accessTokenRepository.UpdateLastUsedAsync(jti);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update token usage for {Jti}", jti);
                return false;
            }
        }

        /// <summary>
        /// Revoke a specific token (logout)
        /// </summary>
        public async Task<bool> RevokeTokenAsync(string jti, string reason = "logout")
        {
            try
            {
                var result = await _accessTokenRepository.RevokeTokenAsync(jti, reason);

                if (result)
                {
                    _logger.LogInformation("Revoked token {Jti} with reason: {Reason}", jti, reason);
                }
                else
                {
                    _logger.LogWarning("Failed to revoke token {Jti} - token not found or already revoked", jti);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to revoke token {Jti}", jti);
                return false;
            }
        }

        /// <summary>
        /// Revoke all tokens for a user
        /// </summary>
        public async Task<int> RevokeAllUserTokensAsync(long userId, string reason = "logout_all")
        {
            try
            {
                var revokedCount = await _accessTokenRepository.RevokeUserTokensAsync(userId, null, reason);

                _logger.LogInformation("Revoked {Count} tokens for user {UserId} with reason: {Reason}",
                    revokedCount, userId, reason);

                return revokedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to revoke all tokens for user {UserId}", userId);
                return 0;
            }
        }

        /// <summary>
        /// Get user token statistics
        /// </summary>
        public async Task<TokenStatistics> GetUserTokenStatisticsAsync(long userId)
        {
            try
            {
                return await _accessTokenRepository.GetUserTokenStatisticsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get token statistics for user {UserId}", userId);
                return new TokenStatistics();
            }
        }

        /// <summary>
        /// Clean up expired tokens (background job)
        /// </summary>
        public async Task<int> CleanupExpiredTokensAsync()
        {
            try
            {
                var cleanedCount = await _accessTokenRepository.CleanupExpiredTokensAsync();

                if (cleanedCount > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} expired tokens", cleanedCount);
                }

                return cleanedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup expired tokens");
                return 0;
            }
        }

        /// <summary>
        /// Hash a token for secure storage
        /// </summary>
        private static string HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}