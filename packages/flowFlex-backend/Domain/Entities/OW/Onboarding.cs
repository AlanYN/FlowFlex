using System.ComponentModel.DataAnnotations;
using FlowFlex.Domain.Entities.Base;
using SqlSugar;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// Onboarding Entity - Each customer/lead's onboarding workflow instance
    /// </summary>
    [SugarTable("ff_onboarding")]
    public class Onboarding : EntityBaseCreateInfo
    {
        /// <summary>
        /// Associated Workflow Primary Key ID
        /// </summary>
        [Required]
        [SugarColumn(ColumnName = "workflow_id")]
        public long WorkflowId { get; set; }

        /// <summary>
        /// Current Stage Primary Key ID
        /// </summary>
        [SugarColumn(ColumnName = "current_stage_id")]
        public long? CurrentStageId { get; set; }

        /// <summary>
        /// Current Progress (Stage Order)
        /// </summary>
        [SugarColumn(ColumnName = "current_stage_order")]
        public int CurrentStageOrder { get; set; } = 1;

        /// <summary>
        /// Customer/Lead ID
        /// </summary>
        [Required]
        [StringLength(100)]
        [SugarColumn(ColumnName = "lead_id")]
        public string LeadId { get; set; }

        /// <summary>
        /// Customer/Lead Name
        /// </summary>
        [StringLength(200)]
        [SugarColumn(ColumnName = "lead_name")]
        public string LeadName { get; set; }

        /// <summary>
        /// Customer/Lead Email
        /// </summary>
        [StringLength(200)]
        [SugarColumn(ColumnName = "lead_email")]
        public string LeadEmail { get; set; }

        /// <summary>
        /// Customer/Lead Phone
        /// </summary>
        [StringLength(50)]
        [SugarColumn(ColumnName = "lead_phone")]
        public string LeadPhone { get; set; }

        /// <summary>
        /// Contact Person Name
        /// </summary>
        [StringLength(200)]
        [SugarColumn(ColumnName = "contact_person")]
        public string ContactPerson { get; set; }

        /// <summary>
        /// Contact Person Email
        /// </summary>
        [StringLength(200)]
        [SugarColumn(ColumnName = "contact_email")]
        public string ContactEmail { get; set; }

        /// <summary>
        /// CRM Lead's Life Cycle Stage ID
        /// </summary>
        [SugarColumn(ColumnName = "life_cycle_stage_id")]
        public long? LifeCycleStageId { get; set; }

        /// <summary>
        /// CRM Lead's Life Cycle Stage Name
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "life_cycle_stage_name")]
        public string LifeCycleStageName { get; set; }

        /// <summary>
        /// Onboarding Status (Started/InProgress/Completed/Paused/Cancelled)
        /// </summary>
        [StringLength(20)]
        [SugarColumn(ColumnName = "status")]
        public string Status { get; set; } = "Started";

        /// <summary>
        /// Overall Completion Rate (0-100)
        /// </summary>
        [SugarColumn(ColumnName = "completion_rate")]
        public decimal CompletionRate { get; set; } = 0;

        /// <summary>
        /// Start Date
        /// </summary>
        [SugarColumn(ColumnName = "start_date")]
        public DateTimeOffset? StartDate { get; set; }

        /// <summary>
        /// Estimated Completion Date
        /// </summary>
        [SugarColumn(ColumnName = "estimated_completion_date")]
        public DateTimeOffset? EstimatedCompletionDate { get; set; }

        /// <summary>
        /// Actual Completion Date
        /// </summary>
        [SugarColumn(ColumnName = "actual_completion_date")]
        public DateTimeOffset? ActualCompletionDate { get; set; }

        /// <summary>
        /// Current Assignee ID
        /// </summary>
        [SugarColumn(ColumnName = "current_assignee_id")]
        public long? CurrentAssigneeId { get; set; }

        /// <summary>
        /// Current Assignee Name
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "current_assignee_name")]
        public string CurrentAssigneeName { get; set; }

        /// <summary>
        /// Current Responsible Team
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "current_team")]
        public string CurrentTeam { get; set; }

        /// <summary>
        /// Stage Updated By ID
        /// </summary>
        [SugarColumn(ColumnName = "stage_updated_by_id")]
        public long? StageUpdatedById { get; set; }

        /// <summary>
        /// Stage Updated By Name
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "stage_updated_by")]
        public string StageUpdatedBy { get; set; }

        /// <summary>
        /// Stage Updated By Email
        /// </summary>
        [StringLength(200)]
        [SugarColumn(ColumnName = "stage_updated_by_email")]
        public string StageUpdatedByEmail { get; set; }

        /// <summary>
        /// Stage Updated Time
        /// </summary>
        [SugarColumn(ColumnName = "stage_updated_time")]
        public DateTimeOffset? StageUpdatedTime { get; set; }

        /// <summary>
        /// Current Stage Start Time
        /// </summary>
        [SugarColumn(ColumnName = "current_stage_start_time")]
        public DateTimeOffset? CurrentStageStartTime { get; set; }

        /// <summary>
        /// Priority (Low/Medium/High/Critical) - Set when Stage 1 is completed
        /// </summary>
        [StringLength(20)]
        [SugarColumn(ColumnName = "priority")]
        public string Priority { get; set; } = "Medium";

        /// <summary>
        /// Is Priority Set (used for validation when Stage 1 is completed)
        /// </summary>
        [SugarColumn(ColumnName = "is_priority_set")]
        public bool IsPrioritySet { get; set; } = false;

        /// <summary>
        /// Dynamic Extension Fields (redundant common fields for easy list search/sorting/export)
        /// </summary>
        [SugarColumn(ColumnName = "custom_fields_json")]
        public string CustomFieldsJson { get; set; }

        /// <summary>
        /// Notes Information
        /// </summary>
        [StringLength(1000)]
        [SugarColumn(ColumnName = "notes")]
        public string Notes { get; set; }

        /// <summary>
        /// Is Active
        /// </summary>
        [SugarColumn(ColumnName = "is_active")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Stage Progress Details (stored in JSONB format for better performance and querying)
        /// </summary>
        [SugarColumn(ColumnName = "stages_progress_json", ColumnDataType = "jsonb")]
        public string StagesProgressJson { get; set; }

        /// <summary>
        /// Stage Progress Details (not mapped to database, used for business logic)
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public List<OnboardingStageProgress> StagesProgress { get; set; } = new List<OnboardingStageProgress>();

        /// <summary>
        /// Calculate Current Stage Timeline Days (not mapped to database)
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public int? CurrentStageTimelineDays
        {
            get
            {
                if (CurrentStageStartTime.HasValue)
                {
                    return (int)(DateTimeOffset.Now - CurrentStageStartTime.Value).TotalDays;
                }
                return null;
            }
        }

        /// <summary>
        /// Calculate Total Timeline Days (not mapped to database)
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public int? TotalTimelineDays
        {
            get
            {
                if (StartDate.HasValue)
                {
                    var endDate = ActualCompletionDate ?? DateTimeOffset.Now;
                    return (int)(endDate - StartDate.Value).TotalDays;
                }
                return null;
            }
        }
    }
}
