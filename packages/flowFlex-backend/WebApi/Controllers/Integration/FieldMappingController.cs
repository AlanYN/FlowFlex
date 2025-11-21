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
    /// Field mapping management API
    /// </summary>
    [ApiController]
    [Route("integration/field-mappings/v{version:apiVersion}")]
    [Display(Name = "field-mapping")]
    [Authorize]
    public class FieldMappingController : Controllers.ControllerBase
    {
        private readonly IFieldMappingService _fieldMappingService;

        public FieldMappingController(IFieldMappingService fieldMappingService)
        {
            _fieldMappingService = fieldMappingService;
        }

        /// <summary>
        /// Create field mapping
        /// </summary>
        [HttpPost]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Create([FromBody] FieldMappingInputDto input)
        {
            var id = await _fieldMappingService.CreateAsync(input);
            return Success(id);
        }

        /// <summary>
        /// Update field mapping
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Update(long id, [FromBody] FieldMappingInputDto input)
        {
            var result = await _fieldMappingService.UpdateAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Delete field mapping
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _fieldMappingService.DeleteAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Get field mapping by id
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType<SuccessResponse<FieldMappingOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetById(long id)
        {
            var data = await _fieldMappingService.GetByIdAsync(id);
            return Success(data);
        }

        /// <summary>
        /// Get field mappings by entity mapping id
        /// </summary>
        [HttpGet("by-entity-mapping/{entityMappingId}")]
        [ProducesResponseType<SuccessResponse<List<FieldMappingOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByEntityMappingId(long entityMappingId)
        {
            var data = await _fieldMappingService.GetByEntityMappingIdAsync(entityMappingId);
            return Success(data);
        }

        /// <summary>
        /// Get field mappings by integration id
        /// </summary>
        [HttpGet("by-integration/{integrationId}")]
        [ProducesResponseType<SuccessResponse<List<FieldMappingOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByIntegrationId(long integrationId)
        {
            var data = await _fieldMappingService.GetByIntegrationIdAsync(integrationId);
            return Success(data);
        }

        /// <summary>
        /// Batch update field mappings
        /// </summary>
        [HttpPut("batch")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> BatchUpdate([FromBody] List<FieldMappingInputDto> inputs)
        {
            var result = await _fieldMappingService.BatchUpdateAsync(inputs);
            return Success(result);
        }
    }
}

