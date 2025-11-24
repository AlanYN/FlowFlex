using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository;

namespace FlowFlex.Domain.Repository.Integration
{
    /// <summary>
    /// Outbound configuration repository interface
    /// </summary>
    public interface IOutboundConfigurationRepository : IBaseRepository<OutboundConfiguration>
    {
        /// <summary>
        /// Get outbound configuration by integration ID
        /// </summary>
        Task<OutboundConfiguration> GetByIntegrationIdAsync(long integrationId);

        /// <summary>
        /// Delete outbound configuration by integration ID
        /// </summary>
        Task<bool> DeleteByIntegrationIdAsync(long integrationId);

        /// <summary>
        /// Get integrations with real-time sync enabled
        /// </summary>
        Task<List<OutboundConfiguration>> GetRealTimeSyncEnabledAsync();

        /// <summary>
        /// Get outbound configurations by integration ID and action ID
        /// </summary>
        Task<List<OutboundConfiguration>> GetByIntegrationIdAndActionIdAsync(long integrationId, long actionId);

        /// <summary>
        /// Get outbound configurations by integration ID
        /// </summary>
        Task<List<OutboundConfiguration>> GetByIntegrationIdListAsync(long integrationId);
    }
}

