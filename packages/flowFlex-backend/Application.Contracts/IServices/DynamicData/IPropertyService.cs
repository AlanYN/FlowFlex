using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Models.DynamicData;

namespace FlowFlex.Application.Contracts.IServices.DynamicData;

/// <summary>
/// Property (field definition) service interface
/// </summary>
public interface IPropertyService
{
    /// <summary>
    /// Get all properties
    /// </summary>
    /// <param name="workflowId">Optional workflow ID to check if properties are used in workflow stages</param>
    Task<List<DefineFieldDto>> GetPropertyListAsync(long? workflowId = null);

    /// <summary>
    /// Get properties with pagination and filters
    /// </summary>
    Task<PagedResult<DefineFieldDto>> GetPropertyPagedListAsync(PropertyQueryRequest request);

    /// <summary>
    /// Export properties to Excel
    /// </summary>
    Task<Stream> ExportToExcelAsync(PropertyQueryRequest request);

    /// <summary>
    /// Get property by ID
    /// </summary>
    Task<DefineFieldDto?> GetPropertyByIdAsync(long propertyId);

    /// <summary>
    /// Get property by name
    /// </summary>
    Task<DefineFieldDto?> GetPropertyByNameAsync(string propertyName);

    /// <summary>
    /// Add new property
    /// </summary>
    Task<long> AddPropertyAsync(DefineFieldDto defineFieldDto);

    /// <summary>
    /// Update property
    /// </summary>
    Task UpdatePropertyAsync(DefineFieldDto defineFieldDto);

    /// <summary>
    /// Delete property
    /// </summary>
    Task DeletePropertyAsync(long propertyId);

    /// <summary>
    /// Move properties to group
    /// </summary>
    Task MovePropertyToGroupAsync(long[] propertyIds, long groupId);

    /// <summary>
    /// Update property sort order
    /// </summary>
    Task UpdatePropertySortAsync(Dictionary<long, int> propertySorts);

    /// <summary>
    /// Initialize default properties from static-field.json
    /// </summary>
    Task<bool> InitializeDefaultPropertiesAsync();

    /// <summary>
    /// Get properties by IDs
    /// </summary>
    Task<List<DefineFieldDto>> GetPropertiesByIdsAsync(IEnumerable<long> ids);
}
