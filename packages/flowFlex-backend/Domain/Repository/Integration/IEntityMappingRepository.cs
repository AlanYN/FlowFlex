using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository;

namespace FlowFlex.Domain.Repository.Integration
{
    /// <summary>
    /// Entity mapping repository interface
    /// </summary>
    public interface IEntityMappingRepository : IBaseRepository<EntityMapping>
    {
        /// <summary>
        /// Get entity mappings by integration ID
        /// </summary>
        Task<List<EntityMapping>> GetByIntegrationIdAsync(long integrationId);

        /// <summary>
        /// Get entity mapping by integration ID and external entity type
        /// </summary>
        Task<EntityMapping> GetByExternalEntityTypeAsync(long integrationId, string externalEntityType);

        /// <summary>
        /// Get entity mapping by integration ID and WFE master data type
        /// </summary>
        Task<EntityMapping> GetByWfeMasterDataTypeAsync(long integrationId, string wfeMasterDataType);

        /// <summary>
        /// Check if entity mapping exists
        /// </summary>
        Task<bool> ExistsAsync(long integrationId, string externalEntityType, long? excludeId = null);

        /// <summary>
        /// Delete entity mappings by integration ID
        /// </summary>
        Task<bool> DeleteByIntegrationIdAsync(long integrationId);

        /// <summary>
        /// Get entity mapping by System ID
        /// </summary>
        Task<EntityMapping?> GetBySystemIdAsync(string systemId);
    }
}

