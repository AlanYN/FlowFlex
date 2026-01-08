using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.StageCondition
{
    /// <summary>
    /// Stage Condition Input DTO - For creating and updating conditions
    /// </summary>
    public class StageConditionInputDto
    {
        /// <summary>
        /// Associated Stage ID
        /// </summary>
        [Required]
        public long StageId { get; set; }

        /// <summary>
        /// Associated Workflow ID
        /// </summary>
        public long WorkflowId { get; set; }

        /// <summary>
        /// Condition Name
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Condition Description
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// RulesEngine Workflow JSON (Microsoft RulesEngine format)
        /// </summary>
        [Required]
        public string RulesJson { get; set; } = string.Empty;

        /// <summary>
        /// Actions JSON array
        /// </summary>
        [Required]
        public string ActionsJson { get; set; } = string.Empty;

        /// <summary>
        /// Fallback Stage ID (when condition is not met)
        /// </summary>
        public long? FallbackStageId { get; set; }

        /// <summary>
        /// Is Active
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
