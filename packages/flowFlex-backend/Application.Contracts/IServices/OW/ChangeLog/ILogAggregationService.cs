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