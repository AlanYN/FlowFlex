using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Helpers;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;
using SqlSugar;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// WorkflowTriggerConnection repository implementation
    /// </summary>
    public class WorkflowTriggerConnectionRepository : BaseRepository<WorkflowTriggerConnection>, IWorkflowTriggerConnectionRepository, IScopedService
    {
        private readonly UserContext _userContext;
        private readonly ILogger<WorkflowTriggerConnectionRepository> _logger;

        public WorkflowTriggerConnectionRepository(
            ISqlSugarClient sqlSugarClient,
            UserContext userContext,
            ILogger<WorkflowTriggerConnectionRepository> logger) : base(sqlSugarClient)
        {
            _userContext = userContext;
            _logger = logger;
        }

        private string GetCurrentTenantId() => TenantContextHelper.GetTenantIdOrDefault(_userContext);
        private string GetCurrentAppCode()  => TenantContextHelper.GetAppCodeOrDefault(_userContext);

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
