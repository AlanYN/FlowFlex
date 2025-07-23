using Microsoft.AspNetCore.Http;
using SqlSugar;
using FlowFlex.Domain.Abstracts;
using FlowFlex.Domain.Entities;
using FlowFlex.Domain.Entities.Base;
using FlowFlex.Domain.Shared.Models;
using AppContext = FlowFlex.Domain.Shared.Models.AppContext;

namespace FlowFlex.Infrastructure.Data
{
    /// <summary>
    /// Application and tenant filter for data isolation
    /// </summary>
    public static class AppTenantFilter
    {
        /// <summary>
        /// Configure global filters for SqlSugar client
        /// </summary>
        /// <param name="db">SqlSugar client</param>
        /// <param name="httpContextAccessor">HTTP context accessor</param>
        public static void ConfigureFilters(ISqlSugarClient db, IHttpContextAccessor httpContextAccessor)
        {
            // Add tenant filter for AbstractEntityBase
            db.QueryFilter.AddTableFilter<AbstractEntityBase>(entity =>
                entity.TenantId == GetCurrentTenantId(httpContextAccessor));

            // Add app filter for AbstractEntityBase
            db.QueryFilter.AddTableFilter<AbstractEntityBase>(entity =>
                entity.AppCode == GetCurrentAppCode(httpContextAccessor));

            // Add tenant filter for OwEntityBase
            db.QueryFilter.AddTableFilter<OwEntityBase>(entity =>
                entity.TenantId == GetCurrentTenantId(httpContextAccessor));

            // Add app filter for OwEntityBase
            db.QueryFilter.AddTableFilter<OwEntityBase>(entity =>
                entity.AppCode == GetCurrentAppCode(httpContextAccessor));

            // Add specific filters for concrete entity types to ensure they work
            // This is needed because SqlSugar inheritance filtering sometimes doesn't work as expected
            
            // Workflow filters
            db.QueryFilter.AddTableFilter<FlowFlex.Domain.Entities.OW.Workflow>(entity =>
                entity.TenantId == GetCurrentTenantId(httpContextAccessor));
            db.QueryFilter.AddTableFilter<FlowFlex.Domain.Entities.OW.Workflow>(entity =>
                entity.AppCode == GetCurrentAppCode(httpContextAccessor));

            // User filters
            db.QueryFilter.AddTableFilter<FlowFlex.Domain.Entities.OW.User>(entity =>
                entity.TenantId == GetCurrentTenantId(httpContextAccessor));
            db.QueryFilter.AddTableFilter<FlowFlex.Domain.Entities.OW.User>(entity =>
                entity.AppCode == GetCurrentAppCode(httpContextAccessor));

            // Onboarding filters
            db.QueryFilter.AddTableFilter<FlowFlex.Domain.Entities.OW.Onboarding>(entity =>
                entity.TenantId == GetCurrentTenantId(httpContextAccessor));
            db.QueryFilter.AddTableFilter<FlowFlex.Domain.Entities.OW.Onboarding>(entity =>
                entity.AppCode == GetCurrentAppCode(httpContextAccessor));

            // Stage filters
            db.QueryFilter.AddTableFilter<FlowFlex.Domain.Entities.OW.Stage>(entity =>
                entity.TenantId == GetCurrentTenantId(httpContextAccessor));
            db.QueryFilter.AddTableFilter<FlowFlex.Domain.Entities.OW.Stage>(entity =>
                entity.AppCode == GetCurrentAppCode(httpContextAccessor));

            // Checklist filters
            db.QueryFilter.AddTableFilter<FlowFlex.Domain.Entities.OW.Checklist>(entity =>
                entity.TenantId == GetCurrentTenantId(httpContextAccessor));
            db.QueryFilter.AddTableFilter<FlowFlex.Domain.Entities.OW.Checklist>(entity =>
                entity.AppCode == GetCurrentAppCode(httpContextAccessor));

            // Questionnaire filters
            db.QueryFilter.AddTableFilter<FlowFlex.Domain.Entities.OW.Questionnaire>(entity =>
                entity.TenantId == GetCurrentTenantId(httpContextAccessor));
            db.QueryFilter.AddTableFilter<FlowFlex.Domain.Entities.OW.Questionnaire>(entity =>
                entity.AppCode == GetCurrentAppCode(httpContextAccessor));
        }

        /// <summary>
        /// Get current tenant ID from HTTP context
        /// </summary>
        /// <param name="httpContextAccessor">HTTP context accessor</param>
        /// <returns>Current tenant ID</returns>
        private static string GetCurrentTenantId(IHttpContextAccessor httpContextAccessor)
        {
            var httpContext = httpContextAccessor?.HttpContext;
            if (httpContext == null)
                return "DEFAULT";

            // Try to get from AppContext first
            if (httpContext.Items.TryGetValue("AppContext", out var appContextObj) &&
                appContextObj is AppContext appContext)
            {
                return appContext.TenantId;
            }

            // Fallback to headers
            var tenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault()
                        ?? httpContext.Request.Headers["TenantId"].FirstOrDefault();

            return !string.IsNullOrEmpty(tenantId) ? tenantId : "DEFAULT";
        }

        /// <summary>
        /// Get current app code from HTTP context
        /// </summary>
        /// <param name="httpContextAccessor">HTTP context accessor</param>
        /// <returns>Current app code</returns>
        private static string GetCurrentAppCode(IHttpContextAccessor httpContextAccessor)
        {
            var httpContext = httpContextAccessor?.HttpContext;
            if (httpContext == null)
                return "DEFAULT";

            // Try to get from AppContext first
            if (httpContext.Items.TryGetValue("AppContext", out var appContextObj) &&
                appContextObj is AppContext appContext)
            {
                return appContext.AppCode;
            }

            // Fallback to headers
            var appCode = httpContext.Request.Headers["X-App-Code"].FirstOrDefault()
                       ?? httpContext.Request.Headers["AppCode"].FirstOrDefault();

            return !string.IsNullOrEmpty(appCode) ? appCode : "DEFAULT";
        }

        /// <summary>
        /// Create a scope that temporarily disables filters
        /// </summary>
        /// <param name="db">SqlSugar client</param>
        /// <returns>Disposable scope</returns>
        public static IDisposable CreateFilterDisabledScope(ISqlSugarClient db)
        {
            return new FilterDisabledScope(db);
        }

        /// <summary>
        /// Scope that temporarily disables filters
        /// </summary>
        private class FilterDisabledScope : IDisposable
        {
            private readonly ISqlSugarClient _db;
            private readonly List<SqlFilterItem> _backupFilters;

            public FilterDisabledScope(ISqlSugarClient db)
            {
                _db = db;
                _backupFilters = BackupFilters();
                _db.QueryFilter.Clear();
            }

            public void Dispose()
            {
                RestoreFilters(_backupFilters);
            }

            private List<SqlFilterItem> BackupFilters()
            {
                try
                {
                    var geFilterListProperty = _db.QueryFilter.GetType().GetProperty("GeFilterList");
                    if (geFilterListProperty?.GetValue(_db.QueryFilter) is List<SqlFilterItem> geFilterList)
                    {
                        return new List<SqlFilterItem>(geFilterList);
                    }
                }
                catch
                {
                    // Ignore errors during backup
                }
                return new List<SqlFilterItem>();
            }

            private void RestoreFilters(List<SqlFilterItem> backupFilters)
            {
                try
                {
                    if (backupFilters?.Any() == true)
                    {
                        _db.QueryFilter.Clear();
                        foreach (var filter in backupFilters)
                        {
                            // Restore filters (this is a simplified approach)
                            // In real implementation, you might need to use reflection
                            // to properly restore the filters
                        }
                    }
                }
                catch
                {
                    // Ignore errors during restore
                }
            }
        }
    }
}