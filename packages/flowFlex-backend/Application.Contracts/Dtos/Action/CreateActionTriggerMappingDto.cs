using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.Action
{
    /// <summary>
    /// DTO for creating new action trigger mapping
    /// </summary>
    public class CreateActionTriggerMappingDto
    {
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
        /// Work flow ID
        /// </summary>
        [Required]
        public long WorkFlowId { get; set; }

        /// <summary>
        /// Stage ID
        /// </summary>
        [Required]
        public long StageId { get; set; }

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
    }
}