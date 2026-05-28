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
    /// Integration management API - Manages external system integrations (CRM, ERP, Marketing, etc.)
    /// including connection configuration, field mappings, attachment sync, and connection testing.
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
        /// Create a new external system integration configuration
        /// </summary>
        /// <param name="input">Integration configuration including name, type, connection details, and credentials</param>
        /// <returns>Created integration ID</returns>
        [HttpPost]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Create([FromBody] IntegrationInputDto input)
        {
            var id = await _integrationService.CreateAsync(input);
            return Success(id);
        }

        /// <summary>
        /// Update an existing integration configuration
        /// </summary>
        /// <param name="id">Integration ID</param>
        /// <param name="input">Updated integration configuration</param>
        /// <returns>Whether update was successful</returns>
        [HttpPut("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Update(long id, [FromBody] IntegrationInputDto input)
        {
            var result = await _integrationService.UpdateAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Delete an integration configuration and its associated mappings
        /// </summary>
        /// <param name="id">Integration ID</param>
        /// <returns>Whether deletion was successful</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _integrationService.DeleteAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Get integration basic information by ID
        /// </summary>
        /// <param name="id">Integration ID</param>
        /// <returns>Integration configuration details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType<SuccessResponse<IntegrationOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetById(long id)
        {
            var data = await _integrationService.GetByIdAsync(id);
            return Success(data);
        }

        /// <summary>
        /// Get integration with all details including field mappings and attachment configurations
        /// </summary>
        /// <param name="id">Integration ID</param>
        /// <returns>Full integration details with all related configurations</returns>
        [HttpGet("{id}/details")]
        [ProducesResponseType<SuccessResponse<IntegrationOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetWithDetails(long id)
        {
            var data = await _integrationService.GetWithDetailsAsync(id);
            return Success(data);
        }

        /// <summary>
        /// Get all integrations with optional filtering by name, type, and status
        /// </summary>
        /// <param name="name">Filter by integration name (optional)</param>
        /// <param name="type">Filter by integration type: CRM, ERP, Marketing, Support, Custom (optional)</param>
        /// <param name="status">Filter by status (optional)</param>
        /// <returns>List of integrations matching the filters</returns>
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
        /// Test integration connection to verify credentials and endpoint accessibility
        /// </summary>
        /// <param name="id">Integration ID</param>
        /// <returns>Connection test result with status and response time</returns>
        [HttpPost("{id}/test-connection")]
        [ProducesResponseType<SuccessResponse<TestConnectionResultDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> TestConnection(long id)
        {
            var result = await _integrationService.TestConnectionAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Update integration status (Active/Inactive/Error)
        /// </summary>
        /// <param name="id">Integration ID</param>
        /// <param name="status">New status value</param>
        /// <returns>Whether status update was successful</returns>
        [HttpPut("{id}/status")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdateStatus(long id, [FromBody] global::Domain.Shared.Enums.IntegrationStatus status)
        {
            var result = await _integrationService.UpdateStatusAsync(id, status);
            return Success(result);
        }

        /// <summary>
        /// Get available integration types for dropdown selection
        /// </summary>
        /// <returns>List of integration type options: CRM, ERP, Marketing, Support, Custom</returns>
        [HttpGet("types")]
        [ProducesResponseType<SuccessResponse<List<string>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetIntegrationTypes()
        {
            // Return common integration types
            var types = new List<string> { "CRM", "ERP", "Marketing", "Support", "Custom" };
            return Success(types);
        }

        /// <summary>
        /// Get all integrations with Active status
        /// </summary>
        /// <returns>List of active integrations</returns>
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
        /// <param name="actionId">Action ID</param>
        /// <param name="externalFieldName">Filter by external field name (optional)</param>
        /// <param name="wfeFieldName">Filter by WFE field name (optional)</param>
        /// <returns>List of inbound field mappings</returns>
        [HttpGet("actions/{actionId}/inbound/field-mappings")]
        [ProducesResponseType<SuccessResponse<List<InboundFieldMappingDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetInboundFieldMappingsByAction(
            long actionId,
            [FromQuery] string? externalFieldName = null,
            [FromQuery] string? wfeFieldName = null)
        {
            var data = await _integrationService.GetInboundFieldMappingsByActionAsync(
                actionId,
                externalFieldName,
                wfeFieldName);
            return Success(data);
        }

        /// <summary>
        /// Get outbound shared fields by action ID (read-only view)
        /// </summary>
        /// <param name="actionId">Action ID</param>
        /// <param name="fieldName">Filter by field name (optional)</param>
        /// <returns>List of outbound shared fields</returns>
        [HttpGet("actions/{actionId}/outbound/shared-fields")]
        [ProducesResponseType<SuccessResponse<List<OutboundSharedFieldDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetOutboundSharedFieldsByAction(
            long actionId,
            [FromQuery] string? fieldName = null)
        {
            var data = await _integrationService.GetOutboundSharedFieldsByActionAsync(
                actionId,
                fieldName);
            return Success(data);
        }

        /// <summary>
        /// Get inbound attachments configuration
        /// </summary>
        /// <param name="integrationId">Integration ID</param>
        /// <returns>Inbound attachments configuration</returns>
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
        /// <param name="integrationId">Integration ID</param>
        /// <param name="input">Inbound attachments configuration data</param>
        /// <returns>Whether save was successful</returns>
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
        /// <param name="integrationId">Integration ID</param>
        /// <returns>Outbound attachments configuration</returns>
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
        /// <param name="integrationId">Integration ID</param>
        /// <param name="input">Outbound attachments configuration data</param>
        /// <returns>Whether save was successful</returns>
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
        /// <param name="integrationId">Integration ID</param>
        /// <returns>Outbound attachments configuration</returns>
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
        /// <param name="integrationId">Integration ID</param>
        /// <param name="input">Outbound attachments configuration data</param>
        /// <returns>Whether save was successful</returns>
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

