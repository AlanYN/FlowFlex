using Microsoft.Extensions.Logging;
using SqlSugar;
using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Shared;
using Microsoft.AspNetCore.Http;

namespace FlowFlex.SqlSugarDB.Implements.Integration
{
    /// <summary>
    /// Inbound field mapping repository implementation
    /// </summary>
    public class InboundFieldMappingRepository : BaseRepository<InboundFieldMapping>, IInboundFieldMappingRepository, IScopedService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<InboundFieldMappingRepository> _logger;

        public InboundFieldMappingRepository(
            ISqlSugarClient sqlSugarClient,
            IHttpContextAccessor httpContextAccessor,
            ILogger<InboundFieldMappingRepository> logger) : base(sqlSugarClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Get field mappings by integration ID
        /// </summary>
        public async Task<List<InboundFieldMapping>> GetByIntegrationIdAsync(long integrationId)
        {
            return await db.Queryable<InboundFieldMapping>()
                .Where(x => x.IntegrationId == integrationId && x.IsValid)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();
        }

        /// <summary>
        /// Get field mappings by action ID
        /// </summary>
        public async Task<List<InboundFieldMapping>> GetByActionIdAsync(long actionId)
        {
            return await db.Queryable<InboundFieldMapping>()
                .Where(x => x.ActionId == actionId && x.IsValid)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();
        }

        /// <summary>
        /// Get field mappings by integration ID and action ID
        /// </summary>
        public async Task<List<InboundFieldMapping>> GetByIntegrationIdAndActionIdAsync(long integrationId, long actionId)
        {
            return await db.Queryable<InboundFieldMapping>()
                .Where(x => x.IntegrationId == integrationId
                    && x.ActionId == actionId
                    && x.IsValid)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();
        }

        /// <summary>
        /// Check if field mapping exists by external field name
        /// </summary>
        public async Task<bool> ExistsAsync(long integrationId, long actionId, string externalFieldName, long? excludeId = null)
        {
            var query = db.Queryable<InboundFieldMapping>()
                .Where(x => x.IntegrationId == integrationId
                    && x.ActionId == actionId
                    && x.ExternalFieldName == externalFieldName
                    && x.IsValid);

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// Delete field mappings by integration ID
        /// </summary>
        public async Task<bool> DeleteByIntegrationIdAsync(long integrationId)
        {
            var mappings = await db.Queryable<InboundFieldMapping>()
                .Where(x => x.IntegrationId == integrationId)
                .ToListAsync();

            if (mappings.Any())
            {
                foreach (var mapping in mappings)
                {
                    mapping.IsValid = false;
                }
                return await db.Updateable(mappings).ExecuteCommandAsync() > 0;
            }

            return true;
        }

        /// <summary>
        /// Delete field mappings by action ID
        /// </summary>
        public async Task<bool> DeleteByActionIdAsync(long actionId)
        {
            var mappings = await db.Queryable<InboundFieldMapping>()
                .Where(x => x.ActionId == actionId)
                .ToListAsync();

            if (mappings.Any())
            {
                foreach (var mapping in mappings)
                {
                    mapping.IsValid = false;
                }
                return await db.Updateable(mappings).ExecuteCommandAsync() > 0;
            }

            return true;
        }
    }
}

