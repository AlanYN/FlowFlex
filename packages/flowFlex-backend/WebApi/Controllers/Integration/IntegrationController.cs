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
        /// Get all integrations
        /// </summary>
        [HttpGet]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetList(
            [FromQuery] string? name = null,
            [FromQuery] string? type = null,
            [FromQuery] string? status = null)
        {
            var items = await _integrationService.GetAllAsync(name, type, status);
            return Success(new { items });
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

        /// <summary>
        /// Get integration types
        /// </summary>
        [HttpGet("types")]
        [ProducesResponseType<SuccessResponse<List<string>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetIntegrationTypes()
        {
            // Return common integration types
            var types = new List<string> { "CRM", "ERP", "Marketing", "Support", "Custom" };
            return Success(types);
        }

        /// <summary>
        /// Get active integrations
        /// </summary>
        [HttpGet("active")]
        [ProducesResponseType<SuccessResponse<List<IntegrationOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetActiveIntegrations()
        {
            var integrations = await _integrationService.GetActiveIntegrationsAsync();
            return Success(integrations);
        }

        /// <summary>
        /// Get inbound field mappings by action ID (read-only view)
        /// </summary>
        [HttpGet("{integrationId}/actions/{actionId}/inbound/field-mappings")]
        [ProducesResponseType<SuccessResponse<List<InboundFieldMappingDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetInboundFieldMappingsByAction(
            long integrationId,
            long actionId,
            [FromQuery] string? externalFieldName = null,
            [FromQuery] string? wfeFieldName = null)
        {
            var data = await _integrationService.GetInboundFieldMappingsByActionAsync(
                integrationId,
                actionId,
                externalFieldName,
                wfeFieldName);
            return Success(data);
        }

        /// <summary>
        /// Get outbound shared fields by action ID (read-only view)
        /// </summary>
        [HttpGet("{integrationId}/actions/{actionId}/outbound/shared-fields")]
        [ProducesResponseType<SuccessResponse<List<OutboundSharedFieldDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetOutboundSharedFieldsByAction(
            long integrationId,
            long actionId,
            [FromQuery] string? fieldName = null)
        {
            var data = await _integrationService.GetOutboundSharedFieldsByActionAsync(
                integrationId,
                actionId,
                fieldName);
            return Success(data);
        }

        /// <summary>
        /// Get inbound attachments configuration
        /// </summary>
        [HttpGet("{integrationId}/inbound/attachments")]
        [ProducesResponseType<SuccessResponse<InboundAttachmentsOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetInboundAttachments(long integrationId)
        {
            var data = await _integrationService.GetInboundAttachmentsAsync(integrationId);
            return Success(data);
        }

        /// <summary>
        /// Save inbound attachments configuration
        /// </summary>
        [HttpPut("{integrationId}/inbound/attachments")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SaveInboundAttachments(
            long integrationId,
            [FromBody] InboundAttachmentsInputDto input)
        {
            var result = await _integrationService.SaveInboundAttachmentsAsync(integrationId, input);
            return Success(result);
        }

        /// <summary>
        /// Get outbound attachments configuration
        /// </summary>
        [HttpGet("{integrationId}/outbound/attachments")]
        [ProducesResponseType<SuccessResponse<OutboundAttachmentsOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetOutboundAttachments(long integrationId)
        {
            var data = await _integrationService.GetOutboundAttachmentsAsync(integrationId);
            return Success(data);
        }

        /// <summary>
        /// Save outbound attachments configuration
        /// </summary>
        [HttpPut("{integrationId}/outbound/attachments")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SaveOutboundAttachments(
            long integrationId,
            [FromBody] OutboundAttachmentsInputDto input)
        {
            var result = await _integrationService.SaveOutboundAttachmentsAsync(integrationId, input);
            return Success(result);
        }

        /// <summary>
        /// Get outbound attachment workflows configuration (legacy compatible route)
        /// </summary>
        [HttpGet("{integrationId}/outbound/attachment-workflows")]
        [ProducesResponseType<SuccessResponse<OutboundAttachmentsOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetOutboundAttachmentWorkflows(long integrationId)
        {
            var data = await _integrationService.GetOutboundAttachmentsAsync(integrationId);
            return Success(data);
        }

        /// <summary>
        /// Save outbound attachment workflows configuration (legacy compatible route)
        /// </summary>
        [HttpPut("{integrationId}/outbound/attachment-workflows")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SaveOutboundAttachmentWorkflows(
            long integrationId,
            [FromBody] OutboundAttachmentsInputDto input)
        {
            var result = await _integrationService.SaveOutboundAttachmentsAsync(integrationId, input);
            return Success(result);
        }
    }
}

