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
    /// Dynamic field management API
    /// </summary>
    [ApiController]
    [Route("integration/dynamic-fields/v{version:apiVersion}")]
    [Display(Name = "dynamic-field")]
    [Authorize]
    public class DynamicFieldController : Controllers.ControllerBase
    {
        private readonly IDynamicFieldService _dynamicFieldService;

        public DynamicFieldController(IDynamicFieldService dynamicFieldService)
        {
            _dynamicFieldService = dynamicFieldService;
        }

        /// <summary>
        /// Create dynamic field
        /// </summary>
        [HttpPost]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Create([FromBody] DynamicFieldInputDto input)
        {
            var id = await _dynamicFieldService.CreateAsync(input);
            return Success(id);
        }

        /// <summary>
        /// Update dynamic field
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Update(long id, [FromBody] DynamicFieldInputDto input)
        {
            var result = await _dynamicFieldService.UpdateAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Delete dynamic field
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _dynamicFieldService.DeleteAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Get dynamic field by id
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType<SuccessResponse<DynamicFieldOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetById(long id)
        {
            var data = await _dynamicFieldService.GetByIdAsync(id);
            return Success(data);
        }

        /// <summary>
        /// Get dynamic field by field ID
        /// </summary>
        [HttpGet("by-field-id/{fieldId}")]
        [ProducesResponseType<SuccessResponse<DynamicFieldOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByFieldId(string fieldId)
        {
            var data = await _dynamicFieldService.GetByFieldIdAsync(fieldId);
            if (data == null)
            {
                return NotFound();
            }
            return Success(data);
        }

        /// <summary>
        /// Get all dynamic fields
        /// </summary>
        [HttpGet]
        [ProducesResponseType<SuccessResponse<List<DynamicFieldOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAll()
        {
            var data = await _dynamicFieldService.GetAllAsync();
            return Success(data);
        }

        /// <summary>
        /// Get dynamic fields by category
        /// </summary>
        [HttpGet("by-category/{category}")]
        [ProducesResponseType<SuccessResponse<List<DynamicFieldOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByCategory(string category)
        {
            var data = await _dynamicFieldService.GetByCategoryAsync(category);
            return Success(data);
        }

        /// <summary>
        /// Initialize default fields from static-field.json
        /// </summary>
        [HttpPost("initialize")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> InitializeDefaultFields()
        {
            var result = await _dynamicFieldService.InitializeDefaultFieldsAsync();
            return Success(result);
        }
    }
}

