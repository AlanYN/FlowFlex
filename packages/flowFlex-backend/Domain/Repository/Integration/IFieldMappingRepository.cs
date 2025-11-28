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
        /// Get field mappings by action ID
        /// </summary>
        Task<List<InboundFieldMapping>> GetByActionIdAsync(long actionId);

        /// <summary>
        /// Check if field mapping exists by external field name
        /// </summary>
        Task<bool> ExistsAsync(long actionId, string externalFieldName, long? excludeId = null);

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
