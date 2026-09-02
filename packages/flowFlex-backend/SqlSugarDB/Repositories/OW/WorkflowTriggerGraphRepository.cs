using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SqlSugar;
using System.Linq;
using System.Threading.Tasks;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// WorkflowTriggerGraph repository implementation
    /// </summary>
    public class WorkflowTriggerGraphRepository : BaseRepository<WorkflowTriggerGraph>, IWorkflowTriggerGraphRepository, IScopedService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<WorkflowTriggerGraphRepository> _logger;

        public WorkflowTriggerGraphRepository(
            ISqlSugarClient sqlSugarClient,
            IHttpContextAccessor httpContextAccessor,
            ILogger<WorkflowTriggerGraphRepository> logger) : base(sqlSugarClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        private string GetCurrentTenantId() =>
            _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault() ?? "default";

        private string GetCurrentAppCode() =>
            _httpContextAccessor.HttpContext?.Request.Headers["X-App-Code"].FirstOrDefault() ?? "default";

        /// <inheritdoc />
        /// <remarks>
        /// workflowId is used only for logging/context. The actual graph loaded is the
        /// GLOBAL graph for this tenant+appCode (workflow_id = 0). All workflows share one graph.
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
