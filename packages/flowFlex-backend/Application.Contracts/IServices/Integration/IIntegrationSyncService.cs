using FlowFlex.Application.Contracts.Dtos.Integration;

namespace FlowFlex.Application.Contracts.IServices.Integration
{
    /// <summary>
    /// Integration sync service interface
    /// </summary>
    public interface IIntegrationSyncService
    {
        /// <summary>
        /// Sync data from external system to WFE (Inbound)
        /// </summary>
        Task<bool> SyncInboundAsync(long integrationId, string entityType, string externalEntityId);

        /// <summary>
        /// Sync data from WFE to external system (Outbound)
        /// </summary>
        Task<bool> SyncOutboundAsync(long integrationId, string entityType, long wfeEntityId);

        /// <summary>
        /// Get sync logs for an integration
        /// </summary>
        Task<(List<IntegrationSyncLogOutputDto> items, int total)> GetSyncLogsAsync(
            long integrationId,
            int pageIndex,
            int pageSize,
            string? syncDirection = null,
            string? status = null);

        /// <summary>
        /// Retry failed sync
        /// </summary>
        Task<bool> RetrySyncAsync(long syncLogId);

        /// <summary>
        /// Log sync operation
        /// </summary>
        Task<long> LogSyncAsync(IntegrationSyncLogInputDto input);

        /// <summary>
        /// Get sync statistics for an integration
        /// </summary>
        Task<Dictionary<string, int>> GetSyncStatisticsAsync(long integrationId, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Get failed sync logs
        /// </summary>
        Task<List<IntegrationSyncLogOutputDto>> GetFailedSyncLogsAsync(long integrationId, int limit = 50);
    }
}
