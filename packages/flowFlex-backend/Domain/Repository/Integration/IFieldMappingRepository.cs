using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository;

namespace FlowFlex.Domain.Repository.Integration
{
    /// <summary>
    /// Inbound field mapping repository interface
    /// </summary>
    public interface IInboundFieldMappingRepository : IBaseRepository<InboundFieldMapping>
    {
        /// <summary>
        /// Get field mappings by integration ID
        /// </summary>
        Task<List<InboundFieldMapping>> GetByIntegrationIdAsync(long integrationId);

        /// <summary>
        /// Get field mappings by action ID
        /// </summary>
        Task<List<InboundFieldMapping>> GetByActionIdAsync(long actionId);

        /// <summary>
        /// Get field mappings by integration ID and action ID
        /// </summary>
        Task<List<InboundFieldMapping>> GetByIntegrationIdAndActionIdAsync(long integrationId, long actionId);

        /// <summary>
        /// Check if field mapping exists by external field name
        /// </summary>
        Task<bool> ExistsAsync(long integrationId, long actionId, string externalFieldName, long? excludeId = null);

        /// <summary>
        /// Delete field mappings by integration ID
        /// </summary>
        Task<bool> DeleteByIntegrationIdAsync(long integrationId);

        /// <summary>
        /// Delete field mappings by action ID
        /// </summary>
        Task<bool> DeleteByActionIdAsync(long actionId);
    }

    /// <summary>
    /// Backward compatibility alias
    /// </summary>
    [Obsolete("Use IInboundFieldMappingRepository instead")]
    public interface IFieldMappingRepository : IInboundFieldMappingRepository { }
}

