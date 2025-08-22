using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.Checklist;
using FlowFlex.Application.Contracts.IServices.OW;


using Item.Internal.StandardApi.Response;
using System.Net;
using System.Linq.Dynamic.Core;

namespace FlowFlex.WebApi.Controllers.OW
{
    /// <summary>
    /// ?? Checklist Management API
    /// </summary>
    /// <remarks>
    /// Provides complete CRUD operations for task checklists, including:
    /// - Creating, updating, deleting, and querying checklists
    /// - Template management and instance creation
    /// - Completion rate calculation and statistical analysis
    /// - PDF export functionality
    /// </remarks>
    [ApiController]
    [Route("ow/checklists/v{version:apiVersion}")]
    [Display(Name = "Checklist Management")]
    [Tags("OW-Checklist", "Onboard Workflow", "Task Management")]
    [Authorize] // 添加授权特性，要求所有checklist API都需要认证
    public class ChecklistController : Controllers.ControllerBase
    {
        private readonly IChecklistService _checklistService;

        public ChecklistController(IChecklistService checklistService)
        {
            _checklistService = checklistService;
        }

        /// <summary>
        /// Create checklist
        /// </summary>
        /// <param name="input">Checklist input data</param>
        /// <returns>Created checklist ID</returns>
        [HttpPost]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> Create([FromBody] ChecklistInputDto input)
        {
            var id = await _checklistService.CreateAsync(input);
            return Success(id);
        }

        /// <summary>
        /// Update checklist
        /// </summary>
        /// <param name="id">Checklist ID</param>
        /// <param name="input">Checklist input data</param>
        /// <returns>Success status</returns>
        [HttpPut("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> Update(long id, [FromBody] ChecklistInputDto input)
        {
            var result = await _checklistService.UpdateAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Delete checklist (with confirmation)
        /// </summary>
        /// <param name="id">Checklist ID</param>
        /// <param name="confirm">Confirmation flag</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> Delete(long id, [FromQuery] bool confirm = false)
        {
            var result = await _checklistService.DeleteAsync(id, confirm);
            return Success(result);
        }

        /// <summary>
        /// Get checklist by id
        /// </summary>
        /// <param name="id">Checklist ID</param>
        /// <returns>Checklist details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType<SuccessResponse<ChecklistOutputDto>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> GetById(long id)
        {
            var data = await _checklistService.GetByIdAsync(id);
            return Success(data);
        }

        /// <summary>
        /// Get checklist list with pagination support
        /// </summary>
        /// <param name="pageIndex">Page index (starting from 1, default: 1)</param>
        /// <param name="pageSize">Page size (default: 15)</param>
        /// <param name="sortField">Sort field (default: CreateDate)</param>
        /// <param name="sortDirection">Sort direction (asc/desc, default: desc)</param>
        /// <param name="name">Filter by name (supports comma-separated values)</param>
        /// <param name="team">Filter by team (optional)</param>
        /// <returns>Paged list of checklists or simple list when no pagination params provided</returns>
        [HttpGet]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetList(
            [FromQuery] int? pageIndex = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] string sortField = null,
            [FromQuery] string sortDirection = null,
            [FromQuery] string name = null,
            [FromQuery] string team = null)
        {
            // If pagination parameters are provided, use paged query
            if (pageIndex.HasValue || pageSize.HasValue)
            {
                var query = new ChecklistQueryRequest
                {
                    PageIndex = pageIndex ?? 1,
                    PageSize = pageSize ?? 15,
                    SortField = sortField ?? "CreateDate",
                    SortDirection = sortDirection ?? "desc",
                    Name = name,
                    Team = team
                };
                var pagedData = await _checklistService.QueryAsync(query);
                return Success(pagedData);
            }
            
            // Otherwise, use original simple list
            var data = await _checklistService.GetListAsync(team);
            return Success(data);
        }

        /// <summary>
        /// Get checklists by multiple IDs (batch query)
        /// </summary>
        /// <param name="ids">List of checklist IDs</param>
        /// <returns>List of checklists</returns>
        [HttpPost("batch/by-ids")]
        [ProducesResponseType<SuccessResponse<List<ChecklistOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByIds([FromBody] List<long> ids)
        {
            var data = await _checklistService.GetByIdsAsync(ids);
            return Success(data);
        }

        /// <summary>
        /// Query checklist (paged)
        /// </summary>
        /// <param name="query">Query parameters</param>
        /// <returns>Paged checklist results</returns>
        [HttpPost("query")]
        [ProducesResponseType<SuccessResponse<PagedResult<ChecklistOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Query([FromBody] ChecklistQueryRequest query)
        {
            var data = await _checklistService.QueryAsync(query);
            return Success(data);
        }

        /// <summary>
        /// Duplicate checklist
        /// </summary>
        /// <param name="id">Source checklist ID</param>
        /// <param name="input">Duplication parameters</param>
        /// <returns>New checklist ID</returns>
        [HttpPost("{id}/duplicate")]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> Duplicate(long id, [FromBody] DuplicateChecklistInputDto input)
        {
            var newId = await _checklistService.DuplicateAsync(id, input);
            return Success(newId);
        }

        /// <summary>
        /// Export checklist to PDF
        /// </summary>
        /// <param name="id">Checklist ID</param>
        /// <returns>PDF file</returns>
        [HttpGet("{id}/export-pdf")]
        [ProducesResponseType(typeof(FileResult), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> ExportToPdf(long id)
        {
            var stream = await _checklistService.ExportToPdfAsync(id);
            var fileName = $"checklist_{id}_{DateTimeOffset.Now:MMddyyyy_HHmmss}.pdf"; // local time for filename
            return File(stream, "application/pdf", fileName);
        }

        /// <summary>
        /// Get checklist templates
        /// </summary>
        /// <returns>List of template checklists</returns>
        [HttpGet("templates")]
        [ProducesResponseType<SuccessResponse<List<ChecklistOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetTemplates()
        {
            var data = await _checklistService.GetTemplatesAsync();
            return Success(data);
        }

        /// <summary>
        /// Create checklist from template
        /// </summary>
        /// <param name="templateId">Template ID</param>
        /// <param name="input">Checklist input data</param>
        /// <returns>New checklist ID</returns>
        [HttpPost("templates/{templateId}/create")]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> CreateFromTemplate(long templateId, [FromBody] ChecklistInputDto input)
        {
            var newId = await _checklistService.CreateFromTemplateAsync(templateId, input);
            return Success(newId);
        }

        /// <summary>
        /// Calculate checklist completion rate
        /// </summary>
        /// <param name="id">Checklist ID</param>
        /// <returns>Completion rate percentage</returns>
        [HttpPost("{id}/calculate-completion")]
        [ProducesResponseType<SuccessResponse<decimal>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> CalculateCompletion(long id)
        {
            var result = await _checklistService.CalculateCompletionAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Get checklist completion rate
        /// </summary>
        /// <param name="id">Checklist ID</param>
        /// <returns>Completion rate percentage</returns>
        [HttpGet("{id}/completion")]
        [ProducesResponseType<SuccessResponse<decimal>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> GetCompletion(long id)
        {
            var checklist = await _checklistService.GetByIdAsync(id);
            return Success(checklist.CompletionRate);
        }

        /// <summary>
        /// Get overall checklist statistics
        /// </summary>
        /// <returns>Overall checklist statistics</returns>
        [HttpGet("statistics")]
        [ProducesResponseType<SuccessResponse<ChecklistStatisticsDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetStatistics()
        {
            // Get statistics for all teams or current user's team
            var data = await _checklistService.GetStatisticsByTeamAsync(null);
            return Success(data);
        }

        /// <summary>
        /// Get checklist statistics by team
        /// </summary>
        /// <param name="team">Team name</param>
        /// <returns>Team checklist statistics</returns>
        [HttpGet("statistics/{team}")]
        [ProducesResponseType<SuccessResponse<ChecklistStatisticsDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetStatisticsByTeam(string team)
        {
            var data = await _checklistService.GetStatisticsByTeamAsync(team);
            return Success(data);
        }

        /// <summary>
        /// Get workflows for dropdown selection
        /// </summary>
        /// <returns>List of workflow options</returns>
        [HttpGet("workflows")]
        [ProducesResponseType<SuccessResponse<List<object>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetWorkflowOptions()
        {
            // Implementation requires IWorkflowService injection - placeholder for future enhancement
            var options = new List<object>();
            return Success(options);
        }

        /// <summary>
        /// Get checklists by stage ID
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <returns>List of checklists for the stage</returns>
        [HttpGet("by-stage/{stageId}")]
        [ProducesResponseType<SuccessResponse<List<ChecklistOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByStage(long stageId)
        {
            var data = await _checklistService.GetByStageIdAsync(stageId);
            return Success(data);
        }

        /// <summary>
        /// Batch get checklists by stage IDs
        /// </summary>
        /// <param name="request">Batch request</param>
        /// <returns>Batch checklist response</returns>
        [HttpPost("batch/by-stages")]
        [ProducesResponseType<SuccessResponse<BatchStageChecklistResponse>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByStageIdsBatch([FromBody] BatchStageChecklistRequest request)
        {
            var result = await _checklistService.GetByStageIdsBatchAsync(request);
            return Success(result);
        }

        /// <summary>
        /// Get stages for dropdown selection
        /// </summary>
        /// <param name="workflowId">Optional workflow ID to filter stages</param>
        /// <returns>List of stage options</returns>
        [HttpGet("stages")]
        [ProducesResponseType<SuccessResponse<List<object>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetStageOptions([FromQuery] long? workflowId = null)
        {
            // Implementation requires IStageService injection - placeholder for future enhancement
            var options = new List<object>();
            return Success(options);
        }

        /// <summary>
        /// Debug: Get all checklists to check data
        /// </summary>
        /// <returns>List of all checklists</returns>
        [HttpGet("debug/all")]
        [ProducesResponseType<SuccessResponse<List<ChecklistOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAllForDebug()
        {
            var data = await _checklistService.GetListAsync();
            return Success(data);
        }
    }
}

