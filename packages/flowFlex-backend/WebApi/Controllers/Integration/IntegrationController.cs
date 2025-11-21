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
    /// Integration management API
    /// </summary>
    [ApiController]
    [Route("integration/v{version:apiVersion}")]
    [Display(Name = "integration")]
    [Authorize]
    public class IntegrationController : Controllers.ControllerBase
    {
        private readonly IIntegrationService _integrationService;

        public IntegrationController(IIntegrationService integrationService)
        {
            _integrationService = integrationService;
        }

        /// <summary>
        /// Create integration
        /// </summary>
        [HttpPost]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Create([FromBody] IntegrationInputDto input)
        {
            var id = await _integrationService.CreateAsync(input);
            return Success(id);
        }

        /// <summary>
        /// Update integration
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Update(long id, [FromBody] IntegrationInputDto input)
        {
            var result = await _integrationService.UpdateAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Delete integration
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _integrationService.DeleteAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Get integration by id
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType<SuccessResponse<IntegrationOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetById(long id)
        {
            var data = await _integrationService.GetByIdAsync(id);
            return Success(data);
        }

        /// <summary>
        /// Get integration with all details
        /// </summary>
        [HttpGet("{id}/details")]
        [ProducesResponseType<SuccessResponse<IntegrationOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetWithDetails(long id)
        {
            var data = await _integrationService.GetWithDetailsAsync(id);
            return Success(data);
        }

        /// <summary>
        /// Get integration list with pagination
        /// </summary>
        [HttpGet]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetList(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 15,
            [FromQuery] string? name = null,
            [FromQuery] string? type = null,
            [FromQuery] string? status = null,
            [FromQuery] string sortField = "CreateDate",
            [FromQuery] string sortDirection = "desc")
        {
            var (items, total) = await _integrationService.GetPagedListAsync(
                pageIndex,
                pageSize,
                name,
                type,
                status,
                sortField,
                sortDirection);

            return Success(new
            {
                items,
                total,
                pageIndex,
                pageSize
            });
        }

        /// <summary>
        /// Test integration connection
        /// </summary>
        [HttpPost("{id}/test-connection")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> TestConnection(long id)
        {
            var result = await _integrationService.TestConnectionAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Update integration status
        /// </summary>
        [HttpPut("{id}/status")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdateStatus(long id, [FromBody] global::Domain.Shared.Enums.IntegrationStatus status)
        {
            var result = await _integrationService.UpdateStatusAsync(id, status);
            return Success(result);
        }
    }
}

