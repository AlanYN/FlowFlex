using Microsoft.Extensions.Logging;
using SqlSugar;
using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Shared;
using Microsoft.AspNetCore.Http;

namespace FlowFlex.SqlSugarDB.Implements.Integration
{
    /// <summary>
    /// Attachment sharing repository implementation
    /// </summary>
    public class AttachmentSharingRepository : BaseRepository<AttachmentSharing>, IAttachmentSharingRepository, IScopedService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AttachmentSharingRepository> _logger;

        public AttachmentSharingRepository(
            ISqlSugarClient sqlSugarClient,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AttachmentSharingRepository> logger) : base(sqlSugarClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Get attachment sharing configurations by integration ID
        /// </summary>
        public async Task<List<AttachmentSharing>> GetByIntegrationIdAsync(long integrationId)
        {
            return await db.Queryable<AttachmentSharing>()
                .Where(x => x.IntegrationId == integrationId && x.IsValid)
                .OrderBy(x => x.CreateDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get attachment sharing by system ID
        /// </summary>
        public async Task<AttachmentSharing?> GetBySystemIdAsync(string systemId)
        {
            return await db.Queryable<AttachmentSharing>()
                .Where(x => x.SystemId == systemId && x.IsValid)
                .FirstAsync();
        }

        /// <summary>
        /// Check if external module name exists for the integration
        /// </summary>
        public async Task<bool> ExistsModuleNameAsync(long integrationId, string externalModuleName, long? excludeId = null)
        {
            var query = db.Queryable<AttachmentSharing>()
                .Where(x => x.IntegrationId == integrationId 
                    && x.ExternalModuleName == externalModuleName 
                    && x.IsValid);

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// Check if system ID exists
        /// </summary>
        public async Task<bool> ExistsSystemIdAsync(string systemId)
        {
            return await db.Queryable<AttachmentSharing>()
                .Where(x => x.SystemId == systemId && x.IsValid)
                .AnyAsync();
        }

        /// <summary>
        /// Delete attachment sharing configurations by integration ID
        /// </summary>
        public async Task<bool> DeleteByIntegrationIdAsync(long integrationId)
        {
            var configs = await db.Queryable<AttachmentSharing>()
                .Where(x => x.IntegrationId == integrationId)
                .ToListAsync();

            if (configs.Any())
            {
                foreach (var config in configs)
                {
                    config.IsValid = false;
                }
                return await db.Updateable(configs).ExecuteCommandAsync() > 0;
            }

            return true;
        }

        /// <summary>
        /// Get active attachment sharing configurations by workflow ID
        /// </summary>
        public async Task<List<AttachmentSharing>> GetByWorkflowIdAsync(long workflowId)
        {
            var workflowIdStr = workflowId.ToString();
            
            // Query all active configurations and filter by workflow ID in application layer
            var allConfigs = await db.Queryable<AttachmentSharing>()
                .Where(x => x.IsActive && x.IsValid)
                .ToListAsync();

            // Filter by workflow ID (stored as JSON array)
            return allConfigs
                .Where(x => x.WorkflowIds.Contains(workflowIdStr))
                .ToList();
        }
    }
}

