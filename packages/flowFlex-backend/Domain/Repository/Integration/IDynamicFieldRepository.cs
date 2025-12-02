using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository;

namespace FlowFlex.Domain.Repository.Integration
{
    /// <summary>
    /// Dynamic field repository interface
    /// </summary>
    public interface IDynamicFieldRepository : IBaseRepository<DynamicField>
    {
        /// <summary>
        /// Get dynamic field by field ID
        /// </summary>
        Task<DynamicField?> GetByFieldIdAsync(string fieldId);

        /// <summary>
        /// Get all dynamic fields by category
        /// </summary>
        Task<List<DynamicField>> GetByCategoryAsync(string category);

        /// <summary>
        /// Get all dynamic fields ordered by category and sort order
        /// </summary>
        Task<List<DynamicField>> GetAllOrderedAsync();

        /// <summary>
        /// Check if field ID exists
        /// </summary>
        Task<bool> ExistsFieldIdAsync(string fieldId, long? excludeId = null);

        /// <summary>
        /// Get dynamic fields by form property name
        /// </summary>
        Task<DynamicField?> GetByFormPropAsync(string formProp);
    }
}

