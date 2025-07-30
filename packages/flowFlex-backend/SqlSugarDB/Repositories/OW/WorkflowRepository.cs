using Microsoft.Extensions.Logging;
using SqlSugar;
using System.Linq;
using System.Linq.Expressions;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using Microsoft.AspNetCore.Http;
using AppContext = FlowFlex.Domain.Shared.Models.AppContext;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// Workflow repository implementation
    /// </summary>
    public class WorkflowRepository : BaseRepository<Workflow>, IWorkflowRepository, IScopedService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<WorkflowRepository> _logger;

        public WorkflowRepository(
            ISqlSugarClient sqlSugarClient,
            IHttpContextAccessor httpContextAccessor,
            ILogger<WorkflowRepository> logger) : base(sqlSugarClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Paginated query of workflows
        /// </summary>
        public async Task<(List<Workflow> items, int total)> QueryPagedAsync(int pageIndex, int pageSize, string name = null, bool? isActive = null)
        {
            // Build query condition list
            var whereExpressions = new List<Expression<Func<Workflow, bool>>>();

            // Basic filter conditions
            whereExpressions.Add(x => x.IsValid == true);

            if (!string.IsNullOrWhiteSpace(name))
            {
                whereExpressions.Add(x => x.Name.ToLower().Contains(name.ToLower()));
            }

            if (isActive.HasValue)
            {
                whereExpressions.Add(x => x.IsActive == isActive.Value);
            }

            // Use BaseRepository's safe pagination method
            var (items, total) = await GetPageListAsync(
                whereExpressions,
                pageIndex,
                pageSize,
                orderByExpression: x => x.CreateDate,
                isAsc: false // Descending order
            );

            return (items, total);
        }

        /// <summary>
        /// Query workflow by name
        /// </summary>
        public async Task<Workflow> GetByNameAsync(string name)
        {
            return await db.Queryable<Workflow>()
                .Where(x => x.Name == name && x.IsValid == true)
                .FirstAsync();
        }

        /// <summary>
        /// Get default workflow
        /// </summary>
        public async Task<Workflow> GetDefaultWorkflowAsync()
        {
            return await db.Queryable<Workflow>()
                .Where(x => x.IsDefault == true && x.IsValid == true)
                .FirstAsync();
        }

        /// <summary>
        /// Set default workflow (cancel other default states)
        /// </summary>
        public async Task<bool> SetDefaultWorkflowAsync(long workflowId)
        {
            try
            {
                await db.Ado.BeginTranAsync();

                // Cancel default state of all other workflows
                await db.Updateable<Workflow>()
                    .SetColumns(x => x.IsDefault == false)
                    .Where(x => x.IsValid == true && x.Id != workflowId)
                    .ExecuteCommandAsync();

                // Set specified workflow as default
                await db.Updateable<Workflow>()
                    .SetColumns(x => x.IsDefault == true)
                    .Where(x => x.Id == workflowId && x.IsValid == true)
                    .ExecuteCommandAsync();

                await db.Ado.CommitTranAsync();
                return true;
            }
            catch
            {
                await db.Ado.RollbackTranAsync();
                throw;
            }
        }

        /// <summary>
        /// Remove default workflow
        /// </summary>
        public async Task<bool> RemoveDefaultWorkflowAsync(long workflowId)
        {
            var result = await db.Updateable<Workflow>()
                .SetColumns(x => x.IsDefault == false)
                .Where(x => x.Id == workflowId && x.IsValid == true)
                .ExecuteCommandAsync();

            return result > 0;
        }

        /// <summary>
        /// Get workflow with stages
        /// </summary>
        public async Task<Workflow> GetWithStagesAsync(long id)
        {
            var workflow = await db.Queryable<Workflow>()
                .Where(x => x.Id == id && x.IsValid == true)
                .FirstAsync();

            if (workflow != null)
            {
                workflow.Stages = await db.Queryable<Stage>()
                    .Where(x => x.WorkflowId == id && x.IsValid == true)
                    .OrderBy(x => x.Order)
                    .ToListAsync();
            }

            return workflow;
        }

        /// <summary>
        /// Check if workflow name exists
        /// </summary>
        public async Task<bool> ExistsNameAsync(string name, long? excludeId = null)
        {
            var query = db.Queryable<Workflow>()
                .Where(x => x.Name == name && x.IsValid == true);

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// Batch update workflow status
        /// </summary>
        public async Task<bool> BatchUpdateStatusAsync(List<long> ids, string status)
        {
            var result = await db.Updateable<Workflow>()
                .SetColumns(x => x.Status == status)
                .Where(x => ids.Contains(x.Id) && x.IsValid == true)
                .ExecuteCommandAsync();

            return result > 0;
        }

        /// <summary>
        /// Get active workflow list
        /// </summary>
        public async Task<List<Workflow>> GetActiveWorkflowsAsync()
        {
            // 记录当前请求头
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var headerTenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
                var headerAppCode = httpContext.Request.Headers["X-App-Code"].FirstOrDefault();
                _logger.LogInformation($"[WorkflowRepository] GetActiveWorkflowsAsync with headers: X-Tenant-Id={headerTenantId}, X-App-Code={headerAppCode}");
            }

            // 显式添加租户和应用过滤条件
            var query = db.Queryable<Workflow>()
                .Where(x => x.IsActive == true && x.IsValid == true);
            
            // 获取当前租户ID和应用代码
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();
            
            _logger.LogInformation($"[WorkflowRepository] GetActiveWorkflowsAsync applying explicit filters: TenantId={currentTenantId}, AppCode={currentAppCode}");
            
            // 显式添加过滤条件
            query = query.Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode);
            
            // 执行查询
            var result = await query.OrderBy(x => x.Name).ToListAsync();
            
            _logger.LogInformation($"[WorkflowRepository] GetActiveWorkflowsAsync returned {result.Count} workflows with TenantId={currentTenantId}, AppCode={currentAppCode}");
            
            return result;
        }

        /// <summary>
        /// Get all workflow list (including stage information)
        /// </summary>
        public async Task<List<Workflow>> GetAllWorkflowsAsync()
        {
            // 记录当前请求头
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var headerTenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
                var headerAppCode = httpContext.Request.Headers["X-App-Code"].FirstOrDefault();
                _logger.LogInformation($"[WorkflowRepository] GetAllWorkflowsAsync with headers: X-Tenant-Id={headerTenantId}, X-App-Code={headerAppCode}");
            }

            // 显式添加租户和应用过滤条件
            var query = db.Queryable<Workflow>().Where(x => x.IsValid == true);
            
            // 获取当前租户ID和应用代码
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();
            
            _logger.LogInformation($"[WorkflowRepository] GetAllWorkflowsAsync applying explicit filters: TenantId={currentTenantId}, AppCode={currentAppCode}");
            
            // 显式添加过滤条件
            query = query.Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode);
            
            // 包含关联的 Stages
            query = query.Includes(x => x.Stages.Where(s => s.IsValid == true).ToList());
            
            // 执行查询
            var result = await query.OrderByDescending(x => x.CreateDate).ToListAsync();
            
            _logger.LogInformation($"[WorkflowRepository] GetAllWorkflowsAsync returned {result.Count} workflows with TenantId={currentTenantId}, AppCode={currentAppCode}");
            
            return result;
        }

        /// <summary>
        /// Get all valid workflows (optimized version)
        /// </summary>
        public async Task<List<Workflow>> GetAllOptimizedAsync()
        {
            // 记录当前请求头
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var headerTenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
                var headerAppCode = httpContext.Request.Headers["X-App-Code"].FirstOrDefault();
                _logger.LogInformation($"[WorkflowRepository] GetAllOptimizedAsync with headers: X-Tenant-Id={headerTenantId}, X-App-Code={headerAppCode}");
            }

            // 显式添加租户和应用过滤条件
            var query = db.Queryable<Workflow>().Where(x => x.IsValid == true);
            
            // 获取当前租户ID和应用代码
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();
            
            _logger.LogInformation($"[WorkflowRepository] GetAllOptimizedAsync applying explicit filters: TenantId={currentTenantId}, AppCode={currentAppCode}");
            
            // 显式添加过滤条件
            query = query.Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode);
            
            // 执行查询
            var result = await query.OrderByDescending(x => x.CreateDate).ToListAsync();
            
            _logger.LogInformation($"[WorkflowRepository] GetAllOptimizedAsync returned {result.Count} workflows with TenantId={currentTenantId}, AppCode={currentAppCode}");
            
            return result;
        }

        /// <summary>
        /// Get expired workflows that are still active
        /// </summary>
        public async Task<List<Workflow>> GetExpiredActiveWorkflowsAsync()
        {
            var currentDate = DateTimeOffset.UtcNow;

            return await db.Queryable<Workflow>()
                .Where(x => x.IsValid == true
                    && x.IsActive == true
                    && x.EndDate.HasValue
                    && x.EndDate.Value < currentDate)
                .OrderBy(x => x.EndDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get workflows expiring soon (advance reminder by specified days)
        /// </summary>
        public async Task<List<Workflow>> GetExpiringWorkflowsAsync(int daysAhead = 7)
        {
            var currentDate = DateTimeOffset.UtcNow;
            var futureDate = currentDate.AddDays(daysAhead);

            return await db.Queryable<Workflow>()
                .Where(x => x.IsValid == true
                    && x.IsActive == true
                    && x.EndDate.HasValue
                    && x.EndDate.Value >= currentDate
                    && x.EndDate.Value <= futureDate)
                .OrderBy(x => x.EndDate)
                .ToListAsync();
        }

        /// <summary>
        /// Batch get workflow information (optimized version)
        /// </summary>
        public async Task<List<Workflow>> GetBatchByIdsAsync(List<long> workflowIds)
        {
            if (!workflowIds?.Any() == true)
                return new List<Workflow>();

            return await db.Queryable<Workflow>()
                .Where(x => workflowIds.Contains(x.Id) && x.IsValid == true)
                .OrderByDescending(x => x.CreateDate)
                .ToListAsync();
        }

        /// <summary>
        /// 获取所有工作流列表，确保应用租户和应用过滤器
        /// </summary>
        public override async Task<List<Workflow>> GetListAsync(CancellationToken cancellationToken = default, bool copyNew = false)
        {
            // 记录当前请求头
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var headerTenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
                var headerAppCode = httpContext.Request.Headers["X-App-Code"].FirstOrDefault();
                _logger.LogInformation($"[WorkflowRepository] GetListAsync with headers: X-Tenant-Id={headerTenantId}, X-App-Code={headerAppCode}");
            }

            // 显式添加租户和应用过滤条件
            var query = db.Queryable<Workflow>().Where(x => x.IsValid == true);
            
            // 获取当前租户ID和应用代码
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();
            
            _logger.LogInformation($"[WorkflowRepository] Applying explicit filters: TenantId={currentTenantId}, AppCode={currentAppCode}");
            
            // 显式添加过滤条件
            query = query.Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode);
            
            // 执行查询
            var result = await query.OrderBy(x => x.CreateDate, OrderByType.Desc).ToListAsync(cancellationToken);
            
            _logger.LogInformation($"[WorkflowRepository] Query returned {result.Count} workflows with TenantId={currentTenantId}, AppCode={currentAppCode}");
            
            return result;
        }

        /// <summary>
        /// 直接查询，使用显式过滤条件
        /// </summary>
        public async Task<List<Workflow>> GetListWithExplicitFiltersAsync(string tenantId, string appCode)
        {
            _logger.LogInformation($"[WorkflowRepository] GetListWithExplicitFiltersAsync with explicit TenantId={tenantId}, AppCode={appCode}");
            
            // 临时禁用全局过滤器
            db.QueryFilter.ClearAndBackup();
            
            try
            {
                // 使用显式过滤条件
                var query = db.Queryable<Workflow>()
                    .Where(x => x.IsValid == true)
                    .Where(x => x.TenantId == tenantId && x.AppCode == appCode);
                
                // 执行查询
                var result = await query.OrderBy(x => x.CreateDate, OrderByType.Desc).ToListAsync();
                
                _logger.LogInformation($"[WorkflowRepository] Query returned {result.Count} workflows with explicit filters");
                
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
                appContextObj is AppContext appContext)
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
                appContextObj is AppContext appContext)
            {
                return appContext.AppCode;
            }

            return "DEFAULT";
        }
    }
}
