using FlowFlex.Domain.Entities.DynamicData;

namespace FlowFlex.Domain.Repository.DynamicData;

/// <summary>
/// Define field repository interface
/// </summary>
public interface IDefineFieldRepository : IBaseRepository<DefineField>
{
    /// <summary>
    /// Get all fields
    /// </summary>
    Task<List<DefineField>> GetAllAsync();

    /// <summary>
    /// Get field by field name
    /// </summary>
    Task<DefineField?> GetByFieldNameAsync(string fieldName);

    /// <summary>
    /// Get field by ID
    /// </summary>
    Task<DefineField?> GetByIdAsync(long fieldId);

    /// <summary>
    /// Check if field name exists
    /// </summary>
    Task<bool> ExistsFieldNameAsync(string fieldName, long? excludeId = null);

    /// <summary>
    /// Get fields by group ID
    /// </summary>
    Task<List<DefineField>> GetByGroupIdAsync(long groupId);

    /// <summary>
    /// Batch update field sort order
    /// </summary>
    Task BatchUpdateSortAsync(Dictionary<long, int> fieldSorts);
}
