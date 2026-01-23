using SqlSugar;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace FlowFlex.SqlSugarDB.Implements.OW;

/// <summary>
/// ChecklistTask repository implementation
/// </summary>
public class ChecklistTaskRepository : BaseRepository<ChecklistTask>, IChecklistTaskRepository, IScopedService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ChecklistTaskRepository> _logger;

    public ChecklistTaskRepository(
        ISqlSugarClient sqlSugarClient,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ChecklistTaskRepository> logger) : base(sqlSugarClient)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Get current tenant ID from HTTP context
    /// </summary>
    private string GetCurrentTenantId()
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext != null)
        {
            var tenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantId))
            {
                return tenantId;
            }
        }
        return "default";
    }

    /// <summary>
    /// Get current app code from HTTP context
    /// </summary>
    private string GetCurrentAppCode()
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext != null)
        {
            var appCode = httpContext.Request.Headers["X-App-Code"].FirstOrDefault();
            if (!string.IsNullOrEmpty(appCode))
            {
                return appCode;
            }
        }
        return "default";
    }

    /// <summary>
    /// Get checklist task list by expression with tenant isolation
    /// </summary>
    public new async Task<List<ChecklistTask>> GetListAsync(Expression<Func<ChecklistTask, bool>> whereExpression, CancellationToken cancellationToken = default, bool copyNew = false)
    {
        var currentTenantId = GetCurrentTenantId();
        var currentAppCode = GetCurrentAppCode();

        _logger.LogInformation($"[ChecklistTaskRepository] GetListAsync(Expression) applying filters: TenantId={currentTenantId}, AppCode={currentAppCode}");

        var dbNew = copyNew ? db.CopyNew() : db;
        dbNew.Ado.CancellationToken = cancellationToken;

        var result = await dbNew.Queryable<ChecklistTask>()
            .Where(whereExpression)
            .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
            .ToListAsync();

        _logger.LogInformation($"[ChecklistTaskRepository] GetListAsync(Expression) returned {result.Count} tasks");

        return result;
    }

    /// <summary>
    /// Get tasks by checklist id
    /// </summary>
    public async Task<List<ChecklistTask>> GetByChecklistIdAsync(long checklistId)
    {
        var currentTenantId = GetCurrentTenantId();
        var currentAppCode = GetCurrentAppCode();

        return await db.Queryable<ChecklistTask>()
            .Where(x => x.ChecklistId == checklistId && x.IsValid == true)
            .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
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

        var currentTenantId = GetCurrentTenantId();
        var currentAppCode = GetCurrentAppCode();

        return await db.Queryable<ChecklistTask>()
            .Where(x => checklistIds.Contains(x.ChecklistId) && x.IsValid == true)
            .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
            .OrderBy(x => x.ChecklistId)
            .OrderBy(x => x.Order)
            .ToListAsync();
    }

    /// <summary>
    /// Get completed tasks count
    /// </summary>
    public async Task<int> GetCompletedCountAsync(long checklistId)
    {
        var currentTenantId = GetCurrentTenantId();
        var currentAppCode = GetCurrentAppCode();

        return await db.Queryable<ChecklistTask>()
            .Where(x => x.ChecklistId == checklistId && x.IsCompleted == true && x.IsValid == true)
            .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
            .CountAsync();
    }

    /// <summary>
    /// Get pending tasks by assignee
    /// </summary>
    public async Task<List<ChecklistTask>> GetPendingTasksByAssigneeAsync(long assigneeId)
    {
        var currentTenantId = GetCurrentTenantId();
        var currentAppCode = GetCurrentAppCode();

        return await db.Queryable<ChecklistTask>()
            .Where(x => x.AssigneeId == assigneeId
                && x.IsCompleted == false
                && x.Status == "Pending"
                && x.IsValid == true)
            .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
            .OrderBy(x => x.DueDate)
            .ToListAsync();
    }

    /// <summary>
    /// Get overdue tasks
    /// </summary>
    public async Task<List<ChecklistTask>> GetOverdueTasksAsync()
    {
        var currentTenantId = GetCurrentTenantId();
        var currentAppCode = GetCurrentAppCode();
        var now = DateTimeOffset.UtcNow;

        return await db.Queryable<ChecklistTask>()
            .Where(x => x.DueDate.HasValue
                && x.DueDate.Value < now
                && x.IsCompleted == false
                && x.IsValid == true)
            .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
            .OrderBy(x => x.DueDate)
            .ToListAsync();
    }

    /// <summary>
    /// Batch complete tasks
    /// </summary>
    public async Task<bool> BatchCompleteAsync(List<long> taskIds, string completionNotes, int actualHours)
    {
        var currentTenantId = GetCurrentTenantId();
        var currentAppCode = GetCurrentAppCode();

        var result = await db.Updateable<ChecklistTask>()
            .SetColumns(x => new ChecklistTask
            {
                IsCompleted = true,
                CompletedDate = DateTimeOffset.UtcNow,
                CompletionNotes = completionNotes,
                ActualHours = actualHours,
                Status = "Completed",
                ModifyDate = DateTimeOffset.UtcNow
            })
            .Where(x => taskIds.Contains(x.Id) && x.IsValid == true)
            .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
            .ExecuteCommandAsync();

        return result > 0;
    }

    /// <summary>
    /// Update task order
    /// </summary>
    public async Task<bool> UpdateOrderAsync(long checklistId, Dictionary<long, int> taskOrders)
    {
        var currentTenantId = GetCurrentTenantId();
        var currentAppCode = GetCurrentAppCode();
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
                    .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
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
        var currentTenantId = GetCurrentTenantId();
        var currentAppCode = GetCurrentAppCode();

        return await db.Queryable<ChecklistTask>()
            .Where(x => x.DependsOnTaskId == taskId && x.IsValid == true)
            .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
            .OrderBy(x => x.Order)
            .ToListAsync();
    }

    /// <summary>
    /// Check if task can be completed
    /// </summary>
    public async Task<bool> CanCompleteAsync(long taskId)
    {
        var currentTenantId = GetCurrentTenantId();
        var currentAppCode = GetCurrentAppCode();

        var task = await db.Queryable<ChecklistTask>()
            .Where(x => x.Id == taskId && x.IsValid == true)
            .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
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
                .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
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
        var currentTenantId = GetCurrentTenantId();
        var currentAppCode = GetCurrentAppCode();

        var whereExpression = Expressionable.Create<ChecklistTask>()
            .And(x => x.AssigneeId == assigneeId && x.IsValid == true)
            .And(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
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
        var currentTenantId = GetCurrentTenantId();
        var currentAppCode = GetCurrentAppCode();

        var statistics = await db.Queryable<ChecklistTask>()
            .Where(x => x.ChecklistId == checklistId && x.IsValid == true)
            .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
            .GroupBy(x => x.ChecklistId)
            .Select(x => new
            {
                TotalTasks = SqlFunc.AggregateCount(x.Id),
                CompletedTasks = SqlFunc.AggregateSum(SqlFunc.IIF(x.IsCompleted == true, 1, 0)),
                RequiredTasks = SqlFunc.AggregateSum(SqlFunc.IIF(x.IsRequired == true, 1, 0)),
                OverdueTasks = SqlFunc.AggregateSum(SqlFunc.IIF(x.DueDate.HasValue && x.DueDate.Value < DateTimeOffset.UtcNow && x.IsCompleted == false, 1, 0)),
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
        var currentTenantId = GetCurrentTenantId();
        var currentAppCode = GetCurrentAppCode();

        var maxOrder = await db.Queryable<ChecklistTask>()
            .Where(x => x.ChecklistId == checklistId && x.IsValid == true)
            .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
            .MaxAsync(x => x.Order);

        return maxOrder + 1;
    }

    /// <summary>
    /// Get tasks by action ID
    /// </summary>
    public async Task<List<ChecklistTask>> GetTasksByActionIdAsync(long actionId)
    {
        var currentTenantId = GetCurrentTenantId();
        var currentAppCode = GetCurrentAppCode();

        return await db.Queryable<ChecklistTask>()
            .Where(x => x.ActionId == actionId && x.IsValid == true)
            .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
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

        var currentTenantId = GetCurrentTenantId();
        var currentAppCode = GetCurrentAppCode();

        var query = db.Queryable<ChecklistTask>()
            .Where(x => x.ChecklistId == checklistId
                && x.Name == taskName.Trim()
                && x.IsValid == true)
            .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode);

        if (excludeTaskId.HasValue)
        {
            query = query.Where(x => x.Id != excludeTaskId.Value);
        }

        return await query.AnyAsync();
    }

    #region Dashboard Methods

    /// <summary>
    /// Get pending tasks for user (assigned to user) with pagination
    /// Only returns tasks that have associated onboarding (case)
    /// </summary>
    public async Task<List<DashboardTaskInfo>> GetPendingTasksForUserAsync(
        long userId, 
        List<long> userTeamIds, 
        string? category, 
        int pageIndex, 
        int pageSize)
    {
        var currentTenantId = GetCurrentTenantId();
        var currentAppCode = GetCurrentAppCode();

        _logger.LogInformation($"[ChecklistTaskRepository] GetPendingTasksForUserAsync with TenantId={currentTenantId}, AppCode={currentAppCode}, UserId={userId}");

        // Step 1: Get tasks assigned to current user and their checklist IDs
        var userTasks = await db.Queryable<ChecklistTask>()
            .Where(t => t.IsValid && t.AssigneeId == userId)
            .Where(t => t.TenantId == currentTenantId && t.AppCode == currentAppCode)
            .ToListAsync();

        if (!userTasks.Any())
        {
            return new List<DashboardTaskInfo>();
        }

        var checklistIds = userTasks.Select(t => t.ChecklistId).Distinct().ToList();
        var taskIds = userTasks.Select(t => t.Id).ToList();

        // Step 2: Get stage IDs from ChecklistStageMapping
        var stageMappings = await db.Queryable<ChecklistStageMapping>()
            .Where(m => m.IsValid && checklistIds.Contains(m.ChecklistId))
            .Select(m => new { m.ChecklistId, m.StageId })
            .ToListAsync();

        if (!stageMappings.Any())
        {
            return new List<DashboardTaskInfo>();
        }

        var stageIds = stageMappings.Select(m => m.StageId).Distinct().ToList();

        // Step 3: Get workflow IDs from stages
        var stages = await db.Queryable<Stage>()
            .Where(s => s.IsValid && stageIds.Contains(s.Id))
            .Select(s => new { s.Id, s.WorkflowId })
            .ToListAsync();

        var workflowIds = stages.Select(s => s.WorkflowId).Distinct().ToList();

        // Step 4: Get active onboardings for these workflows
        var activeStatuses = new[] { "Started", "InProgress", "Active" };
        var onboardings = await db.Queryable<Onboarding>()
            .Where(o => o.IsValid && workflowIds.Contains(o.WorkflowId))
            .Where(o => o.TenantId == currentTenantId && o.AppCode == currentAppCode)
            .Where(o => activeStatuses.Contains(o.Status))
            .Select(o => new { o.Id, o.WorkflowId, o.CaseCode, o.CaseName, o.Priority })
            .ToListAsync();

        if (!onboardings.Any())
        {
            return new List<DashboardTaskInfo>();
        }

        // Build mapping: workflowId -> onboardings
        var workflowOnboardingMap = onboardings
            .GroupBy(o => o.WorkflowId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Build mapping: stageId -> workflowId
        var stageWorkflowMap = stages.ToDictionary(s => s.Id, s => s.WorkflowId);

        // Build mapping: checklistId -> stageIds
        var checklistStageMap = stageMappings
            .GroupBy(m => m.ChecklistId)
            .ToDictionary(g => g.Key, g => g.Select(m => m.StageId).ToList());

        // Step 5: Get completions for these tasks and onboardings
        var onboardingIds = onboardings.Select(o => o.Id).ToList();
        var completions = await db.Queryable<ChecklistTaskCompletion>()
            .Where(c => c.IsValid && onboardingIds.Contains(c.OnboardingId) && taskIds.Contains(c.TaskId) && c.IsCompleted)
            .ToListAsync();

        var completedSet = completions.Select(c => (c.OnboardingId, c.TaskId)).ToHashSet();

        // Step 6: Build result - match tasks to onboardings via checklist -> stage -> workflow -> onboarding
        var results = new List<DashboardTaskInfo>();

        foreach (var task in userTasks)
        {
            // Get stages for this task's checklist
            if (!checklistStageMap.TryGetValue(task.ChecklistId, out var taskStageIds))
                continue;

            // Get workflows for these stages, then get onboardings
            foreach (var stageId in taskStageIds)
            {
                if (!stageWorkflowMap.TryGetValue(stageId, out var workflowId))
                    continue;

                if (!workflowOnboardingMap.TryGetValue(workflowId, out var workflowOnboardings))
                    continue;

                foreach (var onboarding in workflowOnboardings)
                {
                    // Skip if task is completed for this onboarding
                    if (completedSet.Contains((onboarding.Id, task.Id)))
                        continue;

                    results.Add(new DashboardTaskInfo
                    {
                        Id = task.Id,
                        Name = task.Name,
                        Description = task.Description,
                        Priority = onboarding.Priority ?? "Medium",
                        DueDate = task.DueDate,
                        IsCompleted = false,
                        IsRequired = task.IsRequired,
                        AssignedTeam = task.AssignedTeam,
                        AssigneeName = task.AssigneeName,
                        AssigneeId = task.AssigneeId,
                        Status = task.Status,
                        ChecklistId = task.ChecklistId,
                        OnboardingId = onboarding.Id,
                        CaseCode = onboarding.CaseCode,
                        CaseName = onboarding.CaseName
                    });
                }
            }
        }

        // Remove duplicates (same task + onboarding combination)
        results = results
            .GroupBy(r => (r.Id, r.OnboardingId))
            .Select(g => g.First())
            .ToList();

        // Apply category filter
        if (!string.IsNullOrEmpty(category))
        {
            if (category.Equals("Sales", StringComparison.OrdinalIgnoreCase))
            {
                results = results.Where(r => r.AssignedTeam != null && r.AssignedTeam.ToLower().Contains("sales")).ToList();
            }
            else if (category.Equals("Account", StringComparison.OrdinalIgnoreCase))
            {
                results = results.Where(r => r.AssignedTeam != null && r.AssignedTeam.ToLower().Contains("account")).ToList();
            }
        }

        // Sort by DueDate and apply pagination
        return results
            .OrderBy(r => r.DueDate)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    /// <summary>
    /// Get count of pending tasks for user
    /// Only counts tasks assigned to user with active onboardings
    /// </summary>
    public async Task<int> GetPendingTasksCountForUserAsync(long userId, List<long> userTeamIds, string? category)
    {
        var currentTenantId = GetCurrentTenantId();
        var currentAppCode = GetCurrentAppCode();

        _logger.LogInformation($"[ChecklistTaskRepository] GetPendingTasksCountForUserAsync with TenantId={currentTenantId}, AppCode={currentAppCode}, UserId={userId}");

        // Step 1: Get tasks assigned to current user
        var userTasks = await db.Queryable<ChecklistTask>()
            .Where(t => t.IsValid && t.AssigneeId == userId)
            .Where(t => t.TenantId == currentTenantId && t.AppCode == currentAppCode)
            .ToListAsync();

        if (!userTasks.Any())
        {
            return 0;
        }

        var checklistIds = userTasks.Select(t => t.ChecklistId).Distinct().ToList();
        var taskIds = userTasks.Select(t => t.Id).ToList();

        // Step 2: Get stage IDs from ChecklistStageMapping
        var stageMappings = await db.Queryable<ChecklistStageMapping>()
            .Where(m => m.IsValid && checklistIds.Contains(m.ChecklistId))
            .Select(m => new { m.ChecklistId, m.StageId })
            .ToListAsync();

        if (!stageMappings.Any())
        {
            return 0;
        }

        var stageIds = stageMappings.Select(m => m.StageId).Distinct().ToList();

        // Step 3: Get workflow IDs from stages
        var stages = await db.Queryable<Stage>()
            .Where(s => s.IsValid && stageIds.Contains(s.Id))
            .Select(s => new { s.Id, s.WorkflowId })
            .ToListAsync();

        var workflowIds = stages.Select(s => s.WorkflowId).Distinct().ToList();

        // Step 4: Get active onboardings for these workflows
        var activeStatuses = new[] { "Started", "InProgress", "Active" };
        var onboardings = await db.Queryable<Onboarding>()
            .Where(o => o.IsValid && workflowIds.Contains(o.WorkflowId))
            .Where(o => o.TenantId == currentTenantId && o.AppCode == currentAppCode)
            .Where(o => activeStatuses.Contains(o.Status))
            .Select(o => new { o.Id, o.WorkflowId })
            .ToListAsync();

        if (!onboardings.Any())
        {
            return 0;
        }

        var workflowOnboardingMap = onboardings
            .GroupBy(o => o.WorkflowId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var stageWorkflowMap = stages.ToDictionary(s => s.Id, s => s.WorkflowId);

        var checklistStageMap = stageMappings
            .GroupBy(m => m.ChecklistId)
            .ToDictionary(g => g.Key, g => g.Select(m => m.StageId).ToList());

        // Step 5: Get completions
        var onboardingIds = onboardings.Select(o => o.Id).ToList();
        var completions = await db.Queryable<ChecklistTaskCompletion>()
            .Where(c => c.IsValid && onboardingIds.Contains(c.OnboardingId) && taskIds.Contains(c.TaskId) && c.IsCompleted)
            .ToListAsync();

        var completedSet = completions.Select(c => (c.OnboardingId, c.TaskId)).ToHashSet();

        // Step 6: Count pending tasks (unique task + onboarding combinations)
        var taskOnboardingPairs = new HashSet<(long TaskId, long OnboardingId)>();

        foreach (var task in userTasks)
        {
            if (!checklistStageMap.TryGetValue(task.ChecklistId, out var taskStageIds))
                continue;

            // Apply category filter
            if (!string.IsNullOrEmpty(category))
            {
                if (category.Equals("Sales", StringComparison.OrdinalIgnoreCase))
                {
                    if (task.AssignedTeam == null || !task.AssignedTeam.ToLower().Contains("sales")) continue;
                }
                else if (category.Equals("Account", StringComparison.OrdinalIgnoreCase))
                {
                    if (task.AssignedTeam == null || !task.AssignedTeam.ToLower().Contains("account")) continue;
                }
            }

            foreach (var stageId in taskStageIds)
            {
                if (!stageWorkflowMap.TryGetValue(stageId, out var workflowId))
                    continue;

                if (!workflowOnboardingMap.TryGetValue(workflowId, out var workflowOnboardings))
                    continue;

                foreach (var onboarding in workflowOnboardings)
                {
                    if (!completedSet.Contains((onboarding.Id, task.Id)))
                    {
                        taskOnboardingPairs.Add((task.Id, onboarding.Id));
                    }
                }
            }
        }

        return taskOnboardingPairs.Count;
    }

    /// <summary>
    /// Get tasks with upcoming deadlines for user
    /// Only returns tasks assigned to user with active onboardings
    /// </summary>
    public async Task<List<DashboardTaskInfo>> GetUpcomingDeadlinesAsync(
        long userId, 
        List<long> userTeamIds, 
        DateTimeOffset endDate)
    {
        var currentTenantId = GetCurrentTenantId();
        var currentAppCode = GetCurrentAppCode();

        _logger.LogInformation($"[ChecklistTaskRepository] GetUpcomingDeadlinesAsync with TenantId={currentTenantId}, AppCode={currentAppCode}, UserId={userId}");

        // Step 1: Get tasks assigned to current user with due dates
        var userTasks = await db.Queryable<ChecklistTask>()
            .Where(t => t.IsValid && t.AssigneeId == userId)
            .Where(t => t.TenantId == currentTenantId && t.AppCode == currentAppCode)
            .Where(t => t.DueDate.HasValue && t.DueDate.Value <= endDate)
            .ToListAsync();

        if (!userTasks.Any())
        {
            return new List<DashboardTaskInfo>();
        }

        var checklistIds = userTasks.Select(t => t.ChecklistId).Distinct().ToList();
        var taskIds = userTasks.Select(t => t.Id).ToList();

        // Step 2: Get stage IDs from ChecklistStageMapping
        var stageMappings = await db.Queryable<ChecklistStageMapping>()
            .Where(m => m.IsValid && checklistIds.Contains(m.ChecklistId))
            .Select(m => new { m.ChecklistId, m.StageId })
            .ToListAsync();

        if (!stageMappings.Any())
        {
            return new List<DashboardTaskInfo>();
        }

        var stageIds = stageMappings.Select(m => m.StageId).Distinct().ToList();

        // Step 3: Get workflow IDs from stages
        var stages = await db.Queryable<Stage>()
            .Where(s => s.IsValid && stageIds.Contains(s.Id))
            .Select(s => new { s.Id, s.WorkflowId })
            .ToListAsync();

        var workflowIds = stages.Select(s => s.WorkflowId).Distinct().ToList();

        // Step 4: Get active onboardings for these workflows
        var activeStatuses = new[] { "Started", "InProgress", "Active" };
        var onboardings = await db.Queryable<Onboarding>()
            .Where(o => o.IsValid && workflowIds.Contains(o.WorkflowId))
            .Where(o => o.TenantId == currentTenantId && o.AppCode == currentAppCode)
            .Where(o => activeStatuses.Contains(o.Status))
            .Select(o => new { o.Id, o.WorkflowId, o.CaseCode, o.CaseName, o.Priority })
            .ToListAsync();

        if (!onboardings.Any())
        {
            return new List<DashboardTaskInfo>();
        }

        var workflowOnboardingMap = onboardings
            .GroupBy(o => o.WorkflowId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var stageWorkflowMap = stages.ToDictionary(s => s.Id, s => s.WorkflowId);

        var checklistStageMap = stageMappings
            .GroupBy(m => m.ChecklistId)
            .ToDictionary(g => g.Key, g => g.Select(m => m.StageId).ToList());

        // Step 5: Get completions
        var onboardingIds = onboardings.Select(o => o.Id).ToList();
        var completions = await db.Queryable<ChecklistTaskCompletion>()
            .Where(c => c.IsValid && onboardingIds.Contains(c.OnboardingId) && taskIds.Contains(c.TaskId) && c.IsCompleted)
            .ToListAsync();

        var completedSet = completions.Select(c => (c.OnboardingId, c.TaskId)).ToHashSet();

        // Step 6: Build result
        var results = new List<DashboardTaskInfo>();

        foreach (var task in userTasks)
        {
            if (!checklistStageMap.TryGetValue(task.ChecklistId, out var taskStageIds))
                continue;

            foreach (var stageId in taskStageIds)
            {
                if (!stageWorkflowMap.TryGetValue(stageId, out var workflowId))
                    continue;

                if (!workflowOnboardingMap.TryGetValue(workflowId, out var workflowOnboardings))
                    continue;

                foreach (var onboarding in workflowOnboardings)
                {
                    if (completedSet.Contains((onboarding.Id, task.Id)))
                        continue;

                    results.Add(new DashboardTaskInfo
                    {
                        Id = task.Id,
                        Name = task.Name,
                        Description = task.Description,
                        Priority = onboarding.Priority ?? "Medium",
                        DueDate = task.DueDate,
                        IsCompleted = false,
                        IsRequired = task.IsRequired,
                        AssignedTeam = task.AssignedTeam,
                        AssigneeName = task.AssigneeName,
                        AssigneeId = task.AssigneeId,
                        Status = task.Status,
                        ChecklistId = task.ChecklistId,
                        OnboardingId = onboarding.Id,
                        CaseCode = onboarding.CaseCode,
                        CaseName = onboarding.CaseName
                    });
                }
            }
        }

        // Remove duplicates
        results = results
            .GroupBy(r => (r.Id, r.OnboardingId))
            .Select(g => g.First())
            .ToList();

        return results.OrderBy(r => r.DueDate).ToList();
    }

    #endregion
}
