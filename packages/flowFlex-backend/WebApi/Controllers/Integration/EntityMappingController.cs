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
    /// Entity mapping management API
    /// </summary>
    [ApiController]
    [Route("integration/entity-mappings/v{version:apiVersion}")]
    [Display(Name = "entity-mapping")]
    [Authorize]
    public class EntityMappingController : Controllers.ControllerBase
    {
        private readonly IEntityMappingService _entityMappingService;

        public EntityMappingController(IEntityMappingService entityMappingService)
        {
            _entityMappingService = entityMappingService;
        }

        /// <summary>
        /// Create entity mapping
        /// </summary>
        [HttpPost]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Create([FromBody] EntityMappingInputDto input)
        {
            var id = await _entityMappingService.CreateAsync(input);
            return Success(id);
        }

        /// <summary>
        /// Batch save entity mappings (create, update, delete in one operation)
        /// </summary>
        [HttpPost("batch")]
        [ProducesResponseType<SuccessResponse<EntityMappingBatchSaveResultDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> BatchSave([FromBody] EntityMappingBatchSaveDto input)
        {
            var result = await _entityMappingService.BatchSaveAsync(input);
            return Success(result);
        }

        /// <summary>
        /// Update entity mapping
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Update(long id, [FromBody] EntityMappingInputDto input)
        {
            var result = await _entityMappingService.UpdateAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Delete entity mapping
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _entityMappingService.DeleteAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Get entity mapping by id
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType<SuccessResponse<EntityMappingOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetById(long id)
        {
            var data = await _entityMappingService.GetByIdAsync(id);
            return Success(data);
        }

        /// <summary>
        /// Get entity mappings by integration id
        /// </summary>
        [HttpGet("by-integration/{integrationId}")]
        [ProducesResponseType<SuccessResponse<List<EntityMappingOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByIntegrationId(long integrationId)
        {
            var data = await _entityMappingService.GetByIntegrationIdAsync(integrationId);
            return Success(data);
        }

        /// <summary>
        /// Get entity mappings with pagination
        /// </summary>
        [HttpGet]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetList(
            [FromQuery] long integrationId,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 15)
        {
            var (items, total) = await _entityMappingService.GetPagedListAsync(
                integrationId,
                pageIndex,
                pageSize);

            return Success(new
            {
                items,
                total,
                pageIndex,
                pageSize
            });
        }
    }
}

