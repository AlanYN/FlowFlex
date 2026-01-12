using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.Workflow;
using FlowFlex.Application.Contracts.Dtos.OW.Stage;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Shared.Enums.Permission;
using FlowFlex.WebApi.Filters;
using FlowFlex.Domain.Shared.Const;
using WebApi.Authorization;

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
        private readonly IComponentMappingService _componentMappingService;

        public WorkflowController(IWorkflowService workflowService, IStageService stageService, IComponentMappingService componentMappingService)
        {
            _workflowService = workflowService;
            _stageService = stageService;
            _componentMappingService = componentMappingService;
        }

        /// <summary>
        /// Create workflow
        /// Requires WORKFLOW:CREATE permission
        /// </summary>
        [HttpPost]
        [WFEAuthorize(PermissionConsts.Workflow.Create)]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Create([FromBody] WorkflowInputDto input)
        {
            var id = await _workflowService.CreateAsync(input);
            return Success(id);
        }

        /// <summary>
        /// Update workflow
        /// Requires WORKFLOW:UPDATE permission
        /// </summary>
        [HttpPut("{id}")]
        [WFEAuthorize(PermissionConsts.Workflow.Update)]
        [RequirePermission(PermissionEntityTypeEnum.Workflow, OperationTypeEnum.Operate)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Update(long id, [FromBody] WorkflowInputDto input)
        {
            var result = await _workflowService.UpdateAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Delete workflow (with confirmation)
        /// Requires WORKFLOW:DELETE permission
        /// </summary>
        [HttpDelete("{id}")]
        [WFEAuthorize(PermissionConsts.Workflow.Delete)]
        [RequirePermission(PermissionEntityTypeEnum.Workflow, OperationTypeEnum.Delete)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Delete(long id, [FromQuery] bool confirm = false)
        {
            var result = await _workflowService.DeleteAsync(id, confirm);
            return Success(result);
        }

        /// <summary>
        /// Get workflow by id
        /// Requires WORKFLOW:READ permission
        /// </summary>
        [HttpGet("{id}")]
        [WFEAuthorize(PermissionConsts.Workflow.Read)]
        [RequirePermission(PermissionEntityTypeEnum.Workflow, OperationTypeEnum.View)]
        [ProducesResponseType<SuccessResponse<WorkflowOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetById(long id)
        {
            var data = await _workflowService.GetByIdAsync(id);
            return Success(data);
        }

        /// <summary>
        /// Get workflow list with pagination support
        /// Requires any READ permission (WORKFLOW, CASE, CHECKLIST, QUESTION, or TOOL)
        /// This is a shared list API accessible by any module with read permission
        /// </summary>
        /// <param name="pageIndex">Page index (starting from 1, default: 1)</param>
        /// <param name="pageSize">Page size (default: 15)</param>
        /// <param name="sortField">Sort field (default: CreateDate)</param>
        /// <param name="sortDirection">Sort direction (asc/desc, default: desc)</param>
        /// <param name="name">Filter by name (supports multiple values or comma-separated)</param>
        /// <param name="isActive">Filter by active status</param>
        /// <param name="isDefault">Filter by default status</param>
        /// <param name="status">Filter by workflow status (e.g., active, inactive, draft)</param>
        /// <returns>Paged list of workflows or simple list when no pagination params provided</returns>
        [HttpGet]
        [WFEAuthorize(
            PermissionConsts.Workflow.Read,
            PermissionConsts.Case.Read,
            PermissionConsts.Checklist.Read,
            PermissionConsts.Question.Read,
            PermissionConsts.Tool.Read)]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetList(
            [FromQuery] int? pageIndex = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] string sortField = null,
            [FromQuery] string sortDirection = null,
            [FromQuery] string[] name = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] bool? isDefault = null,
            [FromQuery] string status = null)
        {
            // If pagination parameters are provided, use paged query
            if (pageIndex.HasValue || pageSize.HasValue)
            {
                // Combine multiple name parameters into comma-separated string
                var nameFilter = name != null && name.Any()
                    ? string.Join(",", name.Where(n => !string.IsNullOrWhiteSpace(n)))
                    : null;

                var query = new WorkflowQueryRequest
                {
                    PageIndex = pageIndex ?? 1,
                    PageSize = pageSize ?? 15,
                    SortField = sortField ?? "CreateDate",
                    SortDirection = sortDirection ?? "desc",
                    Name = nameFilter,
                    IsActive = isActive,
                    IsDefault = isDefault,
                    Status = status
                };
                var pagedData = await _workflowService.QueryAsync(query);
                return Success(pagedData);
            }

            // Otherwise, use original simple list
            var data = await _workflowService.GetListAsync();
            return Success(data);
        }

        /// <summary>
        /// Get all workflows (no pagination)
        /// Requires any READ permission (WORKFLOW, CASE, CHECKLIST, QUESTION, or TOOL)
        /// This is a shared list API accessible by any module with read permission
        /// </summary>
        [HttpGet("all")]
        [WFEAuthorize(
            PermissionConsts.Workflow.Read,
            PermissionConsts.Case.Read,
            PermissionConsts.Checklist.Read,
            PermissionConsts.Question.Read,
            PermissionConsts.Tool.Read)]
        [ProducesResponseType<SuccessResponse<List<WorkflowOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAll()
        {
            var data = await _workflowService.GetAllAsync();
            return Success(data);
        }

        /// <summary>
        /// Query workflow (paged)
        /// Requires WORKFLOW:READ permission
        /// </summary>
        [HttpPost("query")]
        [WFEAuthorize(PermissionConsts.Workflow.Read)]
        [ProducesResponseType<SuccessResponse<PagedResult<WorkflowOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Query([FromBody] WorkflowQueryRequest query)
        {
            var data = await _workflowService.QueryAsync(query);
            return Success(data);
        }

        /// <summary>
        /// Activate workflow
        /// Requires WORKFLOW:UPDATE permission
        /// </summary>
        [HttpPost("{id}/activate")]
        [WFEAuthorize(PermissionConsts.Workflow.Update)]
        [RequirePermission(PermissionEntityTypeEnum.Workflow, OperationTypeEnum.Operate)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Activate(long id)
        {
            var result = await _workflowService.ActivateAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Deactivate workflow
        /// Requires WORKFLOW:UPDATE permission
        /// </summary>
        [HttpPost("{id}/deactivate")]
        [WFEAuthorize(PermissionConsts.Workflow.Update)]
        [RequirePermission(PermissionEntityTypeEnum.Workflow, OperationTypeEnum.Operate)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Deactivate(long id)
        {
            var result = await _workflowService.DeactivateAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Set as default workflow
        /// Requires WORKFLOW:UPDATE permission
        /// </summary>
        [HttpPost("{id}/set-default")]
        [WFEAuthorize(PermissionConsts.Workflow.Update)]
        [RequirePermission(PermissionEntityTypeEnum.Workflow, OperationTypeEnum.Operate)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SetDefault(long id)
        {
            var result = await _workflowService.SetDefaultAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Remove default workflow
        /// Requires WORKFLOW:UPDATE permission
        /// </summary>
        [HttpPost("{id}/remove-default")]
        [WFEAuthorize(PermissionConsts.Workflow.Update)]
        [RequirePermission(PermissionEntityTypeEnum.Workflow, OperationTypeEnum.Operate)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> RemoveDefault(long id)
        {
            var result = await _workflowService.RemoveDefaultAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Duplicate workflow
        /// Requires WORKFLOW:CREATE permission
        /// </summary>
        [HttpPost("{id}/duplicate")]
        [WFEAuthorize(PermissionConsts.Workflow.Create)]
        [RequirePermission(PermissionEntityTypeEnum.Workflow, OperationTypeEnum.Operate)]
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
        /// Requires WORKFLOW:READ permission
        /// </summary>
        [HttpGet("{id}/stages")]
        [WFEAuthorize(PermissionConsts.Workflow.Read)]
        [RequirePermission(PermissionEntityTypeEnum.Workflow, OperationTypeEnum.View)]
        [ProducesResponseType<SuccessResponse<List<StageOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetStagesByWorkflowId(long id)
        {
            var data = await _stageService.GetListByWorkflowIdAsync(id);
            return Success(data);
        }

        /// <summary>
        /// Export single workflow to detailed Excel format
        /// Requires WORKFLOW:READ permission
        /// </summary>
        [HttpGet("{id}/export-detailed-excel")]
        [WFEAuthorize(PermissionConsts.Workflow.Read)]
        [RequirePermission(PermissionEntityTypeEnum.Workflow, OperationTypeEnum.View)]
        [ProducesResponseType(typeof(FileResult), 200)]
        public async Task<IActionResult> ExportDetailedToExcel(long id)
        {
            var stream = await _workflowService.ExportDetailedToExcelAsync(id);
            var fileName = $"workflow_detailed_{id}_{DateTimeOffset.Now:MMddyyyy_HHmmss}.xlsx"; // local time for filename
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        /// <summary>
        /// Export multiple workflows to detailed Excel format
        /// Requires WORKFLOW:READ permission
        /// </summary>
        [HttpPost("export-detailed-excel")]
        [WFEAuthorize(PermissionConsts.Workflow.Read)]
        [ProducesResponseType(typeof(FileResult), 200)]
        public async Task<IActionResult> ExportMultipleDetailedToExcel([FromBody] WorkflowExportSearch search)
        {
            var stream = await _workflowService.ExportMultipleDetailedToExcelAsync(search);
            var fileName = $"workflows_detailed_export_{DateTimeOffset.Now:MMddyyyy_HHmmss}.xlsx"; // local time for filename
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        /// <summary>
        /// Sync component mappings for a workflow
        /// Requires WORKFLOW:UPDATE permission
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <returns>Success status</returns>
        [HttpPost("{workflowId}/sync-mappings")]
        [WFEAuthorize(PermissionConsts.Workflow.Update)]
        [RequirePermission(PermissionEntityTypeEnum.Workflow, OperationTypeEnum.Operate, "workflowId")]
        [ProducesResponseType<SuccessResponse<string>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> SyncWorkflowMappings(long workflowId)
        {
            try
            {
                // Verify workflow exists
                var workflow = await _workflowService.GetByIdAsync(workflowId);
                if (workflow == null)
                {
                    return NotFound($"Workflow with ID {workflowId} not found");
                }

                // Sync component mappings for all stages in the workflow
                await _componentMappingService.SyncWorkflowMappingsAsync(workflowId);

                return Success($"Component mappings successfully synced for workflow {workflowId}");
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to sync component mappings for workflow {workflowId}: {ex.Message}");
            }
        }
    }
}
