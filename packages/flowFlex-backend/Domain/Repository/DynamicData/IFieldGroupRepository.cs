using FlowFlex.Domain.Entities.DynamicData;

namespace FlowFlex.Domain.Repository.DynamicData;

/// <summary>
/// Field group repository interface
/// </summary>
public interface IFieldGroupRepository : IBaseRepository<FieldGroup>
{
    /// <summary>
    /// Get all groups
    /// </summary>
    Task<List<FieldGroup>> GetAllAsync();

    /// <summary>
    /// Get group by ID
    /// </summary>
    Task<FieldGroup?> GetByIdAsync(long groupId);

    /// <summary>
    /// Get default group
    /// </summary>
    Task<FieldGroup?> GetDefaultGroupAsync();

    /// <summary>
    /// Check if group name exists
    /// </summary>
    Task<bool> ExistsGroupNameAsync(string groupName, long? excludeId = null);

    /// <summary>
    /// Update group fields
    /// </summary>
    Task UpdateGroupFieldsAsync(long groupId, long[] fieldIds);

    /// <summary>
    /// Batch update group sort order
    /// </summary>
    Task BatchUpdateSortAsync(Dictionary<long, int> groupSorts);
}
