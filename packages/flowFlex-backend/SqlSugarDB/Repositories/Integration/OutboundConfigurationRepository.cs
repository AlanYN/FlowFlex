using Microsoft.Extensions.Logging;
using SqlSugar;
using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Shared;
using Microsoft.AspNetCore.Http;

namespace FlowFlex.SqlSugarDB.Implements.Integration
{
    /// <summary>
    /// Outbound configuration repository implementation
    /// </summary>
    public class OutboundConfigurationRepository : BaseRepository<OutboundConfiguration>, IOutboundConfigurationRepository, IScopedService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<OutboundConfigurationRepository> _logger;

        public OutboundConfigurationRepository(
            ISqlSugarClient sqlSugarClient,
            IHttpContextAccessor httpContextAccessor,
            ILogger<OutboundConfigurationRepository> logger) : base(sqlSugarClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Get outbound configuration by integration ID
        /// </summary>
        public async Task<OutboundConfiguration> GetByIntegrationIdAsync(long integrationId)
        {
            return await db.Queryable<OutboundConfiguration>()
                .Where(x => x.IntegrationId == integrationId && x.IsValid)
                .FirstAsync();
        }

        /// <summary>
        /// Delete outbound configuration by integration ID
        /// </summary>
        public async Task<bool> DeleteByIntegrationIdAsync(long integrationId)
        {
            var config = await db.Queryable<OutboundConfiguration>()
                .Where(x => x.IntegrationId == integrationId)
                .FirstAsync();

            if (config != null)
            {
                config.IsValid = false;
                return await db.Updateable(config).ExecuteCommandAsync() > 0;
            }

            return true;
        }

        /// <summary>
        /// Get integrations with real-time sync enabled
        /// </summary>
        public async Task<List<OutboundConfiguration>> GetRealTimeSyncEnabledAsync()
        {
            return await db.Queryable<OutboundConfiguration>()
                .Where(x => x.EnableRealTimeSync && x.IsValid)
                .ToListAsync();
        }
    }
}

