using SqlSugar;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Entities.Base;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// Stage Version Snapshot Entity
    /// </summary>
    [SugarTable("ff_stage_version")]
    public class StageVersion : OwEntityBase
    {
        /// <summary>
        /// Workflow Version ID
        /// </summary>
        [SugarColumn(ColumnName = "workflow_version_id")]
        public long WorkflowVersionId { get; set; }

        /// <summary>
        /// Original Stage ID
        /// </summary>
        [SugarColumn(ColumnName = "original_stage_id")]
        public long OriginalStageId { get; set; }

        /// <summary>
        /// Stage Name
        /// </summary>
        [SugarColumn(ColumnName = "name", Length = 100)]
        public string Name { get; set; }

        /// <summary>
        /// Stage Description
        /// </summary>
        [SugarColumn(ColumnName = "description", Length = 500)]
        public string Description { get; set; }

        /// <summary>
        /// Default Assigned Group
        /// </summary>
        [SugarColumn(ColumnName = "default_assigned_group", Length = 100)]
        public string DefaultAssignedGroup { get; set; }

        /// <summary>
        /// Default Assignee
        /// </summary>
        [SugarColumn(ColumnName = "default_assignee", Length = 100)]
        public string DefaultAssignee { get; set; }

        /// <summary>
        /// Estimated Duration (days, supports decimal)
        /// </summary>
        [SugarColumn(ColumnName = "estimated_duration")]
        public decimal? EstimatedDuration { get; set; }

        /// <summary>
        /// Sort Order
        /// </summary>
        [SugarColumn(ColumnName = "order_index")]
        public int OrderIndex { get; set; }

        /// <summary>
        /// Checklist ID
        /// </summary>
        [SugarColumn(ColumnName = "checklist_id")]
        public long? ChecklistId { get; set; }

        /// <summary>
        /// Questionnaire ID
        /// </summary>
        [SugarColumn(ColumnName = "questionnaire_id")]
        public long? QuestionnaireId { get; set; }

        /// <summary>
        /// Stage Color
        /// </summary>
        [SugarColumn(ColumnName = "color", Length = 20)]
        public string Color { get; set; }



        /// <summary>
        /// Workflow Version
        /// </summary>
        [SugarColumn(ColumnName = "workflow_version", Length = 20)]
        public string WorkflowVersion { get; set; }

        /// <summary>
        /// Is Active
        /// </summary>
        [SugarColumn(ColumnName = "is_active")]
        public bool IsActive { get; set; } = true;
    }
}
