using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Helpers;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;
using SqlSugar;
using System.Threading.Tasks;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// WorkflowTriggerGraph repository implementation
    /// </summary>
    public class WorkflowTriggerGraphRepository : BaseRepository<WorkflowTriggerGraph>, IWorkflowTriggerGraphRepository, IScopedService
    {
        private readonly UserContext _userContext;
        private readonly ILogger<WorkflowTriggerGraphRepository> _logger;

        public WorkflowTriggerGraphRepository(
            ISqlSugarClient sqlSugarClient,
            UserContext userContext,
            ILogger<WorkflowTriggerGraphRepository> logger) : base(sqlSugarClient)
        {
            _userContext = userContext;
            _logger = logger;
        }

        private string GetCurrentTenantId() => TenantContextHelper.GetTenantIdOrDefault(_userContext);
        private string GetCurrentAppCode()  => TenantContextHelper.GetAppCodeOrDefault(_userContext);

        /// <inheritdoc />
        /// <remarks>
        /// workflowId is used only for logging/context. The actual graph loaded is the
        /// GLOBAL graph for this tenant+appCode (workflow_id = 0). All workflows share one graph.
        /// Renamed internally to GetGlobalGraphAsync semantics; the parameter is kept for
        /// interface compatibility and call-site readability only.
        /// </remarks>
        public async Task<WorkflowTriggerGraph> GetByWorkflowIdAsync(long workflowId)
        {
            var tenantId = GetCurrentTenantId();
            var appCode  = GetCurrentAppCode();

            _logger.LogInformation(
                "[WorkflowTriggerGraphRepository] GetGlobalGraphAsync TenantId={TenantId}, AppCode={AppCode} (entry from WorkflowId={WorkflowId})",
                tenantId, appCode, workflowId);

            // Global graph has workflow_id = 0
            return await db.Queryable<WorkflowTriggerGraph>()
                .Where(x => x.WorkflowId == 0
                         && x.IsValid    == true
                         && x.TenantId   == tenantId
                         && x.AppCode    == appCode)
                .FirstAsync();
        }
    }
}
