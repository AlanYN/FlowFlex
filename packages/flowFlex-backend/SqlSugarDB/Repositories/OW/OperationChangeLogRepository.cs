using SqlSugar;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// Operation change log repository implementation
    /// </summary>
    public class OperationChangeLogRepository : BaseRepository<OperationChangeLog>, IOperationChangeLogRepository, IScopedService
    {
        public OperationChangeLogRepository(ISqlSugarClient dbContext) : base(dbContext) { }

        /// <summary>
        /// Get operation logs by Onboarding ID
        /// </summary>
        public async Task<List<OperationChangeLog>> GetByOnboardingIdAsync(long onboardingId)
        {
            return await base.GetListAsync(x => x.OnboardingId == onboardingId && x.IsValid, x => x.OperationTime, OrderByType.Desc);
        }

        /// <summary>
        /// Get operation logs by Stage ID
        /// </summary>
        public async Task<List<OperationChangeLog>> GetByStageIdAsync(long stageId)
        {
            return await base.GetListAsync(x => x.StageId == stageId && x.IsValid, x => x.OperationTime, OrderByType.Desc);
        }

        /// <summary>
        /// Get operation logs by business ID and module
        /// </summary>
        public async Task<List<OperationChangeLog>> GetByBusinessAsync(string businessModule, long businessId)
        {
            return await base.GetListAsync(x => x.BusinessModule == businessModule && x.BusinessId == businessId && x.IsValid, x => x.OperationTime, OrderByType.Desc);
        }

        /// <summary>
        /// Get operation logs by operation type
        /// </summary>
        public async Task<List<OperationChangeLog>> GetByOperationTypeAsync(string operationType, long? onboardingId = null)
        {
            var predicate = Expressionable.Create<OperationChangeLog>();
            predicate.And(x => x.OperationType == operationType && x.IsValid);
            predicate.AndIF(onboardingId.HasValue, x => x.OnboardingId == onboardingId.Value);

            return await base.GetListAsync(predicate.ToExpression(), x => x.OperationTime, OrderByType.Desc);
        }

        /// <summary>
        /// Get operation logs by operator
        /// </summary>
        public async Task<List<OperationChangeLog>> GetByOperatorAsync(long operatorId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            var predicate = Expressionable.Create<OperationChangeLog>();
            predicate.And(x => x.OperatorId == operatorId && x.IsValid);
            predicate.AndIF(startDate.HasValue, x => x.OperationTime >= startDate.Value);
            predicate.AndIF(endDate.HasValue, x => x.OperationTime <= endDate.Value);

            return await base.GetListAsync(predicate.ToExpression(), x => x.OperationTime, OrderByType.Desc);
        }

        /// <summary>
        /// Get operation logs within time range
        /// </summary>
        public async Task<List<OperationChangeLog>> GetByTimeRangeAsync(DateTimeOffset startDate, DateTimeOffset endDate, long? onboardingId = null)
        {
            var predicate = Expressionable.Create<OperationChangeLog>();
            predicate.And(x => x.OperationTime >= startDate && x.OperationTime <= endDate && x.IsValid);
            predicate.AndIF(onboardingId.HasValue, x => x.OnboardingId == onboardingId.Value);

            return await base.GetListAsync(predicate.ToExpression(), x => x.OperationTime, OrderByType.Desc);
        }

        /// <summary>
        /// Get operation statistics
        /// </summary>
        public async Task<Dictionary<string, int>> GetOperationStatisticsAsync(long? onboardingId = null, long? stageId = null)
        {
            var predicate = Expressionable.Create<OperationChangeLog>();
            predicate.And(x => x.IsValid);
            predicate.AndIF(onboardingId.HasValue, x => x.OnboardingId == onboardingId.Value);
            predicate.AndIF(stageId.HasValue, x => x.StageId == stageId.Value);

            var query = base.db.Queryable<OperationChangeLog>()
                .Where(predicate.ToExpression())
                .GroupBy(x => x.OperationType)
                .Select(x => new { OperationType = x.OperationType, Count = SqlFunc.AggregateCount(x.Id) });

            var result = await query.ToListAsync();
            return result.ToDictionary(x => x.OperationType, x => x.Count);
        }

        /// <summary>
        /// Insert operation log using native SQL, specifically handles JSONB fields
        /// </summary>
        public async Task<bool> ExecuteInsertWithJsonbAsync(string sql, object parameters)
        {
            try
            {
                Console.WriteLine($"🔗 [DB Step 1] Starting ExecuteInsertWithJsonbAsync");
                Console.WriteLine($"🔗 [DB Step 2] Preparing to execute SQL command");
                Console.WriteLine($"🔗 [DB Step 3] SQL: {sql.Substring(0, Math.Min(100, sql.Length))}...");
                
                int result;
                
                // 检查参数类型，如果是SugarParameter数组，使用对应的方法
                if (parameters is SugarParameter[] sugarParams)
                {
                    result = await base.db.Ado.ExecuteCommandAsync(sql, sugarParams);
                }
                else
                {
                    result = await base.db.Ado.ExecuteCommandAsync(sql, parameters);
                }
                
                Console.WriteLine($"🔗 [DB Step 4] SQL execution completed with result: {result}");
                Console.WriteLine($"🔗 [DB Step 5] Returning success: {result > 0}");
                
                return result > 0;
            }
            catch (Exception ex)
            {
                // 记录详细的错误信息，但不让程序崩溃
                Console.WriteLine($"❌ ExecuteInsertWithJsonbAsync failed: {ex.Message}");
                Console.WriteLine($"❌ Exception type: {ex.GetType().Name}");
                
                // 安全地访问StackTrace，避免空引用异常
                if (!string.IsNullOrEmpty(ex.StackTrace))
                {
                    Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                }
                
                // 如果是内部异常，也记录
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"❌ Inner exception: {ex.InnerException.Message}");
                    Console.WriteLine($"❌ Inner exception type: {ex.InnerException.GetType().Name}");
                }
                
                // 返回 false 而不是抛出异常，让调用方决定如何处理
                return false;
            }
        }

        /// <summary>
        /// Insert operation log using SqlSugar standard method
        /// </summary>
        public async Task<bool> InsertOperationLogAsync(OperationChangeLog operationLog)
        {
            var insertable = base.db.Insertable(operationLog);
            var result = await insertable.ExecuteCommandAsync();
            return result > 0;
        }
    }
}
