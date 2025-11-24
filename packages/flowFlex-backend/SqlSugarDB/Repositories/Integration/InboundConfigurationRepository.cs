using Microsoft.Extensions.Logging;
using SqlSugar;
using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Shared;
using Microsoft.AspNetCore.Http;

namespace FlowFlex.SqlSugarDB.Implements.Integration
{
    /// <summary>
    /// Inbound configuration repository implementation
    /// </summary>
    public class InboundConfigurationRepository : BaseRepository<InboundConfiguration>, IInboundConfigurationRepository, IScopedService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<InboundConfigurationRepository> _logger;

        public InboundConfigurationRepository(
            ISqlSugarClient sqlSugarClient,
            IHttpContextAccessor httpContextAccessor,
            ILogger<InboundConfigurationRepository> logger) : base(sqlSugarClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Get inbound configuration by integration ID
        /// </summary>
        public async Task<InboundConfiguration> GetByIntegrationIdAsync(long integrationId)
        {
            return await db.Queryable<InboundConfiguration>()
                .Where(x => x.IntegrationId == integrationId && x.IsValid)
                .FirstAsync();
        }

        /// <summary>
        /// Delete inbound configuration by integration ID
        /// </summary>
        public async Task<bool> DeleteByIntegrationIdAsync(long integrationId)
        {
            var config = await db.Queryable<InboundConfiguration>()
                .Where(x => x.IntegrationId == integrationId)
                .FirstAsync();

            if (config != null)
            {
                config.IsValid = false;
                return await db.Updateable(config).ExecuteCommandAsync() > 0;
            }

            return true;
        }
    }
}

