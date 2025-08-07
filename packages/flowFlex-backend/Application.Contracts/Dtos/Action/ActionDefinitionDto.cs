using System.ComponentModel.DataAnnotations;
using FlowFlex.Domain.Shared.Enums.Action;
using Item.Common.Lib.JsonConverts;
using Newtonsoft.Json;

namespace FlowFlex.Application.Contracts.Dtos.Action
{
    /// <summary>
    /// Action definition DTO for CRUD operations
    /// </summary>
    public class ActionDefinitionDto
    {
        /// <summary>
        /// Action definition ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Action code
        /// </summary>
        public string ActionCode { get; set; }

        /// <summary>
        /// Action name
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Action description
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// Action type
        /// </summary>
        [Required]
        public ActionTypeEnum ActionType { get; set; }

        /// <summary>
        /// Action configuration (JSON format)
        /// </summary>
        [Required]
        public string ActionConfig { get; set; }

        /// <summary>
        /// Whether the action is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Creation time
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last update time
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Trigger mappings with related entity details
        /// </summary>
        public List<ActionTriggerMappingInfo> TriggerMappings { get; set; } = new();
    }

    /// <summary>
    /// Action trigger mapping information for DTO
    /// </summary>
    public class ActionTriggerMappingInfo
    {
        /// <summary>
        /// Mapping ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Trigger type (Stage, Task, Question)
        /// </summary>
        public string TriggerType { get; set; } = string.Empty;

        /// <summary>
        /// Trigger source ID
        /// </summary>
        public long TriggerSourceId { get; set; }

        /// <summary>
        /// Trigger source name
        /// </summary>
        public string TriggerSourceName { get; set; } = string.Empty;

        /// <summary>
        /// Workflow ID
        /// </summary>
        public long? WorkFlowId { get; set; }

        /// <summary>
        /// Workflow name
        /// </summary>
        public string WorkFlowName { get; set; } = string.Empty;

        /// <summary>
        /// Stage ID
        /// </summary>
        public long? StageId { get; set; }

        /// <summary>
        /// Stage name
        /// </summary>
        public string StageName { get; set; } = string.Empty;

        /// <summary>
        /// Trigger event
        /// </summary>
        public string TriggerEvent { get; set; } = string.Empty;

        /// <summary>
        /// Whether the mapping is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Execution order
        /// </summary>
        public int ExecutionOrder { get; set; } = 0;

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        [JsonConverter(typeof(DateTimeJsonConverter))]
        public DateTimeOffset? LastApplied { get; set; }
    }
}