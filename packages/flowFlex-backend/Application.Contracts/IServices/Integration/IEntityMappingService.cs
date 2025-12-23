using FlowFlex.Application.Contracts.Dtos.Integration;

namespace FlowFlex.Application.Contracts.IServices.Integration
{
    /// <summary>
    /// Entity mapping service interface
    /// </summary>
    public interface IEntityMappingService
    {
        /// <summary>
        /// Create a new entity mapping
        /// </summary>
        Task<long> CreateAsync(EntityMappingInputDto input);

        /// <summary>
        /// Update an existing entity mapping
        /// </summary>
        Task<bool> UpdateAsync(long id, EntityMappingInputDto input);

        /// <summary>
        /// Delete an entity mapping
        /// </summary>
        Task<bool> DeleteAsync(long id);

        /// <summary>
        /// Get entity mapping by ID
        /// </summary>
        Task<EntityMappingOutputDto> GetByIdAsync(long id);

        /// <summary>
        /// Get all entity mappings for an integration
        /// </summary>
        Task<List<EntityMappingOutputDto>> GetByIntegrationIdAsync(long integrationId);

        /// <summary>
        /// Get paginated list of entity mappings
        /// </summary>
        Task<(List<EntityMappingOutputDto> items, int total)> GetPagedListAsync(
            long integrationId,
            int pageIndex,
            int pageSize);

        /// <summary>
        /// Batch save entity mappings (create, update, delete in one operation)
        /// </summary>
        /// <param name="input">Batch save input containing items to create/update and IDs to delete</param>
        /// <returns>Result containing counts and saved items</returns>
        Task<EntityMappingBatchSaveResultDto> BatchSaveAsync(EntityMappingBatchSaveDto input);
    }
}
