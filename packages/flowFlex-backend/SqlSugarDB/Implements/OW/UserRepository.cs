using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.SqlSugarDB.Context;
using FlowFlex.Domain.Shared;
using SqlSugar;
using Microsoft.AspNetCore.Http;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// User repository implementation
    /// </summary>
    public class UserRepository : OwBaseRepository<User>, IUserRepository, IScopedService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserRepository(ISqlSugarContext context, IHttpContextAccessor httpContextAccessor) : base(context)
        {
            _httpContextAccessor = httpContextAccessor;
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
        /// Get user by email and tenant ID (for multi-tenant support)
        /// </summary>
        /// <param name="email">Email address</param>
        /// <param name="tenantId">Tenant ID</param>
        /// <returns>User</returns>
        public async Task<User> GetByEmailAndTenantAsync(string email, string tenantId)
        {
            // Disable global filters to query across tenants
            return await _db.Queryable<User>()
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
            return await _db.Queryable<User>()
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
            return await _db.Queryable<User>()
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
            return await _db.Queryable<User>()
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
            var result = await _db.Updateable<User>()
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
            var result = await _db.Updateable<User>()
                .SetColumns(u => new User
                {
                    EmailVerified = verified,
                    ModifyDate = DateTimeOffset.Now
                })
                .Where(u => u.Id == userId)
                .ExecuteCommandAsync();

            return result > 0;
        }

        /// <summary>
        /// Get users with pagination and filters
        /// </summary>
        public async Task<(List<User> items, int totalCount)> GetPagedAsync(
            int pageIndex,
            int pageSize,
            string searchText = null,
            string email = null,
            string username = null,
            string team = null,
            string status = null,
            bool? emailVerified = null,
            string sortField = "CreateDate",
            string sortDirection = "desc")
        {
            // Global tenant and app filters will be applied automatically by AppTenantFilter
            var query = _db.Queryable<User>()
                .Where(u => u.IsValid);

            // Search text filter (search in username and email)
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(u => u.Username.Contains(searchText) || u.Email.Contains(searchText));
            }

            // Email filter
            if (!string.IsNullOrWhiteSpace(email))
            {
                query = query.Where(u => u.Email.Contains(email));
            }

            // Username filter
            if (!string.IsNullOrWhiteSpace(username))
            {
                query = query.Where(u => u.Username.Contains(username));
            }

            // Team filter
            if (!string.IsNullOrWhiteSpace(team))
            {
                query = query.Where(u => u.Team == team);
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(u => u.Status == status);
            }

            // Email verification status filter
            if (emailVerified.HasValue)
            {
                query = query.Where(u => u.EmailVerified == emailVerified.Value);
            }

            // Apply sorting
            if (!string.IsNullOrWhiteSpace(sortField))
            {
                var isAsc = sortDirection?.ToLower() != "desc";
                switch (sortField.ToLower())
                {
                    case "username":
                        query = isAsc ? query.OrderBy(u => u.Username) : query.OrderByDescending(u => u.Username);
                        break;
                    case "email":
                        query = isAsc ? query.OrderBy(u => u.Email) : query.OrderByDescending(u => u.Email);
                        break;
                    case "team":
                        query = isAsc ? query.OrderBy(u => u.Team) : query.OrderByDescending(u => u.Team);
                        break;
                    case "status":
                        query = isAsc ? query.OrderBy(u => u.Status) : query.OrderByDescending(u => u.Status);
                        break;
                    case "lastlogindate":
                        query = isAsc ? query.OrderBy(u => u.LastLoginDate) : query.OrderByDescending(u => u.LastLoginDate);
                        break;
                    default: // CreateDate
                        query = isAsc ? query.OrderBy(u => u.CreateDate) : query.OrderByDescending(u => u.CreateDate);
                        break;
                }
            }
            else
            {
                query = query.OrderByDescending(u => u.CreateDate);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Get paged results
            var items = await query.ToPageListAsync(pageIndex, pageSize);

            return (items, totalCount);
        }

        /// <summary>
        /// Get users without team
        /// </summary>
        public async Task<List<User>> GetUsersWithoutTeamAsync()
        {
            // Global tenant and app filters will be applied automatically by AppTenantFilter
            return await _db.Queryable<User>()
                .Where(u => u.IsValid && (u.Team == null || u.Team == ""))
                .ToListAsync();
        }

        /// <summary>
        /// Update user team
        /// </summary>
        public async Task<bool> UpdateUserTeamAsync(long userId, string team)
        {
            var result = await _db.Updateable<User>()
                .SetColumns(u => new User
                {
                    Team = team,
                    ModifyDate = DateTimeOffset.Now
                })
                .Where(u => u.Id == userId)
                .ExecuteCommandAsync();

            return result > 0;
        }

        /// <summary>
        /// Get current tenant ID from HTTP context
        /// </summary>
        private string GetCurrentTenantId()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null)
                return "DEFAULT";

            // Try to get from AppContext first
            if (httpContext.Items.TryGetValue("AppContext", out var appContextObj) &&
                appContextObj is FlowFlex.Domain.Shared.Models.AppContext appContext)
            {
                return appContext.TenantId;
            }

            // Fallback to headers
            var tenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault()
                        ?? httpContext.Request.Headers["TenantId"].FirstOrDefault();

            if (!string.IsNullOrEmpty(tenantId))
            {
                return tenantId;
            }

            return "DEFAULT";
        }

        /// <summary>
        /// Get current app code from HTTP context
        /// </summary>
        private string GetCurrentAppCode()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null)
                return "DEFAULT";

            // Try to get from AppContext first
            if (httpContext.Items.TryGetValue("AppContext", out var appContextObj) &&
                appContextObj is FlowFlex.Domain.Shared.Models.AppContext appContext)
            {
                return appContext.AppCode;
            }

            // Fallback to headers
            var appCode = httpContext.Request.Headers["X-App-Code"].FirstOrDefault()
                       ?? httpContext.Request.Headers["AppCode"].FirstOrDefault();

            if (!string.IsNullOrEmpty(appCode))
            {
                return appCode;
            }

            return "DEFAULT";
        }
    }
}
