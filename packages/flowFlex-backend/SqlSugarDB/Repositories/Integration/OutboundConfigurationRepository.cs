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
        /// SyncMode: 0 = Manual, 1 = Real-time, 2 = Scheduled
        /// </summary>
        public async Task<List<OutboundConfiguration>> GetRealTimeSyncEnabledAsync()
        {
            return await db.Queryable<OutboundConfiguration>()
                .Where(x => x.SyncMode == 1 && x.IsValid) // SyncMode 1 = Real-time sync
                .ToListAsync();
        }

        /// <summary>
        /// Get outbound configurations by integration ID and action ID
        /// </summary>
        public async Task<List<OutboundConfiguration>> GetByIntegrationIdAndActionIdAsync(long integrationId, long actionId)
        {
            return await db.Queryable<OutboundConfiguration>()
                .Where(x => x.IntegrationId == integrationId && x.ActionId == actionId && x.IsValid)
                .ToListAsync();
        }

        /// <summary>
        /// Get outbound configurations by integration ID
        /// </summary>
        public async Task<List<OutboundConfiguration>> GetByIntegrationIdListAsync(long integrationId)
        {
            return await db.Queryable<OutboundConfiguration>()
                .Where(x => x.IntegrationId == integrationId && x.IsValid)
                .ToListAsync();
        }
    }
}

