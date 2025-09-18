using System.ComponentModel.DataAnnotations;
using SqlSugar;
using FlowFlex.Domain.Entities.Base;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// OW Static Field Value Entity
    /// </summary>
    [SugarTable("ff_static_field_values")]
    public class StaticFieldValue : EntityBaseCreateInfo
    {
        /// <summary>
        /// Primary Key ID
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public override long Id { get; set; }

        /// <summary>
        /// Onboarding ID
        /// </summary>

        [SugarColumn(ColumnName = "onboarding_id")]
        public long OnboardingId { get; set; }

        /// <summary>
        /// Stage ID
        /// </summary>

        [SugarColumn(ColumnName = "stage_id")]
        public long StageId { get; set; }

        /// <summary>
        /// Static Field Name
        /// </summary>

        [StringLength(100)]
        [SugarColumn(ColumnName = "field_name")]
        public string FieldName { get; set; }

        /// <summary>
        /// Field Display Name
        /// </summary>
        [StringLength(200)]
        [SugarColumn(ColumnName = "display_name")]
        public string DisplayName { get; set; }

        /// <summary>
        /// Field Value JSON Data
        /// </summary>
        [SugarColumn(ColumnName = "field_value_json", ColumnDataType = "jsonb")]
        public string FieldValueJson { get; set; } = string.Empty;

        /// <summary>
        /// Field Type
        /// </summary>
        [StringLength(50)]
        [SugarColumn(ColumnName = "field_type")]
        public string FieldType { get; set; } = "text";

        /// <summary>
        /// Is Required
        /// </summary>
        [SugarColumn(ColumnName = "is_required")]
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// Submission Status
        /// </summary>
        [StringLength(20)]
        [SugarColumn(ColumnName = "status")]
        public string Status { get; set; } = "Draft";

        /// <summary>
        /// Completion Rate
        /// </summary>
        [Range(0, 100)]
        [SugarColumn(ColumnName = "completion_rate")]
        public int CompletionRate { get; set; } = 0;

        /// <summary>
        /// Submit Time
        /// </summary>
        [SugarColumn(ColumnName = "submit_time")]
        public DateTimeOffset? SubmitTime { get; set; }

        /// <summary>
        /// Review Time
        /// </summary>
        [SugarColumn(ColumnName = "review_time")]
        public DateTimeOffset? ReviewTime { get; set; }

        /// <summary>
        /// Reviewer ID
        /// </summary>
        [SugarColumn(ColumnName = "reviewer_id")]
        public long? ReviewerId { get; set; }

        /// <summary>
        /// Review Notes
        /// </summary>
        [StringLength(1000)]
        [SugarColumn(ColumnName = "review_notes")]
        public string ReviewNotes { get; set; }

        /// <summary>
        /// Version Number
        /// </summary>
        [SugarColumn(ColumnName = "version")]
        public int Version { get; set; } = 1;

        /// <summary>
        /// Is Latest Version
        /// </summary>
        [SugarColumn(ColumnName = "is_latest")]
        public bool IsLatest { get; set; } = true;

        /// <summary>
        /// Is Submitted
        /// </summary>
        [SugarColumn(ColumnName = "is_submitted")]
        public bool IsSubmitted { get; set; } = false;

        /// <summary>
        /// Data Source
        /// </summary>
        [StringLength(50)]
        [SugarColumn(ColumnName = "source")]
        public string Source { get; set; } = "customer_portal";

        /// <summary>
        /// IP Address
        /// </summary>
        [StringLength(45)]
        [SugarColumn(ColumnName = "ip_address")]
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// User Agent
        /// </summary>
        [StringLength(500)]
        [SugarColumn(ColumnName = "user_agent")]
        public string UserAgent { get; set; } = string.Empty;

        /// <summary>
        /// Validation Result
        /// </summary>
        [StringLength(20)]
        [SugarColumn(ColumnName = "validation_status")]
        public string ValidationStatus { get; set; } = "Pending";

        /// <summary>
        /// Validation Error Information
        /// </summary>
        [StringLength(1000)]
        [SugarColumn(ColumnName = "validation_errors")]
        public string ValidationErrors { get; set; } = string.Empty;

        /// <summary>
        /// Additional Metadata (JSON format)
        /// </summary>
        [SugarColumn(ColumnName = "metadata", ColumnDataType = "jsonb")]
        public string Metadata { get; set; }
    }
}
