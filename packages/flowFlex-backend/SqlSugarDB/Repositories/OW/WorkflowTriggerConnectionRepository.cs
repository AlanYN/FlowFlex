using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SqlSugar;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// WorkflowTriggerConnection repository implementation
    /// </summary>
    public class WorkflowTriggerConnectionRepository : BaseRepository<WorkflowTriggerConnection>, IWorkflowTriggerConnectionRepository, IScopedService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<WorkflowTriggerConnectionRepository> _logger;

        public WorkflowTriggerConnectionRepository(
            ISqlSugarClient sqlSugarClient,
            IHttpContextAccessor httpContextAccessor,
            ILogger<WorkflowTriggerConnectionRepository> logger) : base(sqlSugarClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        private string GetCurrentTenantId() =>
            _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault() ?? "default";

        private string GetCurrentAppCode() =>
            _httpContextAccessor.HttpContext?.Request.Headers["X-App-Code"].FirstOrDefault() ?? "default";

        /// <inheritdoc />
        public async Task<List<WorkflowTriggerConnection>> GetByGraphIdAsync(long graphId)
        {
            var tenantId = GetCurrentTenantId();
            var appCode = GetCurrentAppCode();

            return await db.Queryable<WorkflowTriggerConnection>()
                .Where(x => x.GraphId == graphId
                         && x.IsValid == true
                         && x.TenantId == tenantId
                         && x.AppCode == appCode)
                .OrderBy(x => x.ExecutionOrder)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<bool> DeleteByGraphIdAsync(long graphId)
        {
            var tenantId = GetCurrentTenantId();
            var appCode = GetCurrentAppCode();

            var rows = await db.Updateable<WorkflowTriggerConnection>()
                .SetColumns(x => x.IsValid == false)
                .Where(x => x.GraphId == graphId
                         && x.IsValid == true
                         && x.TenantId == tenantId
                         && x.AppCode == appCode)
                .ExecuteCommandAsync();

            return rows > 0;
        }

        /// <inheritdoc />
        public async Task<List<WorkflowTriggerConnection>> GetByWorkflowIdAsync(long workflowId)
        {
            var tenantId = GetCurrentTenantId();
            var appCode = GetCurrentAppCode();

            return await db.Queryable<WorkflowTriggerConnection>()
                .Where(x => (x.SourceWorkflowId == workflowId || x.TargetWorkflowId == workflowId)
                         && x.IsValid == true
                         && x.TenantId == tenantId
                         && x.AppCode == appCode)
                .ToListAsync();
        }
    }
}
