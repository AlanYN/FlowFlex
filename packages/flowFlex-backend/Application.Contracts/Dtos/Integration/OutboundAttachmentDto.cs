using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// Outbound attachment configuration item
    /// </summary>
    public class OutboundAttachmentItemDto
    {
        /// <summary>
        /// Unique ID for this outbound attachment item (auto-generated snowflake ID)
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Workflow ID
        /// </summary>
        [Required]
        public long WorkflowId { get; set; }

        /// <summary>
        /// Stage IDs (array of stage IDs)
        /// </summary>
        public List<long> StageIds { get; set; } = new();
    }

    /// <summary>
    /// Input DTO for outbound attachments configuration
    /// </summary>
    public class OutboundAttachmentsInputDto
    {
        /// <summary>
        /// List of outbound attachment configurations
        /// </summary>
        public List<OutboundAttachmentItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Output DTO for outbound attachments configuration
    /// </summary>
    public class OutboundAttachmentsOutputDto
    {
        /// <summary>
        /// Integration ID
        /// </summary>
        public long IntegrationId { get; set; }

        /// <summary>
        /// List of outbound attachment configurations
        /// </summary>
        public List<OutboundAttachmentItemDto> Items { get; set; } = new();
    }
}

