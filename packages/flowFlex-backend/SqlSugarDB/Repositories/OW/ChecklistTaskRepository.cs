using AutoMapper;
using SqlSugar;
using FlowFlex.SqlSugarDB;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;

namespace FlowFlex.SqlSugarDB.Implements.OW;

/// <summary>
/// ChecklistTask repository implementation
/// </summary>
public class ChecklistTaskRepository : BaseRepository<ChecklistTask>, IChecklistTaskRepository, IScopedService
{
    public ChecklistTaskRepository(ISqlSugarClient sqlSugarClient) : base(sqlSugarClient)
    {
    }

    /// <summary>
    /// Get tasks by checklist id
    /// </summary>
    public async Task<List<ChecklistTask>> GetByChecklistIdAsync(long checklistId)
    {
        return await db.Queryable<ChecklistTask>()
            .Where(x => x.ChecklistId == checklistId && x.IsValid == true)
            .OrderBy(x => x.Order)
            .ToListAsync();
    }

    /// <summary>
    /// Get tasks for multiple checklist ids in a single query
    /// </summary>
    public async Task<List<ChecklistTask>> GetByChecklistIdsAsync(List<long> checklistIds)
    {
        if (checklistIds == null || checklistIds.Count == 0)
        {
            return new List<ChecklistTask>();
        }

        return await db.Queryable<ChecklistTask>()
            .Where(x => checklistIds.Contains(x.ChecklistId) && x.IsValid == true)
            .OrderBy(x => x.ChecklistId)
            .OrderBy(x => x.Order)
            .ToListAsync();
    }

    /// <summary>
    /// Get completed tasks count
    /// </summary>
    public async Task<int> GetCompletedCountAsync(long checklistId)
    {
        return await db.Queryable<ChecklistTask>()
            .Where(x => x.ChecklistId == checklistId && x.IsCompleted == true && x.IsValid == true)
            .CountAsync();
    }

    /// <summary>
    /// Get pending tasks by assignee
    /// </summary>
    public async Task<List<ChecklistTask>> GetPendingTasksByAssigneeAsync(long assigneeId)
    {
        return await db.Queryable<ChecklistTask>()
            .Where(x => x.AssigneeId == assigneeId
                && x.IsCompleted == false
                && x.Status == "Pending"
                && x.IsValid == true)
            .OrderBy(x => x.DueDate)
            .ToListAsync();
    }

    /// <summary>
    /// Get overdue tasks
    /// </summary>
    public async Task<List<ChecklistTask>> GetOverdueTasksAsync()
    {
        var now = DateTimeOffset.Now;
        return await db.Queryable<ChecklistTask>()
            .Where(x => x.DueDate.HasValue
                && x.DueDate.Value < now
                && x.IsCompleted == false
                && x.IsValid == true)
            .OrderBy(x => x.DueDate)
            .ToListAsync();
    }

    /// <summary>
    /// Batch complete tasks
    /// </summary>
    public async Task<bool> BatchCompleteAsync(List<long> taskIds, string completionNotes, int actualHours)
    {
        var result = await db.Updateable<ChecklistTask>()
            .SetColumns(x => new ChecklistTask
            {
                IsCompleted = true,
                CompletedDate = DateTimeOffset.Now,
                CompletionNotes = completionNotes,
                ActualHours = actualHours,
                Status = "Completed",
                ModifyDate = DateTimeOffset.Now
            })
            .Where(x => taskIds.Contains(x.Id) && x.IsValid == true)
            .ExecuteCommandAsync();

        return result > 0;
    }

    /// <summary>
    /// Update task order
    /// </summary>
    public async Task<bool> UpdateOrderAsync(long checklistId, Dictionary<long, int> taskOrders)
    {
        var success = true;
        await db.Ado.BeginTranAsync();

        try
        {
            foreach (var (taskId, order) in taskOrders)
            {
                var result = await db.Updateable<ChecklistTask>()
                    .SetColumns(x => new ChecklistTask
                    {
                        Order = order,
                        ModifyDate = DateTimeOffset.Now
                    })
                    .Where(x => x.Id == taskId && x.ChecklistId == checklistId && x.IsValid == true)
                    .ExecuteCommandAsync();

                if (result == 0)
                {
                    success = false;
                    break;
                }
            }

            if (success)
            {
                await db.Ado.CommitTranAsync();
            }
            else
            {
                await db.Ado.RollbackTranAsync();
            }
        }
        catch
        {
            await db.Ado.RollbackTranAsync();
            success = false;
        }

        return success;
    }

    /// <summary>
    /// Get dependent tasks
    /// </summary>
    public async Task<List<ChecklistTask>> GetDependentTasksAsync(long taskId)
    {
        return await db.Queryable<ChecklistTask>()
            .Where(x => x.DependsOnTaskId == taskId && x.IsValid == true)
            .OrderBy(x => x.Order)
            .ToListAsync();
    }

    /// <summary>
    /// Check if task can be completed
    /// </summary>
    public async Task<bool> CanCompleteAsync(long taskId)
    {
        var task = await db.Queryable<ChecklistTask>()
            .Where(x => x.Id == taskId && x.IsValid == true)
            .FirstAsync();

        if (task == null || task.IsCompleted)
        {
            return false;
        }

        // Check if dependent task is completed
        if (task.DependsOnTaskId.HasValue)
        {
            var dependentTask = await db.Queryable<ChecklistTask>()
                .Where(x => x.Id == task.DependsOnTaskId.Value && x.IsValid == true)
                .FirstAsync();

            if (dependentTask != null && !dependentTask.IsCompleted)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Get tasks by assignee and date range
    /// </summary>
    public async Task<List<ChecklistTask>> GetByAssigneeAndDateRangeAsync(
        long assigneeId,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null)
    {
        var whereExpression = Expressionable.Create<ChecklistTask>()
            .And(x => x.AssigneeId == assigneeId && x.IsValid == true)
            .AndIF(startDate.HasValue, x => x.CreateDate >= startDate.Value)
            .AndIF(endDate.HasValue, x => x.CreateDate <= endDate.Value)
            .ToExpression();

        return await db.Queryable<ChecklistTask>()
            .Where(whereExpression)
            .OrderBy(x => x.CreateDate, SqlSugar.OrderByType.Desc)
            .ToListAsync();
    }

    /// <summary>
    /// Get task statistics for checklist
    /// </summary>
    public async Task<Dictionary<string, object>> GetTaskStatisticsAsync(long checklistId)
    {
        var statistics = await db.Queryable<ChecklistTask>()
            .Where(x => x.ChecklistId == checklistId && x.IsValid == true)
            .GroupBy(x => x.ChecklistId)
            .Select(x => new
            {
                TotalTasks = SqlFunc.AggregateCount(x.Id),
                CompletedTasks = SqlFunc.AggregateSum(SqlFunc.IIF(x.IsCompleted == true, 1, 0)),
                RequiredTasks = SqlFunc.AggregateSum(SqlFunc.IIF(x.IsRequired == true, 1, 0)),
                OverdueTasks = SqlFunc.AggregateSum(SqlFunc.IIF(x.DueDate.HasValue && x.DueDate.Value < DateTimeOffset.Now && x.IsCompleted == false, 1, 0)),
                TotalEstimatedHours = SqlFunc.AggregateSum(x.EstimatedHours),
                TotalActualHours = SqlFunc.AggregateSum(x.ActualHours)
            })
            .FirstAsync();

        return new Dictionary<string, object>
        {
            ["TotalTasks"] = statistics?.TotalTasks ?? 0,
            ["CompletedTasks"] = statistics?.CompletedTasks ?? 0,
            ["RequiredTasks"] = statistics?.RequiredTasks ?? 0,
            ["OverdueTasks"] = statistics?.OverdueTasks ?? 0,
            ["TotalEstimatedHours"] = statistics?.TotalEstimatedHours ?? 0,
            ["TotalActualHours"] = statistics?.TotalActualHours ?? 0
        };
    }

    /// <summary>
    /// Get next order number for checklist
    /// </summary>
    public async Task<int> GetNextOrderAsync(long checklistId)
    {
        var maxOrder = await db.Queryable<ChecklistTask>()
            .Where(x => x.ChecklistId == checklistId && x.IsValid == true)
            .MaxAsync(x => x.Order);

        return maxOrder + 1;
    }

    /// <summary>
    /// Get tasks by action ID
    /// </summary>
    public async Task<List<ChecklistTask>> GetTasksByActionIdAsync(long actionId)
    {
        return await db.Queryable<ChecklistTask>()
            .Where(x => x.ActionId == actionId && x.IsValid == true)
            .OrderBy(x => x.Order)
            .ToListAsync();
    }

    /// <summary>
    /// Check if task name exists in checklist (excluding specific task ID for update scenario)
    /// </summary>
    public async Task<bool> IsTaskNameExistsAsync(long checklistId, string taskName, long? excludeTaskId = null)
    {
        if (string.IsNullOrWhiteSpace(taskName))
            return false;

        var query = db.Queryable<ChecklistTask>()
            .Where(x => x.ChecklistId == checklistId
                && x.Name == taskName.Trim()
                && x.IsValid == true);

        if (excludeTaskId.HasValue)
        {
            query = query.Where(x => x.Id != excludeTaskId.Value);
        }

        return await query.AnyAsync();
    }
}
