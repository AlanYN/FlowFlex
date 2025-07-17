using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace FlowFlex.Application.Contracts.Dtos.Action
{
    /// <summary>
    /// Action trigger mapping create/update input DTO
    /// </summary>
    public class ActionTriggerMappingInputDto
    {
        /// <summary>
        /// Action definition ID
        /// </summary>
        [Required(ErrorMessage = "Action definition ID is required")]
        [DisplayName("Action Definition ID")]
        public long ActionDefinitionId { get; set; }

        /// <summary>
        /// Trigger type (Stage, Task, Question, Workflow, Checklist)
        /// </summary>
        [Required(ErrorMessage = "Trigger type is required")]
        [StringLength(50, ErrorMessage = "Trigger type length cannot exceed 50 characters")]
        [DisplayName("Trigger Type")]
        public string TriggerType { get; set; } = string.Empty;

        /// <summary>
        /// Trigger source ID
        /// </summary>
        [Required(ErrorMessage = "Trigger source ID is required")]
        [DisplayName("Trigger Source ID")]
        public long TriggerSourceId { get; set; }

        /// <summary>
        /// Trigger source name
        /// </summary>
        [StringLength(200, ErrorMessage = "Trigger source name length cannot exceed 200 characters")]
        [DisplayName("Trigger Source Name")]
        public string TriggerSourceName { get; set; } = string.Empty;

        /// <summary>
        /// Trigger event (Started, Completed, Failed, etc.)
        /// </summary>
        [StringLength(50, ErrorMessage = "Trigger event length cannot exceed 50 characters")]
        [DisplayName("Trigger Event")]
        public string TriggerEvent { get; set; } = "Completed";

        /// <summary>
        /// Trigger conditions (JSON format)
        /// </summary>
        [DisplayName("Trigger Conditions")]
        public Dictionary<string, object>? TriggerConditions { get; set; }

        /// <summary>
        /// Is enabled
        /// </summary>
        [DisplayName("Is Enabled")]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Execution order
        /// </summary>
        [DisplayName("Execution Order")]
        public int ExecutionOrder { get; set; } = 0;

        /// <summary>
        /// Mapping configuration (JSON format)
        /// </summary>
        [DisplayName("Mapping Configuration")]
        public Dictionary<string, object>? MappingConfig { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        [StringLength(500, ErrorMessage = "Description length cannot exceed 500 characters")]
        [DisplayName("Description")]
        public string Description { get; set; } = string.Empty;
    }
} 