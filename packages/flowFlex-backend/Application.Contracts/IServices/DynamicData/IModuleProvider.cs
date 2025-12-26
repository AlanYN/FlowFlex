using FlowFlex.Domain.Shared.Models.DynamicData;

namespace FlowFlex.Application.Contracts.IServices.DynamicData;

/// <summary>
/// Module provider interface - provides module-specific business logic
/// </summary>
public interface IModuleProvider
{
    /// <summary>
    /// Module ID
    /// </summary>
    int ModuleId { get; }

    /// <summary>
    /// Get business data by ID
    /// </summary>
    Task<DynamicDataObject?> GetBusinessDataAsync(long id);

    /// <summary>
    /// Get business data list by IDs
    /// </summary>
    Task<List<DynamicDataObject>> GetBusinessDataListAsync(List<long> ids);

    /// <summary>
    /// Create business data
    /// </summary>
    Task<long> CreateBusinessDataAsync(DynamicDataObject dynamicDataObject);

    /// <summary>
    /// Update business data
    /// </summary>
    Task UpdateBusinessDataAsync(DynamicDataObject data);

    /// <summary>
    /// Delete business data
    /// </summary>
    Task DeleteBusinessDataAsync(long businessId);

    /// <summary>
    /// Get property list
    /// </summary>
    Task<List<DefineFieldDto>> GetPropertyListAsync();

    /// <summary>
    /// Add property
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
    /// Before create hook
    /// </summary>
    Task<DynamicDataObject> BeforeCreateAsync(DynamicDataObject data);

    /// <summary>
    /// After create hook
    /// </summary>
    Task AfterCreateAsync(long businessId, DynamicDataObject data);

    /// <summary>
    /// Before update hook
    /// </summary>
    Task<DynamicDataObject> BeforeUpdateAsync(DynamicDataObject data);

    /// <summary>
    /// After update hook
    /// </summary>
    Task AfterUpdateAsync(DynamicDataObject data);

    /// <summary>
    /// Before delete hook
    /// </summary>
    Task BeforeDeleteAsync(long businessId);

    /// <summary>
    /// After delete hook
    /// </summary>
    Task AfterDeleteAsync(long businessId);
}
