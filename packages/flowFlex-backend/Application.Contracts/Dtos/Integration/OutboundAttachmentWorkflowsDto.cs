using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// DTO for Outbound Attachment Workflows configuration
    /// Used to configure which workflow attachments can be shared with external system
    /// </summary>
    public class OutboundAttachmentWorkflowsInputDto
    {
        /// <summary>
        /// Workflow IDs whose attachments can be shared with the external system
        /// </summary>
        public List<long> WorkflowIds { get; set; } = new();
    }

    /// <summary>
    /// Output DTO for Outbound Attachment Workflows configuration
    /// </summary>
    public class OutboundAttachmentWorkflowsOutputDto
    {
        /// <summary>
        /// Integration ID
        /// </summary>
        public long IntegrationId { get; set; }

        /// <summary>
        /// Workflow IDs whose attachments can be shared with the external system
        /// </summary>
        public List<long> WorkflowIds { get; set; } = new();

        /// <summary>
        /// Workflow details (optional, for display purposes)
        /// </summary>
        public List<WorkflowSummaryDto>? Workflows { get; set; }
    }

    /// <summary>
    /// Summary DTO for workflow information
    /// </summary>
    public class WorkflowSummaryDto
    {
        /// <summary>
        /// Workflow ID
        /// </summary>
        public long WorkflowId { get; set; }

        /// <summary>
        /// Workflow name
        /// </summary>
        public string WorkflowName { get; set; } = string.Empty;
    }
}

