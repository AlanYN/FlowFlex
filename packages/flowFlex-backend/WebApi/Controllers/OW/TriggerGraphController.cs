using FlowFlex.Application.Contracts.Dtos.OW.TriggerGraph;
using FlowFlex.Application.Contracts.Dtos.OW.TriggerGraph;
using FlowFlex.Application.Contracts.Dtos.OW.Workflow;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Const;
using Item.Internal.StandardApi.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using WebApi.Authorization;

namespace FlowFlex.WebApi.Controllers.OW
{
    /// <summary>
    /// Workflow Trigger Graph API — manage the trigger graph canvas and connections (OW-723 / OW-725)
    /// </summary>
    [ApiController]
    [Route("ow/trigger-graph/v{version:apiVersion}")]
    [Display(Name = "trigger-graph")]
    [Authorize]
    public class TriggerGraphController : Controllers.ControllerBase
    {
        private readonly ITriggerGraphService             _triggerGraphService;
        private readonly ITriggerExecutionService         _triggerExecutionService;
        private readonly IWorkflowTriggerLogRepository    _triggerLogRepo;
        private readonly SqlSugar.ISqlSugarClient         _db;

        public TriggerGraphController(
            ITriggerGraphService          triggerGraphService,
            ITriggerExecutionService      triggerExecutionService,
            IWorkflowTriggerLogRepository triggerLogRepo,
            SqlSugar.ISqlSugarClient      db)
        {
            _triggerGraphService     = triggerGraphService;
            _triggerExecutionService = triggerExecutionService;
            _triggerLogRepo          = triggerLogRepo;
            _db                      = db;
        }

        #region OW-723 — Graph CRUD

        /// <summary>
        /// Get the trigger graph for a workflow.
        /// Returns an empty stub if the graph has not been saved yet.
        /// </summary>
        [HttpGet("{workflowId}")]
        [WFEAuthorize(PermissionConsts.Workflow.Read)]
        [ProducesResponseType<SuccessResponse<TriggerGraphDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Get(long workflowId)
        {
            var result = await _triggerGraphService.GetByWorkflowIdAsync(workflowId);
            return Success(result);
        }

        /// <summary>
        /// Create or update the trigger graph for a workflow (full-replace save).
        /// </summary>
        [HttpPost]
        [WFEAuthorize(PermissionConsts.Workflow.Update)]
        [ProducesResponseType<SuccessResponse<TriggerGraphDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Save([FromBody] SaveTriggerGraphInput input)
        {
            var result = await _triggerGraphService.SaveAsync(input);
            return Success(result);
        }

        #endregion

        #region OW-725 — Query interfaces

        /// <summary>
        /// Get all workflows (id + name + status) for the trigger graph left panel.
        /// </summary>
        [HttpGet("workflows/all")]
        [WFEAuthorize(PermissionConsts.Workflow.Read)]
        [ProducesResponseType<SuccessResponse<List<WorkflowOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAllWorkflows()
        {
            var result = await _triggerGraphService.GetAllWorkflowsAsync();
            return Success(result);
        }

        /// <summary>
        /// Get detailed node info for a workflow:
        /// stages → fields / questionnaire questions / checklist tasks.
        /// Used for condition configuration in the connection panel.
        /// </summary>
        [HttpGet("workflows/{workflowId}/node-info")]
        [WFEAuthorize(PermissionConsts.Workflow.Read)]
        [ProducesResponseType<SuccessResponse<WorkflowNodeInfoDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetWorkflowNodeInfo(long workflowId)
        {
            var result = await _triggerGraphService.GetWorkflowNodeInfoAsync(workflowId);
            return Success(result);
        }

        #endregion

        #region OW-729: Trigger History API

        /// <summary>
        /// Get trigger logs for a source Case (all logs where this Case was the source).
        /// Used in Case detail page to show trigger history.
        /// GET /ow/trigger-graph/v1/logs/by-onboarding/{sourceOnboardingId}
        /// </summary>
        [HttpGet("logs/by-onboarding/{sourceOnboardingId}")]
        [WFEAuthorize(PermissionConsts.Workflow.Read)]
        public async Task<IActionResult> GetLogsByOnboarding(long sourceOnboardingId)
        {
            var logs = await _triggerLogRepo.GetBySourceOnboardingIdAsync(sourceOnboardingId);
            return Success(logs);
        }

        /// <summary>
        /// Get Related Cases for a Case: upstream (logs where this Case is the target)
        /// and downstream (logs where this Case is the source, status = Triggered).
        /// GET /ow/trigger-graph/v1/logs/related-cases/{onboardingId}
        /// </summary>
        [HttpGet("logs/related-cases/{onboardingId}")]
        [WFEAuthorize(PermissionConsts.Workflow.Read)]
        public async Task<IActionResult> GetRelatedCases(long onboardingId)
        {
            // Downstream: this Case triggered others
            var downstreamLogs = await _triggerLogRepo.GetBySourceOnboardingIdAsync(onboardingId);
            // Upstream: this Case was created by others
            var upstreamLogs   = await _triggerLogRepo.GetByTargetOnboardingIdAsync(onboardingId);

            // Collect all case IDs to batch-load names
            var allIds = new HashSet<long>();
            foreach (var l in downstreamLogs)
            {
                if (l.TargetOnboardingId.HasValue) allIds.Add(l.TargetOnboardingId.Value);
            }
            foreach (var l in upstreamLogs)
            {
                allIds.Add(l.SourceOnboardingId);
            }

            Dictionary<long, (string CaseName, string CaseCode)> caseMap = new();
            if (allIds.Count > 0)
            {
                var cases = await _db.Queryable<FlowFlex.Domain.Entities.OW.Onboarding>()
                    .Where(o => allIds.ToList().Contains(o.Id) && o.IsValid == true)
                    .Select(o => new { o.Id, o.CaseName, o.CaseCode })
                    .ToListAsync();
                foreach (var c in cases)
                    caseMap[c.Id] = (c.CaseName ?? string.Empty, c.CaseCode ?? string.Empty);
            }

            var downstream = downstreamLogs
                .Where(l => l.TargetOnboardingId.HasValue && l.Status == "Triggered")
                .Select(l => new
                {
                    logId             = l.Id.ToString(),
                    onboardingId      = l.TargetOnboardingId!.Value.ToString(),
                    caseName          = caseMap.TryGetValue(l.TargetOnboardingId.Value, out var d) ? d.CaseName : string.Empty,
                    caseCode          = caseMap.TryGetValue(l.TargetOnboardingId.Value, out var d2) ? d2.CaseCode : string.Empty,
                    completionType    = l.CompletionType,
                    createDate        = l.CreateDate,
                    direction         = "downstream"
                });

            var upstream = upstreamLogs
                .Where(l => l.Status == "Triggered")
                .Select(l => new
                {
                    logId             = l.Id.ToString(),
                    onboardingId      = l.SourceOnboardingId.ToString(),
                    caseName          = caseMap.TryGetValue(l.SourceOnboardingId, out var u) ? u.CaseName : string.Empty,
                    caseCode          = caseMap.TryGetValue(l.SourceOnboardingId, out var u2) ? u2.CaseCode : string.Empty,
                    completionType    = l.CompletionType,
                    createDate        = l.CreateDate,
                    direction         = "upstream"
                });

            return Success(new { upstream, downstream });
        }

        /// <summary>
        /// Get paged trigger logs for a source Workflow.
        /// Used in Trigger Editor history panel.
        /// GET /ow/trigger-graph/v1/logs/by-workflow/{sourceWorkflowId}?pageIndex=1&pageSize=20&status=Triggered
        /// </summary>
        [HttpGet("logs/by-workflow/{sourceWorkflowId}")]
        [WFEAuthorize(PermissionConsts.Workflow.Read)]
        public async Task<IActionResult> GetLogsByWorkflow(
            long sourceWorkflowId,
            [Microsoft.AspNetCore.Mvc.FromQuery] int pageIndex = 1,
            [Microsoft.AspNetCore.Mvc.FromQuery] int pageSize = 20,
            [Microsoft.AspNetCore.Mvc.FromQuery] string? status = null)
        {
            var (items, total) = await _triggerLogRepo.GetPagedByWorkflowAsync(
                sourceWorkflowId, pageIndex, pageSize, status);
            return Success(new { items, total, pageIndex, pageSize });
        }

        #endregion

        #region Debug — manual fire (remove after validation)

        /// <summary>
        /// [Debug] Get trigger logs for a source Case — legacy debug endpoint, use logs/by-onboarding instead.
        /// </summary>
        [HttpGet("debug/logs/{sourceOnboardingId}")]
        [WFEAuthorize(PermissionConsts.Workflow.Read)]
        public async Task<IActionResult> GetTriggerLogs(long sourceOnboardingId)
        {
            var logs = await _triggerLogRepo.GetBySourceOnboardingIdAsync(sourceOnboardingId);
            return Success(logs);
        }

        /// <summary>
        /// [Debug] Manually fire the trigger engine for a completed Case.
        /// </summary>
        [HttpPost("debug/fire/{onboardingId}/{workflowId}")]
        [WFEAuthorize(PermissionConsts.Workflow.Update)]
        public async Task<IActionResult> ManualFire(long onboardingId, long workflowId)
        {
            await _triggerExecutionService.ExecuteTriggersAsync(onboardingId, workflowId, "Completed");
            var logs = await _triggerLogRepo.GetBySourceOnboardingIdAsync(onboardingId);
            return Success(logs);
        }

        #endregion
    }
}
