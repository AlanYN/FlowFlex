using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.StageCondition;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.WebApi.Filters;
using Item.Internal.StandardApi.Response;
using FlowFlex.Domain.Shared.Const;
using WebApi.Authorization;

namespace FlowFlex.WebApi.Controllers.OW
{
    /// <summary>
    /// Stage Condition management API - Manage condition rules and actions for workflow stages
    /// </summary>
    [ApiController]
    [Route("ow/stage-conditions/v{version:apiVersion}")]
    [Display(Name = "stage-condition")]
    [Authorize]
    public class StageConditionController : Controllers.ControllerBase
    {
        private readonly IStageConditionService _conditionService;
        private readonly IComponentDataService _componentDataService;
        private readonly IRulesEngineService _rulesEngineService;

        public StageConditionController(
            IStageConditionService conditionService,
            IComponentDataService componentDataService,
            IRulesEngineService rulesEngineService)
        {
            _conditionService = conditionService;
            _componentDataService = componentDataService;
            _rulesEngineService = rulesEngineService;
        }

        #region CRUD Operations

        /// <summary>
        /// Create a new stage condition
        /// Requires WORKFLOW:CREATE permission
        /// </summary>
        [HttpPost]
        [WFEAuthorize(PermissionConsts.Workflow.Create)]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Create([FromBody] StageConditionInputDto input)
        {
            var id = await _conditionService.CreateAsync(input);
            return Success(id);
        }

        /// <summary>
        /// Update an existing stage condition
        /// Requires WORKFLOW:UPDATE permission
        /// </summary>
        [HttpPut("{id}")]
        [WFEAuthorize(PermissionConsts.Workflow.Update)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Update(long id, [FromBody] StageConditionInputDto input)
        {
            var result = await _conditionService.UpdateAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Delete a stage condition
        /// Requires WORKFLOW:DELETE permission
        /// </summary>
        [HttpDelete("{id}")]
        [WFEAuthorize(PermissionConsts.Workflow.Delete)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _conditionService.DeleteAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Get stage condition by ID
        /// Requires WORKFLOW:READ permission
        /// </summary>
        [HttpGet("{id}")]
        [WFEAuthorize(PermissionConsts.Workflow.Read)]
        [ProducesResponseType<SuccessResponse<StageConditionOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetById(long id)
        {
            var data = await _conditionService.GetByIdAsync(id);
            return Success(data);
        }

        #endregion

        #region Query Operations

        /// <summary>
        /// Get condition by stage ID
        /// Requires WORKFLOW:READ permission
        /// </summary>
        [HttpGet("by-stage/{stageId}")]
        [WFEAuthorize(PermissionConsts.Workflow.Read)]
        [ProducesResponseType<SuccessResponse<StageConditionOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByStageId(long stageId)
        {
            var data = await _conditionService.GetByStageIdAsync(stageId);
            return Success(data);
        }

        /// <summary>
        /// Get all conditions for a workflow
        /// Requires WORKFLOW:READ permission
        /// </summary>
        [HttpGet("by-workflow/{workflowId}")]
        [WFEAuthorize(PermissionConsts.Workflow.Read)]
        [ProducesResponseType<SuccessResponse<List<StageConditionOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByWorkflowId(long workflowId)
        {
            var data = await _conditionService.GetByWorkflowIdAsync(workflowId);
            return Success(data);
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validate a condition configuration
        /// Requires WORKFLOW:READ permission
        /// </summary>
        [HttpPost("{id}/validate")]
        [WFEAuthorize(PermissionConsts.Workflow.Read)]
        [ProducesResponseType<SuccessResponse<ConditionValidationResult>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Validate(long id)
        {
            var result = await _conditionService.ValidateAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Validate RulesJson format without saving
        /// Requires WORKFLOW:READ permission
        /// </summary>
        [HttpPost("validate-rules")]
        [WFEAuthorize(PermissionConsts.Workflow.Read)]
        [ProducesResponseType<SuccessResponse<ConditionValidationResult>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> ValidateRulesJson([FromBody] ValidateRulesJsonRequest request)
        {
            var result = await _conditionService.ValidateRulesJsonAsync(request.RulesJson);
            return Success(result);
        }

        #endregion

        #region Evaluation

        /// <summary>
        /// Evaluate stage condition by case code and stage ID
        /// Requires CASE:READ permission
        /// </summary>
        /// <param name="caseCode">Case code (unique identifier for the case)</param>
        /// <param name="stageId">Stage ID to evaluate condition for</param>
        /// <returns>Condition evaluation result</returns>
        [HttpPost("evaluate/by-case/{caseCode}/stage/{stageId}")]
        [WFEAuthorize(PermissionConsts.Case.Read)]
        [ProducesResponseType<SuccessResponse<ConditionEvaluationResult>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> EvaluateConditionByCaseCode(string caseCode, long stageId)
        {
            var result = await _rulesEngineService.EvaluateConditionByCaseCodeAsync(caseCode, stageId);
            return Success(result);
        }

        /// <summary>
        /// Evaluate stage condition by onboarding ID and stage ID
        /// Requires CASE:READ permission
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID to evaluate condition for</param>
        /// <returns>Condition evaluation result</returns>
        [HttpPost("evaluate/by-onboarding/{onboardingId}/stage/{stageId}")]
        [WFEAuthorize(PermissionConsts.Case.Read)]
        [ProducesResponseType<SuccessResponse<ConditionEvaluationResult>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> EvaluateConditionByOnboardingId(long onboardingId, long stageId)
        {
            var result = await _rulesEngineService.EvaluateConditionAsync(onboardingId, stageId);
            return Success(result);
        }

        #endregion

        #region Component Data

        // Component data endpoints removed - frontend uses ComponentsJson directly
        // and field definitions are hardcoded in frontend

        #endregion
    }

    /// <summary>
    /// Request model for validating RulesJson
    /// </summary>
    public class ValidateRulesJsonRequest
    {
        /// <summary>
        /// RulesEngine Workflow JSON to validate
        /// </summary>
        public string RulesJson { get; set; } = string.Empty;
    }
}
