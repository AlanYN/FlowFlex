using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository;

namespace FlowFlex.Domain.Repository.Integration
{
    /// <summary>
    /// Integration sync log repository interface
    /// </summary>
    public interface IIntegrationSyncLogRepository : IBaseRepository<IntegrationSyncLog>
    {
        /// <summary>
        /// Query sync logs with pagination
        /// </summary>
        Task<(List<IntegrationSyncLog> items, int total)> QueryPagedAsync(
            int pageIndex,
            int pageSize,
            long? integrationId = null,
            string direction = null,
            string status = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string sortField = "SyncTime",
            string sortDirection = "desc");

        /// <summary>
        /// Get sync logs by integration ID
        /// </summary>
        Task<List<IntegrationSyncLog>> GetByIntegrationIdAsync(long integrationId, int limit = 100);

        /// <summary>
        /// Get failed sync logs
        /// </summary>
        Task<List<IntegrationSyncLog>> GetFailedLogsAsync(long? integrationId = null, int limit = 100);

        /// <summary>
        /// Get sync logs by external record ID
        /// </summary>
        Task<List<IntegrationSyncLog>> GetByExternalRecordIdAsync(string externalRecordId);

        /// <summary>
        /// Get sync logs by WFE record ID
        /// </summary>
        Task<List<IntegrationSyncLog>> GetByWfeRecordIdAsync(long wfeRecordId);

        /// <summary>
        /// Delete old sync logs
        /// </summary>
        Task<bool> DeleteOldLogsAsync(DateTime beforeDate);

        /// <summary>
        /// Get sync statistics
        /// </summary>
        Task<Dictionary<string, int>> GetSyncStatisticsAsync(long integrationId, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Get failed sync logs for a specific integration
        /// </summary>
        Task<List<IntegrationSyncLog>> GetFailedSyncLogsAsync(long integrationId, int limit = 50);
    }
}

