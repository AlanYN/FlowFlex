using SqlSugar;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.SqlSugarDB.Context;
using FlowFlex.Domain.Shared;
using System.Linq.Expressions;

namespace FlowFlex.SqlSugarDB.Repositories.OW;

/// <summary>
/// Checklist Repository
/// </summary>
public class ChecklistRepository : BaseRepository<Checklist>, IChecklistRepository, IScopedService
{
    public ChecklistRepository(ISqlSugarClient sqlSugarClient) : base(sqlSugarClient)
    {
    }

    /// <summary>
    /// Get checklists by team
    /// </summary>
    public async Task<List<Checklist>> GetByTeamAsync(string team)
    {
        return await db.Queryable<Checklist>()
            .WhereIF(!string.IsNullOrEmpty(team), x => x.Team == team)
            .Where(x => x.IsValid == true)
            .OrderBy(x => x.CreateDate, SqlSugar.OrderByType.Desc)
            .ToListAsync();
    }

    /// <summary>
    /// Get template checklists
    /// </summary>
    public async Task<List<Checklist>> GetTemplatesAsync()
    {
        return await db.Queryable<Checklist>()
            .Where(x => x.IsTemplate == true && x.IsValid == true)
            .OrderBy(x => x.CreateDate, SqlSugar.OrderByType.Desc)
            .ToListAsync();
    }

    /// <summary>
    /// Get checklists by template ID
    /// </summary>
    public async Task<List<Checklist>> GetByTemplateIdAsync(long templateId)
    {
        return await db.Queryable<Checklist>()
            .Where(x => x.TemplateId == templateId && x.IsValid == true)
            .OrderBy(x => x.CreateDate, SqlSugar.OrderByType.Desc)
            .ToListAsync();
    }

    /// <summary>
    /// Check if name exists
    /// </summary>
    public async Task<bool> IsNameExistsAsync(string name, string team, long? excludeId = null)
    {
        var whereExpression = Expressionable.Create<Checklist>()
            .And(x => x.Name == name)
            .And(x => x.Team == team)
            .And(x => x.IsValid == true)
            .AndIF(excludeId.HasValue, x => x.Id != excludeId.Value)
            .ToExpression();

        var count = await db.Queryable<Checklist>()
            .Where(whereExpression)
            .CountAsync();

        return count > 0;
    }

    /// <summary>
    /// Get checklists with pagination and filters
    /// </summary>
    public async Task<(List<Checklist> items, int totalCount)> GetPagedAsync(
        int pageIndex,
        int pageSize,
        string name = null,
        string team = null,
        string type = null,
        string status = null,
        bool? isTemplate = null,
        bool? isActive = null,
        long? workflowId = null,
        long? stageId = null,
        string sortField = "CreateDate",
        string sortDirection = "desc")
    {
        // Build query condition list
        var whereExpressions = new List<Expression<Func<Checklist, bool>>>();

        // Basic filter conditions
        whereExpressions.Add(x => x.IsValid == true);

        if (!string.IsNullOrEmpty(name))
        {
            whereExpressions.Add(x => x.Name.ToLower().Contains(name.ToLower()));
        }

        if (!string.IsNullOrEmpty(team))
        {
            whereExpressions.Add(x => x.Team == team);
        }

        if (!string.IsNullOrEmpty(type))
        {
            whereExpressions.Add(x => x.Type == type);
        }

        if (!string.IsNullOrEmpty(status))
        {
            whereExpressions.Add(x => x.Status == status);
        }

        if (isTemplate.HasValue)
        {
            whereExpressions.Add(x => x.IsTemplate == isTemplate.Value);
        }

        if (isActive.HasValue)
        {
            whereExpressions.Add(x => x.IsActive == isActive.Value);
        }

        // Note: workflowId and stageId filters are removed as these fields no longer exist

        // Determine sort field and direction
        Expression<Func<Checklist, object>> orderByExpression = sortField switch
        {
            "Name" => x => x.Name,
            "Team" => x => x.Team,
            _ => x => x.CreateDate
        };

        bool isAsc = sortDirection?.ToLower() == "asc";

        // Use BaseRepository's safe pagination method
        var (items, totalCount) = await GetPageListAsync(
            whereExpressions,
            pageIndex,
            pageSize,
            orderByExpression,
            isAsc
        );

        return (items, totalCount);
    }

    /// <summary>
    /// Get checklist statistics by team
    /// </summary>
    public async Task<Dictionary<string, object>> GetStatisticsByTeamAsync(string team)
    {
        var statistics = await db.Queryable<Checklist>()
            .Where(x => x.Team == team && x.IsValid == true)
            .GroupBy(x => x.Team)
            .Select(x => new
            {
                TotalChecklists = SqlFunc.AggregateCount(x.Id),
                ActiveChecklists = SqlFunc.AggregateSum(SqlFunc.IIF(x.IsActive == true, 1, 0)),
                TemplateCount = SqlFunc.AggregateSum(SqlFunc.IIF(x.IsTemplate == true, 1, 0)),
                InstanceCount = SqlFunc.AggregateSum(SqlFunc.IIF(x.IsTemplate == false, 1, 0)),
                TotalEstimatedHours = SqlFunc.AggregateSum(x.EstimatedHours)
            })
            .FirstAsync();

        return new Dictionary<string, object>
        {
            ["TotalChecklists"] = statistics?.TotalChecklists ?? 0,
            ["ActiveChecklists"] = statistics?.ActiveChecklists ?? 0,
            ["TemplateCount"] = statistics?.TemplateCount ?? 0,
            ["InstanceCount"] = statistics?.InstanceCount ?? 0,
            ["TotalEstimatedHours"] = statistics?.TotalEstimatedHours ?? 0
        };
    }

    /// <summary>
    /// Get checklists by name
    /// </summary>
    public async Task<List<Checklist>> GetByNamesAsync(List<string> names)
    {
        if (names == null || !names.Any())
        {
            return new List<Checklist>();
        }

        return await db.Queryable<Checklist>()
            .Where(x => names.Contains(x.Name) && x.IsValid == true)
            .OrderBy(x => x.CreateDate, SqlSugar.OrderByType.Desc)
            .ToListAsync();
    }

    /// <summary>
    /// Get checklists by name
    /// </summary>
    public async Task<List<Checklist>> GetByNameAsync(string name)
    {
        return await db.Queryable<Checklist>()
            .Where(x => x.Name == name && x.IsValid == true)
            .OrderBy(x => x.CreateDate, SqlSugar.OrderByType.Desc)
            .ToListAsync();
    }
}
