using FlowFlex.Domain.Shared.Models.DynamicData;

namespace FlowFlex.Application.Contracts.IServices.DynamicData;

/// <summary>
/// Property group service interface
/// </summary>
public interface IPropertyGroupService
{
    /// <summary>
    /// Get all groups
    /// </summary>
    Task<List<PropertyGroupDto>> GetGroupListAsync();

    /// <summary>
    /// Get group by ID
    /// </summary>
    Task<PropertyGroupDto?> GetGroupByIdAsync(long groupId);

    /// <summary>
    /// Add new group
    /// </summary>
    Task<long> AddGroupAsync(PropertyGroupDto groupDto);

    /// <summary>
    /// Update group
    /// </summary>
    Task UpdateGroupAsync(PropertyGroupDto groupDto);

    /// <summary>
    /// Delete group
    /// </summary>
    Task DeleteGroupAsync(long groupId);

    /// <summary>
    /// Update group sort order
    /// </summary>
    Task UpdateGroupSortAsync(Dictionary<long, int> groupSorts);

    /// <summary>
    /// Get groups with fields
    /// </summary>
    Task<List<PropertyGroupDto>> GetGroupsWithFieldsAsync();
}
