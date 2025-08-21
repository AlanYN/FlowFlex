using System.ComponentModel.DataAnnotations;
using FlowFlex.Domain.Entities.Base;
using SqlSugar;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// Questionnaire-Stage mapping table for fast assignment queries
    /// </summary>
    [SugarTable("ff_questionnaire_stage_mapping")]
    public class QuestionnaireStageMapping : EntityBaseCreateInfo
    {
        /// <summary>
        /// Questionnaire ID
        /// </summary>
        [SugarColumn(ColumnName = "questionnaire_id")]
        public long QuestionnaireId { get; set; }

        /// <summary>
        /// Stage ID
        /// </summary>
        [SugarColumn(ColumnName = "stage_id")]
        public long StageId { get; set; }

        /// <summary>
        /// Workflow ID (for faster queries)
        /// </summary>
        [SugarColumn(ColumnName = "workflow_id")]
        public long WorkflowId { get; set; }

        /// <summary>
        /// Created timestamp (use created_at for mapping table)
        /// </summary>
        [SugarColumn(ColumnName = "created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Updated timestamp (use updated_at for mapping table)
        /// </summary>
        [SugarColumn(ColumnName = "updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}