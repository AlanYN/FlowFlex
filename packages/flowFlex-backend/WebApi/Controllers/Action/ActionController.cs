using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Domain.Shared.Enums.Action;
using FlowFlex.Domain.Shared.Models;
using Item.Internal.StandardApi.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Net;

namespace FlowFlex.WebApi.Controllers.Action
{
    /// <summary>
    /// Action management controller
    /// </summary>
    [ApiController]
    [Route("action/v{version:apiVersion}")]
    [Authorize]
    public class ActionController : ControllerBase
    {
        private readonly IActionManagementService _actionManagementService;
        private readonly IActionExecutionService _actionExecutionService;

        public ActionController(
            IActionManagementService actionManagementService,
            IActionExecutionService actionExecutionService)
        {
            _actionManagementService = actionManagementService;
            _actionExecutionService = actionExecutionService;
        }

        #region Action Definition Management

        /// <summary>
        /// Get action definition by ID
        /// </summary>
        /// <param name="id">Action definition ID</param>
        /// <returns>Action definition</returns>
        [HttpGet("definitions/{id}")]
        [ProducesResponseType<SuccessResponse<ActionDefinitionDto>>((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetActionDefinition(long id)
        {
            var result = await _actionManagementService.GetActionDefinitionAsync(id);
            if (result == null)
            {
                return NotFound();
            }
            return Success(result);
        }

        /// <summary>
        /// Get action definitions
        /// </summary>
        /// <param name="actionName"></param>
        /// <param name="actionType"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="isAssignmentStage"></param>
        /// <param name="isAssignmentChecklist"></param>
        /// <param name="isAssignmentQuestionnaire"></param>
        /// <returns></returns>
        [HttpGet("definitions")]
        [ProducesResponseType<SuccessResponse<PageModelDto<ActionDefinitionDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetPagedActionDefinitions(string? search,
            ActionTypeEnum? actionType,
            int pageIndex = 1,
            int pageSize = 10,
            bool? isAssignmentStage = null,
            bool? isAssignmentChecklist = null,
            bool? isAssignmentQuestionnaire = null,
            bool? isAssignmentWorkflow = null)
        {
            var result = await _actionManagementService.GetPagedActionDefinitionsAsync(search,
                actionType,
                pageIndex,
                pageSize,
                isAssignmentStage,
                isAssignmentChecklist,
                isAssignmentQuestionnaire,
                isAssignmentWorkflow);
            return Success(result);
        }

        /// <summary>
        /// Get enabled action definitions
        /// </summary>
        /// <returns>List of enabled action definitions</returns>
        [HttpGet("definitions/enabled")]
        [ProducesResponseType<SuccessResponse<List<ActionDefinitionDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetEnabledActionDefinitions()
        {
            var result = await _actionManagementService.GetEnabledActionDefinitionsAsync();
            return Success(result);
        }

        /// <summary>
        /// Create new action definition
        /// </summary>
        /// <param name="dto">Create action definition DTO. 
        /// Optional parameters (WorkflowId, TriggerSourceId, TriggerType) can be provided to simultaneously create an Action Trigger Mapping.</param>
        /// <returns>Created action definition with trigger mapping creation status</returns>
        [HttpPost("definitions")]
        [ProducesResponseType<SuccessResponse<ActionDefinitionDto>>((int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CreateActionDefinition(CreateActionDefinitionDto dto)
        {
            var result = await _actionManagementService.CreateActionDefinitionAsync(dto);
            return Success(result);
        }

        /// <summary>
        /// Update action definition
        /// </summary>
        /// <param name="id">Action definition ID</param>
        /// <param name="dto">Update action definition DTO</param>
        /// <returns>Updated action definition</returns>
        [HttpPut("definitions/{id}")]
        [ProducesResponseType<SuccessResponse<ActionDefinitionDto>>((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> UpdateActionDefinition(long id, UpdateActionDefinitionDto dto)
        {
            try
            {
                var result = await _actionManagementService.UpdateActionDefinitionAsync(id, dto);
                return Success(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Delete action definition
        /// </summary>
        /// <param name="id">Action definition ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("definitions/{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteActionDefinition(long id)
        {
            var result = await _actionManagementService.DeleteActionDefinitionAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return Success(result);
        }

        /// <summary>
        /// Update action definition status
        /// </summary>
        /// <param name="id">Action definition ID</param>
        /// <param name="isEnabled">Enable or disable</param>
        /// <returns>Success status</returns>
        [HttpPatch("definitions/{id}/status")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> UpdateActionDefinitionStatus(long id, [FromBody] bool isEnabled)
        {
            var result = await _actionManagementService.UpdateActionDefinitionStatusAsync(id, isEnabled);
            if (!result)
            {
                return NotFound();
            }
            return Success(result);
        }

        /// <summary>
        /// Export factoring company
        /// </summary>
        /// <param name="request">Export request parameters</param>
        /// <returns>Exported file</returns>
        [HttpGet("definitions/export")]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportAsync(string? search,
            ActionTypeEnum? actionType,
            bool? isAssignmentStage = null,
            bool? isAssignmentChecklist = null,
            bool? isAssignmentQuestionnaire = null,
            bool? isAssignmentWorkflow = null)
        {
            return File(await _actionManagementService.ExportAsync(search,
                actionType,
                isAssignmentStage,
                isAssignmentChecklist,
                isAssignmentQuestionnaire,
                isAssignmentWorkflow), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Action_{DateTimeOffset.Now.LocalDateTime:yyyyMMddHHmmss}.xlsx");
        }

        #endregion

        #region Action Trigger Mapping Management

        /// <summary>
        /// Get action trigger mapping by ID
        /// </summary>
        /// <param name="id">Mapping ID</param>
        /// <returns>Action trigger mapping</returns>
        [HttpGet("mappings/{id}")]
        [ProducesResponseType<SuccessResponse<ActionTriggerMappingDto>>((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetActionTriggerMapping(long id)
        {
            var result = await _actionManagementService.GetActionTriggerMappingAsync(id);
            if (result == null)
            {
                return NotFound();
            }
            return Success(result);
        }

        /// <summary>
        /// Get all action trigger mappings
        /// </summary>
        /// <returns>List of action trigger mappings</returns>
        [HttpGet("mappings")]
        [ProducesResponseType<SuccessResponse<List<ActionTriggerMappingDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAllActionTriggerMappings()
        {
            var result = await _actionManagementService.GetAllActionTriggerMappingsAsync();
            return Success(result);
        }

        /// <summary>
        /// Get action trigger mappings by action definition ID
        /// </summary>
        /// <param name="actionDefinitionId">Action definition ID</param>
        /// <returns>List of action trigger mappings</returns>
        [HttpGet("mappings/action/{actionDefinitionId}")]
        [ProducesResponseType<SuccessResponse<List<ActionTriggerMappingInfo>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetActionTriggerMappingsByActionId(long actionDefinitionId)
        {
            var result = await _actionManagementService.GetActionTriggerMappingsByActionIdAsync(actionDefinitionId);
            return Success(result);
        }

        /// <summary>
        /// Get action trigger mappings by trigger type
        /// </summary>
        /// <param name="triggerType">Trigger type</param>
        /// <returns>List of action trigger mappings</returns>
        [HttpGet("mappings/trigger-type/{triggerType}")]
        [ProducesResponseType<SuccessResponse<List<ActionTriggerMappingDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetActionTriggerMappingsByTriggerType(string triggerType)
        {
            var result = await _actionManagementService.GetActionTriggerMappingsByTriggerTypeAsync(triggerType);
            return Success(result);
        }

        /// <summary>
        /// Get action trigger mappings by trigger source id
        /// </summary>
        /// <param name="triggerSourceId"></param>
        /// <returns></returns>
        [HttpGet("mappings/trigger-source/{triggerSourceId}")]
        [ProducesResponseType<SuccessResponse<List<ActionTriggerMappingDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetActionTriggerMappingsByTriggerSourceId(long triggerSourceId)
        {
            var result = await _actionManagementService.GetActionTriggerMappingsByTriggerSourceIdAsync(triggerSourceId);
            return Success(result);
        }

        /// <summary>
        /// Create new action trigger mapping
        /// </summary>
        /// <param name="dto">Create action trigger mapping DTO</param>
        /// <returns>Created action trigger mapping</returns>
        [HttpPost("mappings")]
        [ProducesResponseType<SuccessResponse<ActionTriggerMappingDto>>((int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CreateActionTriggerMapping(CreateActionTriggerMappingDto dto)
        {
            try
            {
                var result = await _actionManagementService.CreateActionTriggerMappingAsync(dto);
                return Success(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest();
            }
        }

        /// <summary>
        /// Delete action trigger mapping
        /// </summary>
        /// <param name="id">Mapping ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("mappings/{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteActionTriggerMapping(long id)
        {
            var result = await _actionManagementService.DeleteActionTriggerMappingAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return Success(result);
        }

        /// <summary>
        /// Update action trigger mapping status
        /// </summary>
        /// <param name="id">Mapping ID</param>
        /// <param name="isEnabled">Enable or disable</param>
        /// <returns>Success status</returns>
        [HttpPatch("mappings/{id}/status")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> UpdateActionTriggerMappingStatus(long id, [FromBody] bool isEnabled)
        {
            var result = await _actionManagementService.UpdateActionTriggerMappingStatusAsync(id, isEnabled);
            if (!result)
            {
                return NotFound();
            }
            return Success(result);
        }

        #endregion

        #region Action Execution

        /// <summary>
        /// Test execute action
        /// </summary>
        /// <param name="actionDefinitionId">Action definition ID</param>
        /// <param name="contextData">Context data for execution</param>
        /// <returns>Execution result</returns>
        [HttpPost("definitions/{actionDefinitionId}/test")]
        [ProducesResponseType<SuccessResponse<JToken>>((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> TestAction(long actionDefinitionId, [FromBody] object contextData = null)
        {
            try
            {
                var result = await _actionExecutionService.ExecuteActionAsync(actionDefinitionId, contextData);
                return Success(result);
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }

        #endregion
    }
}