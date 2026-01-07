using SqlSugar;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Linq.Expressions;
using System.Collections.Generic;
using AppContext = FlowFlex.Domain.Shared.Models.AppContext;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// Onboarding repository implementation
    /// </summary>
    public class OnboardingRepository : BaseRepository<Onboarding>, IOnboardingRepository, IScopedService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<OnboardingRepository> _logger;

        public OnboardingRepository(
            ISqlSugarClient context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<OnboardingRepository> logger) : base(context)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Ensure the onboarding table exists, create if necessary
        /// </summary>
        public async Task EnsureTableExistsAsync()
        {
            try
            {
                // Check if table exists
                var tableExists = db.DbMaintenance.IsAnyTable("ff_onboarding", false);

                if (!tableExists)
                {
                    // Debug logging handled by structured logging
                    // Create table using SqlSugar code first
                    db.CodeFirst.SetStringDefaultLength(200).InitTables<Onboarding>();
                    // Debug logging handled by structured logging
                    // Create performance optimization indexes
                    await CreatePerformanceIndexesAsync();
                }
                else
                {
                    // Debug logging handled by structured logging
                    // Ensure indexes exist
                    await EnsurePerformanceIndexesAsync();
                }
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                // Log but don't throw, let the insert try anyway
            }
        }

        /// <summary>
        /// Create performance optimization indexes
        /// </summary>
        private async Task CreatePerformanceIndexesAsync()
        {
            try
            {
                var indexQueries = new[]
                {
                    // Tenant ID + validity index - most important query condition
                    "CREATE INDEX IF NOT EXISTS idx_onboarding_tenant_valid ON ff_onboarding(tenant_id, is_valid)",
                    
                    // Tenant ID + validity + active status index
                    "CREATE INDEX IF NOT EXISTS idx_onboarding_tenant_valid_active ON ff_onboarding(tenant_id, is_valid, is_active)",
                    
                    // Lead ID index - for Lead related queries
                    "CREATE INDEX IF NOT EXISTS idx_onboarding_lead_id ON ff_onboarding(lead_id)",
                    
                    // Workflow ID index - for workflow queries
                    "CREATE INDEX IF NOT EXISTS idx_onboarding_workflow_id ON ff_onboarding(workflow_id)",
                    
                    // Current Stage ID index - for stage queries
                    "CREATE INDEX IF NOT EXISTS idx_onboarding_stage_id ON ff_onboarding(current_stage_id)",
                    
                    // Status index - for status filtering
                    "CREATE INDEX IF NOT EXISTS idx_onboarding_status ON ff_onboarding(status)",
                    
                    // Priority index - for priority filtering
                    "CREATE INDEX IF NOT EXISTS idx_onboarding_priority ON ff_onboarding(priority)",
                    
                    // Current assignee index - for assignee queries
                    "CREATE INDEX IF NOT EXISTS idx_onboarding_assignee_id ON ff_onboarding(current_assignee_id)",
                    
                    // Create time index - for sorting and time range queries
                    "CREATE INDEX IF NOT EXISTS idx_onboarding_create_date ON ff_onboarding(create_date)",
                    
                    // Start time index - for time range queries
                    "CREATE INDEX IF NOT EXISTS idx_onboarding_start_date ON ff_onboarding(start_date)",
                    
                    // Completion rate index - for progress queries
                    "CREATE INDEX IF NOT EXISTS idx_onboarding_completion_rate ON ff_onboarding(completion_rate)",
                    
                    // Composite index: tenant + Lead name (for fuzzy queries)
                    "CREATE INDEX IF NOT EXISTS idx_onboarding_tenant_lead_name ON ff_onboarding(tenant_id, lead_name)",
                    
                    // Composite index: tenant + status + create time (common query combination)
                    "CREATE INDEX IF NOT EXISTS idx_onboarding_tenant_status_create ON ff_onboarding(tenant_id, status, create_date)",
                };

                foreach (var query in indexQueries)
                {
                    try
                    {
                        await db.Ado.ExecuteCommandAsync(query);
                        // Debug logging handled by structured logging[5]}"); // Extract index name
                    }
                    catch (Exception ex)
                    {
                        // Debug logging handled by structured logging
                    }
                }
                // Debug logging handled by structured logging
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
            }
        }

        /// <summary>
        /// Ensure performance indexes exist
        /// </summary>
        private async Task EnsurePerformanceIndexesAsync()
        {
            try
            {
                // Check if key indexes exist, create if not
                var checkIndexQuery = @"
                    SELECT COUNT(*) 
                    FROM pg_indexes 
                    WHERE tablename = 'ff_onboarding' 
                    AND indexname = 'idx_onboarding_tenant_valid'";

                var indexExists = await db.Ado.GetIntAsync(checkIndexQuery);

                if (indexExists == 0)
                {
                    // Debug logging handled by structured logging
                    await CreatePerformanceIndexesAsync();
                }
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
            }
        }

        /// <summary>
        /// Get SqlSugar client for direct testing purposes
        /// </summary>
        public ISqlSugarClient GetSqlSugarClient()
        {
            return db;
        }

        /// <summary>
        /// Get onboarding by ID with tenant isolation
        /// Case-insensitive app_code comparison to support "default", "Default", "DEFAULT", etc.
        /// </summary>
        public new async Task<Onboarding> GetByIdAsync(object id, bool copyNew = false, CancellationToken cancellationToken = default)
        {
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();
            
            db.Ado.CancellationToken = cancellationToken;
            var dbNew = copyNew ? db.CopyNew() : db;
            
            // Use case-insensitive comparison for app_code to support all variations (default, Default, DEFAULT)
            return await dbNew.Queryable<Onboarding>()
                .Where(x => x.TenantId == currentTenantId && 
                           SqlFunc.ToUpper(x.AppCode) == SqlFunc.ToUpper(currentAppCode) && 
                           x.Id == Convert.ToInt64(id))
                .FirstAsync();
        }

        /// <summary>
        /// Get onboarding by ID without tenant isolation
        /// Used for background tasks where HttpContext is not available (e.g., AI Summary updates)
        /// </summary>
        public async Task<Onboarding> GetByIdWithoutTenantFilterAsync(long id, CancellationToken cancellationToken = default)
        {
            db.Ado.CancellationToken = cancellationToken;
            
            return await db.Queryable<Onboarding>()
                .Where(x => x.Id == id && x.IsValid)
                .FirstAsync();
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
        /// Keep original case for compatibility - comparison is case-insensitive at database level
        /// </summary>
        private string GetCurrentAppCode()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null)
                return "DEFAULT";

            string appCode = null;

            // 从请求头获取
            appCode = httpContext.Request.Headers["X-App-Code"].FirstOrDefault();
            if (string.IsNullOrEmpty(appCode))
            {
                appCode = httpContext.Request.Headers["AppCode"].FirstOrDefault();
            }

            // 从 AppContext 获取
            if (string.IsNullOrEmpty(appCode) &&
                httpContext.Items.TryGetValue("AppContext", out var appContextObj) &&
                appContextObj is AppContext appContext)
            {
                appCode = appContext.AppCode;
            }

            // Return original case - comparison will be case-insensitive at database level
            if (string.IsNullOrEmpty(appCode))
            {
                return "DEFAULT";
            }

            return appCode;
        }


        /// <summary>
        /// Get onboarding list by lead IDs (batch operation)
        /// </summary>
        public async Task<List<Onboarding>> GetByLeadIdsAsync(List<string> leadIds)
        {
            if (leadIds?.Any() != true)
            {
                return new List<Onboarding>();
            }

            return await GetListAsync(x => leadIds.Contains(x.LeadId) && x.IsValid);
        }

        /// <summary>
        /// Get onboarding list by workflow ID
        /// </summary>
        public async Task<List<Onboarding>> GetListByWorkflowIdAsync(long workflowId)
        {
            return await GetListAsync(x => x.WorkflowId == workflowId && x.IsValid);
        }

        /// <summary>
        /// Get onboarding list by stage ID
        /// </summary>
        public async Task<List<Onboarding>> GetListByStageIdAsync(long stageId)
        {
            return await GetListAsync(x => x.CurrentStageId == stageId && x.IsValid);
        }

        /// <summary>
        /// Get onboarding list by status
        /// </summary>
        public async Task<List<Onboarding>> GetListByStatusAsync(string status)
        {
            return await GetListAsync(x => x.Status == status && x.IsValid);
        }

        /// <summary>
        /// Get onboarding list by assignee ID
        /// </summary>
        public async Task<List<Onboarding>> GetListByAssigneeIdAsync(long assigneeId)
        {
            return await GetListAsync(x => x.CurrentAssigneeId == assigneeId && x.IsValid);
        }

        /// <summary>
        /// Update stage for onboarding
        /// </summary>
        public async Task<bool> UpdateStageAsync(long id, long stageId, int stageOrder)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            entity.CurrentStageId = stageId;
            entity.CurrentStageOrder = stageOrder;
            entity.ModifyDate = DateTimeOffset.UtcNow;

            // Use selective update to avoid JSONB type conflicts
            return await UpdateAsync(entity, it => new
            {
                it.CurrentStageId,
                it.CurrentStageOrder,
                it.ModifyDate,
                it.ModifyBy,
                it.ModifyUserId
            });
        }

        /// <summary>
        /// Update completion rate
        /// </summary>
        public async Task<bool> UpdateCompletionRateAsync(long id, decimal completionRate)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }
            // Debug logging handled by structured logging
            // Try raw SQL update as the most direct approach
            try
            {
                var modifyDate = DateTimeOffset.UtcNow;
                // Debug logging handled by structured logging
                var rawSql = @"
                    UPDATE ff_onboarding 
                    SET completion_rate = @CompletionRate, 
                        modify_date = @ModifyDate 
                    WHERE id = @Id";

                var parameters = new
                {
                    CompletionRate = completionRate,
                    ModifyDate = modifyDate,
                    Id = id
                };
                // Debug logging handled by structured logging
                var rowsAffected = await db.Ado.ExecuteCommandAsync(rawSql, parameters);
                // Debug logging handled by structured logging
                if (rowsAffected > 0)
                {
                    // Debug logging handled by structured logging
                    // Verify the update by reading back the data
                    var verifyEntity = await GetByIdAsync(id);
                    // Debug logging handled by structured logging
                    return true;
                }
                else
                {
                    // Debug logging handled by structured logging
                    return false;
                }
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
            }

            // Try direct SQL update with SqlSugar
            try
            {
                var modifyDate = DateTimeOffset.UtcNow;
                // Debug logging handled by structured logging
                var sqlResult = await db.Updateable<Onboarding>()
                    .SetColumns(x => new Onboarding
                    {
                        CompletionRate = completionRate,
                        ModifyDate = modifyDate
                    })
                    .Where(x => x.Id == id)
                    .ExecuteCommandAsync();
                // Debug logging handled by structured logging
                if (sqlResult > 0)
                {
                    // Debug logging handled by structured logging
                    // Verify the update by reading back the data
                    var verifyEntity = await GetByIdAsync(id);
                    // Debug logging handled by structured logging
                    return true;
                }
                else
                {
                    // Debug logging handled by structured logging
                }
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
            }

            // Fallback to entity update
            entity.CompletionRate = completionRate;
            entity.ModifyDate = DateTimeOffset.UtcNow;
            // Debug logging handled by structured logging
            var result = await UpdateAsync(entity);
            // Debug logging handled by structured logging
            return result;
        }

        /// <summary>
        /// Update status
        /// </summary>
        public async Task<bool> UpdateStatusAsync(long id, string status)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            // Only update specific fields to avoid JSONB type conversion issues
            var updateable = db.Updateable<Onboarding>()
                .SetColumns(x => new Onboarding
                {
                    Status = status,
                    ModifyDate = DateTimeOffset.UtcNow,
                    ModifyBy = entity.ModifyBy,
                    ModifyUserId = entity.ModifyUserId
                })
                .Where(x => x.Id == id);

            // If completed, set actual completion date
            if (status == "Completed" && !entity.ActualCompletionDate.HasValue)
            {
                updateable = updateable.SetColumns(x => new Onboarding
                {
                    Status = status,
                    ModifyDate = DateTimeOffset.UtcNow,
                    ModifyBy = entity.ModifyBy,
                    ModifyUserId = entity.ModifyUserId,
                    ActualCompletionDate = DateTimeOffset.UtcNow,
                    CompletionRate = 100
                });
            }

            var result = await updateable.ExecuteCommandAsync();

            return result > 0;
        }

        /// <summary>
        /// Get onboarding statistics
        /// </summary>
        public async Task<Dictionary<string, object>> GetStatisticsAsync()
        {
            var totalCount = await CountAsync(x => x.IsValid);
            var inactiveCount = await CountAsync(x => x.Status == "Inactive" && x.IsValid);
            var activeCount = await CountAsync(x => x.Status == "Active" && x.IsValid);
            var completedCount = await CountAsync(x => (x.Status == "Completed" || x.Status == "Force Completed") && x.IsValid);
            var pausedCount = await CountAsync(x => x.Status == "Paused" && x.IsValid);
            var abortedCount = await CountAsync(x => x.Status == "Aborted" && x.IsValid);

            // Legacy status support for backward compatibility
            var inProgressCount = await CountAsync(x => x.Status == "InProgress" && x.IsValid);
            var cancelledCount = await CountAsync(x => x.Status == "Cancelled" && x.IsValid);

            var overdueCount = await CountAsync(x =>
                x.EstimatedCompletionDate.HasValue &&
                x.EstimatedCompletionDate.Value < DateTimeOffset.UtcNow &&
                x.Status != "Completed" &&
                x.Status != "Force Completed" &&
                x.Status != "Aborted" &&
                x.IsValid);

            var allOnboardings = await GetListAsync(x => x.IsValid);
            var averageCompletionRate = allOnboardings.Any() ? allOnboardings.Average(x => x.CompletionRate) : 0;

            return new Dictionary<string, object>
            {
                { "TotalCount", totalCount },
                { "InactiveCount", inactiveCount },
                { "ActiveCount", activeCount },
                { "CompletedCount", completedCount },
                { "PausedCount", pausedCount },
                { "AbortedCount", abortedCount },
                // Legacy status counts for backward compatibility
                { "InProgressCount", inProgressCount },
                { "CancelledCount", cancelledCount },
                { "OverdueCount", overdueCount },
                { "AverageCompletionRate", averageCompletionRate }
            };
        }

        /// <summary>
        /// Batch update status
        /// </summary>
        public async Task<bool> BatchUpdateStatusAsync(List<long> ids, string status)
        {
            var entities = await GetListAsync(x => ids.Contains(x.Id) && x.IsValid);
            if (!entities.Any())
            {
                return false;
            }

            foreach (var entity in entities)
            {
                entity.Status = status;
                entity.ModifyDate = DateTimeOffset.UtcNow;

                if (status == "Completed" && !entity.ActualCompletionDate.HasValue)
                {
                    entity.ActualCompletionDate = DateTimeOffset.UtcNow;
                    entity.CompletionRate = 100;
                }
            }

            return await UpdateRangeAsync(entities);
        }

        /// <summary>
        /// Get overdue onboarding list
        /// </summary>
        public async Task<List<Onboarding>> GetOverdueListAsync()
        {
            return await GetListAsync(x =>
                x.EstimatedCompletionDate.HasValue &&
                x.EstimatedCompletionDate.Value < DateTimeOffset.UtcNow &&
                x.Status != "Completed" &&
                x.IsValid);
        }

        /// <summary>
        /// Get onboarding count by status
        /// </summary>
        public async Task<Dictionary<string, int>> GetCountByStatusAsync()
        {
            var allOnboardings = await GetListAsync(x => x.IsValid);

            return allOnboardings
                .GroupBy(x => x.Status)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// 获取所有入职流程列表，确保应用租户和应用过滤器
        /// </summary>
        public override async Task<List<Onboarding>> GetListAsync(CancellationToken cancellationToken = default, bool copyNew = false)
        {
            // 记录当前请求头
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var headerTenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
                var headerAppCode = httpContext.Request.Headers["X-App-Code"].FirstOrDefault();
                _logger.LogInformation($"[OnboardingRepository] GetListAsync with headers: X-Tenant-Id={headerTenantId}, X-App-Code={headerAppCode}");
            }

            // 显式添加租户和应用过滤条件
            var query = db.Queryable<Onboarding>().Where(x => x.IsValid == true);

            // 获取当前租户ID和应用代码
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            _logger.LogInformation($"[OnboardingRepository] GetListAsync applying explicit filters: TenantId={currentTenantId}, AppCode={currentAppCode}");

            // 显式添加过滤条件
            query = query.Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode);

            // 执行查询
            var result = await query.OrderByDescending(x => x.CreateDate).ToListAsync(cancellationToken);

            _logger.LogInformation($"[OnboardingRepository] GetListAsync returned {result.Count} onboardings with TenantId={currentTenantId}, AppCode={currentAppCode}");

            return result;
        }

        /// <summary>
        /// Get onboarding list by expression with tenant isolation
        /// </summary>
        public new async Task<List<Onboarding>> GetListAsync(Expression<Func<Onboarding, bool>> whereExpression, CancellationToken cancellationToken = default, bool copyNew = false)
        {
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            _logger.LogInformation($"[OnboardingRepository] GetListAsync(Expression) applying filters: TenantId={currentTenantId}, AppCode={currentAppCode}");

            var dbNew = copyNew ? db.CopyNew() : db;
            dbNew.Ado.CancellationToken = cancellationToken;

            var result = await dbNew.Queryable<Onboarding>()
                .Where(whereExpression)
                .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
                .ToListAsync();

            _logger.LogInformation($"[OnboardingRepository] GetListAsync(Expression) returned {result.Count} onboardings");

            return result;
        }

        /// <summary>
        /// 分页查询方法，添加显式过滤条件
        /// </summary>
        public new async Task<(List<Onboarding> datas, int total)> GetPageListAsync(
            List<Expression<Func<Onboarding, bool>>> whereExpressionList,
            int pageIndex,
            int pageSize,
            Expression<Func<Onboarding, object>> orderByExpression = null,
            bool isAsc = false,
            Expression<Func<Onboarding, Onboarding>> selectedColumnExpression = null,
            CancellationToken cancellationToken = default)
        {
            // 记录当前请求头
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var headerTenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
                var headerAppCode = httpContext.Request.Headers["X-App-Code"].FirstOrDefault();
                _logger.LogInformation($"[OnboardingRepository] GetPageListAsync with headers: X-Tenant-Id={headerTenantId}, X-App-Code={headerAppCode}");
            }

            // 获取当前租户ID和应用代码
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            _logger.LogInformation($"[OnboardingRepository] GetPageListAsync applying explicit filters: TenantId={currentTenantId}, AppCode={currentAppCode}");

            // 添加租户和应用过滤条件
            whereExpressionList.Add(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode);

            // 调用基类方法
            var result = await base.GetPageListAsync(whereExpressionList, pageIndex, pageSize, orderByExpression, isAsc, selectedColumnExpression, cancellationToken);

            _logger.LogInformation($"[OnboardingRepository] GetPageListAsync returned {result.datas.Count} onboardings out of {result.total} total with TenantId={currentTenantId}, AppCode={currentAppCode}");

            return result;
        }

        /// <summary>
        /// 直接查询，使用显式过滤条件
        /// </summary>
        public async Task<List<Onboarding>> GetListWithExplicitFiltersAsync(string tenantId, string appCode)
        {
            _logger.LogInformation($"[OnboardingRepository] GetListWithExplicitFiltersAsync with explicit TenantId={tenantId}, AppCode={appCode}");

            // 临时禁用全局过滤器
            db.QueryFilter.ClearAndBackup();

            try
            {
                // 使用显式过滤条件
                var query = db.Queryable<Onboarding>()
                    .Where(x => x.IsValid == true)
                    .Where(x => x.TenantId == tenantId && x.AppCode == appCode);

                // 执行查询
                var result = await query.OrderByDescending(x => x.CreateDate).ToListAsync();

                _logger.LogInformation($"[OnboardingRepository] GetListWithExplicitFiltersAsync returned {result.Count} onboardings with explicit filters");

                return result;
            }
            finally
            {
                // 恢复全局过滤器
                db.QueryFilter.Restore();
            }
        }

        #region Dashboard Methods

        /// <summary>
        /// Get recently completed cases for achievements
        /// </summary>
        public async Task<List<Onboarding>> GetRecentlyCompletedAsync(int limit, string? team = null)
        {
            // Get current tenant ID and app code for filtering
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            _logger.LogInformation($"[OnboardingRepository] GetRecentlyCompletedAsync with TenantId={currentTenantId}, AppCode={currentAppCode}");

            var query = db.Queryable<Onboarding>()
                .Where(x => x.IsValid == true)
                .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
                .Where(x => x.Status == "Completed" || x.Status == "Force Completed");

            if (!string.IsNullOrEmpty(team))
            {
                query = query.Where(x => x.CurrentTeam == team);
            }

            return await query
                .OrderByDescending(x => x.ActualCompletionDate)
                .Take(limit)
                .ToListAsync();
        }

        #endregion
    }
}
