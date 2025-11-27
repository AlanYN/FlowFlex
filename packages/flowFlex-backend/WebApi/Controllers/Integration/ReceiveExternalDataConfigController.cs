using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.Dtos.Integration;
using FlowFlex.Application.Contracts.IServices.Integration;
using Item.Internal.StandardApi.Response;
using System.Net;
using FlowFlex.Domain.Shared;

namespace FlowFlex.WebApi.Controllers.Integration
{
    /// <summary>
    /// Receive External Data Configuration API
    /// </summary>
    [ApiController]
    [Route("integration/receive-external-data/v{version:apiVersion}")]
    [Display(Name = "receive-external-data")]
    [Authorize]
    public class ReceiveExternalDataConfigController : Controllers.ControllerBase
    {
        private readonly IReceiveExternalDataConfigService _service;
        private readonly ILogger<ReceiveExternalDataConfigController> _logger;

        public ReceiveExternalDataConfigController(
            IReceiveExternalDataConfigService service,
            ILogger<ReceiveExternalDataConfigController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Create receive external data config
        /// </summary>
        [HttpPost("{integrationId}")]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Create(long integrationId, [FromBody] ReceiveExternalDataConfigInputDto input)
        {
            try
            {
                var id = await _service.CreateAsync(integrationId, input);
                return Success(id);
            }
            catch (CRMException ex)
            {
                _logger.LogWarning(ex, "Failed to create receive external data config");
                return BadRequest(new { success = false, msg = ex.Message, code = ex.Code.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating receive external data config");
                return StatusCode(500, new { success = false, msg = "Internal server error", code = "500" });
            }
        }

        /// <summary>
        /// Get receive external data configs by integration
        /// </summary>
        [HttpGet("{integrationId}")]
        [ProducesResponseType<SuccessResponse<List<ReceiveExternalDataConfigOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByIntegration(long integrationId)
        {
            try
            {
                var configs = await _service.GetByIntegrationIdAsync(integrationId);
                return Success(configs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting receive external data configs for integration {IntegrationId}: {Message}", integrationId, ex.Message);
                return StatusCode(500, new { success = false, msg = $"Internal server error: {ex.Message}", code = "500", error = ex.ToString() });
            }
        }

        /// <summary>
        /// Get receive external data config by ID
        /// </summary>
        [HttpGet("{integrationId}/{id}")]
        [ProducesResponseType<SuccessResponse<ReceiveExternalDataConfigOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetById(long integrationId, long id)
        {
            try
            {
                var config = await _service.GetByIdAsync(id);
                if (config == null)
                {
                    return NotFound(new { success = false, msg = "Config not found", code = "404" });
                }
                return Success(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting receive external data config {ConfigId} for integration {IntegrationId}: {Message}", id, integrationId, ex.Message);
                return StatusCode(500, new { success = false, msg = $"Internal server error: {ex.Message}", code = "500", error = ex.ToString() });
            }
        }

        /// <summary>
        /// Update receive external data config
        /// </summary>
        [HttpPut("{integrationId}/{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Update(long integrationId, long id, [FromBody] ReceiveExternalDataConfigInputDto input)
        {
            try
            {
                // Note: UpdateAsync method needs to be added to IReceiveExternalDataConfigService
                // For now, we'll delete and recreate
                await _service.DeleteAsync(id);
                var newId = await _service.CreateAsync(integrationId, input);
                return Success(true);
            }
            catch (CRMException ex)
            {
                _logger.LogWarning(ex, "Failed to update receive external data config");
                return BadRequest(new { success = false, msg = ex.Message, code = ex.Code.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating receive external data config");
                return StatusCode(500, new { success = false, msg = "Internal server error", code = "500" });
            }
        }

        /// <summary>
        /// Delete receive external data config
        /// </summary>
        [HttpDelete("{integrationId}/{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Delete(long integrationId, long id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting receive external data config");
                return StatusCode(500, new { success = false, msg = "Internal server error", code = "500" });
            }
        }

        /// <summary>
        /// Get available workflows
        /// </summary>
        [HttpGet("available-workflows")]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAvailableWorkflows([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                // Note: This method needs to be added to IReceiveExternalDataConfigService
                // For now, return empty list
                return Success(new
                {
                    items = new List<object>(),
                    total = 0,
                    pageIndex,
                    pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available workflows");
                return StatusCode(500, new { success = false, msg = "Internal server error", code = "500" });
            }
        }
    }
}
