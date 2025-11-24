using Microsoft.Extensions.Logging;
using SqlSugar;
using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Shared;
using Microsoft.AspNetCore.Http;
using Domain.Shared.Enums;

namespace FlowFlex.SqlSugarDB.Implements.Integration
{
    /// <summary>
    /// Field mapping repository implementation
    /// </summary>
    public class FieldMappingRepository : BaseRepository<FieldMapping>, IFieldMappingRepository, IScopedService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<FieldMappingRepository> _logger;

        public FieldMappingRepository(
            ISqlSugarClient sqlSugarClient,
            IHttpContextAccessor httpContextAccessor,
            ILogger<FieldMappingRepository> logger) : base(sqlSugarClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Get field mappings by integration ID
        /// </summary>
        public async Task<List<FieldMapping>> GetByIntegrationIdAsync(long integrationId)
        {
            return await db.Queryable<FieldMapping>()
                .Where(x => x.IntegrationId == integrationId && x.IsValid)
                .ToListAsync();
        }

        /// <summary>
        /// Get field mappings by entity mapping ID
        /// </summary>
        public async Task<List<FieldMapping>> GetByEntityMappingIdAsync(long entityMappingId)
        {
            return await db.Queryable<FieldMapping>()
                .Where(x => x.EntityMappingId == entityMappingId && x.IsValid)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();
        }

        /// <summary>
        /// Get field mapping by external field name
        /// </summary>
        public async Task<FieldMapping> GetByExternalFieldAsync(long entityMappingId, string externalFieldName)
        {
            return await db.Queryable<FieldMapping>()
                .Where(x => x.EntityMappingId == entityMappingId && x.ExternalFieldName == externalFieldName && x.IsValid)
                .FirstAsync();
        }

        /// <summary>
        /// Get field mapping by WFE field name
        /// </summary>
        public async Task<FieldMapping> GetByWfeFieldAsync(long entityMappingId, string wfeFieldName)
        {
            return await db.Queryable<FieldMapping>()
                .Where(x => x.EntityMappingId == entityMappingId && x.WfeFieldId == wfeFieldName && x.IsValid)
                .FirstAsync();
        }

        /// <summary>
        /// Check if field mapping exists
        /// </summary>
        public async Task<bool> ExistsAsync(long entityMappingId, string externalFieldName, long? excludeId = null)
        {
            var query = db.Queryable<FieldMapping>()
                .Where(x => x.EntityMappingId == entityMappingId && x.ExternalFieldName == externalFieldName && x.IsValid);

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
            var mappings = await db.Queryable<FieldMapping>()
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
        /// Delete field mappings by entity mapping ID
        /// </summary>
        public async Task<bool> DeleteByEntityMappingIdAsync(long entityMappingId)
        {
            var mappings = await db.Queryable<FieldMapping>()
                .Where(x => x.EntityMappingId == entityMappingId)
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
        /// Get bidirectional field mappings
        /// </summary>
        public async Task<List<FieldMapping>> GetBidirectionalMappingsAsync(long entityMappingId)
        {
            return await db.Queryable<FieldMapping>()
                .Where(x => x.EntityMappingId == entityMappingId 
                    && x.SyncDirection == SyncDirection.Editable 
                    && x.IsValid)
                .ToListAsync();
        }
    }
}

