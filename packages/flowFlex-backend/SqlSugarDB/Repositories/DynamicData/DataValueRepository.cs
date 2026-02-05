using FlowFlex.Domain.Entities.DynamicData;
using FlowFlex.Domain.Repository.DynamicData;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Helpers;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace FlowFlex.SqlSugarDB.Repositories.DynamicData;

/// <summary>
/// Data value repository implementation
/// </summary>
public class DataValueRepository : BaseRepository<DataValue>, IDataValueRepository, IScopedService
{
    private readonly UserContext _userContext;
    private readonly ILogger<DataValueRepository> _logger;

    public DataValueRepository(
        ISqlSugarClient sqlSugarClient,
        UserContext userContext,
        ILogger<DataValueRepository> logger) : base(sqlSugarClient)
    {
        _userContext = userContext;
        _logger = logger;
    }

    public async Task<List<DataValue>> GetByBusinessIdAsync(long businessId)
    {
        var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
        var appCode = TenantContextHelper.GetAppCodeOrDefault(_userContext);
        
        return await db.Queryable<DataValue>()
            .Where(x => x.BusinessId == businessId && x.IsValid)
            .Where(x => x.TenantId == tenantId && x.AppCode == appCode)
            .ToListAsync();
    }

    public async Task<List<DataValue>> GetByBusinessIdsAsync(List<long> businessIds)
    {
        var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
        var appCode = TenantContextHelper.GetAppCodeOrDefault(_userContext);
        
        return await db.Queryable<DataValue>()
            .Where(x => businessIds.Contains(x.BusinessId) && x.IsValid)
            .Where(x => x.TenantId == tenantId && x.AppCode == appCode)
            .ToListAsync();
    }

    public async Task<DataValue?> GetByBusinessIdAndFieldNameAsync(long businessId, string fieldName)
    {
        var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
        var appCode = TenantContextHelper.GetAppCodeOrDefault(_userContext);
        
        return await db.Queryable<DataValue>()
            .Where(x => x.BusinessId == businessId && x.FieldName == fieldName && x.IsValid)
            .Where(x => x.TenantId == tenantId && x.AppCode == appCode)
            .FirstAsync();
    }

    public async Task BatchInsertAsync(List<DataValue> dataValues)
    {
        if (dataValues == null || !dataValues.Any())
            return;

        foreach (var dataValue in dataValues)
        {
            // Generate snowflake ID if not set
            if (dataValue.Id == 0)
            {
                dataValue.InitNewId();
            }
            dataValue.TenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
            dataValue.AppCode = TenantContextHelper.GetAppCodeOrDefault(_userContext);
        }

        await db.Insertable(dataValues).ExecuteCommandAsync();
    }

    public async Task BatchUpdateAsync(List<DataValue> dataValues)
    {
        if (dataValues == null || !dataValues.Any())
            return;

        await db.Updateable(dataValues).ExecuteCommandAsync();
    }

    public async Task DeleteByBusinessIdAsync(long businessId)
    {
        var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
        var appCode = TenantContextHelper.GetAppCodeOrDefault(_userContext);
        
        await db.Updateable<DataValue>()
            .SetColumns(x => x.IsValid == false)
            .Where(x => x.BusinessId == businessId)
            .Where(x => x.TenantId == tenantId && x.AppCode == appCode)
            .ExecuteCommandAsync();
    }

    public async Task DeleteByBusinessIdsAsync(List<long> businessIds)
    {
        var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
        var appCode = TenantContextHelper.GetAppCodeOrDefault(_userContext);
        
        await db.Updateable<DataValue>()
            .SetColumns(x => x.IsValid == false)
            .Where(x => businessIds.Contains(x.BusinessId))
            .Where(x => x.TenantId == tenantId && x.AppCode == appCode)
            .ExecuteCommandAsync();
    }
}
