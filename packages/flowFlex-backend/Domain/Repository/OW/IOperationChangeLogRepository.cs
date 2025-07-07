using FlowFlex.Domain.Repository;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// Operation change log repository interface
    /// </summary>
    public interface IOperationChangeLogRepository : IBaseRepository<OperationChangeLog>, IScopedService
    {
        /// <summary>
        /// Get operation logs by Onboarding ID
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>Operation log list</returns>
        Task<List<OperationChangeLog>> GetByOnboardingIdAsync(long onboardingId);

        /// <summary>
        /// Get operation logs by Stage ID
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Operation log list</returns>
        Task<List<OperationChangeLog>> GetByStageIdAsync(long stageId);

        /// <summary>
        /// Get operation logs by business ID and module
        /// </summary>
        /// <param name="businessModule">Business module</param>
        /// <param name="businessId">Business ID</param>
        /// <returns>Operation log list</returns>
        Task<List<OperationChangeLog>> GetByBusinessAsync(string businessModule, long businessId);

        /// <summary>
        /// Get operation logs by operation type
        /// </summary>
        /// <param name="operationType">Operation type</param>
        /// <param name="onboardingId">Onboarding ID (optional)</param>
        /// <returns>Operation log list</returns>
        Task<List<OperationChangeLog>> GetByOperationTypeAsync(string operationType, long? onboardingId = null);

        /// <summary>
        /// Get operation logs by operator
        /// </summary>
        /// <param name="operatorId">Operator ID</param>
        /// <param name="startDate">Start date (optional)</param>
        /// <param name="endDate">End date (optional)</param>
        /// <returns>Operation log list</returns>
        Task<List<OperationChangeLog>> GetByOperatorAsync(long operatorId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);

        /// <summary>
        /// Get operation logs within time range
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <param name="onboardingId">Onboarding ID (optional)</param>
        /// <returns>Operation log list</returns>
        Task<List<OperationChangeLog>> GetByTimeRangeAsync(DateTimeOffset startDate, DateTimeOffset endDate, long? onboardingId = null);

        /// <summary>
        /// Get operation statistics
        /// </summary>
        /// <param name="onboardingId">Onboarding ID (optional)</param>
        /// <param name="stageId">Stage ID (optional)</param>
        /// <returns>Operation statistics</returns>
        Task<Dictionary<string, int>> GetOperationStatisticsAsync(long? onboardingId = null, long? stageId = null);

        /// <summary>
        /// Insert operation log using native SQL, specifically handles JSONB fields
        /// </summary>
        /// <param name="sql">SQL statement</param>
        /// <param name="parameters">Parameters</param>
        /// <returns>Whether successful</returns>
        Task<bool> ExecuteInsertWithJsonbAsync(string sql, object parameters);

        /// <summary>
        /// Insert operation log using SqlSugar standard method
        /// </summary>
        /// <param name="operationLog">Operation log entity</param>
        /// <returns>Whether successful</returns>
        Task<bool> InsertOperationLogAsync(OperationChangeLog operationLog);
    }
}
