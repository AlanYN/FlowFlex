using System.ComponentModel.DataAnnotations;
using FlowFlex.Domain.Entities;
using SqlSugar;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// Stage Condition Entity - Defines condition rules and actions for a stage
    /// </summary>
    [SugarTable("ff_stage_condition")]
    public class StageCondition : OwEntityBase
    {
        /// <summary>
        /// Associated Stage ID
        /// </summary>
        [SugarColumn(ColumnName = "stage_id")]
        public long StageId { get; set; }

        /// <summary>
        /// Associated Workflow ID (denormalized for query performance)
        /// </summary>
        [SugarColumn(ColumnName = "workflow_id")]
        public long WorkflowId { get; set; }

        /// <summary>
        /// Condition Name
        /// </summary>
        [Required]
        [StringLength(100)]
        [SugarColumn(ColumnName = "name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Condition Description
        /// </summary>
        [StringLength(500)]
        [SugarColumn(ColumnName = "description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// RulesEngine Workflow JSON (Microsoft RulesEngine format)
        /// </summary>
        [SugarColumn(ColumnName = "rules_json", ColumnDataType = "jsonb", IsJson = true)]
        public string RulesJson { get; set; } = string.Empty;

        /// <summary>
        /// Actions JSON (array of action configurations)
        /// </summary>
        [SugarColumn(ColumnName = "actions_json", ColumnDataType = "jsonb", IsJson = true)]
        public string ActionsJson { get; set; } = string.Empty;

        /// <summary>
        /// Fallback Stage ID (when condition is not met)
        /// </summary>
        [SugarColumn(ColumnName = "fallback_stage_id")]
        public long? FallbackStageId { get; set; }

        /// <summary>
        /// Is Active
        /// </summary>
        [SugarColumn(ColumnName = "is_active")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Condition Status (Valid, Invalid, Draft)
        /// </summary>
        [StringLength(20)]
        [SugarColumn(ColumnName = "status")]
        public string Status { get; set; } = "Valid";
    }
}
