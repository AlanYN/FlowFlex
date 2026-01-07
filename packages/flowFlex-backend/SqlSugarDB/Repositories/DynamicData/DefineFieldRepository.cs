using FlowFlex.Domain.Entities.DynamicData;
using FlowFlex.Domain.Repository.DynamicData;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Models.DynamicData;
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

    public async Task<PagedResult<DefineField>> GetPagedListAsync(PropertyQueryRequest request)
    {
        var query = db.Queryable<DefineField>()
            .Where(x => x.IsValid)
            .Where(x => x.TenantId == _userContext.TenantId && x.AppCode == _userContext.AppCode);

        // Apply filters - support comma-separated values
        if (!string.IsNullOrWhiteSpace(request.FieldName))
        {
            var fieldNames = request.GetFieldNameList();
            if (fieldNames.Any())
            {
                query = query.Where(x => fieldNames.Any(n => x.FieldName.ToLower().Contains(n.ToLower())));
            }
        }

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
        {
            var displayNames = request.GetDisplayNameList();
            if (displayNames.Any())
            {
                query = query.Where(x => displayNames.Any(n => x.DisplayName.ToLower().Contains(n.ToLower())));
            }
        }

        if (request.DataType.HasValue)
            query = query.Where(x => x.DataType == request.DataType.Value);

        if (!string.IsNullOrWhiteSpace(request.CreateBy))
        {
            var createByList = request.GetCreateByList();
            if (createByList.Any())
            {
                query = query.Where(x => createByList.Any(n => x.CreateBy.ToLower().Contains(n.ToLower())));
            }
        }

        if (!string.IsNullOrWhiteSpace(request.ModifyBy))
        {
            var modifyByList = request.GetModifyByList();
            if (modifyByList.Any())
            {
                query = query.Where(x => modifyByList.Any(n => x.ModifyBy.ToLower().Contains(n.ToLower())));
            }
        }

        if (request.CreateDateStart.HasValue)
            query = query.Where(x => x.CreateDate >= request.CreateDateStart.Value);

        if (request.CreateDateEnd.HasValue)
            query = query.Where(x => x.CreateDate <= request.CreateDateEnd.Value);

        if (request.ModifyDateStart.HasValue)
            query = query.Where(x => x.ModifyDate >= request.ModifyDateStart.Value);

        if (request.ModifyDateEnd.HasValue)
            query = query.Where(x => x.ModifyDate <= request.ModifyDateEnd.Value);

        // Apply sorting
        query = ApplySorting(query, request.SortField, request.IsAsc);

        // Get total count
        var totalCount = await query.CountAsync();

        // Get paged data
        var items = await query
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResult<DefineField>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        };
    }

    private static ISugarQueryable<DefineField> ApplySorting(ISugarQueryable<DefineField> query, string? sortField, bool isAsc)
    {
        return sortField?.ToLowerInvariant() switch
        {
            "fieldname" => isAsc ? query.OrderBy(x => x.FieldName) : query.OrderByDescending(x => x.FieldName),
            "displayname" => isAsc ? query.OrderBy(x => x.DisplayName) : query.OrderByDescending(x => x.DisplayName),
            "datatype" => isAsc ? query.OrderBy(x => x.DataType) : query.OrderByDescending(x => x.DataType),
            "createdate" => isAsc ? query.OrderBy(x => x.CreateDate) : query.OrderByDescending(x => x.CreateDate),
            "modifydate" => isAsc ? query.OrderBy(x => x.ModifyDate) : query.OrderByDescending(x => x.ModifyDate),
            "sort" => isAsc ? query.OrderBy(x => x.Sort) : query.OrderByDescending(x => x.Sort),
            _ => query.OrderByDescending(x => x.CreateDate)
        };
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

    public async Task<List<DefineField>> GetByIdsAsync(IEnumerable<long> ids)
    {
        if (ids == null || !ids.Any())
            return new List<DefineField>();

        var idList = ids.ToList();
        return await db.Queryable<DefineField>()
            .Where(x => idList.Contains(x.Id) && x.IsValid)
            .Where(x => x.TenantId == _userContext.TenantId && x.AppCode == _userContext.AppCode)
            .OrderBy(x => x.Sort)
            .ToListAsync();
    }
}
