using FlowFlex.Domain.Entities.Action;
using FlowFlex.Domain.Repository.Action;
using FlowFlex.Domain.Shared.Models;
using SqlSugar;

namespace FlowFlex.SqlSugarDB.Repositories.Action
{
    /// <summary>
    /// Action Execution repository implementation
    /// </summary>
    public class ActionExecutionRepository : BaseRepository<ActionExecution>, IActionExecutionRepository
    {
        public ActionExecutionRepository(ISqlSugarClient dbContext) : base(dbContext) { }

        /// <summary>
        /// Get executions by action definition ID
        /// </summary>
        public async Task<List<ActionExecution>> GetByActionDefinitionIdAsync(long actionDefinitionId, int days = 30)
        {
            var startDate = DateTimeOffset.UtcNow.AddDays(-days);
            return await db.Queryable<ActionExecution>()
                .Where(x => x.ActionDefinitionId == actionDefinitionId
                         && x.CreateDate >= startDate
                         && x.IsValid)
                .OrderByDescending(x => x.CreateDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get executions by execution status
        /// </summary>
        public async Task<List<ActionExecution>> GetByStatusAsync(string status, int days = 7)
        {
            var startDate = DateTimeOffset.UtcNow.AddDays(-days);
            return await db.Queryable<ActionExecution>()
                .Where(x => x.ExecutionStatus == status
                         && x.CreateDate >= startDate
                         && x.IsValid)
                .OrderByDescending(x => x.CreateDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get execution by execution ID
        /// </summary>
        public async Task<ActionExecution?> GetByExecutionIdAsync(string executionId)
        {
            return await db.Queryable<ActionExecution>()
                .Where(x => x.ExecutionId == executionId && x.IsValid)
                .FirstAsync();
        }

        /// <summary>
        /// Get failed executions that need retry
        /// </summary>
        public async Task<List<ActionExecution>> GetFailedExecutionsAsync(int maxRetryCount = 3)
        {
            return await db.Queryable<ActionExecution>()
                .Where(x => x.ExecutionStatus == "Failed"
                         && x.IsValid)
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
            var query = db.Queryable<ActionExecution>()
                .Where(x => x.IsValid);

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
            // Then query executions with action information
            var query = db.Queryable<ActionExecution>()
                .InnerJoin<ActionTriggerMapping>((e, m) => e.ActionTriggerMappingId == m.Id)
                .InnerJoin<ActionDefinition>((e, m, a) => m.ActionDefinitionId == a.Id && e.ActionDefinitionId == a.Id)
                .Where((e, m, a) => m.TriggerSourceId == triggerSourceId);

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
            var jsonPathParts = condition.JsonPath.Split('.');

            switch (condition.Operator.ToLower())
            {
                case "=":
                    return query.WhereIF(jsonPathParts.Length == 1, (e, m, a) =>
                        SqlFunc.JsonField(e.TriggerContext, jsonPathParts[0]) == condition.Value)
                        .WhereIF(jsonPathParts.Length == 2, (e, m, a) =>
                        SqlFunc.JsonField(e.TriggerContext, jsonPathParts[0], jsonPathParts[1]) == condition.Value)
                        .WhereIF(jsonPathParts.Length == 3, (e, m, a) =>
                        SqlFunc.JsonField(e.TriggerContext, jsonPathParts[0], jsonPathParts[1], jsonPathParts[2]) == condition.Value)
                        .WhereIF(jsonPathParts.Length == 4, (e, m, a) =>
                        SqlFunc.JsonField(e.TriggerContext, jsonPathParts[0], jsonPathParts[1], jsonPathParts[2], jsonPathParts[3]) == condition.Value);

                case "!=":
                    return query.WhereIF(jsonPathParts.Length == 1, (e, m, a) =>
                        SqlFunc.JsonField(e.TriggerContext, jsonPathParts[0]) != condition.Value)
                        .WhereIF(jsonPathParts.Length == 2, (e, m, a) =>
                        SqlFunc.JsonField(e.TriggerContext, jsonPathParts[0], jsonPathParts[1]) != condition.Value)
                        .WhereIF(jsonPathParts.Length == 3, (e, m, a) =>
                        SqlFunc.JsonField(e.TriggerContext, jsonPathParts[0], jsonPathParts[1], jsonPathParts[2]) != condition.Value)
                        .WhereIF(jsonPathParts.Length == 4, (e, m, a) =>
                        SqlFunc.JsonField(e.TriggerContext, jsonPathParts[0], jsonPathParts[1], jsonPathParts[2], jsonPathParts[3]) != condition.Value);

                case "contains":
                    return query.WhereIF(jsonPathParts.Length == 1, (e, m, a) =>
                        SqlFunc.JsonField(e.TriggerContext, jsonPathParts[0]).Contains(condition.Value))
                        .WhereIF(jsonPathParts.Length == 2, (e, m, a) =>
                        SqlFunc.JsonField(e.TriggerContext, jsonPathParts[0], jsonPathParts[1]).Contains(condition.Value))
                        .WhereIF(jsonPathParts.Length == 3, (e, m, a) =>
                        SqlFunc.JsonField(e.TriggerContext, jsonPathParts[0], jsonPathParts[1], jsonPathParts[2]).Contains(condition.Value))
                        .WhereIF(jsonPathParts.Length == 4, (e, m, a) =>
                        SqlFunc.JsonField(e.TriggerContext, jsonPathParts[0], jsonPathParts[1], jsonPathParts[2], jsonPathParts[3]).Contains(condition.Value));

                default:
                    return query;
            }
        }
    }
}