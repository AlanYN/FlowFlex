using AutoMapper;
using Microsoft.Extensions.Logging;
using SqlSugar;
using System.Linq.Expressions;
using FlowFlex.SqlSugarDB;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.OW;

namespace FlowFlex.SqlSugarDB.Implements.OW;

/// <summary>
/// Checklist repository implementation
/// </summary>
public class ChecklistRepository : BaseRepository<Checklist>, IChecklistRepository, IScopedService
{
    public ChecklistRepository(ISqlSugarClient sqlSugarClient) : base(sqlSugarClient)
    {
    }

    /// <summary>
    /// Get checklist list by team
    /// </summary>
    public async Task<List<Checklist>> GetByTeamAsync(string team)
    {
        var whereExpression = Expressionable.Create<Checklist>()
            .AndIF(!string.IsNullOrEmpty(team), x => x.Team == team)
            .And(x => x.IsValid == true)
            .ToExpression();

        return await db.Queryable<Checklist>()
            .Where(whereExpression)
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
    /// Get checklist instances by template
    /// </summary>
    public async Task<List<Checklist>> GetByTemplateIdAsync(long templateId)
    {
        return await db.Queryable<Checklist>()
            .Where(x => x.TemplateId == templateId && x.IsValid == true)
            .OrderBy(x => x.CreateDate, SqlSugar.OrderByType.Desc)
            .ToListAsync();
    }

    /// <summary>
    /// Update completion rate
    /// </summary>
    public async Task<bool> UpdateCompletionRateAsync(long id, decimal completionRate, int totalTasks, int completedTasks)
    {
        var result = await db.Updateable<Checklist>()
            .SetColumns(x => new Checklist
            {
                CompletionRate = completionRate,
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                ModifyDate = DateTimeOffset.Now
            })
            .Where(x => x.Id == id && x.IsValid == true)
            .ExecuteCommandAsync();

        return result > 0;
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
    /// Get checklist with tasks
    /// </summary>
    public async Task<Checklist> GetWithTasksAsync(long id)
    {
        var checklist = await db.Queryable<Checklist>()
            .Where(x => x.Id == id && x.IsValid == true)
            .FirstAsync();

        if (checklist != null)
        {
            checklist.Tasks = await db.Queryable<ChecklistTask>()
                .Where(x => x.ChecklistId == id && x.IsValid == true)
                .OrderBy(x => x.Order)
                .ToListAsync();
        }

        return checklist;
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

        if (workflowId.HasValue)
        {
            whereExpressions.Add(x => x.WorkflowId == workflowId.Value);
        }

        if (stageId.HasValue)
        {
            whereExpressions.Add(x => x.StageId == stageId.Value);
        }

        // Determine sort field and direction
        Expression<Func<Checklist, object>> orderByExpression = sortField switch
        {
            "Name" => x => x.Name,
            "Team" => x => x.Team,
            "CompletionRate" => x => x.CompletionRate,
            "TotalTasks" => x => x.TotalTasks,
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
                TotalTasks = SqlFunc.AggregateSum(x.TotalTasks),
                CompletedTasks = SqlFunc.AggregateSum(x.CompletedTasks),
                AverageCompletionRate = SqlFunc.AggregateAvg(x.CompletionRate),
                TotalEstimatedHours = SqlFunc.AggregateSum(x.EstimatedHours)
            })
            .FirstAsync();

        return new Dictionary<string, object>
        {
            ["TotalChecklists"] = statistics?.TotalChecklists ?? 0,
            ["ActiveChecklists"] = statistics?.ActiveChecklists ?? 0,
            ["TemplateCount"] = statistics?.TemplateCount ?? 0,
            ["InstanceCount"] = statistics?.InstanceCount ?? 0,
            ["TotalTasks"] = statistics?.TotalTasks ?? 0,
            ["CompletedTasks"] = statistics?.CompletedTasks ?? 0,
            ["AverageCompletionRate"] = statistics?.AverageCompletionRate ?? 0,
            ["TotalEstimatedHours"] = statistics?.TotalEstimatedHours ?? 0
        };
    }

    /// <summary>
    /// Get checklists by stage ID
    /// </summary>
    public async Task<List<Checklist>> GetByStageIdAsync(long stageId)
    {
        return await db.Queryable<Checklist>()
            .Where(x => x.StageId == stageId && x.IsValid == true)
            .OrderBy(x => x.CreateDate, SqlSugar.OrderByType.Desc)
            .ToListAsync();
    }

    /// <summary>
    /// Get checklists by stage ID with tasks
    /// </summary>
    public async Task<List<Checklist>> GetByStageIdWithTasksAsync(long stageId)
    {
        var checklists = await db.Queryable<Checklist>()
            .Where(x => x.StageId == stageId && x.IsValid == true)
            .OrderBy(x => x.CreateDate, SqlSugar.OrderByType.Desc)
            .ToListAsync();

        // Load tasks for each checklist
        foreach (var checklist in checklists)
        {
            checklist.Tasks = await db.Queryable<ChecklistTask>()
                .Where(x => x.ChecklistId == checklist.Id && x.IsValid == true)
                .OrderBy(x => x.Order)
                .ToListAsync();
        }

        return checklists;
    }

    /// <summary>
    /// Get checklists by workflow ID
    /// </summary>
    public async Task<List<Checklist>> GetByWorkflowIdAsync(long workflowId)
    {
        return await db.Queryable<Checklist>()
            .Where(x => x.WorkflowId == workflowId && x.IsValid == true)
            .OrderBy(x => x.CreateDate, SqlSugar.OrderByType.Desc)
            .ToListAsync();
    }

    /// <summary>
    /// Update completion rate (simple version)
    /// </summary>
    public async Task<bool> UpdateCompletionRateAsync(long checklistId, decimal completionRate, int completedTasks)
    {
        var result = await db.Updateable<Checklist>()
            .SetColumns(x => new Checklist
            {
                CompletionRate = completionRate,
                CompletedTasks = completedTasks,
                ModifyDate = DateTimeOffset.Now
            })
            .Where(x => x.Id == checklistId && x.IsValid == true)
            .ExecuteCommandAsync();

        return result > 0;
    }
}
