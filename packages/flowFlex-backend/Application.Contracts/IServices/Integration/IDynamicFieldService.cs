using FlowFlex.Application.Contracts.Dtos.Integration;

namespace FlowFlex.Application.Contracts.IServices.Integration
{
    /// <summary>
    /// Dynamic field service interface
    /// </summary>
    public interface IDynamicFieldService
    {
        /// <summary>
        /// Create a new dynamic field
        /// </summary>
        Task<long> CreateAsync(DynamicFieldInputDto input);

        /// <summary>
        /// Update an existing dynamic field
        /// </summary>
        Task<bool> UpdateAsync(long id, DynamicFieldInputDto input);

        /// <summary>
        /// Delete a dynamic field (cannot delete system fields)
        /// </summary>
        Task<bool> DeleteAsync(long id);

        /// <summary>
        /// Get dynamic field by ID
        /// </summary>
        Task<DynamicFieldOutputDto> GetByIdAsync(long id);

        /// <summary>
        /// Get dynamic field by field ID
        /// </summary>
        Task<DynamicFieldOutputDto?> GetByFieldIdAsync(string fieldId);

        /// <summary>
        /// Get all dynamic fields
        /// </summary>
        Task<List<DynamicFieldOutputDto>> GetAllAsync();

        /// <summary>
        /// Get dynamic fields by category
        /// </summary>
        Task<List<DynamicFieldOutputDto>> GetByCategoryAsync(string category);

        /// <summary>
        /// Initialize default fields from static-field.json (only for current tenant)
        /// </summary>
        Task<bool> InitializeDefaultFieldsAsync();
    }
}

