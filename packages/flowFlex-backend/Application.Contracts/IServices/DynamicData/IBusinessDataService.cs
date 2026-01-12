using FlowFlex.Domain.Shared.Models.DynamicData;

namespace FlowFlex.Application.Contracts.IServices.DynamicData;

/// <summary>
/// Business data service interface
/// </summary>
public interface IBusinessDataService
{
    /// <summary>
    /// Get business data object by ID
    /// </summary>
    Task<DynamicDataObject?> GetBusinessDataObjectAsync(long businessId);

    /// <summary>
    /// Get business data object list by IDs
    /// </summary>
    Task<List<DynamicDataObject>> GetBusinessDataObjectListAsync(List<long> businessIds);

    /// <summary>
    /// Create business data
    /// </summary>
    Task<long> CreateBusinessDataAsync(DynamicDataObject data);

    /// <summary>
    /// Update business data
    /// </summary>
    Task UpdateBusinessDataAsync(DynamicDataObject data);

    /// <summary>
    /// Update specific fields of business data
    /// </summary>
    Task UpdateBusinessDataAsync(long businessId, Dictionary<string, object?> fields);

    /// <summary>
    /// Delete business data
    /// </summary>
    Task DeleteBusinessDataAsync(long businessId);

    /// <summary>
    /// Batch delete business data
    /// </summary>
    Task BatchDeleteBusinessDataAsync(List<long> businessIds);

    /// <summary>
    /// Convert object to business data
    /// </summary>
    DynamicDataObject ObjectToBusinessData(object obj);
}
