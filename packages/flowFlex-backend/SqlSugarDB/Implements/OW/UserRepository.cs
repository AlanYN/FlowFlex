using System;
using System.Threading.Tasks;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.SqlSugarDB.Context;
using FlowFlex.Domain.Shared;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// User repository implementation
    /// </summary>
    public class UserRepository : OwBaseRepository<User>, IUserRepository, IScopedService
    {
        public UserRepository(ISqlSugarContext context) : base(context)
        {
        }

        /// <summary>
        /// Get user by email
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>User</returns>
        public async Task<User> GetByEmailAsync(string email)
        {
            return await _db.Queryable<User>()
                .Where(u => u.Email == email && u.IsValid)
                .FirstAsync();
        }

        /// <summary>
        /// Check if email already exists
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>Whether exists</returns>
        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _db.Queryable<User>()
                .AnyAsync(u => u.Email == email && u.IsValid);
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
            return await _db.Updateable<User>()
                .SetColumns(u => new User
                {
                    LastLoginDate = loginTime,
                    LastLoginIp = loginIp,
                    ModifyDate = DateTimeOffset.Now
                })
                .Where(u => u.Id == userId && u.IsValid)
                .ExecuteCommandAsync() > 0;
        }

        /// <summary>
        /// Update user email verification status
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="verified">Whether verified</param>
        /// <returns>Whether update successful</returns>
        public async Task<bool> UpdateEmailVerificationStatusAsync(long userId, bool verified)
        {
            return await _db.Updateable<User>()
                .SetColumns(u => new User
                {
                    EmailVerified = verified,
                    ModifyDate = DateTimeOffset.Now
                })
                .Where(u => u.Id == userId && u.IsValid)
                .ExecuteCommandAsync() > 0;
        }
    }
} 
