using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using FlowFlex.Domain.Entities.Base;
using SqlSugar;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// Simple assignment DTO for Questionnaire assignments
    /// </summary>
    public class QuestionnaireAssignmentDto
    {
        public long WorkflowId { get; set; }
        public long StageId { get; set; }
    }

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
        /// Questionnaire Status (Draft/Published/Archived)
        /// </summary>
        [StringLength(20)]
        public string Status { get; set; } = "Draft";

        /// <summary>
        /// Questionnaire Structure Definition (JSONB)
        /// </summary>
        [SugarColumn(ColumnName = "structure_json", ColumnDataType = "jsonb", IsJson = true)]
        public JToken Structure { get; set; }

        /// <summary>
        /// Questionnaire Version Number
        /// </summary>
        public int Version { get; set; } = 1;



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
        /// Questionnaire Tags (JSONB array)
        /// </summary>
        [SugarColumn(ColumnName = "tags_json", ColumnDataType = "jsonb", IsJson = true)]
        public JToken Tags { get; set; }

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
        /// Assignments JSONB storage (ORM-serialized as array)
        /// </summary>
        [SugarColumn(ColumnName = "assignments_json", ColumnDataType = "jsonb", IsJson = true)]
        public List<QuestionnaireAssignmentDto>? Assignments { get; set; } = new List<QuestionnaireAssignmentDto>();
    }
}
