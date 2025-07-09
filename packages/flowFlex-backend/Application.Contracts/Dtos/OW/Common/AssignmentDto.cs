using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FlowFlex.Application.Contracts.Dtos.OW.Common
{
    /// <summary>
    /// Assignment DTO for workflow and stage combination
    /// </summary>
    public class AssignmentDto
    {
        /// <summary>
        /// Workflow ID
        /// </summary>
        [JsonPropertyName("workflowId")]
        public long WorkflowId { get; set; }

        /// <summary>
        /// Stage ID
        /// </summary>
        [JsonPropertyName("stageId")]
        public long StageId { get; set; }
    }
} 