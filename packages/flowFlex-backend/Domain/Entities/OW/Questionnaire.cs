using System.ComponentModel.DataAnnotations;
using FlowFlex.Domain.Entities.Base;
using SqlSugar;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// Onboard Questionnaire Entity - Dynamic questionnaire (supports multiple question types, table type, nested)
    /// </summary>
    [SugarTable("ff_questionnaire")]
    public class Questionnaire : EntityBaseCreateInfo
    {
        /// <summary>
        /// Questionnaire Name
        /// </summary>

        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Questionnaire Description
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// Questionnaire Type (Template/Instance)
        /// </summary>
        [StringLength(20)]
        public string Type { get; set; } = "Template";

        /// <summary>
        /// Questionnaire Status (Draft/Published/Archived)
        /// </summary>
        [StringLength(20)]
        public string Status { get; set; } = "Draft";

        /// <summary>
        /// Questionnaire Structure Definition (JSON, see structure description below)
        /// </summary>
        [SugarColumn(ColumnName = "structure_json")]
        public string StructureJson { get; set; }

        /// <summary>
        /// Questionnaire Version Number
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// Is Template
        /// </summary>
        public bool IsTemplate { get; set; } = true;

        /// <summary>
        /// Template Source ID (if created from template instance)
        /// </summary>
        [SugarColumn(ColumnName = "template_id")]
        public long? TemplateId { get; set; }

        /// <summary>
        /// Preview Image URL
        /// </summary>
        [StringLength(500)]
        [SugarColumn(ColumnName = "preview_image_url")]
        public string PreviewImageUrl { get; set; }

        /// <summary>
        /// Questionnaire Category
        /// </summary>
        [StringLength(50)]
        public string Category { get; set; }

        /// <summary>
        /// Questionnaire Tags (JSON array)
        /// </summary>
        [SugarColumn(ColumnName = "tags_json")]
        public string TagsJson { get; set; }

        /// <summary>
        /// Estimated Fill Time (minutes)
        /// </summary>
        public int EstimatedMinutes { get; set; } = 0;

        /// <summary>
        /// Total Questions
        /// </summary>
        public int TotalQuestions { get; set; } = 0;

        /// <summary>
        /// Required Questions Count
        /// </summary>
        public int RequiredQuestions { get; set; } = 0;

        /// <summary>
        /// Allow Save Draft
        /// </summary>
        public bool AllowDraft { get; set; } = true;

        /// <summary>
        /// Allow Multiple Submissions
        /// </summary>
        public bool AllowMultipleSubmissions { get; set; } = false;

        /// <summary>
        /// Is Active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Associated Workflow ID
        /// </summary>
        [SugarColumn(ColumnName = "workflow_id")]
        public long? WorkflowId { get; set; }

        /// <summary>
        /// Associated Stage ID
        /// </summary>
        [SugarColumn(ColumnName = "stage_id")]
        public long? StageId { get; set; }
    }
}
