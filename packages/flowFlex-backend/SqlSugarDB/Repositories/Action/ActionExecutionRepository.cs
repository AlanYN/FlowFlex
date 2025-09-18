using FlowFlex.Domain.Entities.Action;
using FlowFlex.Domain.Repository.Action;
using FlowFlex.Domain.Shared.Models;
using SqlSugar;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace FlowFlex.SqlSugarDB.Repositories.Action
{
    /// <summary>
    /// Action Execution repository implementation
    /// </summary>
    public class ActionExecutionRepository : BaseRepository<ActionExecution>, IActionExecutionRepository
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ActionExecutionRepository(ISqlSugarClient dbContext, IHttpContextAccessor httpContextAccessor) : base(dbContext)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Get executions by action definition ID
        /// </summary>
        public async Task<List<ActionExecution>> GetByActionDefinitionIdAsync(long actionDefinitionId, int days = 30)
        {
            var startDate = DateTimeOffset.UtcNow.AddDays(-days);
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            return await db.Queryable<ActionExecution>()
                .Where(x => x.ActionDefinitionId == actionDefinitionId
                         && x.CreateDate >= startDate
                         && x.IsValid
                         && x.TenantId == currentTenantId
                         && x.AppCode == currentAppCode)
                .OrderByDescending(x => x.CreateDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get executions by execution status
        /// </summary>
        public async Task<List<ActionExecution>> GetByStatusAsync(string status, int days = 7)
        {
            var startDate = DateTimeOffset.UtcNow.AddDays(-days);
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            return await db.Queryable<ActionExecution>()
                .Where(x => x.ExecutionStatus == status
                         && x.CreateDate >= startDate
                         && x.IsValid
                         && x.TenantId == currentTenantId
                         && x.AppCode == currentAppCode)
                .OrderByDescending(x => x.CreateDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get execution by execution ID
        /// </summary>
        public async Task<ActionExecution?> GetByExecutionIdAsync(string executionId)
        {
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            return await db.Queryable<ActionExecution>()
                .Where(x => x.ExecutionId == executionId
                         && x.IsValid
                         && x.TenantId == currentTenantId
                         && x.AppCode == currentAppCode)
                .FirstAsync();
        }

        /// <summary>
        /// Get failed executions that need retry
        /// </summary>
        public async Task<List<ActionExecution>> GetFailedExecutionsAsync(int maxRetryCount = 3)
        {
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            return await db.Queryable<ActionExecution>()
                .Where(x => x.ExecutionStatus == "Failed"
                         && x.IsValid
                         && x.TenantId == currentTenantId
                         && x.AppCode == currentAppCode)
                .OrderBy(x => x.CreateDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get executions with pagination
        /// </summary>
        public async Task<(List<ActionExecution> Data, int TotalCount)> GetPagedAsync(
            int pageIndex,
            int pageSize,
            long? actionDefinitionId = null,
            string? status = null,
            DateTimeOffset? startDate = null,
            DateTimeOffset? endDate = null)
        {
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            var query = db.Queryable<ActionExecution>()
                .Where(x => x.IsValid
                         && x.TenantId == currentTenantId
                         && x.AppCode == currentAppCode);

            // Filter by action definition ID
            if (actionDefinitionId.HasValue)
            {
                query = query.Where(x => x.ActionDefinitionId == actionDefinitionId.Value);
            }

            // Filter by status
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(x => x.ExecutionStatus == status);
            }

            // Filter by date range
            if (startDate.HasValue)
            {
                query = query.Where(x => x.CreateDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(x => x.CreateDate <= endDate.Value);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Get paged data
            var data = await query
                .OrderByDescending(x => x.CreateDate)
                .ToPageListAsync(pageIndex, pageSize);

            return (data, totalCount);
        }

        /// <summary>
        /// Get execution statistics by action definition
        /// </summary>
        public async Task<(int TotalCount, int SuccessCount, int FailedCount, double AvgDurationMs)> GetExecutionStatsAsync(long actionDefinitionId, int days = 30)
        {
            var startDate = DateTimeOffset.UtcNow.AddDays(-days);

            var executions = await db.Queryable<ActionExecution>()
                .Where(x => x.ActionDefinitionId == actionDefinitionId
                         && x.CreateDate >= startDate
                         && x.IsValid)
                .Select(x => new { x.ExecutionStatus, x.DurationMs })
                .ToListAsync();

            var totalCount = executions.Count;
            var successCount = executions.Count(x => x.ExecutionStatus == "Success");
            var failedCount = executions.Count(x => x.ExecutionStatus == "Failed");
            var avgDurationMs = executions.Where(x => x.DurationMs.HasValue)
                                         .Average(x => (double?)x.DurationMs) ?? 0;

            return (totalCount, successCount, failedCount, avgDurationMs);
        }

        /// <summary>
        /// Get overall execution statistics
        /// </summary>
        public async Task<Dictionary<string, int>> GetOverallStatsAsync(int days = 7)
        {
            var startDate = DateTimeOffset.UtcNow.AddDays(-days);

            var executions = await db.Queryable<ActionExecution>()
                .Where(x => x.CreateDate >= startDate && x.IsValid)
                .Select(x => x.ExecutionStatus)
                .ToListAsync();

            return executions.GroupBy(x => x)
                           .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Clean up old execution records
        /// </summary>
        public async Task<int> CleanupOldExecutionsAsync(int keepDays = 90)
        {
            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-keepDays);

            var affectedRows = await db.Updateable<ActionExecution>()
                .SetColumns(x => x.IsValid == false)
                .SetColumns(x => x.ModifyDate == DateTimeOffset.UtcNow)
                .Where(x => x.CreateDate < cutoffDate && x.IsValid)
                .ExecuteCommandAsync();

            return affectedRows;
        }

        /// <summary>
        /// Get executions by trigger source ID with action information
        /// </summary>
        public async Task<(List<ActionExecutionWithActionInfo> Data, int TotalCount)> GetByTriggerSourceIdWithActionInfoAsync(
            long triggerSourceId,
            int pageIndex = 1,
            int pageSize = 10,
            List<JsonQueryCondition>? jsonConditions = null)
        {
            // Get current tenant and app context for filtering
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();

            // Then query executions with action information
            var query = db.Queryable<ActionExecution>()
                .InnerJoin<ActionTriggerMapping>((e, m) => e.ActionTriggerMappingId == m.Id)
                .InnerJoin<ActionDefinition>((e, m, a) => m.ActionDefinitionId == a.Id && e.ActionDefinitionId == a.Id)
                .Where((e, m, a) => m.TriggerSourceId == triggerSourceId && e.IsValid)
                .Where((e, m, a) => e.TenantId == currentTenantId && e.AppCode == currentAppCode);

            // Apply JSON conditions if provided
            if (jsonConditions != null && jsonConditions.Count != 0)
            {
                foreach (var condition in jsonConditions)
                {
                    query = ApplyJsonCondition(query, condition);
                }
            }

            var finalQuery = query.OrderByDescending((e, m, a) => e.CreateDate)
                .Select((e, m, a) => new ActionExecutionWithActionInfo
                {
                    Id = e.Id,
                    ActionDefinitionId = e.ActionDefinitionId,
                    ActionCode = a.ActionCode,
                    ExecutionId = e.ExecutionId,
                    ActionTriggerMappingId = e.ActionTriggerMappingId,
                    ActionName = e.ActionName,
                    ActionType = e.ActionType,
                    TriggerContext = SqlFunc.JsonParse(e.TriggerContext),
                    ExecutionStatus = e.ExecutionStatus,
                    StartedAt = e.StartedAt,
                    CompletedAt = e.CompletedAt,
                    DurationMs = e.DurationMs,
                    ExecutionInput = SqlFunc.JsonParse(e.ExecutionInput),
                    ExecutionOutput = SqlFunc.JsonParse(e.ExecutionOutput),
                    ErrorMessage = e.ErrorMessage,
                    ErrorStackTrace = e.ErrorStackTrace,
                    ExecutorInfo = SqlFunc.JsonParse(e.ExecutorInfo),
                    CreatedAt = e.CreateDate,
                    CreatedBy = e.CreateBy
                });

            // Get total count
            var totalCount = await finalQuery.CountAsync();

            // Get paged data
            var data = await finalQuery.ToPageListAsync(pageIndex, pageSize);

            return (data, totalCount);
        }

        /// <summary>
        /// Apply JSON condition to query
        /// </summary>
        private ISugarQueryable<ActionExecution, ActionTriggerMapping, ActionDefinition> ApplyJsonCondition(
            ISugarQueryable<ActionExecution, ActionTriggerMapping, ActionDefinition> query,
            JsonQueryCondition condition)
        {
            var fieldName = condition.FieldName.ToLower();
            var jsonPath = condition.JsonPath;
            var value = condition.Value;
            var operatorType = condition.Operator.ToLower();

            var jsonPathExpression = BuildJsonPathExpression(jsonPath);

            var dbField = fieldName switch
            {
                "triggercontext" => "e.trigger_context",
                "executioninput" => "e.execution_input",
                "executionoutput" => "e.execution_output",
                "executorinfo" => "e.executor_info",
                _ => "e.trigger_context"
            };

            var sqlCondition = operatorType switch
            {
                "=" => $"{dbField}->{jsonPathExpression} = '{value}'",
                "!=" => $"{dbField}->{jsonPathExpression} != '{value}'",
                "contains" => $"{dbField}->{jsonPathExpression}::text LIKE '%{value}%'",
                ">" => $"{dbField}->{jsonPathExpression} > '{value}'",
                "<" => $"{dbField}->{jsonPathExpression} < '{value}'",
                ">=" => $"{dbField}->{jsonPathExpression} >= '{value}'",
                "<=" => $"{dbField}->{jsonPathExpression} <= '{value}'",
                _ => $"{dbField}->{jsonPathExpression} = '{value}'"
            };

            return query.Where(sqlCondition);
        }

        /// <summary>
        /// Build JSON path expression from dot-separated path
        /// </summary>
        private string BuildJsonPathExpression(string jsonPath)
        {
            if (string.IsNullOrEmpty(jsonPath))
                return string.Empty;

            if (jsonPath.Contains("->"))
                return jsonPath;

            var parts = jsonPath.Split('.');
            var pathParts = new List<string>();

            foreach (var part in parts)
            {
                if (int.TryParse(part, out _))
                {
                    pathParts.Add(part);
                }
                else
                {
                    pathParts.Add($"'{part}'");
                }
            }

            return string.Join("->", pathParts);
        }

        /// <summary>
        /// Get current tenant ID from HTTP context
        /// </summary>
        private string GetCurrentTenantId()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null)
                return "DEFAULT";

            // Try to get from AppContext first
            if (httpContext.Items.TryGetValue("AppContext", out var appContextObj) &&
                appContextObj is FlowFlex.Domain.Shared.Models.AppContext appContext)
            {
                return appContext.TenantId;
            }

            // Fallback to headers
            var tenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault()
                        ?? httpContext.Request.Headers["TenantId"].FirstOrDefault();

            if (!string.IsNullOrEmpty(tenantId))
            {
                return tenantId;
            }

            return "DEFAULT";
        }

        /// <summary>
        /// Get current app code from HTTP context
        /// </summary>
        private string GetCurrentAppCode()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null)
                return "DEFAULT";

            // Try to get from AppContext first
            if (httpContext.Items.TryGetValue("AppContext", out var appContextObj) &&
                appContextObj is FlowFlex.Domain.Shared.Models.AppContext appContext)
            {
                return appContext.AppCode;
            }

            // Fallback to headers
            var appCode = httpContext.Request.Headers["X-App-Code"].FirstOrDefault()
                       ?? httpContext.Request.Headers["AppCode"].FirstOrDefault();

            if (!string.IsNullOrEmpty(appCode))
            {
                return appCode;
            }

            return "DEFAULT";
        }
    }
}