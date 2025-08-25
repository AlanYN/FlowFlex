using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using SqlSugar;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// User repository implementation
    /// </summary>
    public class UserRepository : BaseRepository<User>, IUserRepository, IScopedService
    {
        public UserRepository(ISqlSugarClient context) : base(context)
        {
        }

        /// <summary>
        /// Get user by email
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>User</returns>
        public async Task<User> GetByEmailAsync(string email)
        {
            return await db.Queryable<User>()
                .Where(u => u.Email == email && u.IsValid)
                .FirstAsync();
        }

        /// <summary>
        /// Get user by email and tenant ID (for multi-tenant support)
        /// </summary>
        /// <param name="email">Email address</param>
        /// <param name="tenantId">Tenant ID</param>
        /// <returns>User</returns>
        public async Task<User> GetByEmailAndTenantAsync(string email, string tenantId)
        {
            // Disable global filters to query across tenants
            return await db.Queryable<User>()
                .ClearFilter<User>() // Clear tenant filters
                .Where(u => u.Email == email && u.TenantId == tenantId && u.IsValid)
                .FirstAsync();
        }

        /// <summary>
        /// Get user by email and app code (for cross-tenant user management)
        /// </summary>
        /// <param name="email">Email address</param>
        /// <param name="appCode">App Code</param>
        /// <returns>User</returns>
        public async Task<User> GetByEmailAndAppCodeAsync(string email, string appCode)
        {
            // Disable global filters to query across all tenants
            return await db.Queryable<User>()
                .ClearFilter<User>() // Clear all filters
                .Where(u => u.Email == email && u.AppCode == appCode && u.IsValid)
                .OrderBy(u => u.CreateDate) // Get the first created user
                .FirstAsync();
        }

        /// <summary>
        /// Check if email already exists
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>Whether exists</returns>
        public async Task<bool> EmailExistsAsync(string email)
        {
            return await db.Queryable<User>()
                .AnyAsync(u => u.Email == email && u.IsValid);
        }

        /// <summary>
        /// Check if email exists in specific tenant
        /// </summary>
        /// <param name="email">Email address</param>
        /// <param name="tenantId">Tenant ID</param>
        /// <returns>Whether exists</returns>
        public async Task<bool> EmailExistsInTenantAsync(string email, string tenantId)
        {
            // Disable global filters to query across tenants
            return await db.Queryable<User>()
                .ClearFilter<User>() // Clear tenant filters
                .AnyAsync(u => u.Email == email && u.TenantId == tenantId && u.IsValid);
        }

        /// <summary>
        /// Update user last login information
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="loginTime">Login time</param>
        /// <param name="loginIp">Login IP</param>
        /// <returns>Whether update successful</returns>
        public async Task<bool> UpdateLastLoginInfoAsync(long userId, DateTimeOffset loginTime, string loginIp)
        {
            var result = await db.Updateable<User>()
                .SetColumns(u => new User
                {
                    LastLoginDate = loginTime,
                    LastLoginIp = loginIp,
                    ModifyDate = DateTimeOffset.Now
                })
                .Where(u => u.Id == userId)
                .ExecuteCommandAsync();

            return result > 0;
        }

        /// <summary>
        /// Update user email verification status
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="verified">Whether verified</param>
        /// <returns>Whether update successful</returns>
        public async Task<bool> UpdateEmailVerificationStatusAsync(long userId, bool verified)
        {
            var result = await db.Updateable<User>()
                .SetColumns(u => new User
                {
                    EmailVerified = verified,
                    ModifyDate = DateTimeOffset.Now
                })
                .Where(u => u.Id == userId)
                .ExecuteCommandAsync();

            return result > 0;
        }
    }
}
