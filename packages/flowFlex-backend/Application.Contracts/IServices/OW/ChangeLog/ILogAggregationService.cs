using FlowFlex.Application.Contracts.Dtos.OW.OperationChangeLog;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.IServices.OW.ChangeLog
{
    /// <summary>
    /// Log aggregation service for cross-module log queries and analytics
    /// </summary>
    public interface ILogAggregationService : IScopedService
    {
        /// <summary>
        /// Get aggregated logs across multiple business modules
        /// </summary>
        Task<PagedResult<OperationChangeLogOutputDto>> GetAggregatedLogsAsync(
            long? onboardingId = null,
            long? stageId = null,
            List<BusinessModuleEnum> businessModules = null,
            List<OperationTypeEnum> operationTypes = null,
            DateTimeOffset? startDate = null,
            DateTimeOffset? endDate = null,
            int pageIndex = 1,
            int pageSize = 20);

        /// <summary>
        /// Get logs by multiple business IDs across different modules
        /// </summary>
        Task<PagedResult<OperationChangeLogOutputDto>> GetLogsByBusinessIdsAsync(
            List<long> businessIds,
            BusinessModuleEnum? businessModule = null,
            long? onboardingId = null,
            int pageIndex = 1,
            int pageSize = 20);

        /// <summary>
        /// Get comprehensive operation statistics across all modules
        /// </summary>
        Task<Dictionary<string, object>> GetComprehensiveStatisticsAsync(
            long? onboardingId = null,
            long? stageId = null,
            DateTimeOffset? startDate = null,
            DateTimeOffset? endDate = null);

        /// <summary>
        /// Get operation timeline for analysis
        /// </summary>
        Task<List<OperationTimelineDto>> GetOperationTimelineAsync(
            long? onboardingId = null,
            long? stageId = null,
            DateTimeOffset? startDate = null,
            DateTimeOffset? endDate = null);

        /// <summary>
        /// Search logs across all modules with full-text search
        /// </summary>
        Task<PagedResult<OperationChangeLogOutputDto>> SearchLogsAsync(
            string searchTerm,
            long? onboardingId = null,
            long? stageId = null,
            List<BusinessModuleEnum> businessModules = null,
            int pageIndex = 1,
            int pageSize = 20);

        /// <summary>
        /// Get user activity logs across all modules
        /// </summary>
        Task<PagedResult<OperationChangeLogOutputDto>> GetUserActivityLogsAsync(
            long operatorId,
            DateTimeOffset? startDate = null,
            DateTimeOffset? endDate = null,
            List<BusinessModuleEnum> businessModules = null,
            int pageIndex = 1,
            int pageSize = 20);

        /// <summary>
        /// Export logs to various formats (CSV, Excel, JSON)
        /// </summary>
        Task<byte[]> ExportLogsAsync(
            string format,
            long? onboardingId = null,
            long? stageId = null,
            List<BusinessModuleEnum> businessModules = null,
            DateTimeOffset? startDate = null,
            DateTimeOffset? endDate = null);

        /// <summary>
        /// Get condition evaluation history for an onboarding
        /// Implements Requirements 8.5: Query condition evaluation history by onboardingId
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="pageIndex">Page index (1-based)</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paged condition evaluation logs</returns>
        Task<PagedResult<OperationChangeLogOutputDto>> GetConditionEvaluationHistoryByOnboardingAsync(
            long onboardingId,
            int pageIndex = 1,
            int pageSize = 20);

        /// <summary>
        /// Get condition evaluation history for a stage
        /// Implements Requirements 8.6: Query condition evaluation history by stageId
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Optional onboarding ID filter</param>
        /// <param name="pageIndex">Page index (1-based)</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paged condition evaluation logs</returns>
        Task<PagedResult<OperationChangeLogOutputDto>> GetConditionEvaluationHistoryByStageAsync(
            long stageId,
            long? onboardingId = null,
            int pageIndex = 1,
            int pageSize = 20);

        /// <summary>
        /// Get condition action execution history
        /// Implements Requirements 8.7: Query condition action execution logs
        /// </summary>
        /// <param name="onboardingId">Optional onboarding ID filter</param>
        /// <param name="stageId">Optional stage ID filter</param>
        /// <param name="pageIndex">Page index (1-based)</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paged condition action execution logs</returns>
        Task<PagedResult<OperationChangeLogOutputDto>> GetConditionActionExecutionHistoryAsync(
            long? onboardingId = null,
            long? stageId = null,
            int pageIndex = 1,
            int pageSize = 20);
    }

    /// <summary>
    /// Operation timeline DTO for analytics
    /// </summary>
    public class OperationTimelineDto
    {
        public DateTime Date { get; set; }
        public string BusinessModule { get; set; }
        public string OperationType { get; set; }
        public int Count { get; set; }
        public List<string> TopOperators { get; set; }
    }
}