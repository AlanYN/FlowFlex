using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;

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
        /// Stage ID (can be null or 0 for empty stage)
        /// </summary>
        [JsonPropertyName("stageId")]
        [JsonConverter(typeof(NullableLongConverter))]
        public long? StageId { get; set; }
    }
} 