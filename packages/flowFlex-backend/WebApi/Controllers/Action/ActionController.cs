
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Domain.Shared.Enums.Action;
using FlowFlex.Domain.Shared.Models;
using Item.Internal.StandardApi.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<ActionController> _logger;

        public ActionController(
            IActionManagementService actionManagementService,
            IActionExecutionService actionExecutionService,
            ILogger<ActionController> logger)
        {
            _actionManagementService = actionManagementService;
            _actionExecutionService = actionExecutionService;
            _logger = logger;
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
        /// <param name="isTools"></param>
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
            bool? isAssignmentWorkflow = null,
            bool? isTools = null,
            bool? isSystemTools = null)
        {
            // If isSystemTools is true, override actionType to System and ignore isTools
            if (isSystemTools == true)
            {
                actionType = ActionTypeEnum.System;
                isTools = null; // Ignore isTools when isSystemTools is true
            }

            var result = await _actionManagementService.GetPagedActionDefinitionsAsync(search,
                actionType,
                pageIndex,
                pageSize,
                isAssignmentStage,
                isAssignmentChecklist,
                isAssignmentQuestionnaire,
                isAssignmentWorkflow,
                isTools);
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
        public async Task<IActionResult> UpdateActionDefinition(long id, [FromBody] JObject requestData)
        {
            _logger.LogInformation("Received PUT request for action definition ID: {ActionId} with data: {Data}", id, requestData);
            
            try
            {
                // Extract basic DTO fields
                var dto = new UpdateActionDefinitionDto
                {
                    Name = requestData["name"]?.ToString() ?? "",
                    Description = requestData["description"]?.ToString() ?? "",
                    ActionType = (ActionTypeEnum)(requestData["actionType"]?.ToObject<int>() ?? 0),
                    ActionConfig = requestData["actionConfig"]?.ToString() ?? "{}",
                    IsEnabled = requestData["isEnabled"]?.ToObject<bool>() ?? true,
                    IsTools = requestData["isTools"]?.ToObject<bool>() ?? false
                };

                var result = await _actionManagementService.UpdateActionDefinitionAsync(id, dto);
                
                // Handle triggerMappings for System Actions
                if (dto.ActionType == ActionTypeEnum.System && requestData["triggerMappings"] != null)
                {
                    _logger.LogInformation("Processing triggerMappings update for System Action {ActionId}", id);
                    
                    var triggerMappings = requestData["triggerMappings"]?.ToObject<List<JObject>>();
                    if (triggerMappings != null && triggerMappings.Any())
                    {
                        // For now, we'll just log the trigger mappings
                        // In a full implementation, you might want to update existing mappings
                        foreach (var mapping in triggerMappings)
                        {
                            var mappingId = mapping["id"]?.ToString();
                            var isEnabled = mapping["isEnabled"]?.ToObject<bool>() ?? true;
                            
                            _logger.LogInformation("TriggerMapping {MappingId} - IsEnabled: {IsEnabled}", mappingId, isEnabled);
                            
                            // Update mapping status if needed
                            if (long.TryParse(mappingId, out var mappingIdLong))
                            {
                                await _actionManagementService.UpdateActionTriggerMappingStatusAsync(mappingIdLong, isEnabled);
                            }
                        }
                    }
                }
                
                _logger.LogInformation("Successfully updated action definition with ID: {ActionId}", id);
                return Success(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Action definition not found for ID: {ActionId}, Error: {ErrorMessage}", id, ex.Message);
                return NotFound(new { message = $"Action definition with ID {id} not found", error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating action definition with ID: {ActionId}", id);
                return BadRequest(new { message = "Error updating action definition", error = ex.Message });
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
            bool? isAssignmentWorkflow = null,
            bool? isTools = null,
            bool? isSystemTools = null)
        {
            // If isSystemTools is true, override actionType to System and ignore isTools
            if (isSystemTools == true)
            {
                actionType = ActionTypeEnum.System;
                isTools = null; // Ignore isTools when isSystemTools is true
            }

            return File(await _actionManagementService.ExportAsync(search,
                actionType,
                isAssignmentStage,
                isAssignmentChecklist,
                isAssignmentQuestionnaire,
                isAssignmentWorkflow,
                isTools), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Action_{DateTimeOffset.Now.LocalDateTime:yyyyMMddHHmmss}.xlsx");
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
        [ProducesResponseType<SuccessResponse<List<ActionTriggerMappingWithActionInfo>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetActionTriggerMappingsByTriggerSourceId(long triggerSourceId)
        {
            var result = await _actionManagementService.GetActionTriggerMappingsByTriggerSourceIdAsync(triggerSourceId);
            return Success(result);
        }

        /// <summary>
        /// Create new action trigger mapping
        /// </summary>
        /// <param name="dto">Create action trigger mapping DTO. 
        /// TriggerEvent defaults to "Completed" if not provided.</param>
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
        public async Task<IActionResult> DeleteActionTriggerMapping(long id)
        {
            var result = await _actionManagementService.DeleteActionTriggerMappingAsync(id);
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

        /// <summary>
        /// Test execute action directly without saving ActionDefinition
        /// </summary>
        /// <param name="request">Direct execution request</param>
        /// <returns>Execution result</returns>
        [HttpPost("test/direct")]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> TestActionDirectly([FromBody] DirectActionExecutionRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Request body cannot be null");
                }

                if (string.IsNullOrWhiteSpace(request.ActionConfig))
                {
                    return BadRequest("Action configuration cannot be empty");
                }

                var result = await _actionExecutionService.ExecuteActionDirectlyAsync(
                    request.ActionType,
                    request.ActionConfig,
                    request.ContextData);

                return Success(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest($"Execution failed: {ex.Message}");
            }
        }

        #endregion

        #region Action Execution History

        /// <summary>
        /// Get executions by trigger source ID (simple GET)
        /// </summary>
        /// <param name="triggerSourceId">Trigger source ID</param>
        /// <param name="pageIndex">Page index (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10)</param>
        /// <returns>Paginated executions with action information</returns>
        [HttpGet("executions/trigger-source/{triggerSourceId}")]
        [ProducesResponseType<SuccessResponse<PageModelDto<ActionExecutionWithActionInfoDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetExecutionsByTriggerSourceId(
            long triggerSourceId,
            int pageIndex = 1,
            int pageSize = 10)
        {
            var result = await _actionExecutionService.GetExecutionsByTriggerSourceIdAsync(
                triggerSourceId, pageIndex, pageSize);
            return Success(result);
        }

        /// <summary>
        /// Get executions by trigger source ID with JSON conditions
        /// </summary>
        /// <param name="triggerSourceId">Trigger source ID</param>
        /// <param name="request">Search request with JSON conditions</param>
        /// <returns>Paginated executions with action information</returns>
        [HttpPost("executions/trigger-source/{triggerSourceId}/search")]
        [ProducesResponseType<SuccessResponse<PageModelDto<ActionExecutionWithActionInfoDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetExecutionsByTriggerSourceIdWithConditions(
            [FromRoute] long triggerSourceId,
            [FromBody] GetExecutionsByTriggerSourceIdRequest request)
        {
            var result = await _actionExecutionService.GetExecutionsByTriggerSourceIdAsync(
                triggerSourceId, request.PageIndex, request.PageSize, request.JsonConditions);
            return Success(result);
        }

        #endregion

        #region System Predefined Actions

        /// <summary>
        /// Get system predefined actions
        /// </summary>
        /// <returns>List of system predefined actions</returns>
        [HttpGet("system/predefined")]
        [ProducesResponseType<SuccessResponse<List<SystemActionDefinitionDto>>>((int)HttpStatusCode.OK)]
        public IActionResult GetSystemPredefinedActions()
        {
            var systemActions = new List<SystemActionDefinitionDto>
            {
                new SystemActionDefinitionDto
                {
                    ActionName = "CompleteStage",
                    DisplayName = "Complete Stage",
                    Description = "Complete a specific stage in the workflow",
                    TriggerType = TriggerTypeEnum.Task, // 在Task完成时触发
                     ConfigSchema = new
                     {
                         actionName = "CompleteStage",
                         stageId = "Optional: Stage ID to complete (can be extracted from trigger context)",
                         onboardingId = "Optional: Onboarding ID (can be extracted from trigger context)",
                         completionNotes = "Optional: Completion notes (default: 'Completed by system action')",
                         autoMoveToNext = "Optional: Auto move to next stage (default: true)"
                     },
                     ExampleConfig = @"{
  ""actionName"": ""CompleteStage"",
  ""completionNotes"": ""Stage completed automatically"",
  ""autoMoveToNext"": true
}"
                },
                new SystemActionDefinitionDto
                {
                    ActionName = "MoveToStage",
                    DisplayName = "Move to Stage",
                    Description = "Move onboarding to a specific stage",
                    TriggerType = TriggerTypeEnum.Stage, // 在Stage完成时触发
                    ConfigSchema = new
                    {
                        actionName = "MoveToStage",
                        targetStageId = "Required: Target stage ID to move to",
                        onboardingId = "Optional: Onboarding ID (can be extracted from trigger context)",
                        notes = "Optional: Move notes (default: 'Moved by system action')"
                    },
                    ExampleConfig = @"{
  ""actionName"": ""MoveToStage"",
  ""targetStageId"": 123,
  ""notes"": ""Moved to next stage automatically""
}"
                },
                new SystemActionDefinitionDto
                {
                    ActionName = "AssignOnboarding",
                    DisplayName = "Assign Onboarding",
                    Description = "Assign an onboarding to a specific user",
                    TriggerType = TriggerTypeEnum.Workflow, // 在Workflow级别触发
                    ConfigSchema = new
                    {
                        actionName = "AssignOnboarding",
                        onboardingId = "Optional: Onboarding ID (can be extracted from trigger context)",
                        assigneeId = "Required: User ID to assign",
                        assigneeName = "Optional: User name",
                        team = "Optional: Team name",
                        notes = "Optional: Assignment notes (default: 'Assigned by system action')"
                    },
                    ExampleConfig = @"{
  ""actionName"": ""AssignOnboarding"",
  ""assigneeId"": 123,
  ""assigneeName"": ""John Doe"",
  ""team"": ""Support Team"",
  ""notes"": ""Onboarding assigned automatically""
}"
                }
            };

            return Success(systemActions);
        }

        /// <summary>
        /// Get system action configuration template
        /// </summary>
        /// <param name="actionName">System action name</param>
        /// <returns>Configuration template</returns>
        [HttpGet("system/template/{actionName}")]
        [ProducesResponseType<SuccessResponse<SystemActionTemplateDto>>((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public IActionResult GetSystemActionTemplate(string actionName)
        {
            var templates = new Dictionary<string, SystemActionTemplateDto>
            {
                ["CompleteStage"] = new SystemActionTemplateDto
                {
                    ActionName = "CompleteStage",
                     Template = @"{
   ""actionName"": ""CompleteStage"",
   ""stageId"": null,
   ""onboardingId"": null,
   ""completionNotes"": ""Completed by system action"",
   ""autoMoveToNext"": true
 }",
                     Parameters = new List<SystemActionParameterDto>
                     {
                         new SystemActionParameterDto { Name = "stageId", Type = "number", Required = false, Description = "Stage ID to complete (can be extracted from trigger context)" },
                         new SystemActionParameterDto { Name = "onboardingId", Type = "number", Required = false, Description = "Onboarding ID (can be extracted from trigger context)" },
                         new SystemActionParameterDto { Name = "completionNotes", Type = "string", Required = false, Description = "Completion notes" },
                         new SystemActionParameterDto { Name = "autoMoveToNext", Type = "boolean", Required = false, Description = "Auto move to next stage" }
                     }
                },
                ["MoveToStage"] = new SystemActionTemplateDto
                {
                    ActionName = "MoveToStage",
                    Template = @"{
  ""actionName"": ""MoveToStage"",
  ""targetStageId"": null,
  ""onboardingId"": null,
  ""notes"": ""Moved by system action""
}",
                    Parameters = new List<SystemActionParameterDto>
                    {
                        new SystemActionParameterDto { Name = "targetStageId", Type = "number", Required = true, Description = "Target stage ID to move to" },
                        new SystemActionParameterDto { Name = "onboardingId", Type = "number", Required = false, Description = "Onboarding ID (can be extracted from trigger context)" },
                        new SystemActionParameterDto { Name = "notes", Type = "string", Required = false, Description = "Move notes" }
                    }
                },
                ["AssignOnboarding"] = new SystemActionTemplateDto
                {
                    ActionName = "AssignOnboarding",
                    Template = @"{
  ""actionName"": ""AssignOnboarding"",
  ""onboardingId"": null,
  ""assigneeId"": null,
  ""assigneeName"": null,
  ""team"": null,
  ""notes"": ""Assigned by system action""
}",
                    Parameters = new List<SystemActionParameterDto>
                    {
                        new SystemActionParameterDto { Name = "onboardingId", Type = "number", Required = false, Description = "Onboarding ID (can be extracted from trigger context)" },
                        new SystemActionParameterDto { Name = "assigneeId", Type = "number", Required = true, Description = "User ID to assign" },
                        new SystemActionParameterDto { Name = "assigneeName", Type = "string", Required = false, Description = "User name" },
                        new SystemActionParameterDto { Name = "team", Type = "string", Required = false, Description = "Team name" },
                        new SystemActionParameterDto { Name = "notes", Type = "string", Required = false, Description = "Assignment notes" }
                    }
                }
            };

            if (!templates.TryGetValue(actionName, out var template))
            {
                return NotFound($"System action template '{actionName}' not found");
            }

            return Success(template);
        }

        #endregion
    }
}