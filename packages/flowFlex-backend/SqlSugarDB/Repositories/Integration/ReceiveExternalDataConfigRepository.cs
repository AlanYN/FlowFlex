using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Shared;
using Microsoft.AspNetCore.Http;
using SqlSugar;

namespace FlowFlex.SqlSugarDB.Repositories.Integration;

/// <summary>
/// Repository implementation for ReceiveExternalDataConfig
/// </summary>
public class ReceiveExternalDataConfigRepository : BaseRepository<ReceiveExternalDataConfig>, IReceiveExternalDataConfigRepository, IScopedService
{
    public ReceiveExternalDataConfigRepository(ISqlSugarClient context)
        : base(context)
    {
    }

    /// <summary>
    /// Get all configurations for an integration
    /// </summary>
    public async Task<List<ReceiveExternalDataConfig>> GetByIntegrationIdAsync(long integrationId)
    {
        return await db.Queryable<ReceiveExternalDataConfig>()
            .Where(x => x.IntegrationId == integrationId && x.IsValid)
            .OrderBy(x => x.EntityName)
            .ToListAsync();
    }

    /// <summary>
    /// Get configuration by entity name
    /// </summary>
    public async Task<ReceiveExternalDataConfig?> GetByEntityNameAsync(long integrationId, string entityName)
    {
        return await db.Queryable<ReceiveExternalDataConfig>()
            .Where(x => x.IntegrationId == integrationId
                && x.EntityName == entityName
                && x.IsValid)
            .FirstAsync();
    }

    /// <summary>
    /// Check if entity name exists for an integration
    /// </summary>
    public async Task<bool> ExistsEntityNameAsync(long integrationId, string entityName, long? excludeId = null)
    {
        var query = db.Queryable<ReceiveExternalDataConfig>()
            .Where(x => x.IntegrationId == integrationId
                && x.EntityName == entityName
                && x.IsValid);

        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    /// <summary>
    /// Get active configurations for an integration
    /// </summary>
    public async Task<List<ReceiveExternalDataConfig>> GetActiveConfigsAsync(long integrationId)
    {
        return await db.Queryable<ReceiveExternalDataConfig>()
            .Where(x => x.IntegrationId == integrationId
                && x.IsActive
                && x.IsValid)
            .OrderBy(x => x.EntityName)
            .ToListAsync();
    }

    /// <summary>
    /// Get configurations by workflow ID
    /// </summary>
    public async Task<List<ReceiveExternalDataConfig>> GetByWorkflowIdAsync(long workflowId)
    {
        return await db.Queryable<ReceiveExternalDataConfig>()
            .Where(x => x.TriggerWorkflowId == workflowId && x.IsValid)
            .OrderBy(x => x.EntityName)
            .ToListAsync();
    }
}

