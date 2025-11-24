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
        /// Query integrations with pagination
        /// </summary>
        public async Task<(List<Domain.Entities.Integration.Integration> items, int total)> QueryPagedAsync(
            int pageIndex,
            int pageSize,
            string name = null,
            string type = null,
            string status = null,
            string sortField = "CreateDate",
            string sortDirection = "desc")
        {
            var currentTenantId = GetCurrentTenantId();
            _logger.LogInformation($"[IntegrationRepository] QueryPagedAsync with TenantId={currentTenantId}");

            var whereExpressions = new List<Expression<Func<Domain.Entities.Integration.Integration, bool>>>();
            whereExpressions.Add(x => x.IsValid == true);
            whereExpressions.Add(x => x.TenantId == currentTenantId);

            if (!string.IsNullOrWhiteSpace(name))
            {
                whereExpressions.Add(x => x.Name.Contains(name));
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                whereExpressions.Add(x => x.Type == type);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                whereExpressions.Add(x => x.Status.ToString() == status);
            }

            var query = db.Queryable<Domain.Entities.Integration.Integration>();

            foreach (var expr in whereExpressions)
            {
                query = query.Where(expr);
            }

            var total = await query.CountAsync();

            // Convert sortField from PascalCase to snake_case for database column name
            var dbSortField = SqlSugar.UtilMethods.ToUnderLine(sortField);
            var orderByClause = sortDirection.ToLower() == "asc" 
                ? $"{dbSortField} ASC" 
                : $"{dbSortField} DESC";

            var items = await query
                .OrderBy(orderByClause)
                .ToPageListAsync(pageIndex, pageSize);

            return (items, total);
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
                .Where(x => x.TenantId == currentTenantId && x.Id == id && x.IsValid)
                .FirstAsync();

            if (integration == null)
            {
                return null;
            }

            // Manually load related data since navigation properties are marked as IsIgnore
            integration.EntityMappings = await db.Queryable<Domain.Entities.Integration.EntityMapping>()
                .Where(x => x.IntegrationId == integration.Id && x.IsValid)
                .ToListAsync();

            integration.FieldMappings = await db.Queryable<Domain.Entities.Integration.FieldMapping>()
                .Where(x => x.IntegrationId == integration.Id && x.IsValid)
                .ToListAsync();

            integration.QuickLinks = await db.Queryable<Domain.Entities.Integration.QuickLink>()
                .Where(x => x.IntegrationId == integration.Id && x.IsValid)
                .ToListAsync();

            // Load first inbound config (for backward compatibility)
            integration.InboundConfig = await db.Queryable<Domain.Entities.Integration.InboundConfiguration>()
                .Where(x => x.IntegrationId == integration.Id && x.IsValid)
                .FirstAsync();

            // Load first outbound config (for backward compatibility)
            integration.OutboundConfig = await db.Queryable<Domain.Entities.Integration.OutboundConfiguration>()
                .Where(x => x.IntegrationId == integration.Id && x.IsValid)
                .FirstAsync();

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

        private string GetCurrentTenantId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null && httpContext.Items.TryGetValue("TenantId", out var tenantId))
            {
                return tenantId?.ToString() ?? "DEFAULT";
            }
            return "DEFAULT";
        }
    }
}

