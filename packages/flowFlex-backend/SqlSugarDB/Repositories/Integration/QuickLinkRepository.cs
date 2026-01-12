using Microsoft.Extensions.Logging;
using SqlSugar;
using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using Microsoft.AspNetCore.Http;

namespace FlowFlex.SqlSugarDB.Implements.Integration
{
    /// <summary>
    /// Quick link repository implementation
    /// </summary>
    public class QuickLinkRepository : BaseRepository<QuickLink>, IQuickLinkRepository, IScopedService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<QuickLinkRepository> _logger;

        public QuickLinkRepository(
            ISqlSugarClient sqlSugarClient,
            IHttpContextAccessor httpContextAccessor,
            ILogger<QuickLinkRepository> logger) : base(sqlSugarClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Get quick links by integration ID (includes both active and inactive)
        /// </summary>
        public async Task<List<QuickLink>> GetByIntegrationIdAsync(long integrationId)
        {
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            return await db.Queryable<QuickLink>()
                .Where(x => x.IntegrationId == integrationId && x.IsValid)
                .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
                .OrderByDescending(x => x.CreateDate)
                .ToListAsync();
        }

        /// <summary>
        /// Check if quick link label exists
        /// </summary>
        public async Task<bool> ExistsLabelAsync(long integrationId, string linkName, long? excludeId = null)
        {
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            var query = db.Queryable<QuickLink>()
                .Where(x => x.IntegrationId == integrationId && x.LinkName == linkName && x.IsValid)
                .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode);

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// Get quick link by label
        /// </summary>
        public async Task<QuickLink> GetByLabelAsync(long integrationId, string label)
        {
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            return await db.Queryable<QuickLink>()
                .Where(x => x.IntegrationId == integrationId && x.LinkName == label && x.IsValid)
                .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
                .FirstAsync();
        }

        /// <summary>
        /// Delete quick links by integration ID
        /// </summary>
        public async Task<bool> DeleteByIntegrationIdAsync(long integrationId)
        {
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            var links = await db.Queryable<QuickLink>()
                .Where(x => x.IntegrationId == integrationId)
                .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
                .ToListAsync();

            if (links.Any())
            {
                foreach (var link in links)
                {
                    link.IsValid = false;
                }
                return await db.Updateable(links).ExecuteCommandAsync() > 0;
            }

            return true;
        }

        /// <summary>
        /// Reorder quick links
        /// </summary>
        public async Task<bool> ReorderAsync(List<(long id, int displayOrder)> orders)
        {
            if (orders == null || !orders.Any())
            {
                return true;
            }

            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            foreach (var (id, displayOrder) in orders)
            {
                await db.Updateable<QuickLink>()
                    .SetColumns(x => x.SortOrder == displayOrder)
                    .Where(x => x.Id == id && x.TenantId == currentTenantId && x.AppCode == currentAppCode)
                    .ExecuteCommandAsync();
            }

            return true;
        }

        /// <summary>
        /// Get all quick links (including invalid ones) with tenant and app isolation
        /// </summary>
        public async Task<List<QuickLink>> GetAllAsync()
        {
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            return await db.Queryable<QuickLink>()
                .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
                .OrderByDescending(x => x.CreateDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get current tenant ID from HTTP context
        /// </summary>
        private string GetCurrentTenantId()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null)
                return "default";

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

            return "default";
        }

        /// <summary>
        /// Get current app code from HTTP context
        /// </summary>
        private string GetCurrentAppCode()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null)
                return "default";

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

            return "default";
        }
    }
}

