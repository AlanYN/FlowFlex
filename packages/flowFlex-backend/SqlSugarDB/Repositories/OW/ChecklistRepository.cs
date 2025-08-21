using SqlSugar;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.SqlSugarDB.Context;
using FlowFlex.Domain.Shared;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;

namespace FlowFlex.SqlSugarDB.Repositories.OW;

/// <summary>
/// Checklist Repository
/// </summary>
public class ChecklistRepository : BaseRepository<Checklist>, IChecklistRepository, IScopedService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ChecklistRepository> _logger;

    public ChecklistRepository(
        ISqlSugarClient sqlSugarClient, 
        IHttpContextAccessor httpContextAccessor,
        ILogger<ChecklistRepository> logger) : base(sqlSugarClient)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有清单列表，确保应用租户和应用过滤器
    /// </summary>
    public override async Task<List<Checklist>> GetListAsync(CancellationToken cancellationToken = default, bool copyNew = false)
    {
        // 记录当前请求头
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var headerTenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            var headerAppCode = httpContext.Request.Headers["X-App-Code"].FirstOrDefault();
            _logger.LogInformation($"[ChecklistRepository] GetListAsync with headers: X-Tenant-Id={headerTenantId}, X-App-Code={headerAppCode}");
        }

        // 显式添加租户和应用过滤条件
        var query = db.Queryable<Checklist>().Where(x => x.IsValid == true);
        
        // 获取当前租户ID和应用代码
        var currentTenantId = GetCurrentTenantId();
        var currentAppCode = GetCurrentAppCode();
        
        _logger.LogInformation($"[ChecklistRepository] Applying explicit filters: TenantId={currentTenantId}, AppCode={currentAppCode}");
        
        // 显式添加过滤条件
        query = query.Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode);
        
        // 执行查询
        var result = await query.OrderBy(x => x.CreateDate, OrderByType.Desc).ToListAsync(cancellationToken);
        
        _logger.LogInformation($"[ChecklistRepository] Query returned {result.Count} checklists with TenantId={currentTenantId}, AppCode={currentAppCode}");
        
        return result;
    }

    /// <summary>
    /// Get checklists by team
    /// </summary>
    public async Task<List<Checklist>> GetByTeamAsync(string team)
    {
        // 获取当前租户ID和应用代码
        var currentTenantId = GetCurrentTenantId();
        var currentAppCode = GetCurrentAppCode();
        
        _logger.LogInformation($"[ChecklistRepository] GetByTeamAsync with team={team}, TenantId={currentTenantId}, AppCode={currentAppCode}");
        
        return await db.Queryable<Checklist>()
            .WhereIF(!string.IsNullOrEmpty(team), x => x.Team == team)
            .Where(x => x.IsValid == true)
            .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode) // 显式添加过滤条件
            .OrderBy(x => x.CreateDate, SqlSugar.OrderByType.Desc)
            .ToListAsync();
    }

    /// <summary>
    /// Get checklists by multiple IDs (batch query)
    /// </summary>
    public async Task<List<Checklist>> GetByIdsAsync(List<long> ids)
    {
        if (ids == null || !ids.Any())
        {
            return new List<Checklist>();
        }

        return await db.Queryable<Checklist>()
            .Where(x => x.IsValid == true && ids.Contains(x.Id))
            .OrderBy(x => x.CreateDate, SqlSugar.OrderByType.Desc)
            .ToListAsync();
    }

    /// <summary>
    /// Get template checklists
    /// </summary>
    public async Task<List<Checklist>> GetTemplatesAsync()
    {
        // 获取当前租户ID和应用代码
        var currentTenantId = GetCurrentTenantId();
        var currentAppCode = GetCurrentAppCode();
        
        _logger.LogInformation($"[ChecklistRepository] GetTemplatesAsync with TenantId={currentTenantId}, AppCode={currentAppCode}");
        
        return await db.Queryable<Checklist>()
            .Where(x => x.IsTemplate == true && x.IsValid == true)
            .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode) // 显式添加过滤条件
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
        // 获取当前租户ID和应用代码
        var currentTenantId = GetCurrentTenantId();
        var currentAppCode = GetCurrentAppCode();
        
        _logger.LogInformation($"[ChecklistRepository] IsNameExistsAsync with name={name}, team={team}, TenantId={currentTenantId}, AppCode={currentAppCode}");
        
        var whereExpression = Expressionable.Create<Checklist>()
            .And(x => x.Name == name)
            .And(x => x.Team == team)
            .And(x => x.IsValid == true)
            .And(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode) // 添加租户和应用代码过滤
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
        // 记录当前请求头
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var headerTenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            var headerAppCode = httpContext.Request.Headers["X-App-Code"].FirstOrDefault();
            _logger.LogInformation($"[ChecklistRepository] GetPagedAsync with headers: X-Tenant-Id={headerTenantId}, X-App-Code={headerAppCode}");
        }

        // Build query condition list
        var whereExpressions = new List<Expression<Func<Checklist, bool>>>();

        // Basic filter conditions
        whereExpressions.Add(x => x.IsValid == true);

        // 获取当前租户ID和应用代码
        var currentTenantId = GetCurrentTenantId();
        var currentAppCode = GetCurrentAppCode();
        
        _logger.LogInformation($"[ChecklistRepository] GetPagedAsync applying explicit filters: TenantId={currentTenantId}, AppCode={currentAppCode}");
        
        // 添加租户和应用过滤条件
        whereExpressions.Add(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode);

        if (!string.IsNullOrEmpty(name))
        {
            // Support comma-separated checklist names
            var names = name.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(n => n.Trim())
                           .Where(n => !string.IsNullOrEmpty(n))
                           .ToList();

            if (names.Any())
            {
                // Use OR condition to match any of the checklist names (case-insensitive)
                whereExpressions.Add(x => names.Any(n => x.Name.ToLower().Contains(n.ToLower())));
            }
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
        Expression<Func<Checklist, object>> orderByExpression = sortField?.ToLower() switch
        {
            "name" => x => x.Name,
            "team" => x => x.Team,
            "createdate" => x => x.CreateDate,
            "modifydate" => x => x.ModifyDate,
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

        _logger.LogInformation($"[ChecklistRepository] GetPagedAsync returned {items.Count} items, total count: {totalCount} with TenantId={currentTenantId}, AppCode={currentAppCode}");

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

    /// <summary>
    /// 直接查询，使用显式过滤条件
    /// </summary>
    public async Task<List<Checklist>> GetListWithExplicitFiltersAsync(string tenantId, string appCode)
    {
        _logger.LogInformation($"[ChecklistRepository] GetListWithExplicitFiltersAsync with explicit TenantId={tenantId}, AppCode={appCode}");
        
        // 临时禁用全局过滤器
        db.QueryFilter.ClearAndBackup();
        
        try
        {
            // 使用显式过滤条件
            var query = db.Queryable<Checklist>()
                .Where(x => x.IsValid == true)
                .Where(x => x.TenantId == tenantId && x.AppCode == appCode);
            
            // 执行查询
            var result = await query.OrderBy(x => x.CreateDate, OrderByType.Desc).ToListAsync();
            
            _logger.LogInformation($"[ChecklistRepository] Query returned {result.Count} checklists with explicit filters");
            
            return result;
        }
        finally
        {
            // 恢复全局过滤器
            db.QueryFilter.Restore();
        }
    }

    /// <summary>
    /// 获取当前租户ID
    /// </summary>
    private string GetCurrentTenantId()
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext == null)
            return "DEFAULT";

        // 从请求头获取
        var tenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(tenantId))
        {
            return tenantId;
        }

        // 从 AppContext 获取
        if (httpContext.Items.TryGetValue("AppContext", out var appContextObj) &&
            appContextObj is FlowFlex.Domain.Shared.Models.AppContext appContext)
        {
            return appContext.TenantId;
        }

        return "DEFAULT";
    }

    /// <summary>
    /// 获取当前应用代码
    /// </summary>
    private string GetCurrentAppCode()
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext == null)
            return "DEFAULT";

        // 从请求头获取
        var appCode = httpContext.Request.Headers["X-App-Code"].FirstOrDefault();
        if (!string.IsNullOrEmpty(appCode))
        {
            return appCode;
        }

        // 从 AppContext 获取
        if (httpContext.Items.TryGetValue("AppContext", out var appContextObj) &&
            appContextObj is FlowFlex.Domain.Shared.Models.AppContext appContext)
        {
            return appContext.AppCode;
        }

        return "DEFAULT";
    }
}
