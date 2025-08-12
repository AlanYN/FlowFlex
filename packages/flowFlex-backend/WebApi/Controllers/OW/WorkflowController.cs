using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.Workflow;
using FlowFlex.Application.Contracts.Dtos.OW.Stage;
using FlowFlex.Application.Contracts.IServices.OW;


using Item.Internal.StandardApi.Response;
using System.Net;
using System.Linq.Dynamic.Core;

namespace FlowFlex.WebApi.Controllers.OW
{
    /// <summary>
    /// Workflow management API
    /// </summary>
    [ApiController]
    [Route("ow/workflows/v{version:apiVersion}")]
    [Display(Name = "workflow")]
    [Authorize] // 添加授权特性，要求所有workflows API都需要认证
    public class WorkflowController : Controllers.ControllerBase
    {
        private readonly IWorkflowService _workflowService;
        private readonly IStageService _stageService;

        public WorkflowController(IWorkflowService workflowService, IStageService stageService)
        {
            _workflowService = workflowService;
            _stageService = stageService;
        }

        /// <summary>
        /// Create workflow
        /// </summary>
        [HttpPost]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Create([FromBody] WorkflowInputDto input)
        {
            var id = await _workflowService.CreateAsync(input);
            return Success(id);
        }

        /// <summary>
        /// Update workflow
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Update(long id, [FromBody] WorkflowInputDto input)
        {
            var result = await _workflowService.UpdateAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Delete workflow (with confirmation)
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Delete(long id, [FromQuery] bool confirm = false)
        {
            var result = await _workflowService.DeleteAsync(id, confirm);
            return Success(result);
        }

        /// <summary>
        /// Get workflow by id
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType<SuccessResponse<WorkflowOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetById(long id)
        {
            var data = await _workflowService.GetByIdAsync(id);
            return Success(data);
        }

        /// <summary>
        /// Get workflow list
        /// </summary>
        [HttpGet]
        [ProducesResponseType<SuccessResponse<List<WorkflowOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetList()
        {
            var data = await _workflowService.GetListAsync();
            return Success(data);
        }

        /// <summary>
        /// Get all workflows (no pagination)
        /// </summary>
        [HttpGet("all")]
        [ProducesResponseType<SuccessResponse<List<WorkflowOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAll()
        {
            var data = await _workflowService.GetAllAsync();
            return Success(data);
        }

        /// <summary>
        /// Query workflow (paged)
        /// </summary>
        [HttpPost("query")]
        [ProducesResponseType<SuccessResponse<PagedResult<WorkflowOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Query([FromBody] WorkflowQueryRequest query)
        {
            var data = await _workflowService.QueryAsync(query);
            return Success(data);
        }

        /// <summary>
        /// Activate workflow
        /// </summary>
        [HttpPost("{id}/activate")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Activate(long id)
        {
            var result = await _workflowService.ActivateAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Deactivate workflow
        /// </summary>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Deactivate(long id)
        {
            var result = await _workflowService.DeactivateAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Set as default workflow
        /// </summary>
        [HttpPost("{id}/set-default")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SetDefault(long id)
        {
            var result = await _workflowService.SetDefaultAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Remove default workflow
        /// </summary>
        [HttpPost("{id}/remove-default")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> RemoveDefault(long id)
        {
            var result = await _workflowService.RemoveDefaultAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Duplicate workflow
        /// </summary>
        [HttpPost("{id}/duplicate")]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Duplicate(long id, [FromBody] DuplicateWorkflowInputDto input)
        {
            var newId = await _workflowService.DuplicateAsync(id, input);
            return Success(newId);
        }

        /// <summary>
        /// Get workflow version history
        /// </summary>
        // 移除GetVersionHistory、GetStagesByVersionId、GetVersionDetail、CreateFromVersion、CreateNewVersion等与版本历史相关的接口实现

        /// <summary>
        /// Get stages by workflow id
        /// </summary>
        [HttpGet("{id}/stages")]
        [ProducesResponseType<SuccessResponse<List<StageOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetStagesByWorkflowId(long id)
        {
            var data = await _stageService.GetListByWorkflowIdAsync(id);
            return Success(data);
        }

        /// <summary>
        /// Export single workflow to detailed Excel format
        /// </summary>
        [HttpGet("{id}/export-detailed-excel")]
        [ProducesResponseType(typeof(FileResult), 200)]
        public async Task<IActionResult> ExportDetailedToExcel(long id)
        {
            var stream = await _workflowService.ExportDetailedToExcelAsync(id);
            var fileName = $"workflow_detailed_{id}_{DateTimeOffset.Now:MMddyyyy_HHmmss}.xlsx"; // local time for filename
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        /// <summary>
        /// Export multiple workflows to detailed Excel format
        /// </summary>
        [HttpPost("export-detailed-excel")]
        [ProducesResponseType(typeof(FileResult), 200)]
        public async Task<IActionResult> ExportMultipleDetailedToExcel([FromBody] WorkflowExportSearch search)
        {
            var stream = await _workflowService.ExportMultipleDetailedToExcelAsync(search);
            var fileName = $"workflows_detailed_export_{DateTimeOffset.Now:MMddyyyy_HHmmss}.xlsx"; // local time for filename
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }

    /// <summary>
    /// Request model for creating a new version
    /// </summary>
    public class CreateVersionRequest
    {
        /// <summary>
        /// Optional reason for creating the version
        /// </summary>
        public string ChangeReason { get; set; }
    }
}
