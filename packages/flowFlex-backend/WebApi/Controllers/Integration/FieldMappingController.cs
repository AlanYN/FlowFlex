using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.Dtos.Integration;
using FlowFlex.Application.Contracts.IServices.Integration;
using Item.Internal.StandardApi.Response;
using System.Net;

namespace FlowFlex.WebApi.Controllers.Integration
{
    /// <summary>
    /// Inbound field mapping management API
    /// </summary>
    [ApiController]
    [Route("integration/inbound-field-mappings/v{version:apiVersion}")]
    [Display(Name = "inbound-field-mapping")]
    [Authorize]
    public class InboundFieldMappingController : Controllers.ControllerBase
    {
        private readonly IInboundFieldMappingService _fieldMappingService;

        public InboundFieldMappingController(IInboundFieldMappingService fieldMappingService)
        {
            _fieldMappingService = fieldMappingService;
        }

        /// <summary>
        /// Create inbound field mapping
        /// </summary>
        [HttpPost]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Create([FromBody] InboundFieldMappingInputDto input)
        {
            var id = await _fieldMappingService.CreateAsync(input);
            return Success(id);
        }

        /// <summary>
        /// Update inbound field mapping
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Update(long id, [FromBody] InboundFieldMappingInputDto input)
        {
            var result = await _fieldMappingService.UpdateAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Delete inbound field mapping
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _fieldMappingService.DeleteAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Get inbound field mapping by id
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType<SuccessResponse<InboundFieldMappingOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetById(long id)
        {
            var data = await _fieldMappingService.GetByIdAsync(id);
            return Success(data);
        }

        /// <summary>
        /// Get inbound field mappings by integration id
        /// </summary>
        [HttpGet("by-integration/{integrationId}")]
        [ProducesResponseType<SuccessResponse<List<InboundFieldMappingOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByIntegrationId(long integrationId)
        {
            var data = await _fieldMappingService.GetByIntegrationIdAsync(integrationId);
            return Success(data);
        }

        /// <summary>
        /// Get inbound field mappings by action id
        /// </summary>
        [HttpGet("by-action/{actionId}")]
        [ProducesResponseType<SuccessResponse<List<InboundFieldMappingOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByActionId(long actionId)
        {
            var data = await _fieldMappingService.GetByActionIdAsync(actionId);
            return Success(data);
        }

        /// <summary>
        /// Get inbound field mappings by integration id and action id
        /// </summary>
        [HttpGet("by-integration-action")]
        [ProducesResponseType<SuccessResponse<List<InboundFieldMappingOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByIntegrationIdAndActionId([FromQuery] long integrationId, [FromQuery] long actionId)
        {
            var data = await _fieldMappingService.GetByIntegrationIdAndActionIdAsync(integrationId, actionId);
            return Success(data);
        }

        /// <summary>
        /// Batch create inbound field mappings
        /// </summary>
        [HttpPost("batch")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> BatchCreate([FromBody] List<InboundFieldMappingInputDto> inputs)
        {
            var result = await _fieldMappingService.BatchUpdateAsync(inputs);
            return Success(result);
        }
    }
}
