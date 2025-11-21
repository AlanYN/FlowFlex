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

            var orderByClause = sortDirection.ToLower() == "asc" 
                ? $"{sortField} ASC" 
                : $"{sortField} DESC";

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
            return await db.Queryable<Domain.Entities.Integration.Integration>()
                .Includes(x => x.EntityMappings)
                .Includes(x => x.FieldMappings)
                .Includes(x => x.QuickLinks)
                .Includes(x => x.InboundConfig)
                .Includes(x => x.OutboundConfig)
                .Where(x => x.TenantId == currentTenantId && x.Id == id && x.IsValid)
                .FirstAsync();
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

