using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Item.Internal.StandardApi.Response;
using System.Net;
using FlowFlex.Application.Contracts.Dtos.OW.OperationChangeLog;
using FlowFlex.Application.Contracts.Dtos.OW.ChangeLog;
using FlowFlex.Application.Contracts.IServices.OW.ChangeLog;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.WebApi.Controllers.OW.ChangeLog
{
    /// <summary>
    /// Workflow operation log controller - specialized for workflow operations
    /// </summary>
    [ApiController]
    [Route("ow/logs/workflow/v{version:apiVersion}")]
    [Display(Name = "Workflow Operation Logs")]
    public class WorkflowLogController : Controllers.ControllerBase
    {
        private readonly IWorkflowLogService _workflowLogService;

        public WorkflowLogController(IWorkflowLogService workflowLogService)
        {
            _workflowLogService = workflowLogService;
        }

        /// <summary>
        /// Get workflow operation logs
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="pageIndex">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paginated workflow log list</returns>
        [HttpGet("{workflowId}")]
        [ProducesResponseType<SuccessResponse<PagedResult<OperationChangeLogOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetWorkflowLogsAsync(
            long workflowId,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _workflowLogService.GetWorkflowLogsAsync(workflowId, pageIndex, pageSize);
            return Success(result);
        }

        /// <summary>
        /// Get workflow operation statistics
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <returns>Workflow operation statistics</returns>
        [HttpGet("{workflowId}/statistics")]
        [ProducesResponseType<SuccessResponse<Dictionary<string, int>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetWorkflowStatisticsAsync(long workflowId)
        {
            var result = await _workflowLogService.GetWorkflowOperationStatisticsAsync(workflowId);
            return Success(result);
        }

        /// <summary>
        /// Log workflow creation
        /// </summary>
        /// <param name="request">Workflow creation log request</param>
        /// <returns>Success result</returns>
        [HttpPost("create")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> LogWorkflowCreateAsync([FromBody] WorkflowCreateLogRequest request)
        {
            var result = await _workflowLogService.LogWorkflowCreateAsync(
                request.WorkflowId,
                request.WorkflowName,
                request.WorkflowDescription,
                request.ExtendedData);

            return Success(result);
        }

        /// <summary>
        /// Log workflow update
        /// </summary>
        /// <param name="request">Workflow update log request</param>
        /// <returns>Success result</returns>
        [HttpPost("update")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> LogWorkflowUpdateAsync([FromBody] WorkflowUpdateLogRequest request)
        {
            var result = await _workflowLogService.LogWorkflowUpdateAsync(
                request.WorkflowId,
                request.WorkflowName,
                request.BeforeData,
                request.AfterData,
                request.ChangedFields,
                request.ExtendedData);

            return Success(result);
        }

        /// <summary>
        /// Log workflow deletion
        /// </summary>
        /// <param name="request">Workflow deletion log request</param>
        /// <returns>Success result</returns>
        [HttpPost("delete")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> LogWorkflowDeleteAsync([FromBody] WorkflowDeleteLogRequest request)
        {
            var result = await _workflowLogService.LogWorkflowDeleteAsync(
                request.WorkflowId,
                request.WorkflowName,
                request.Reason,
                request.ExtendedData);

            return Success(result);
        }

        /// <summary>
        /// Log workflow publish
        /// </summary>
        /// <param name="request">Workflow publish log request</param>
        /// <returns>Success result</returns>
        [HttpPost("publish")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> LogWorkflowPublishAsync([FromBody] WorkflowPublishLogRequest request)
        {
            var result = await _workflowLogService.LogWorkflowPublishAsync(
                request.WorkflowId,
                request.WorkflowName,
                request.Version,
                request.ExtendedData);

            return Success(result);
        }

        /// <summary>
        /// Log workflow unpublish
        /// </summary>
        /// <param name="request">Workflow unpublish log request</param>
        /// <returns>Success result</returns>
        [HttpPost("unpublish")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> LogWorkflowUnpublishAsync([FromBody] WorkflowUnpublishLogRequest request)
        {
            var result = await _workflowLogService.LogWorkflowUnpublishAsync(
                request.WorkflowId,
                request.WorkflowName,
                request.Reason,
                request.ExtendedData);

            return Success(result);
        }

        /// <summary>
        /// Log workflow activation
        /// </summary>
        /// <param name="request">Workflow activation log request</param>
        /// <returns>Success result</returns>
        [HttpPost("activate")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> LogWorkflowActivateAsync([FromBody] WorkflowActivateLogRequest request)
        {
            var result = await _workflowLogService.LogWorkflowActivateAsync(
                request.WorkflowId,
                request.WorkflowName,
                request.ExtendedData);

            return Success(result);
        }

        /// <summary>
        /// Log workflow deactivation
        /// </summary>
        /// <param name="request">Workflow deactivation log request</param>
        /// <returns>Success result</returns>
        [HttpPost("deactivate")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> LogWorkflowDeactivateAsync([FromBody] WorkflowDeactivateLogRequest request)
        {
            var result = await _workflowLogService.LogWorkflowDeactivateAsync(
                request.WorkflowId,
                request.WorkflowName,
                request.Reason,
                request.ExtendedData);

            return Success(result);
        }
    }
}