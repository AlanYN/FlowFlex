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
        /// Get operation logs by Onboarding ID and Stage ID
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Operation log list</returns>
        Task<List<OperationChangeLog>> GetByOnboardingAndStageAsync(long onboardingId, long stageId);

        /// <summary>
        /// Get operation logs by business ID and module
        /// </summary>
        /// <param name="businessModule">Business module</param>
        /// <param name="businessId">Business ID</param>
        /// <returns>Operation log list</returns>
        Task<List<OperationChangeLog>> GetByBusinessAsync(string businessModule, long businessId);

        /// <summary>
        /// Get operation logs by business ID (without specifying business module)
        /// </summary>
        /// <param name="businessId">Business ID</param>
        /// <returns>Operation log list</returns>
        Task<List<OperationChangeLog>> GetByBusinessIdAsync(long businessId);

        /// <summary>
        /// Get operation logs by multiple business IDs (batch query)
        /// </summary>
        /// <param name="businessIds">List of business IDs</param>
        /// <returns>Operation log list</returns>
        Task<List<OperationChangeLog>> GetByBusinessIdsAsync(List<long> businessIds);

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

        /// <summary>
        /// Get operation logs by multiple business IDs and module (batch query to avoid N+1 problem)
        /// </summary>
        /// <param name="businessModule">Business module</param>
        /// <param name="businessIds">Business ID list</param>
        /// <param name="onboardingId">Onboarding ID filter (optional)</param>
        /// <returns>Operation log list</returns>
        Task<List<OperationChangeLog>> GetByBusinessIdsAsync(string businessModule, List<long> businessIds, long? onboardingId = null);

        /// <summary>
        /// Get operation logs with pagination for stage components (optimized for large datasets)
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <param name="taskIds">Task ID list</param>
        /// <param name="questionIds">Question ID list</param>
        /// <param name="operationType">Operation type filter (optional)</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paginated operation log list</returns>
        Task<(List<OperationChangeLog> logs, int totalCount)> GetStageComponentLogsPaginatedAsync(
            long? onboardingId,
            long stageId,
            List<long> taskIds,
            List<long> questionIds,
            string operationType,
            int pageIndex,
            int pageSize);
    }
}
