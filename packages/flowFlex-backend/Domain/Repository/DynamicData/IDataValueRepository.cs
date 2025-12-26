using FlowFlex.Domain.Entities.DynamicData;

namespace FlowFlex.Domain.Repository.DynamicData;

/// <summary>
/// Data value repository interface
/// </summary>
public interface IDataValueRepository : IBaseRepository<DataValue>
{
    /// <summary>
    /// Get all data values for a business data
    /// </summary>
    Task<List<DataValue>> GetByBusinessIdAsync(long businessId);

    /// <summary>
    /// Get data values for multiple business data
    /// </summary>
    Task<List<DataValue>> GetByBusinessIdsAsync(List<long> businessIds);

    /// <summary>
    /// Get data value by business ID and field name
    /// </summary>
    Task<DataValue?> GetByBusinessIdAndFieldNameAsync(long businessId, string fieldName);

    /// <summary>
    /// Batch insert data values
    /// </summary>
    Task BatchInsertAsync(List<DataValue> dataValues);

    /// <summary>
    /// Batch update data values
    /// </summary>
    Task BatchUpdateAsync(List<DataValue> dataValues);

    /// <summary>
    /// Delete data values by business ID
    /// </summary>
    Task DeleteByBusinessIdAsync(long businessId);

    /// <summary>
    /// Delete data values by business IDs
    /// </summary>
    Task DeleteByBusinessIdsAsync(List<long> businessIds);
}
