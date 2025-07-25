using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.Action
{
    /// <summary>
    /// Action trigger mapping DTO for CRUD operations
    /// </summary>
    public class ActionTriggerMappingDto
    {
        /// <summary>
        /// Mapping ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Action definition ID
        /// </summary>
        [Required]
        public long ActionDefinitionId { get; set; }

        /// <summary>
        /// Trigger type (Stage, Task, Question, etc.)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string TriggerType { get; set; }

        /// <summary>
        /// Trigger source ID
        /// </summary>
        [Required]
        public long TriggerSourceId { get; set; }

        /// <summary>
        /// Trigger source name
        /// </summary>
        [StringLength(100)]
        public string TriggerSourceName { get; set; }

        /// <summary>
        /// Trigger event (Completed, Created, Updated, etc.)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string TriggerEvent { get; set; }

        /// <summary>
        /// Trigger conditions (JSON format)
        /// </summary>
        public string TriggerConditions { get; set; }

        /// <summary>
        /// Execution order
        /// </summary>
        public int ExecutionOrder { get; set; } = 1;

        /// <summary>
        /// Whether the mapping is enabled
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
    }
}