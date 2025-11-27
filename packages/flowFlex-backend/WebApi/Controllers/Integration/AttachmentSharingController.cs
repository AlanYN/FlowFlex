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
    /// Attachment sharing management API (legacy compatible routes)
    /// Uses ff_integration.inbound_attachments column
    /// </summary>
    [ApiController]
    [Route("integration/attachment-sharing/v{version:apiVersion}")]
    [Display(Name = "attachment-sharing")]
    [Authorize]
    public class AttachmentSharingController : Controllers.ControllerBase
    {
        private readonly IIntegrationService _integrationService;

        public AttachmentSharingController(IIntegrationService integrationService)
        {
            _integrationService = integrationService;
        }

        /// <summary>
        /// Get attachment sharing configuration by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType<SuccessResponse<InboundAttachmentItemDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetById(string id, [FromQuery] long integrationId)
        {
            var data = await _integrationService.GetInboundAttachmentsAsync(integrationId);
            var item = data.Items.FirstOrDefault(x => x.Id == id);
            if (item == null)
            {
                return NotFound($"Attachment sharing with ID '{id}' not found");
            }
            return Success(item);
        }

        /// <summary>
        /// Get attachment sharing configurations by integration ID
        /// Returns inbound attachments from ff_integration.inbound_attachments
        /// </summary>
        [HttpGet("by-integration/{integrationId}")]
        [ProducesResponseType<SuccessResponse<List<InboundAttachmentItemDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByIntegrationId(long integrationId)
        {
            var data = await _integrationService.GetInboundAttachmentsAsync(integrationId);
            return Success(data.Items);
        }

        /// <summary>
        /// Create attachment sharing configuration item
        /// Saves to ff_integration.inbound_attachments
        /// </summary>
        [HttpPost]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Create([FromBody] AttachmentSharingCreateDto input)
        {
            // Get existing items
            var existing = await _integrationService.GetInboundAttachmentsAsync(input.IntegrationId);
            var items = existing.Items ?? new List<InboundAttachmentItemDto>();

            // Generate a unique ID for the new item
            var newItem = new InboundAttachmentItemDto
            {
                Id = GenerateItemId(),
                ModuleName = input.ModuleName,
                WorkflowId = input.WorkflowId,
                StageId = input.StageId
            };

            items.Add(newItem);

            // Save back
            var saveInput = new InboundAttachmentsInputDto
            {
                IntegrationId = input.IntegrationId,
                Items = items
            };
            await _integrationService.SaveInboundAttachmentsAsync(input.IntegrationId, saveInput);

            // Return the generated ID
            return Success(newItem.Id);
        }

        /// <summary>
        /// Update attachment sharing configuration item by ID
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Update(string id, [FromBody] AttachmentSharingUpdateDto input)
        {
            // Get existing items
            var existing = await _integrationService.GetInboundAttachmentsAsync(input.IntegrationId);
            var items = existing.Items ?? new List<InboundAttachmentItemDto>();

            // Find the item to update
            var itemIndex = items.FindIndex(x => x.Id == id);
            if (itemIndex < 0)
            {
                return NotFound($"Attachment sharing with ID '{id}' not found");
            }

            // Update the item
            items[itemIndex].ModuleName = input.ModuleName;
            items[itemIndex].WorkflowId = input.WorkflowId;
            items[itemIndex].StageId = input.StageId;

            // Save back
            var saveInput = new InboundAttachmentsInputDto
            {
                IntegrationId = input.IntegrationId,
                Items = items
            };
            var result = await _integrationService.SaveInboundAttachmentsAsync(input.IntegrationId, saveInput);
            return Success(result);
        }

        /// <summary>
        /// Delete attachment sharing configuration item by ID
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Delete(string id, [FromQuery] long integrationId)
        {
            // Get existing items
            var existing = await _integrationService.GetInboundAttachmentsAsync(integrationId);
            var items = existing.Items ?? new List<InboundAttachmentItemDto>();

            // Find and remove the item
            var itemIndex = items.FindIndex(x => x.Id == id);
            if (itemIndex < 0)
            {
                return NotFound($"Attachment sharing with ID '{id}' not found");
            }

            items.RemoveAt(itemIndex);

            // Save back
            var saveInput = new InboundAttachmentsInputDto
            {
                IntegrationId = integrationId,
                Items = items
            };
            var result = await _integrationService.SaveInboundAttachmentsAsync(integrationId, saveInput);
            return Success(result);
        }

        /// <summary>
        /// Generate a unique ID for attachment sharing item (using Snowflake ID)
        /// </summary>
        private string GenerateItemId()
        {
            return SqlSugar.SnowFlakeSingle.Instance.NextId().ToString();
        }
    }

    /// <summary>
    /// DTO for creating attachment sharing configuration
    /// </summary>
    public class AttachmentSharingCreateDto
    {
        /// <summary>
        /// Integration ID
        /// </summary>
        [Required]
        public long IntegrationId { get; set; }

        /// <summary>
        /// Module name
        /// </summary>
        [Required]
        [StringLength(200)]
        public string ModuleName { get; set; } = string.Empty;

        /// <summary>
        /// Workflow ID
        /// </summary>
        public long WorkflowId { get; set; }

        /// <summary>
        /// Stage ID
        /// </summary>
        public long StageId { get; set; }
    }

    /// <summary>
    /// DTO for updating attachment sharing configuration
    /// </summary>
    public class AttachmentSharingUpdateDto
    {
        /// <summary>
        /// Integration ID
        /// </summary>
        [Required]
        public long IntegrationId { get; set; }

        /// <summary>
        /// Module name
        /// </summary>
        [Required]
        [StringLength(200)]
        public string ModuleName { get; set; } = string.Empty;

        /// <summary>
        /// Workflow ID
        /// </summary>
        public long WorkflowId { get; set; }

        /// <summary>
        /// Stage ID
        /// </summary>
        public long StageId { get; set; }
    }
}
