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
                // Debug logging handled by structured logging
                // Debug logging handled by structured logging)}...");

                int result;

                // Check parameter type, use corresponding method if it's SugarParameter array
                if (parameters is SugarParameter[] sugarParams)
                {
                    result = await base.db.Ado.ExecuteCommandAsync(sql, sugarParams);
                }
                else
                {
                    result = await base.db.Ado.ExecuteCommandAsync(sql, parameters);
                }
                // Debug logging handled by structured logging
                return result > 0;
            }
            catch (Exception ex)
            {
                // Log detailed error information, but don't let the program crash
                // Debug logging handled by structured logging
                // Debug logging handled by structured logging.Name}");

                // Safely access StackTrace, avoid null reference exception
                if (!string.IsNullOrEmpty(ex.StackTrace))
                {
                    // Debug logging handled by structured logging
                }

                // If there's an inner exception, also log it
                if (ex.InnerException != null)
                {
                    // Debug logging handled by structured logging
                    // Debug logging handled by structured logging.Name}");
                }

                // Return false instead of throwing exception, let caller decide how to handle
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
