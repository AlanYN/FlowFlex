using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using SqlSugar;
using System.Linq;
using System.Linq.Expressions;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// Stage repository implementation
    /// </summary>
    public class StageRepository : BaseRepository<Stage>, IStageRepository, IScopedService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<StageRepository> _logger;

        public StageRepository(ISqlSugarClient sqlSugarClient, IHttpContextAccessor httpContextAccessor, ILogger<StageRepository> logger) : base(sqlSugarClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Get current tenant ID from HTTP context
        /// </summary>
        private string GetCurrentTenantId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var tenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
                if (!string.IsNullOrEmpty(tenantId))
                {
                    return tenantId;
                }
            }
            return "999"; // Default tenant ID
        }

        /// <summary>
        /// Get current app code from HTTP context
        /// </summary>
        private string GetCurrentAppCode()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var appCode = httpContext.Request.Headers["X-App-Code"].FirstOrDefault();
                if (!string.IsNullOrEmpty(appCode))
                {
                    return appCode;
                }
            }
            return "OW"; // Default app code
        }

        /// <summary>
        /// Get stage list by expression with tenant isolation
        /// </summary>
        public new async Task<List<Stage>> GetListAsync(Expression<Func<Stage, bool>> whereExpression, CancellationToken cancellationToken = default, bool copyNew = false)
        {
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            _logger.LogInformation($"[StageRepository] GetListAsync(Expression) applying filters: TenantId={currentTenantId}, AppCode={currentAppCode}");

            var dbNew = copyNew ? db.CopyNew() : db;
            dbNew.Ado.CancellationToken = cancellationToken;

            var result = await dbNew.Queryable<Stage>()
                .Where(whereExpression)
                .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
                .ToListAsync();

            _logger.LogInformation($"[StageRepository] GetListAsync(Expression) returned {result.Count} stages");

            return result;
        }

        /// <summary>
        /// Get stage list by workflow ID
        /// </summary>
        public async Task<List<Stage>> GetByWorkflowIdAsync(long workflowId)
        {
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            _logger.LogInformation($"[StageRepository] GetByWorkflowIdAsync with TenantId={currentTenantId}, AppCode={currentAppCode}, WorkflowId={workflowId}");

            return await db.Queryable<Stage>()
                .Where(x => x.WorkflowId == workflowId && x.IsValid == true)
                .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
                .OrderBy(x => x.Order)
                .ToListAsync();
        }

        /// <summary>
        /// Batch get stage lists by multiple workflow IDs (performance optimization)
        /// </summary>
        public async Task<List<Stage>> GetByWorkflowIdsAsync(List<long> workflowIds)
        {
            if (workflowIds == null || !workflowIds.Any())
            {
                return new List<Stage>();
            }

            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            _logger.LogInformation($"[StageRepository] GetByWorkflowIdsAsync with TenantId={currentTenantId}, AppCode={currentAppCode}, WorkflowIds count={workflowIds.Count}");

            return await db.Queryable<Stage>()
                .Where(x => workflowIds.Contains(x.WorkflowId) && x.IsValid == true)
                .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
                .OrderBy(x => x.WorkflowId)
                .OrderBy(x => x.Order)
                .ToListAsync();
        }

        /// <summary>
        /// Paginated query of stages
        /// </summary>
        public async Task<(List<Stage> items, int total)> QueryPagedAsync(int pageIndex, int pageSize, long? workflowId = null, string name = null)
        {
            // Get current tenant ID and app code
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            _logger.LogInformation($"[StageRepository] QueryPagedAsync with TenantId={currentTenantId}, AppCode={currentAppCode}, WorkflowId={workflowId}");

            // Build query condition list
            var whereExpressions = new List<Expression<Func<Stage, bool>>>();

            // Basic filter conditions
            whereExpressions.Add(x => x.IsValid == true);

            // Add tenant and app code filters
            whereExpressions.Add(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode);

            if (workflowId.HasValue)
            {
                whereExpressions.Add(x => x.WorkflowId == workflowId.Value);
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                whereExpressions.Add(x => x.Name.ToLower().Contains(name.ToLower()));
            }

            // Use BaseRepository's safe pagination method
            var (items, total) = await GetPageListAsync(
                whereExpressions,
                pageIndex,
                pageSize,
                orderByExpression: x => x.Order, // Sort by Order
                isAsc: true // Ascending order
            );

            return (items, total);
        }

        /// <summary>
        /// Get maximum order number in workflow
        /// </summary>
        public async Task<int> GetMaxOrderByWorkflowIdAsync(long workflowId)
        {
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            var maxOrder = await db.Queryable<Stage>()
                .Where(x => x.WorkflowId == workflowId && x.IsValid == true)
                .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
                .MaxAsync(x => (int?)x.Order);

            return maxOrder ?? 0;
        }

        /// <summary>
        /// Batch update stage order
        /// </summary>
        public async Task<bool> BatchUpdateOrderAsync(List<(long stageId, int order)> stageOrders)
        {
            try
            {
                await db.Ado.BeginTranAsync();

                foreach (var (stageId, order) in stageOrders)
                {
                    await db.Updateable<Stage>()
                        .SetColumns(x => x.Order == order)
                        .Where(x => x.Id == stageId && x.IsValid == true)
                        .ExecuteCommandAsync();
                }

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
        /// Get stages by workflow ID and order range
        /// </summary>
        public async Task<List<Stage>> GetByWorkflowIdAndOrderRangeAsync(long workflowId, int minOrder, int maxOrder)
        {
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            return await db.Queryable<Stage>()
                .Where(x => x.WorkflowId == workflowId && x.IsValid == true && x.Order >= minOrder && x.Order <= maxOrder)
                .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
                .OrderBy(x => x.Order)
                .ToListAsync();
        }

        /// <summary>
        /// Delete multiple stages
        /// </summary>
        public async Task<bool> BatchDeleteAsync(List<long> stageIds)
        {
            var result = await db.Updateable<Stage>()
                .SetColumns(x => x.IsValid == false)
                .Where(x => stageIds.Contains(x.Id))
                .ExecuteCommandAsync();

            return result > 0;
        }

        /// <summary>
        /// Get next order number for stage
        /// </summary>
        public async Task<int> GetNextOrderAsync(long workflowId)
        {
            var maxOrder = await GetMaxOrderByWorkflowIdAsync(workflowId);
            return maxOrder + 1;
        }

        /// <summary>
        /// Get stage count by color
        /// </summary>
        public async Task<int> GetCountByColorAsync(string color)
        {
            // Get current tenant ID and app code
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            _logger.LogInformation($"[StageRepository] GetCountByColorAsync with Color={color}, TenantId={currentTenantId}, AppCode={currentAppCode}");

            return await db.Queryable<Stage>()
                .Where(x => x.Color == color && x.IsValid == true)
                .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
                .CountAsync();
        }

        /// <summary>
        /// Get active stages in workflow
        /// </summary>
        public async Task<List<Stage>> GetActiveStagesByWorkflowIdAsync(long workflowId)
        {
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            _logger.LogInformation($"[StageRepository] GetActiveStagesByWorkflowIdAsync with TenantId={currentTenantId}, AppCode={currentAppCode}, WorkflowId={workflowId}");

            return await db.Queryable<Stage>()
                .Where(x => x.WorkflowId == workflowId && x.IsActive == true && x.IsValid == true)
                .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
                .OrderBy(x => x.Order)
                .ToListAsync();
        }

        /// <summary>
        /// Batch update stage status
        /// </summary>
        public async Task<bool> BatchUpdateActiveStatusAsync(List<long> stageIds, bool isActive)
        {
            var result = await db.Updateable<Stage>()
                .SetColumns(x => x.IsActive == isActive)
                .Where(x => stageIds.Contains(x.Id) && x.IsValid == true)
                .ExecuteCommandAsync();

            return result > 0;
        }

        /// <summary>
        /// Get all valid stages (optimized version)
        /// </summary>
        public async Task<List<Stage>> GetAllOptimizedAsync()
        {
            // Get current tenant ID and app code
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            _logger.LogInformation($"[StageRepository] GetAllOptimizedAsync with TenantId={currentTenantId}, AppCode={currentAppCode}");

            return await db.Queryable<Stage>()
                .Where(x => x.IsValid == true)
                .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
                .OrderBy(x => x.WorkflowId, OrderByType.Asc)
                .OrderBy(x => x.Order, OrderByType.Asc)
                .ToListAsync();
        }

        /// <summary>
        /// Check if stage name exists in workflow
        /// </summary>
        public async Task<bool> ExistsNameInWorkflowAsync(long workflowId, string name, long? excludeId = null)
        {
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            var query = db.Queryable<Stage>()
                .Where(x => x.WorkflowId == workflowId && x.Name == name && x.IsValid == true)
                .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode);

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// Check if stage name exists in workflow (renamed method)
        /// </summary>
        public async Task<bool> IsNameExistsInWorkflowAsync(long workflowId, string name, long? excludeId = null)
        {
            return await ExistsNameInWorkflowAsync(workflowId, name, excludeId);
        }

        /// <summary>
        /// Batch get stage information (optimized version)
        /// </summary>
        public async Task<List<Stage>> GetBatchByIdsAsync(List<long> stageIds)
        {
            if (!stageIds?.Any() == true)
                return new List<Stage>();

            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            return await db.Queryable<Stage>()
                .Where(x => stageIds.Contains(x.Id) && x.IsValid == true)
                .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
                .OrderBy(x => x.WorkflowId, OrderByType.Asc)
                .OrderBy(x => x.Order, OrderByType.Asc)
                .ToListAsync();
        }
    }
}
