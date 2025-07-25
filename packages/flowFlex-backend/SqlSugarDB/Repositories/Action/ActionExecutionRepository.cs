using FlowFlex.Domain.Entities.Action;
using FlowFlex.Domain.Repository.Action;
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
    }
} 