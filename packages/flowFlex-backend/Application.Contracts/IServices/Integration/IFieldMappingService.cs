using FlowFlex.Application.Contracts.Dtos.Integration;

namespace FlowFlex.Application.Contracts.IServices.Integration
{
    /// <summary>
    /// Field mapping service interface
    /// </summary>
    public interface IFieldMappingService
    {
        /// <summary>
        /// Create a new field mapping
        /// </summary>
        Task<long> CreateAsync(FieldMappingInputDto input);

        /// <summary>
        /// Update an existing field mapping
        /// </summary>
        Task<bool> UpdateAsync(long id, FieldMappingInputDto input);

        /// <summary>
        /// Delete a field mapping
        /// </summary>
        Task<bool> DeleteAsync(long id);

        /// <summary>
        /// Get field mapping by ID
        /// </summary>
        Task<FieldMappingOutputDto> GetByIdAsync(long id);

        /// <summary>
        /// Get all field mappings for an entity mapping
        /// </summary>
        Task<List<FieldMappingOutputDto>> GetByEntityMappingIdAsync(long entityMappingId);

        /// <summary>
        /// Get all field mappings for an integration
        /// </summary>
        Task<List<FieldMappingOutputDto>> GetByIntegrationIdAsync(long integrationId);

        /// <summary>
        /// Batch update field mappings
        /// </summary>
        Task<bool> BatchUpdateAsync(List<FieldMappingInputDto> inputs);

        /// <summary>
        /// Get bidirectional (editable) field mappings for an entity mapping
        /// </summary>
        Task<List<FieldMappingOutputDto>> GetBidirectionalMappingsAsync(long entityMappingId);

        /// <summary>
        /// Get field mappings by integration ID and action ID
        /// </summary>
        Task<List<FieldMappingOutputDto>> GetByIntegrationIdAndActionIdAsync(long integrationId, long actionId);
    }
}
