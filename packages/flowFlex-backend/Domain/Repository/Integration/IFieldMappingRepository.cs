using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository;

namespace FlowFlex.Domain.Repository.Integration
{
    /// <summary>
    /// Field mapping repository interface
    /// </summary>
    public interface IFieldMappingRepository : IBaseRepository<FieldMapping>
    {
        /// <summary>
        /// Get field mappings by integration ID
        /// </summary>
        Task<List<FieldMapping>> GetByIntegrationIdAsync(long integrationId);

        /// <summary>
        /// Get field mappings by entity mapping ID
        /// </summary>
        Task<List<FieldMapping>> GetByEntityMappingIdAsync(long entityMappingId);

        /// <summary>
        /// Get field mapping by external field name
        /// </summary>
        Task<FieldMapping> GetByExternalFieldAsync(long entityMappingId, string externalFieldName);

        /// <summary>
        /// Get field mapping by WFE field name
        /// </summary>
        Task<FieldMapping> GetByWfeFieldAsync(long entityMappingId, string wfeFieldName);

        /// <summary>
        /// Check if field mapping exists
        /// </summary>
        Task<bool> ExistsAsync(long entityMappingId, string externalFieldName, long? excludeId = null);

        /// <summary>
        /// Delete field mappings by integration ID
        /// </summary>
        Task<bool> DeleteByIntegrationIdAsync(long integrationId);

        /// <summary>
        /// Delete field mappings by entity mapping ID
        /// </summary>
        Task<bool> DeleteByEntityMappingIdAsync(long entityMappingId);

        /// <summary>
        /// Get bidirectional field mappings
        /// </summary>
        Task<List<FieldMapping>> GetBidirectionalMappingsAsync(long entityMappingId);

        /// <summary>
        /// Get field mappings by integration ID and action ID
        /// </summary>
        Task<List<FieldMapping>> GetByIntegrationIdAndActionIdAsync(long integrationId, long actionId);
    }
}

