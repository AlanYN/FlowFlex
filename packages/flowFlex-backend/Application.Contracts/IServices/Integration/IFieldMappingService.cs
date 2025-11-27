using FlowFlex.Application.Contracts.Dtos.Integration;

namespace FlowFlex.Application.Contracts.IServices.Integration
{
    /// <summary>
    /// Inbound field mapping service interface
    /// </summary>
    public interface IInboundFieldMappingService
    {
        /// <summary>
        /// Create a new field mapping
        /// </summary>
        Task<long> CreateAsync(InboundFieldMappingInputDto input);

        /// <summary>
        /// Update an existing field mapping
        /// </summary>
        Task<bool> UpdateAsync(long id, InboundFieldMappingInputDto input);

        /// <summary>
        /// Delete a field mapping
        /// </summary>
        Task<bool> DeleteAsync(long id);

        /// <summary>
        /// Get field mapping by ID
        /// </summary>
        Task<InboundFieldMappingOutputDto> GetByIdAsync(long id);

        /// <summary>
        /// Get all field mappings for an integration
        /// </summary>
        Task<List<InboundFieldMappingOutputDto>> GetByIntegrationIdAsync(long integrationId);

        /// <summary>
        /// Get field mappings by action ID
        /// </summary>
        Task<List<InboundFieldMappingOutputDto>> GetByActionIdAsync(long actionId);

        /// <summary>
        /// Get field mappings by integration ID and action ID
        /// </summary>
        Task<List<InboundFieldMappingOutputDto>> GetByIntegrationIdAndActionIdAsync(long integrationId, long actionId);

        /// <summary>
        /// Batch update field mappings
        /// </summary>
        Task<bool> BatchUpdateAsync(List<InboundFieldMappingInputDto> inputs);
    }

        /// <summary>
    /// Backward compatibility alias
        /// </summary>
    [Obsolete("Use IInboundFieldMappingService instead")]
    public interface IFieldMappingService : IInboundFieldMappingService { }
}
