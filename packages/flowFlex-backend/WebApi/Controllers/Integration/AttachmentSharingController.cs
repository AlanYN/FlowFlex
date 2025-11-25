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
    /// Attachment sharing management API
    /// </summary>
    [ApiController]
    [Route("integration/attachment-sharing/v{version:apiVersion}")]
    [Display(Name = "attachment-sharing")]
    [Authorize]
    public class AttachmentSharingController : Controllers.ControllerBase
    {
        private readonly IAttachmentSharingService _attachmentSharingService;

        public AttachmentSharingController(IAttachmentSharingService attachmentSharingService)
        {
            _attachmentSharingService = attachmentSharingService;
        }

        /// <summary>
        /// Create attachment sharing configuration
        /// </summary>
        [HttpPost]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Create([FromBody] AttachmentSharingInputDto input)
        {
            var id = await _attachmentSharingService.CreateAsync(input);
            return Success(id);
        }

        /// <summary>
        /// Update attachment sharing configuration
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Update(long id, [FromBody] AttachmentSharingInputDto input)
        {
            var result = await _attachmentSharingService.UpdateAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Delete attachment sharing configuration
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _attachmentSharingService.DeleteAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Get attachment sharing by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType<SuccessResponse<AttachmentSharingOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetById(long id)
        {
            var data = await _attachmentSharingService.GetByIdAsync(id);
            if (data == null)
            {
                return NotFound("Attachment sharing configuration not found");
            }
            return Success(data);
        }

        /// <summary>
        /// Get attachment sharing by System ID
        /// </summary>
        [HttpGet("by-system-id/{systemId}")]
        [ProducesResponseType<SuccessResponse<AttachmentSharingOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetBySystemId(string systemId)
        {
            var data = await _attachmentSharingService.GetBySystemIdAsync(systemId);
            if (data == null)
            {
                return NotFound("Attachment sharing configuration not found");
            }
            return Success(data);
        }

        /// <summary>
        /// Get attachment sharing configurations by integration ID
        /// </summary>
        [HttpGet("by-integration/{integrationId}")]
        [ProducesResponseType<SuccessResponse<List<AttachmentSharingOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByIntegrationId(long integrationId)
        {
            var data = await _attachmentSharingService.GetByIntegrationIdAsync(integrationId);
            return Success(data);
        }

        /// <summary>
        /// Get active attachment sharing configurations by workflow ID
        /// </summary>
        [HttpGet("by-workflow/{workflowId}")]
        [ProducesResponseType<SuccessResponse<List<AttachmentSharingOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByWorkflowId(long workflowId)
        {
            var data = await _attachmentSharingService.GetByWorkflowIdAsync(workflowId);
            return Success(data);
        }
    }
}

