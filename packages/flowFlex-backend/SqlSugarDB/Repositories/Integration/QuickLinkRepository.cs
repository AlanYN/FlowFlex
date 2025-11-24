using Microsoft.Extensions.Logging;
using SqlSugar;
using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Shared;
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
        /// Get quick links by integration ID
        /// </summary>
        public async Task<List<QuickLink>> GetByIntegrationIdAsync(long integrationId)
        {
            return await db.Queryable<QuickLink>()
                .Where(x => x.IntegrationId == integrationId && x.IsValid && x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();
        }

        /// <summary>
        /// Check if quick link label exists
        /// </summary>
        public async Task<bool> ExistsLabelAsync(long integrationId, string linkName, long? excludeId = null)
        {
            var query = db.Queryable<QuickLink>()
                .Where(x => x.IntegrationId == integrationId && x.LinkName == linkName && x.IsValid);

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
            return await db.Queryable<QuickLink>()
                .Where(x => x.IntegrationId == integrationId && x.LinkName == label && x.IsValid)
                .FirstAsync();
        }

        /// <summary>
        /// Delete quick links by integration ID
        /// </summary>
        public async Task<bool> DeleteByIntegrationIdAsync(long integrationId)
        {
            var links = await db.Queryable<QuickLink>()
                .Where(x => x.IntegrationId == integrationId)
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

            foreach (var (id, displayOrder) in orders)
            {
                await db.Updateable<QuickLink>()
                    .SetColumns(x => x.SortOrder == displayOrder)
                    .Where(x => x.Id == id)
                    .ExecuteCommandAsync();
            }

            return true;
        }
    }
}

