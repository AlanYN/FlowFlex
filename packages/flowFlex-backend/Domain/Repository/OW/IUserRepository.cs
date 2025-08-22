using System;
using System.Threading.Tasks;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// User repository interface
    /// </summary>
    public interface IUserRepository : IOwBaseRepository<User>
    {
        /// <summary>
        /// Get user by email
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>User</returns>
        Task<User> GetByEmailAsync(string email);

        /// <summary>
        /// Get user by email and tenant ID (for multi-tenant support)
        /// </summary>
        /// <param name="email">Email address</param>
        /// <param name="tenantId">Tenant ID</param>
        /// <returns>User</returns>
        Task<User> GetByEmailAndTenantAsync(string email, string tenantId);

        /// <summary>
        /// Get user by email and app code (for cross-tenant user management)
        /// </summary>
        /// <param name="email">Email address</param>
        /// <param name="appCode">App Code</param>
        /// <returns>User</returns>
        Task<User> GetByEmailAndAppCodeAsync(string email, string appCode);

        /// <summary>
        /// Check if email already exists
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>Whether exists</returns>
        Task<bool> EmailExistsAsync(string email);

        /// <summary>
        /// Check if email exists in specific tenant
        /// </summary>
        /// <param name="email">Email address</param>
        /// <param name="tenantId">Tenant ID</param>
        /// <returns>Whether exists</returns>
        Task<bool> EmailExistsInTenantAsync(string email, string tenantId);

        /// <summary>
        /// Update user last login information
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="loginTime">Login time</param>
        /// <param name="loginIp">Login IP</param>
        /// <returns>Whether update was successful</returns>
        Task<bool> UpdateLastLoginInfoAsync(long userId, DateTimeOffset loginTime, string loginIp);

        /// <summary>
        /// Update user email verification status
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="verified">Whether verified</param>
        /// <returns>Whether update was successful</returns>
        Task<bool> UpdateEmailVerificationStatusAsync(long userId, bool verified);

        /// <summary>
        /// Get users with pagination and filters
        /// </summary>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="searchText">Search text (username or email)</param>
        /// <param name="email">Email filter</param>
        /// <param name="username">Username filter</param>
        /// <param name="team">Team filter</param>
        /// <param name="status">Status filter</param>
        /// <param name="emailVerified">Email verification status filter</param>
        /// <param name="sortField">Sort field</param>
        /// <param name="sortDirection">Sort direction</param>
        /// <returns>User list and total count</returns>
        Task<(List<User> items, int totalCount)> GetPagedAsync(
            int pageIndex,
            int pageSize,
            string searchText = null,
            string email = null,
            string username = null,
            string team = null,
            string status = null,
            bool? emailVerified = null,
            string sortField = "CreateDate",
            string sortDirection = "desc");

        /// <summary>
        /// Get users without team
        /// </summary>
        /// <returns>List of users without team</returns>
        Task<List<User>> GetUsersWithoutTeamAsync();

        /// <summary>
        /// Update user team
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="team">Team name</param>
        /// <returns>Whether update was successful</returns>
        Task<bool> UpdateUserTeamAsync(long userId, string team);
    }
}
