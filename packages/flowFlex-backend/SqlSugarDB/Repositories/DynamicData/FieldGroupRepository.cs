using FlowFlex.Domain.Entities.DynamicData;
using FlowFlex.Domain.Repository.DynamicData;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace FlowFlex.SqlSugarDB.Repositories.DynamicData;

/// <summary>
/// Field group repository implementation
/// </summary>
public class FieldGroupRepository : BaseRepository<FieldGroup>, IFieldGroupRepository, IScopedService
{
    private readonly UserContext _userContext;
    private readonly ILogger<FieldGroupRepository> _logger;

    public FieldGroupRepository(
        ISqlSugarClient sqlSugarClient,
        UserContext userContext,
        ILogger<FieldGroupRepository> logger) : base(sqlSugarClient)
    {
        _userContext = userContext;
        _logger = logger;
    }

    public async Task<List<FieldGroup>> GetAllAsync()
    {
        return await db.Queryable<FieldGroup>()
            .Where(x => x.IsValid)
            .Where(x => x.TenantId == _userContext.TenantId && x.AppCode == _userContext.AppCode)
            .OrderBy(x => x.Sort)
            .ToListAsync();
    }

    public async Task<FieldGroup?> GetByIdAsync(long groupId)
    {
        return await db.Queryable<FieldGroup>()
            .Where(x => x.Id == groupId && x.IsValid)
            .Where(x => x.TenantId == _userContext.TenantId && x.AppCode == _userContext.AppCode)
            .FirstAsync();
    }

    public async Task<FieldGroup?> GetDefaultGroupAsync()
    {
        return await db.Queryable<FieldGroup>()
            .Where(x => x.IsDefault && x.IsValid)
            .Where(x => x.TenantId == _userContext.TenantId && x.AppCode == _userContext.AppCode)
            .FirstAsync();
    }

    public async Task<bool> ExistsGroupNameAsync(string groupName, long? excludeId = null)
    {
        var query = db.Queryable<FieldGroup>()
            .Where(x => x.GroupName == groupName && x.IsValid)
            .Where(x => x.TenantId == _userContext.TenantId && x.AppCode == _userContext.AppCode);

        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);

        return await query.AnyAsync();
    }

    public async Task UpdateGroupFieldsAsync(long groupId, long[] fieldIds)
    {
        var modifyBy = _userContext.UserName ?? "SYSTEM";
        // Parse UserId to long, default to 0 if parsing fails
        long.TryParse(_userContext.UserId, out var modifyUserId);
        
        await db.Updateable<FieldGroup>()
            .SetColumns(x => x.Fields == fieldIds)
            .SetColumns(x => x.ModifyDate == DateTimeOffset.UtcNow)
            .SetColumns(x => x.ModifyBy == modifyBy)
            .SetColumns(x => x.ModifyUserId == modifyUserId)
            .Where(x => x.Id == groupId)
            .Where(x => x.TenantId == _userContext.TenantId && x.AppCode == _userContext.AppCode)
            .ExecuteCommandAsync();
    }

    public async Task BatchUpdateSortAsync(Dictionary<long, int> groupSorts)
    {
        foreach (var kvp in groupSorts)
        {
            await db.Updateable<FieldGroup>()
                .SetColumns(x => x.Sort == kvp.Value)
                .Where(x => x.Id == kvp.Key)
                .Where(x => x.TenantId == _userContext.TenantId && x.AppCode == _userContext.AppCode)
                .ExecuteCommandAsync();
        }
    }

    public async Task<List<FieldGroup>> GetByModuleIdAsync(int moduleId)
    {
        // For backward compatibility - moduleId is not used as query condition
        return await GetAllAsync();
    }

    public async Task<FieldGroup?> GetByIdAsync(int moduleId, long groupId)
    {
        // For backward compatibility - moduleId is not used as query condition
        return await GetByIdAsync(groupId);
    }

    public async Task<bool> ExistsGroupNameAsync(int moduleId, string groupName, long? excludeId = null)
    {
        // For backward compatibility - moduleId is not used as query condition
        return await ExistsGroupNameAsync(groupName, excludeId);
    }

    public async Task UpdateGroupFieldsAsync(int moduleId, long groupId, long[] fieldIds)
    {
        // For backward compatibility - moduleId is not used as query condition
        await UpdateGroupFieldsAsync(groupId, fieldIds);
    }

    public async Task BatchUpdateSortAsync(int moduleId, Dictionary<long, int> groupSorts)
    {
        // For backward compatibility - moduleId is not used as query condition
        await BatchUpdateSortAsync(groupSorts);
    }
}
