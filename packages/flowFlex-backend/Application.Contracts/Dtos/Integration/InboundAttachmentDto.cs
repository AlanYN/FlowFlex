using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// Inbound attachment configuration item
    /// </summary>
    public class InboundAttachmentItemDto
    {
        /// <summary>
        /// Unique ID for this attachment sharing item (auto-generated)
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// External module name
        /// </summary>
        [Required]
        [StringLength(200)]
        public string ModuleName { get; set; } = string.Empty;

        /// <summary>
        /// Workflow ID
        /// </summary>
        [Required]
        public long WorkflowId { get; set; }

        /// <summary>
        /// Stage ID
        /// </summary>
        [Required]
        public long StageId { get; set; }
    }

    /// <summary>
    /// Input DTO for inbound attachments configuration
    /// </summary>
    public class InboundAttachmentsInputDto
    {
        /// <summary>
        /// Integration ID (required for POST, optional for PUT with path parameter)
        /// </summary>
        public long IntegrationId { get; set; }

        /// <summary>
        /// List of inbound attachment configurations
        /// </summary>
        public List<InboundAttachmentItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Output DTO for inbound attachments configuration
    /// </summary>
    public class InboundAttachmentsOutputDto
    {
        /// <summary>
        /// Integration ID
        /// </summary>
        public long IntegrationId { get; set; }

        /// <summary>
        /// List of inbound attachment configurations
        /// </summary>
        public List<InboundAttachmentItemDto> Items { get; set; } = new();
    }
}

