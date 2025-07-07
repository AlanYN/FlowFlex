using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FlowFlex.Domain.Entities.Base;
using SqlSugar;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// Stage Completion Log Entity
    /// </summary>
    [Table("ff_stage_completion_logs")]
    [SugarTable("ff_stage_completion_log")]
    public class StageCompletionLog : EntityBaseCreateInfo
    {
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
        /// Stage Name
        /// </summary>
        [StringLength(200)]
        [SugarColumn(ColumnName = "stage_name")]
        public string StageName { get; set; } = string.Empty;

        /// <summary>
        /// Log Type (start, complete, error, questionnaire_save_start, etc.)
        /// </summary>
       
        [StringLength(50)]
        [SugarColumn(ColumnName = "log_type")]
        public string LogType { get; set; } = string.Empty;

        /// <summary>
        /// Operation Action
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "action")]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Log Data (JSON format)
        /// </summary>
        [SugarColumn(ColumnName = "log_data", ColumnDataType = "jsonb", IsJson = true)]
        public string LogData { get; set; } = string.Empty;

        /// <summary>
        /// Is Success
        /// </summary>
        [SugarColumn(ColumnName = "success")]
        public bool Success { get; set; } = true;

        /// <summary>
        /// Error Message
        /// </summary>
        [StringLength(2000)]
        [SugarColumn(ColumnName = "error_message")]
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Network Status (online/offline)
        /// </summary>
        [StringLength(20)]
        [SugarColumn(ColumnName = "network_status")]
        public string NetworkStatus { get; set; } = string.Empty;

        /// <summary>
        /// Response Time (milliseconds)
        /// </summary>
        [SugarColumn(ColumnName = "response_time")]
        public int? ResponseTime { get; set; }

        /// <summary>
        /// User Agent
        /// </summary>
        [StringLength(500)]
        [SugarColumn(ColumnName = "user_agent")]
        public string UserAgent { get; set; } = string.Empty;

        /// <summary>
        /// Request URL
        /// </summary>
        [StringLength(500)]
        [SugarColumn(ColumnName = "request_url")]
        public string RequestUrl { get; set; } = string.Empty;

        /// <summary>
        /// Data Source (customer_portal, admin_panel, api, etc.)
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
        /// Session ID
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "session_id")]
        public string SessionId { get; set; } = string.Empty;

        // Navigation Properties
        [SugarColumn(IsIgnore = true)]
        [Navigate(NavigateType.OneToOne, nameof(OnboardingId))]
        public virtual Onboarding? Onboarding { get; set; }

        [SugarColumn(IsIgnore = true)]
        [Navigate(NavigateType.OneToOne, nameof(StageId))]
        public virtual Stage? Stage { get; set; }
    }
}
