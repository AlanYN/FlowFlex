using Microsoft.Extensions.Logging;
using SqlSugar;
using System.Linq.Expressions;
using FlowFlex.Domain.Abstracts;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// Stage completion log repository implementation
    /// </summary>
    public class StageCompletionLogRepository : BaseRepository<StageCompletionLog>, IStageCompletionLogRepository, IScopedService
    {
        public StageCompletionLogRepository(ISqlSugarClient dbContext) : base(dbContext) { }

        /// <summary>
        /// Get log list by Onboarding ID
        /// </summary>
        public async Task<List<StageCompletionLog>> GetByOnboardingIdAsync(long onboardingId)
        {
            return await base.GetListAsync(x => x.OnboardingId == onboardingId && x.IsValid, x => x.CreateDate, SqlSugar.OrderByType.Desc);
        }

        /// <summary>
        /// Get log list by Stage ID
        /// </summary>
        public async Task<List<StageCompletionLog>> GetByStageIdAsync(long stageId)
        {
            return await base.GetListAsync(x => x.StageId == stageId && x.IsValid, x => x.CreateDate, SqlSugar.OrderByType.Desc);
        }

        /// <summary>
        /// Get log list by Onboarding ID and Stage ID
        /// </summary>
        public async Task<List<StageCompletionLog>> GetByOnboardingAndStageAsync(long onboardingId, long stageId)
        {
            Console.WriteLine($"🔍 Repository: GetByOnboardingAndStageAsync - OnboardingId: {onboardingId}, StageId: {stageId}");

            // Get current filter status
            var filterCount = db.QueryFilter?.GeFilterList?.Count ?? 0;
            Console.WriteLine($"🔍 Repository: Current filter count: {filterCount}");

            // Backup current filters
            var backupFilters = BackupFilters();

            try
            {
                // Temporarily clear TenantId filter, but keep other filters (like IsValid)
                db.QueryFilter.ClearAndBackup();
                db.QueryFilter.AddTableFilter<IValidFilter>(x => x.IsValid);

                Console.WriteLine($"🔍 Repository: Temporarily cleared TenantId filter for historical data access");

                var result = await db.Queryable<StageCompletionLog>()
                    .Where(x => x.OnboardingId == onboardingId && x.StageId == stageId && x.IsValid)
                    .OrderByDescending(x => x.CreateDate)
                    .ToListAsync();

                Console.WriteLine($"🔍 Repository: Query result count: {result?.Count ?? 0}");

                if (result == null || result.Count == 0)
                {
                    Console.WriteLine($"🔍 Repository: No logs found, checking if any logs exist for this onboarding...");

                    var totalLogsForOnboarding = await db.Queryable<StageCompletionLog>()
                        .Where(x => x.OnboardingId == onboardingId && x.IsValid)
                        .CountAsync();
                    Console.WriteLine($"🔍 Repository: Total logs for onboarding {onboardingId}: {totalLogsForOnboarding}");

                    if (totalLogsForOnboarding > 0)
                    {
                        var availableLogs = await db.Queryable<StageCompletionLog>()
                            .Where(x => x.OnboardingId == onboardingId && x.IsValid)
                            .OrderByDescending(x => x.CreateDate)
                            .Take(5)
                            .Select(x => new { x.Id, x.StageId, x.TenantId, x.LogType, x.Action })
                            .ToListAsync();

                        Console.WriteLine($"🔍 Repository: Available logs for this onboarding:");
                        foreach (var log in availableLogs)
                        {
                            Console.WriteLine($"🔍 Repository: - Id: {log.Id}, StageId: {log.StageId}, TenantId: '{log.TenantId}', LogType: '{log.LogType}', Action: '{log.Action}'");
                        }
                    }
                }

                return result ?? new List<StageCompletionLog>();
            }
            finally
            {
                // Restore filters
                if (backupFilters != null)
                {
                    db.QueryFilter.Restore();
                    Console.WriteLine($"🔍 Repository: Restored original filters");
                }
            }
        }

        /// <summary>
        /// Get log list by log type
        /// </summary>
        public async Task<List<StageCompletionLog>> GetByLogTypeAsync(string logType, int days = 7)
        {
            var startDate = DateTimeOffset.Now.AddDays(-days);
            return await base.GetListAsync(x => x.LogType == logType && x.CreateDate >= startDate && x.IsValid, x => x.CreateDate, SqlSugar.OrderByType.Desc);
        }

        /// <summary>
        /// Get error log list
        /// </summary>
        public async Task<List<StageCompletionLog>> GetErrorLogsAsync(int days = 7)
        {
            var startDate = DateTimeOffset.Now.AddDays(-days);
            return await base.GetListAsync(x => x.Success == false && x.CreateDate >= startDate && x.IsValid, x => x.CreateDate, SqlSugar.OrderByType.Desc);
        }

        /// <summary>
        /// Get log statistics
        /// </summary>
        public async Task<Dictionary<string, object>> GetLogStatisticsAsync(long? onboardingId = null, int days = 7)
        {
            var startDate = DateTimeOffset.Now.AddDays(-days);
            var query = db.Queryable<StageCompletionLog>().Where(x => x.CreateDate >= startDate && x.IsValid);

            if (onboardingId.HasValue)
                query = query.Where(x => x.OnboardingId == onboardingId.Value);

            var totalCount = await query.CountAsync();
            var errorCount = await query.Where(x => x.Success == false).CountAsync();
            var avgResponseTime = await query.Where(x => x.ResponseTime.HasValue).AvgAsync(x => x.ResponseTime.Value);

            return new Dictionary<string, object>
            {
                ["total"] = totalCount,
                ["errors"] = errorCount,
                ["successRate"] = totalCount > 0 ? (double)(totalCount - errorCount) / totalCount * 100 : 100,
                ["averageResponseTime"] = avgResponseTime
            };
        }

        /// <summary>
        /// Create logs in batch
        /// </summary>
        public async Task<bool> CreateBatchAsync(List<StageCompletionLog> logs)
        {
            try
            {
                Console.WriteLine($"🔍 Repository: Starting CreateBatchAsync with {logs?.Count ?? 0} logs");

                if (logs == null || logs.Count == 0)
                {
                    Console.WriteLine("⚠️ Repository: No logs to insert");
                    return true;
                }

                // Validate required fields for each log
                foreach (var log in logs)
                {
                    Console.WriteLine($"🔍 Repository: Validating log - TenantId: '{log.TenantId}', OnboardingId: {log.OnboardingId}, StageId: {log.StageId}, LogType: '{log.LogType}'");

                    if (string.IsNullOrEmpty(log.TenantId))
                    {
                        Console.WriteLine($"�?Repository: TenantId is null or empty for log with OnboardingId: {log.OnboardingId}");
                    }

                    if (string.IsNullOrEmpty(log.LogType))
                    {
                        Console.WriteLine($"�?Repository: LogType is null or empty for log with OnboardingId: {log.OnboardingId}");
                    }
                }

                var result = await base.InsertRangeAsync(logs);
                Console.WriteLine($"🔍 Repository: InsertRangeAsync result: {result}");

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"�?Repository: Failed to create batch logs: {ex.Message}");
                Console.WriteLine($"�?Repository: Stack trace: {ex.StackTrace}");
                Console.WriteLine($"�?Repository: Inner exception: {ex.InnerException?.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clean up expired logs
        /// </summary>
        public async Task<int> CleanupExpiredLogsAsync(int days = 30)
        {
            var expiredDate = DateTimeOffset.Now.AddDays(-days);
            var expiredLogs = await base.GetListAsync(x => x.CreateDate < expiredDate && x.IsValid);

            if (expiredLogs.Any())
            {
                await base.DeleteAsync(expiredLogs);
                return expiredLogs.Count;
            }

            return 0;
        }

        /// <summary>
        /// Log list (paginated/conditional query)
        /// </summary>
        public async Task<(List<StageCompletionLog> Data, int Total)> ListAsync(long? onboardingId, long? stageId, string? logType, bool? success, DateTimeOffset? startDate, DateTimeOffset? endDate, int pageIndex, int pageSize)
        {
            // Build query condition list
            var whereExpressions = new List<Expression<Func<StageCompletionLog, bool>>>();
            
            // Basic filter conditions
            whereExpressions.Add(x => x.IsValid);

            if (onboardingId.HasValue)
            {
                whereExpressions.Add(x => x.OnboardingId == onboardingId.Value);
            }

            if (stageId.HasValue)
            {
                whereExpressions.Add(x => x.StageId == stageId.Value);
            }

            if (!string.IsNullOrEmpty(logType))
            {
                whereExpressions.Add(x => x.LogType == logType);
            }

            if (success.HasValue)
            {
                whereExpressions.Add(x => x.Success == success.Value);
            }

            if (startDate.HasValue)
            {
                whereExpressions.Add(x => x.CreateDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                whereExpressions.Add(x => x.CreateDate <= endDate.Value);
            }

            // Use BaseRepository's safe pagination method
            var (items, totalCount) = await GetPageListAsync(
                whereExpressions,
                pageIndex,
                pageSize,
                orderByExpression: x => x.CreateDate,
                isAsc: false // Descending order
            );

            return (items, totalCount);
        }

        /// <summary>
        /// Delete single log
        /// </summary>
        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await base.GetFirstAsync(x => x.Id == id && x.IsValid);
            if (entity == null) return false;
            entity.IsValid = false;
            entity.ModifyDate = DateTimeOffset.Now;
            return await base.UpdateAsync(entity);
        }
    }
}
