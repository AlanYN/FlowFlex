using Microsoft.Extensions.Logging;
using SqlSugar;
using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Shared;
using Microsoft.AspNetCore.Http;

namespace FlowFlex.SqlSugarDB.Implements.Integration
{
    /// <summary>
    /// Entity mapping repository implementation
    /// </summary>
    public class EntityMappingRepository : BaseRepository<EntityMapping>, IEntityMappingRepository, IScopedService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<EntityMappingRepository> _logger;

        public EntityMappingRepository(
            ISqlSugarClient sqlSugarClient,
            IHttpContextAccessor httpContextAccessor,
            ILogger<EntityMappingRepository> logger) : base(sqlSugarClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Get entity mappings by integration ID
        /// </summary>
        public async Task<List<EntityMapping>> GetByIntegrationIdAsync(long integrationId)
        {
            return await db.Queryable<EntityMapping>()
                .Where(x => x.IntegrationId == integrationId && x.IsValid)
                .ToListAsync();
        }

        /// <summary>
        /// Get entity mapping by integration ID and external entity type
        /// </summary>
        public async Task<EntityMapping> GetByExternalEntityTypeAsync(long integrationId, string externalEntityType)
        {
            return await db.Queryable<EntityMapping>()
                .Where(x => x.IntegrationId == integrationId && x.ExternalEntityType == externalEntityType && x.IsValid)
                .FirstAsync();
        }

        /// <summary>
        /// Get entity mapping by integration ID and WFE master data type
        /// </summary>
        public async Task<EntityMapping> GetByWfeMasterDataTypeAsync(long integrationId, string wfeMasterDataType)
        {
            return await db.Queryable<EntityMapping>()
                .Where(x => x.IntegrationId == integrationId && x.WfeEntityType == wfeMasterDataType && x.IsValid)
                .FirstAsync();
        }

        /// <summary>
        /// Check if entity mapping exists
        /// </summary>
        public async Task<bool> ExistsAsync(long integrationId, string externalEntityType, long? excludeId = null)
        {
            var query = db.Queryable<EntityMapping>()
                .Where(x => x.IntegrationId == integrationId && x.ExternalEntityType == externalEntityType && x.IsValid);

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// Delete entity mappings by integration ID
        /// </summary>
        public async Task<bool> DeleteByIntegrationIdAsync(long integrationId)
        {
            var mappings = await db.Queryable<EntityMapping>()
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
        /// Get entity mapping by System ID
        /// </summary>
        public async Task<EntityMapping?> GetBySystemIdAsync(string systemId)
        {
            return await db.Queryable<EntityMapping>()
                .Where(x => x.SystemId == systemId && x.IsValid && x.IsActive)
                .FirstAsync();
        }
    }
}

