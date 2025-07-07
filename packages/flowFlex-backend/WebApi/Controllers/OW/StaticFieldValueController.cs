using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;
using FlowFlex.Application.Contracts.Dtos.OW.StaticField;
using FlowFlex.Application.Contracts.IServices.OW;

using Item.Internal.StandardApi.Response;
using FlowFlex.Domain.Shared.Const;

namespace FlowFlex.WebApi.Controllers.OW
{
    /// <summary>
    /// Static field value management API - Includes static field value CRUD and content management functions
    /// </summary>
 
    [ApiController]
 
    [Route("ow/static-field-values/v{version:apiVersion}")]
    [Display(Name = "static field values")]
   
    public class StaticFieldValueController : Controllers.ControllerBase
    {
        private readonly IStaticFieldValueService _staticFieldValueService;

        public StaticFieldValueController(IStaticFieldValueService staticFieldValueService)
        {
            _staticFieldValueService = staticFieldValueService;
        }

        /// <summary>
        /// Save static field value
        /// </summary>
        /// <param name="input">Static field value input DTO</param>
        /// <returns>Saved ID</returns>
        [HttpPost]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SaveAsync([FromBody] StaticFieldValueInputDto input)
        {
            long result = await _staticFieldValueService.SaveAsync(input);
            return Success(result);
        }

        /// <summary>
        /// Batch save static field values
        /// </summary>
        /// <param name="input">Batch save input DTO</param>
        /// <returns>Success result</returns>
        [HttpPost("batch")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> BatchSaveAsync([FromBody] BatchStaticFieldValueInputDto input)
        {
            bool result = await _staticFieldValueService.BatchSaveAsync(input);
            return Success(result);
        }

        /// <summary>
        /// Update static field value
        /// </summary>
        /// <param name="id">Static field value ID</param>
        /// <param name="input">Static field value input DTO</param>
        /// <returns>Updated data</returns>
        [HttpPut("{id}")]
        [ProducesResponseType<SuccessResponse<StaticFieldValueOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdateAsync(long id, [FromBody] StaticFieldValueInputDto input)
        {
            // Set ID to ensure correct record update
            input.Id = id;

            // Save first (update if exists)
            await _staticFieldValueService.SaveAsync(input);

            // Return updated data
            StaticFieldValueOutputDto? result = await _staticFieldValueService.GetByIdAsync(id);
            if (result == null)
            {
                return NotFound("Static field value not found");
            }
            return Success(result);
        }

        /// <summary>
        /// Get static field value by ID
        /// </summary>
        /// <param name="id">Static field value ID</param>
        /// <returns>Static field value details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType<SuccessResponse<StaticFieldValueOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByIdAsync(long id)
        {
            StaticFieldValueOutputDto? result = await _staticFieldValueService.GetByIdAsync(id);
            if (result == null)
            {
                return NotFound("Static field value not found");
            }
            return Success(result);
        }

        /// <summary>
        /// Get static field value list by Onboarding ID
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>Static field value list</returns>
        [HttpGet("by-onboarding/{onboardingId}")]
        [ProducesResponseType<SuccessResponse<List<StaticFieldValueOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByOnboardingIdAsync(long onboardingId)
        {
            List<StaticFieldValueOutputDto> result = await _staticFieldValueService.GetByOnboardingIdAsync(onboardingId);
            return Success(result);
        }

        /// <summary>
        /// Get static field value list by Onboarding ID and Stage ID
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Static field value list</returns>
        [HttpGet("onboarding/{onboardingId}/stage/{stageId}")]
        [ProducesResponseType<SuccessResponse<List<StaticFieldValueOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByOnboardingAndStageAsync(long onboardingId, long stageId)
        {
            List<StaticFieldValueOutputDto> result = await _staticFieldValueService.GetByOnboardingAndStageAsync(onboardingId, stageId);
            return Success(result);
        }

        /// <summary>
        /// Get static field value by Onboarding ID, Stage ID and field name
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <param name="fieldName">Field name</param>
        /// <returns>Static field value</returns>
        [HttpGet("onboarding/{onboardingId}/stage/{stageId}/field/{fieldName}")]
        [ProducesResponseType<SuccessResponse<StaticFieldValueOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByOnboardingStageAndFieldAsync(long onboardingId, long stageId, string fieldName)
        {
            StaticFieldValueOutputDto? result = await _staticFieldValueService.GetByOnboardingStageAndFieldAsync(onboardingId, stageId, fieldName);
            if (result == null)
            {
                return NotFound("Static field value not found");
            }
            return Success(result);
        }

        /// <summary>
        /// Get latest version static field values
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Latest version static field value list</returns>
        [HttpGet("latest/onboarding/{onboardingId}/stage/{stageId}")]
        [ProducesResponseType<SuccessResponse<List<StaticFieldValueOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetLatestByOnboardingAndStageAsync(long onboardingId, long stageId)
        {
            List<StaticFieldValueOutputDto> result = await _staticFieldValueService.GetLatestByOnboardingAndStageAsync(onboardingId, stageId);
            return Success(result);
        }

        /// <summary>
        /// Delete static field value
        /// </summary>
        /// <param name="id">Static field value ID</param>
        /// <returns>Success result</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteAsync(long id)
        {
            bool result = await _staticFieldValueService.DeleteAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Delete static field values by Onboarding ID
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>Success result</returns>
        [HttpDelete("onboarding/{onboardingId}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteByOnboardingIdAsync(long onboardingId)
        {
            bool result = await _staticFieldValueService.DeleteByOnboardingIdAsync(onboardingId);
            return Success(result);
        }

        /// <summary>
        /// Delete static field values by Onboarding ID and Stage ID
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Success result</returns>
        [HttpDelete("onboarding/{onboardingId}/stage/{stageId}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteByOnboardingAndStageAsync(long onboardingId, long stageId)
        {
            bool result = await _staticFieldValueService.DeleteByOnboardingAndStageAsync(onboardingId, stageId);
            return Success(result);
        }

        /// <summary>
        /// Validate static field values
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Validation result</returns>
        [HttpPost("validate/{onboardingId}/stage/{stageId}")]
        [ProducesResponseType<SuccessResponse<Dictionary<string, string>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> ValidateFieldValuesAsync(long onboardingId, long stageId)
        {
            Dictionary<string, string> result = await _staticFieldValueService.ValidateFieldValuesAsync(onboardingId, stageId);
            return Success(result);
        }

        /// <summary>
        /// Submit static field values
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Success result</returns>
        [HttpPost("submit/{onboardingId}/stage/{stageId}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SubmitFieldValuesAsync(long onboardingId, long stageId)
        {
            bool result = await _staticFieldValueService.SubmitFieldValuesAsync(onboardingId, stageId);
            return Success(result);
        }

        /// <summary>
        /// Get static field value history versions
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <param name="fieldName">Field name</param>
        /// <returns>History version list</returns>
        [HttpGet("history/{onboardingId}/stage/{stageId}/field/{fieldName}")]
        [ProducesResponseType<SuccessResponse<List<StaticFieldValueOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetFieldHistoryAsync(long onboardingId, long stageId, string fieldName)
        {
            List<StaticFieldValueOutputDto> result = await _staticFieldValueService.GetFieldHistoryAsync(onboardingId, stageId, fieldName);
            return Success(result);
        }

        /// <summary>
        /// Copy static field values to other Onboardings
        /// </summary>
        /// <param name="sourceOnboardingId">Source Onboarding ID</param>
        /// <param name="request">Copy request parameters</param>
        /// <returns>Success result</returns>
        [HttpPost("copy/{sourceOnboardingId}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> CopyFieldValuesAsync(long sourceOnboardingId, [FromBody] CopyStaticFieldValueRequest request)
        {
            bool result = await _staticFieldValueService.CopyFieldValuesAsync(sourceOnboardingId, request.TargetOnboardingIds, request.StageId, request.FieldNames);
            return Success(result);
        }
    }

    /// <summary>
    /// Copy static field value request
    /// </summary>
    public class CopyStaticFieldValueRequest
    {
        /// <summary>
        /// Target Onboarding IDs
        /// </summary>
        public List<long> TargetOnboardingIds { get; set; } = new List<long>();

        /// <summary>
        /// Stage ID
        /// </summary>
        public long StageId { get; set; }

        /// <summary>
        /// Field names (if null, copy all fields)
        /// </summary>
        public List<string>? FieldNames { get; set; }
    }
}
