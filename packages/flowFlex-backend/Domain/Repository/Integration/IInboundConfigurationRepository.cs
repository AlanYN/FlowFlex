using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository;

namespace FlowFlex.Domain.Repository.Integration
{
    /// <summary>
    /// Inbound configuration repository interface
    /// </summary>
    public interface IInboundConfigurationRepository : IBaseRepository<InboundConfiguration>
    {
        /// <summary>
        /// Get inbound configuration by integration ID
        /// </summary>
        Task<InboundConfiguration> GetByIntegrationIdAsync(long integrationId);

        /// <summary>
        /// Delete inbound configuration by integration ID
        /// </summary>
        Task<bool> DeleteByIntegrationIdAsync(long integrationId);
    }
}

