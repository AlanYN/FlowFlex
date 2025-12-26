using FlowFlex.Domain.Entities.DynamicData;
using FlowFlex.Domain.Repository.DynamicData;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace FlowFlex.SqlSugarDB.Repositories.DynamicData;

/// <summary>
/// Define field repository implementation
/// </summary>
public class DefineFieldRepository : BaseRepository<DefineField>, IDefineFieldRepository, IScopedService
{
    private readonly UserContext _userContext;
    private readonly ILogger<DefineFieldRepository> _logger;

    public DefineFieldRepository(
        ISqlSugarClient sqlSugarClient,
        UserContext userContext,
        ILogger<DefineFieldRepository> logger) : base(sqlSugarClient)
    {
        _userContext = userContext;
        _logger = logger;
    }

    public async Task<List<DefineField>> GetAllAsync()
    {
        return await db.Queryable<DefineField>()
            .Where(x => x.IsValid)
            .Where(x => x.TenantId == _userContext.TenantId && x.AppCode == _userContext.AppCode)
            .OrderBy(x => x.Sort)
            .ToListAsync();
    }

    public async Task<DefineField?> GetByFieldNameAsync(string fieldName)
    {
        return await db.Queryable<DefineField>()
            .Where(x => x.FieldName == fieldName && x.IsValid)
            .Where(x => x.TenantId == _userContext.TenantId && x.AppCode == _userContext.AppCode)
            .FirstAsync();
    }

    public async Task<DefineField?> GetByIdAsync(long fieldId)
    {
        return await db.Queryable<DefineField>()
            .Where(x => x.Id == fieldId && x.IsValid)
            .Where(x => x.TenantId == _userContext.TenantId && x.AppCode == _userContext.AppCode)
            .FirstAsync();
    }

    public async Task<bool> ExistsFieldNameAsync(string fieldName, long? excludeId = null)
    {
        var query = db.Queryable<DefineField>()
            .Where(x => x.FieldName == fieldName && x.IsValid)
            .Where(x => x.TenantId == _userContext.TenantId && x.AppCode == _userContext.AppCode);

        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);

        return await query.AnyAsync();
    }

    public async Task<List<DefineField>> GetByGroupIdAsync(long groupId)
    {
        // Get group first to get field IDs
        var group = await db.Queryable<FieldGroup>()
            .Where(x => x.Id == groupId && x.IsValid)
            .Where(x => x.TenantId == _userContext.TenantId && x.AppCode == _userContext.AppCode)
            .FirstAsync();

        if (group?.Fields == null || !group.Fields.Any())
            return new List<DefineField>();

        return await db.Queryable<DefineField>()
            .Where(x => group.Fields.Contains(x.Id) && x.IsValid)
            .Where(x => x.TenantId == _userContext.TenantId && x.AppCode == _userContext.AppCode)
            .OrderBy(x => x.Sort)
            .ToListAsync();
    }

    public async Task BatchUpdateSortAsync(Dictionary<long, int> fieldSorts)
    {
        foreach (var kvp in fieldSorts)
        {
            await db.Updateable<DefineField>()
                .SetColumns(x => x.Sort == kvp.Value)
                .Where(x => x.Id == kvp.Key)
                .Where(x => x.TenantId == _userContext.TenantId && x.AppCode == _userContext.AppCode)
                .ExecuteCommandAsync();
        }
    }

    public async Task<List<DefineField>> GetByModuleIdAsync(int moduleId)
    {
        // For backward compatibility - moduleId is not used as query condition
        return await GetAllAsync();
    }

    public async Task<DefineField?> GetByIdAsync(int moduleId, long fieldId)
    {
        // For backward compatibility - moduleId is not used as query condition
        return await GetByIdAsync(fieldId);
    }

    public async Task<DefineField?> GetByModuleIdAndFieldNameAsync(int moduleId, string fieldName)
    {
        // For backward compatibility - moduleId is not used as query condition
        return await GetByFieldNameAsync(fieldName);
    }

    public async Task<bool> ExistsFieldNameAsync(int moduleId, string fieldName, long? excludeId = null)
    {
        // For backward compatibility - moduleId is not used as query condition
        return await ExistsFieldNameAsync(fieldName, excludeId);
    }

    public async Task BatchUpdateSortAsync(int moduleId, Dictionary<long, int> fieldSorts)
    {
        // For backward compatibility - moduleId is not used as query condition
        await BatchUpdateSortAsync(fieldSorts);
    }
}
