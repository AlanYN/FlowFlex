using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Linq;
using FlowFlex.Application.Contracts.Dtos.OW.Checklist;
using FlowFlex.Application.Contracts.IServices.OW;

using Item.Internal.StandardApi.Response;
using System.Net;
using FlowFlex.Application.Contracts.Dtos.OW.ChecklistTask;

namespace FlowFlex.WebApi.Controllers.OW
{
    /// <summary>
    /// ? ChecklistTask Management API
    /// </summary>
    /// <remarks>
    /// Provides detailed management functions for checklist tasks, including:
    /// - Creating, updating, deleting, and querying tasks
    /// - Task completion status management (complete/uncomplete)
    /// - Batch operations (batch complete, sorting)
    /// - Task assignment and responsibility management
    /// - Overdue tasks and pending tasks query
    /// </remarks>

    [ApiController]

    [Route("ow/checklist-tasks/v{version:apiVersion}")]
    [Route("ow/checklist-task/v{version:apiVersion}")] // Alternative route for compatibility
    [Display(Name = "ChecklistTask Management")]
    [Tags("ChecklistTask", "Onboard Workflow", "Task Items")]

    public class ChecklistTaskController : Controllers.ControllerBase
    {
        private readonly IChecklistTaskService _checklistTaskService;

        public ChecklistTaskController(IChecklistTaskService checklistTaskService)
        {
            _checklistTaskService = checklistTaskService;
        }

        /// <summary>
        /// Create checklist task
        /// </summary>
        [HttpPost]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Create([FromBody] ChecklistTaskInputDto input)
        {
            var id = await _checklistTaskService.CreateAsync(input);
            return Success(id);
        }

        /// <summary>
        /// Update checklist task
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Update(long id, [FromBody] ChecklistTaskInputDto input)
        {
            var result = await _checklistTaskService.UpdateAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Delete checklist task (with confirmation)
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Delete(long id, [FromQuery] bool confirm = false)
        {
            var result = await _checklistTaskService.DeleteAsync(id, confirm);
            return Success(result);
        }

        /// <summary>
        /// Get checklist task by id
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType<SuccessResponse<ChecklistTaskOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetById(long id)
        {
            var data = await _checklistTaskService.GetByIdAsync(id);
            return Success(data);
        }

        /// <summary>
        /// Get tasks by checklist id
        /// </summary>
        [HttpGet("checklist/{checklistId}")]
        [HttpGet("list/{checklistId}")] // Alternative route for compatibility
        [ProducesResponseType<SuccessResponse<List<ChecklistTaskOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetListByChecklistId(long checklistId)
        {
            var data = await _checklistTaskService.GetListByChecklistIdAsync(checklistId);
            return Success(data);
        }

        /// <summary>
        /// Complete task
        /// </summary>
        [HttpPost("{id}/complete")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> CompleteTask(long id, [FromBody] CompleteTaskInputDto input)
        {
            var result = await _checklistTaskService.CompleteTaskAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Uncomplete task
        /// </summary>
        [HttpPost("{id}/uncomplete")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> UncompleteTask(long id)
        {
            var result = await _checklistTaskService.UncompleteTaskAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Batch complete tasks
        /// </summary>
        [HttpPost("batch-complete")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> BatchComplete([FromBody] BatchCompleteTasksInputDto input)
        {
            if (input == null || input.TaskIds == null || !input.TaskIds.Any())
            {
                return BadRequest("TaskIds cannot be null or empty");
            }

            var result = await _checklistTaskService.BatchCompleteAsync(input.TaskIds, new CompleteTaskInputDto
            {
                CompletionNotes = input.CompletionNotes,
                ActualHours = input.ActualHours
            });
            return Success(result);
        }

        /// <summary>
        /// Sort tasks within checklist
        /// </summary>
        [HttpPost("checklist/{checklistId}/sort")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SortTasks(long checklistId, [FromBody] Dictionary<long, int> taskOrders)
        {
            var result = await _checklistTaskService.SortTasksAsync(checklistId, taskOrders);
            return Success(result);
        }

        /// <summary>
        /// Assign task to user
        /// </summary>
        [HttpPost("{id}/assign")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> AssignTask(long id, [FromBody] AssignTaskInputDto input)
        {
            var result = await _checklistTaskService.AssignTaskAsync(id, input.AssigneeId, input.AssigneeName);
            return Success(result);
        }

        /// <summary>
        /// Get pending tasks by assignee
        /// </summary>
        [HttpGet("assignee/{assigneeId}/pending")]
        [ProducesResponseType<SuccessResponse<List<ChecklistTaskOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetPendingTasksByAssignee(long assigneeId)
        {
            var data = await _checklistTaskService.GetPendingTasksByAssigneeAsync(assigneeId);
            return Success(data);
        }

        /// <summary>
        /// Get overdue tasks
        /// </summary>
        [HttpGet("overdue")]
        [ProducesResponseType<SuccessResponse<List<ChecklistTaskOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetOverdueTasks()
        {
            var data = await _checklistTaskService.GetOverdueTasksAsync();
            return Success(data);
        }

        /// <summary>
        /// Get task dependencies for a checklist
        /// </summary>
        [HttpGet("{checklistId}/dependencies")]
        [ProducesResponseType<SuccessResponse<Dictionary<long, List<long>>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetDependencies(long checklistId)
        {
            // For now, return empty dependencies
            // Dependency validation logic to be implemented
            var dependencies = new Dictionary<long, List<long>>();
            return Success(dependencies);
        }
    }
}

