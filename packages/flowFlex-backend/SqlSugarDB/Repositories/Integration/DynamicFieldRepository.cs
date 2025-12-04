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
    /// Dynamic field repository implementation
    /// </summary>
    public class DynamicFieldRepository : BaseRepository<DynamicField>, IDynamicFieldRepository, IScopedService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<DynamicFieldRepository> _logger;

        public DynamicFieldRepository(
            ISqlSugarClient sqlSugarClient,
            IHttpContextAccessor httpContextAccessor,
            ILogger<DynamicFieldRepository> logger) : base(sqlSugarClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Get dynamic field by field ID
        /// </summary>
        public async Task<DynamicField?> GetByFieldIdAsync(string fieldId)
        {
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            return await db.Queryable<DynamicField>()
                .Where(x => x.FieldId == fieldId && x.IsValid)
                .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
                .FirstAsync();
        }

        /// <summary>
        /// Get all dynamic fields by category
        /// </summary>
        public async Task<List<DynamicField>> GetByCategoryAsync(string category)
        {
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            return await db.Queryable<DynamicField>()
                .Where(x => x.Category == category && x.IsValid)
                .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();
        }

        /// <summary>
        /// Get all dynamic fields ordered by sort order
        /// </summary>
        public async Task<List<DynamicField>> GetAllOrderedAsync()
        {
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            _logger.LogDebug($"GetAllOrderedAsync - TenantId: {currentTenantId}, AppCode: {currentAppCode}");

            return await db.Queryable<DynamicField>()
                .Where(x => x.IsValid)
                .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();
        }

        /// <summary>
        /// Check if field ID exists
        /// </summary>
        public async Task<bool> ExistsFieldIdAsync(string fieldId, long? excludeId = null)
        {
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            var query = db.Queryable<DynamicField>()
                .Where(x => x.FieldId == fieldId && x.IsValid)
                .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode);

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// Get dynamic field by form property name
        /// </summary>
        public async Task<DynamicField?> GetByFormPropAsync(string formProp)
        {
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            return await db.Queryable<DynamicField>()
                .Where(x => x.FormProp == formProp && x.IsValid)
                .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
                .FirstAsync();
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

