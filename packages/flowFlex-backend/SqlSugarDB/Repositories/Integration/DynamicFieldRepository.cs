using Microsoft.Extensions.Logging;
using SqlSugar;
using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Shared;
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
            // Get current tenant and app code from context
            var httpContext = _httpContextAccessor?.HttpContext;
            var tenantId = httpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault() 
                ?? httpContext?.Request.Headers["TenantId"].FirstOrDefault()
                ?? "DEFAULT";
            var appCode = httpContext?.Request.Headers["X-App-Code"].FirstOrDefault()
                ?? httpContext?.Request.Headers["AppCode"].FirstOrDefault()
                ?? "DEFAULT";

            return await db.Queryable<DynamicField>()
                .Where(x => x.FieldId == fieldId && x.IsValid && x.TenantId == tenantId && x.AppCode == appCode)
                .FirstAsync();
        }

        /// <summary>
        /// Get all dynamic fields by category
        /// </summary>
        public async Task<List<DynamicField>> GetByCategoryAsync(string category)
        {
            // Get current tenant and app code from context
            var httpContext = _httpContextAccessor?.HttpContext;
            var tenantId = httpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault() 
                ?? httpContext?.Request.Headers["TenantId"].FirstOrDefault()
                ?? "DEFAULT";
            var appCode = httpContext?.Request.Headers["X-App-Code"].FirstOrDefault()
                ?? httpContext?.Request.Headers["AppCode"].FirstOrDefault()
                ?? "DEFAULT";

            return await db.Queryable<DynamicField>()
                .Where(x => x.Category == category && x.IsValid && x.TenantId == tenantId && x.AppCode == appCode)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();
        }

        /// <summary>
        /// Get all dynamic fields ordered by sort order
        /// </summary>
        public async Task<List<DynamicField>> GetAllOrderedAsync()
        {
            // Get current tenant and app code from context
            var httpContext = _httpContextAccessor?.HttpContext;
            var tenantId = httpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault() 
                ?? httpContext?.Request.Headers["TenantId"].FirstOrDefault()
                ?? "DEFAULT";
            var appCode = httpContext?.Request.Headers["X-App-Code"].FirstOrDefault()
                ?? httpContext?.Request.Headers["AppCode"].FirstOrDefault()
                ?? "DEFAULT";

            _logger.LogDebug($"GetAllOrderedAsync - TenantId: {tenantId}, AppCode: {appCode}");

            return await db.Queryable<DynamicField>()
                .Where(x => x.IsValid && x.TenantId == tenantId && x.AppCode == appCode)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();
        }

        /// <summary>
        /// Check if field ID exists
        /// </summary>
        public async Task<bool> ExistsFieldIdAsync(string fieldId, long? excludeId = null)
        {
            // Get current tenant and app code from context
            var httpContext = _httpContextAccessor?.HttpContext;
            var tenantId = httpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault() 
                ?? httpContext?.Request.Headers["TenantId"].FirstOrDefault()
                ?? "DEFAULT";
            var appCode = httpContext?.Request.Headers["X-App-Code"].FirstOrDefault()
                ?? httpContext?.Request.Headers["AppCode"].FirstOrDefault()
                ?? "DEFAULT";

            var query = db.Queryable<DynamicField>()
                .Where(x => x.FieldId == fieldId && x.IsValid && x.TenantId == tenantId && x.AppCode == appCode);

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
            // Get current tenant and app code from context
            var httpContext = _httpContextAccessor?.HttpContext;
            var tenantId = httpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault() 
                ?? httpContext?.Request.Headers["TenantId"].FirstOrDefault()
                ?? "DEFAULT";
            var appCode = httpContext?.Request.Headers["X-App-Code"].FirstOrDefault()
                ?? httpContext?.Request.Headers["AppCode"].FirstOrDefault()
                ?? "DEFAULT";

            return await db.Queryable<DynamicField>()
                .Where(x => x.FormProp == formProp && x.IsValid && x.TenantId == tenantId && x.AppCode == appCode)
                .FirstAsync();
        }
    }
}

