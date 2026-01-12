using Microsoft.Extensions.Logging;
using SqlSugar;
using System.Linq.Expressions;
using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Shared;
using Microsoft.AspNetCore.Http;
using Domain.Shared.Enums;

namespace FlowFlex.SqlSugarDB.Implements.Integration
{
    /// <summary>
    /// Integration repository implementation
    /// </summary>
    public class IntegrationRepository : BaseRepository<Domain.Entities.Integration.Integration>, IIntegrationRepository, IScopedService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<IntegrationRepository> _logger;

        public IntegrationRepository(
            ISqlSugarClient sqlSugarClient,
            IHttpContextAccessor httpContextAccessor,
            ILogger<IntegrationRepository> logger) : base(sqlSugarClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Get all integrations with optional filters
        /// </summary>
        public async Task<List<Domain.Entities.Integration.Integration>> GetAllAsync(
            string? name = null,
            string? type = null,
            string? status = null)
        {
            var currentTenantId = GetCurrentTenantId();
            _logger.LogInformation($"[IntegrationRepository] GetAllAsync with TenantId={currentTenantId}");

            var query = db.Queryable<Domain.Entities.Integration.Integration>()
                .Where(x => x.TenantId == currentTenantId);

            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(x => x.Name.Contains(name));
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(x => x.Type == type);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Status.ToString() == status);
            }

            return await query.OrderBy(x => x.CreateDate, SqlSugar.OrderByType.Desc).ToListAsync();
        }

        /// <summary>
        /// Get integration by ID (including invalid ones)
        /// </summary>
        public new async Task<Domain.Entities.Integration.Integration> GetByIdAsync(object id, bool copyNew = false, CancellationToken cancellationToken = default)
        {
            var currentTenantId = GetCurrentTenantId();
            db.Ado.CancellationToken = cancellationToken;
            var dbNew = copyNew ? db.CopyNew() : db;
            return await dbNew.Queryable<Domain.Entities.Integration.Integration>()
                .Where(x => x.TenantId == currentTenantId && x.Id == Convert.ToInt64(id))
                .FirstAsync();
        }

        /// <summary>
        /// Get integration by name
        /// </summary>
        public async Task<Domain.Entities.Integration.Integration> GetByNameAsync(string name)
        {
            var currentTenantId = GetCurrentTenantId();
            return await db.Queryable<Domain.Entities.Integration.Integration>()
                .Where(x => x.TenantId == currentTenantId && x.Name == name && x.IsValid)
                .FirstAsync();
        }

        /// <summary>
        /// Check if integration name exists
        /// </summary>
        public async Task<bool> ExistsNameAsync(string name, long? excludeId = null)
        {
            var currentTenantId = GetCurrentTenantId();
            var query = db.Queryable<Domain.Entities.Integration.Integration>()
                .Where(x => x.TenantId == currentTenantId && x.Name == name && x.IsValid);

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// Get integration with all related data
        /// </summary>
        public async Task<Domain.Entities.Integration.Integration> GetWithDetailsAsync(long id)
        {
            var currentTenantId = GetCurrentTenantId();
            var integration = await db.Queryable<Domain.Entities.Integration.Integration>()
                .Where(x => x.TenantId == currentTenantId && x.Id == id)
                .FirstAsync();

            if (integration == null)
            {
                return null;
            }

            // Manually load related data since navigation properties are marked as IsIgnore
            integration.EntityMappings = await db.Queryable<Domain.Entities.Integration.EntityMapping>()
                .Where(x => x.IntegrationId == integration.Id && x.IsValid)
                .ToListAsync();

            // Note: FieldMappings are now associated with Actions, not Integrations

            integration.QuickLinks = await db.Queryable<Domain.Entities.Integration.QuickLink>()
                .Where(x => x.IntegrationId == integration.Id && x.IsValid)
                .ToListAsync();

            return integration;
        }

        /// <summary>
        /// Get integrations by type
        /// </summary>
        public async Task<List<Domain.Entities.Integration.Integration>> GetByTypeAsync(string type)
        {
            var currentTenantId = GetCurrentTenantId();
            return await db.Queryable<Domain.Entities.Integration.Integration>()
                .Where(x => x.TenantId == currentTenantId && x.Type == type && x.IsValid)
                .ToListAsync();
        }

        /// <summary>
        /// Get integrations by status
        /// </summary>
        public async Task<List<Domain.Entities.Integration.Integration>> GetByStatusAsync(string status)
        {
            var currentTenantId = GetCurrentTenantId();
            return await db.Queryable<Domain.Entities.Integration.Integration>()
                .Where(x => x.TenantId == currentTenantId && x.Status.ToString() == status && x.IsValid)
                .ToListAsync();
        }

        /// <summary>
        /// Get all active integrations
        /// </summary>
        public async Task<List<Domain.Entities.Integration.Integration>> GetActiveIntegrationsAsync()
        {
            var currentTenantId = GetCurrentTenantId();
            return await db.Queryable<Domain.Entities.Integration.Integration>()
                .Where(x => x.TenantId == currentTenantId && x.Status == IntegrationStatus.Connected && x.IsValid)
                .ToListAsync();
        }

        /// <summary>
        /// Get integration by system name
        /// </summary>
        public async Task<Domain.Entities.Integration.Integration?> GetBySystemNameAsync(string systemName)
        {
            var currentTenantId = GetCurrentTenantId();
            return await db.Queryable<Domain.Entities.Integration.Integration>()
                .Where(x => x.TenantId == currentTenantId && x.SystemName == systemName && x.IsValid)
                .FirstAsync();
        }

        private string GetCurrentTenantId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null && httpContext.Items.TryGetValue("TenantId", out var tenantId))
            {
                return tenantId?.ToString() ?? "default";
            }
            return "default";
        }
    }
}

