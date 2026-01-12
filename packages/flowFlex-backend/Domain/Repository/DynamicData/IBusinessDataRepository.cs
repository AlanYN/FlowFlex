using FlowFlex.Domain.Entities.DynamicData;
using FlowFlex.Domain.Shared.Models.DynamicData;

namespace FlowFlex.Domain.Repository.DynamicData;

/// <summary>
/// Business data repository interface
/// </summary>
public interface IBusinessDataRepository : IBaseRepository<BusinessData>
{
    /// <summary>
    /// Get business data by ID with all field values
    /// </summary>
    Task<DynamicDataObject?> GetBusinessDataObjectAsync(long businessId);

    /// <summary>
    /// Get business data list by IDs
    /// </summary>
    Task<List<DynamicDataObject>> GetBusinessDataObjectListAsync(List<long> businessIds);

    /// <summary>
    /// Create business data with field values
    /// </summary>
    Task<long> CreateBusinessDataAsync(DynamicDataObject data);

    /// <summary>
    /// Update business data with field values
    /// </summary>
    Task UpdateBusinessDataAsync(DynamicDataObject data);

    /// <summary>
    /// Update specific fields of business data
    /// </summary>
    Task UpdateBusinessDataFieldsAsync(long businessId, Dictionary<string, object?> fields);

    /// <summary>
    /// Delete business data (soft delete)
    /// </summary>
    Task DeleteBusinessDataAsync(long businessId);

    /// <summary>
    /// Batch delete business data
    /// </summary>
    Task BatchDeleteBusinessDataAsync(List<long> businessIds);
}
