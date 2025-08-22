using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FlowFlex.Domain.Entities.Base;
using Newtonsoft.Json.Linq;
using SqlSugar;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// Questionnaire Answer Entity
    /// </summary>
    [Table("ff_questionnaire_answers")]
    [SugarTable("ff_questionnaire_answers")]
    public class QuestionnaireAnswer : EntityBaseCreateInfo
    {
        /// <summary>
        /// Onboarding ID
        /// </summary>

        [Column("onboarding_id")]
        [SugarColumn(ColumnName = "onboarding_id")]
        public long OnboardingId { get; set; }

        /// <summary>
        /// Stage ID
        /// </summary>

        [Column("stage_id")]
        [SugarColumn(ColumnName = "stage_id")]
        public long StageId { get; set; }

        /// <summary>
        /// Questionnaire ID
        /// </summary>
        [Column("questionnaire_id")]
        [SugarColumn(ColumnName = "questionnaire_id", IsNullable = true)]
        public long? QuestionnaireId { get; set; }

        /// <summary>
        /// Answer JSONB Data
        /// </summary>

        [Column("answer_json", TypeName = "jsonb")]
        [SugarColumn(ColumnName = "answer_json", ColumnDataType = "jsonb", IsJson = true)]
        public JToken Answer { get; set; }

        /// <summary>
        /// Submission Status (Draft, Submitted, Approved, etc.)
        /// </summary>
        [StringLength(20)]
        [Column("status")]
        [SugarColumn(ColumnName = "status")]
        public string Status { get; set; } = "Draft";

        /// <summary>
        /// Completion Rate (0-100)
        /// </summary>
        [Column("completion_rate")]
        [SugarColumn(ColumnName = "completion_rate")]
        public int CompletionRate { get; set; } = 0;

        /// <summary>
        /// Current Section Index
        /// </summary>
        [Column("current_section_index")]
        [SugarColumn(ColumnName = "current_section_index")]
        public int CurrentSectionIndex { get; set; } = 0;

        /// <summary>
        /// Submit Time
        /// </summary>
        [Column("submit_time")]
        [SugarColumn(ColumnName = "submit_time", IsNullable = true)]
        public DateTimeOffset? SubmitTime { get; set; }

        /// <summary>
        /// Review Time
        /// </summary>
        [Column("review_time")]
        [SugarColumn(ColumnName = "review_time", IsNullable = true)]
        public DateTimeOffset? ReviewTime { get; set; }

        /// <summary>
        /// Reviewer ID
        /// </summary>
        [Column("reviewer_id")]
        [SugarColumn(ColumnName = "reviewer_id", IsNullable = true)]
        public long? ReviewerId { get; set; }

        /// <summary>
        /// Review Notes
        /// </summary>
        [StringLength(1000)]
        [Column("review_notes")]
        [SugarColumn(ColumnName = "review_notes")]
        public string ReviewNotes { get; set; } = string.Empty;

        /// <summary>
        /// Version Number
        /// </summary>
        [Column("version")]
        [SugarColumn(ColumnName = "version")]
        public int Version { get; set; } = 1;

        /// <summary>
        /// Is Latest Version
        /// </summary>
        [Column("is_latest")]
        [SugarColumn(ColumnName = "is_latest")]
        public bool IsLatest { get; set; } = true;

        /// <summary>
        /// Data Source
        /// </summary>
        [StringLength(50)]
        [Column("source")]
        [SugarColumn(ColumnName = "source")]
        public string Source { get; set; } = "customer_portal";

        /// <summary>
        /// IP Address
        /// </summary>
        [StringLength(45)]
        [Column("ip_address")]
        [SugarColumn(ColumnName = "ip_address")]
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// User Agent
        /// </summary>
        [StringLength(500)]
        [Column("user_agent")]
        [SugarColumn(ColumnName = "user_agent")]
        public string UserAgent { get; set; } = string.Empty;

        // Navigation Properties
        [SugarColumn(IsIgnore = true)]
        [Navigate(NavigateType.OneToOne, nameof(OnboardingId))]
        public virtual Onboarding? Onboarding { get; set; }

        [SugarColumn(IsIgnore = true)]
        [Navigate(NavigateType.OneToOne, nameof(StageId))]
        public virtual Stage? Stage { get; set; }

        [SugarColumn(IsIgnore = true)]
        [Navigate(NavigateType.OneToOne, nameof(QuestionnaireId))]
        public virtual Questionnaire? Questionnaire { get; set; }
    }
}
