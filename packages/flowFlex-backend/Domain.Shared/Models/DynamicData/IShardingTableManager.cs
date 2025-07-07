using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlowFlex.Domain.Shared.Models.DynamicData;

/// <summary>
/// Sharding table manager interface
/// </summary>
public interface IShardingTableManager
{
    /// <summary>
    /// Get sharding table name
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="moduleId">Module ID</param>
    /// <returns>Sharding table name</returns>
    Task<string> GetShardingTableNameAsync(string tenantId, int moduleId);

    /// <summary>
    /// Ensure sharding table exists
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="moduleId">Module ID</param>
    /// <returns>Whether creation succeeded</returns>
    Task<bool> EnsureShardingTableExistsAsync(string tenantId, int moduleId);

    /// <summary>
    /// Get query table name (supports union queries)
    /// </summary>
    /// <param name="tenantId">Tenant ID, query all tenants when empty</param>
    /// <param name="moduleId">Module ID</param>
    /// <returns>Query table name or union query statement</returns>
    Task<string> GetQueryTableNameAsync(string tenantId, int moduleId);

    /// <summary>
    /// Initialize all sharding tables for tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>Whether initialization succeeded</returns>
    Task<bool> InitializeTenantTablesAsync(string tenantId);

    /// <summary>
    /// Get all existing sharding table information
    /// </summary>
    /// <returns>Sharding table information list</returns>
    Task<List<ShardingTableInfo>> GetAllShardingTablesAsync();
}

/// <summary>
/// Sharding table information
/// </summary>
public class ShardingTableInfo
{
    public string TenantId { get; set; } = string.Empty;
    public int ModuleId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public DateTimeOffset CreateTime { get; set; }
    public long RecordCount { get; set; }
}
