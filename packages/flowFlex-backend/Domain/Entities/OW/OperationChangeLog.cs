using System.ComponentModel.DataAnnotations;
using FlowFlex.Domain.Entities.Base;
using SqlSugar;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// Operation Change Log Entity - Records detailed operation change information
    /// </summary>
    [SugarTable("ff_operation_change_log")]
    public class OperationChangeLog : OwEntityBase
    {
        /// <summary>
        /// Operation Type
        /// </summary>

        [StringLength(50)]
        [SugarColumn(ColumnName = "operation_type")]
        public string OperationType { get; set; }

        /// <summary>
        /// Business Module
        /// </summary>

        [StringLength(50)]
        [SugarColumn(ColumnName = "business_module")]
        public string BusinessModule { get; set; }

        /// <summary>
        /// Business ID
        /// </summary>

        [SugarColumn(ColumnName = "business_id")]
        public long BusinessId { get; set; }

        /// <summary>
        /// Onboarding ID
        /// </summary>
        [SugarColumn(ColumnName = "onboarding_id")]
        public long? OnboardingId { get; set; }

        /// <summary>
        /// Stage ID
        /// </summary>
        [SugarColumn(ColumnName = "stage_id")]
        public long? StageId { get; set; }

        /// <summary>
        /// Operation Status
        /// </summary>
        [StringLength(20)]
        [SugarColumn(ColumnName = "operation_status")]
        public string OperationStatus { get; set; }

        /// <summary>
        /// Operation Description
        /// </summary>
        [StringLength(500)]
        [SugarColumn(ColumnName = "operation_description")]
        public string OperationDescription { get; set; }

        /// <summary>
        /// Operation Title
        /// </summary>
        [StringLength(200)]
        [SugarColumn(ColumnName = "operation_title")]
        public string OperationTitle { get; set; }

        /// <summary>
        /// Operation Source
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "operation_source")]
        public string OperationSource { get; set; }

        /// <summary>
        /// Before Data (JSON)
        /// </summary>
        [SugarColumn(ColumnName = "before_data", ColumnDataType = "jsonb")]
        public string BeforeData { get; set; }

        /// <summary>
        /// After Data (JSON)
        /// </summary>
        [SugarColumn(ColumnName = "after_data", ColumnDataType = "jsonb")]
        public string AfterData { get; set; }

        /// <summary>
        /// Changed Fields (JSON)
        /// </summary>
        [SugarColumn(ColumnName = "changed_fields", ColumnDataType = "jsonb")]
        public string ChangedFields { get; set; }

        /// <summary>
        /// Operator ID
        /// </summary>

        [SugarColumn(ColumnName = "operator_id")]
        public long OperatorId { get; set; }

        /// <summary>
        /// Operator Name
        /// </summary>

        [StringLength(100)]
        [SugarColumn(ColumnName = "operator_name")]
        public string OperatorName { get; set; }

        /// <summary>
        /// Operation Time
        /// </summary>

        [SugarColumn(ColumnName = "operation_time")]
        public DateTimeOffset OperationTime { get; set; }

        /// <summary>
        /// IP Address
        /// </summary>
        [StringLength(50)]
        [SugarColumn(ColumnName = "ip_address")]
        public string IpAddress { get; set; }

        /// <summary>
        /// User Agent
        /// </summary>
        [StringLength(500)]
        [SugarColumn(ColumnName = "user_agent")]
        public string UserAgent { get; set; }

        /// <summary>
        /// Extended Data (JSON)
        /// </summary>
        [SugarColumn(ColumnName = "extended_data", ColumnDataType = "jsonb")]
        public string ExtendedData { get; set; }

        /// <summary>
        /// Error Message
        /// </summary>
        [StringLength(1000)]
        [SugarColumn(ColumnName = "error_message")]
        public string ErrorMessage { get; set; }
    }
}
