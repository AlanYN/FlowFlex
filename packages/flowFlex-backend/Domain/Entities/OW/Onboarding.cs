using System.ComponentModel.DataAnnotations;
using FlowFlex.Domain.Entities.Base;
using FlowFlex.Domain.Shared.Enums.OW;
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
        /// Customer/Lead ID (optional - Case Code is the primary identifier)
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "lead_id", IsNullable = true)]
        public string? LeadId { get; set; }

        /// <summary>
        /// Customer/Lead Name
        /// </summary>
        [StringLength(200)]
        [SugarColumn(ColumnName = "lead_name")]
        public string LeadName { get; set; }

        /// <summary>
        /// Case Code - Unique identifier generated from Lead Name
        /// </summary>
        [StringLength(50)]
        [SugarColumn(ColumnName = "case_code")]
        public string CaseCode { get; set; }

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
        /// Onboarding Status (Inactive/Active/Completed/Force Completed/Paused/Aborted)
        /// </summary>
        [StringLength(20)]
        [SugarColumn(ColumnName = "status")]
        public string Status { get; set; } = "Inactive";

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
        /// Ownership - User ID who owns this onboarding
        /// </summary>
        [SugarColumn(ColumnName = "ownership")]
        public long? Ownership { get; set; }

        /// <summary>
        /// Ownership Name - User name who owns this onboarding
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "ownership_name")]
        public string OwnershipName { get; set; }

        /// <summary>
        /// Ownership Email - User email who owns this onboarding
        /// </summary>
        [StringLength(200)]
        [SugarColumn(ColumnName = "ownership_email")]
        public string OwnershipEmail { get; set; }

        /// <summary>
        /// Dynamic Extension Fields (redundant common fields for easy list search/sorting/export) - JSONB
        /// </summary>
        [SugarColumn(ColumnName = "custom_fields_json", ColumnDataType = "jsonb", IsJson = true)]
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
        /// View Permission Subject Type - Defines whether view permissions are based on Teams or Individual Users
        /// Team: View permission based on team membership (default)
        /// User: View permission based on specific user IDs
        /// </summary>
        [SugarColumn(ColumnName = "view_permission_subject_type")]
        public PermissionSubjectTypeEnum ViewPermissionSubjectType { get; set; } = PermissionSubjectTypeEnum.Team;

        /// <summary>
        /// Operate Permission Subject Type - Defines whether operate permissions are based on Teams or Individual Users
        /// Team: Operate permission based on team membership (default)
        /// User: Operate permission based on specific user IDs
        /// </summary>
        [SugarColumn(ColumnName = "operate_permission_subject_type")]
        public PermissionSubjectTypeEnum OperatePermissionSubjectType { get; set; } = PermissionSubjectTypeEnum.Team;

        /// <summary>
        /// View Permission Mode - Defines how view permissions are controlled
        /// Public: All users can view (default)
        /// VisibleToTeams: Only listed teams/users can view
        /// InvisibleToTeams: All teams/users except listed can view
        /// Private: Only the owner can view
        /// </summary>
        [SugarColumn(ColumnName = "view_permission_mode")]
        public ViewPermissionModeEnum ViewPermissionMode { get; set; } = ViewPermissionModeEnum.Public;

        /// <summary>
        /// View Teams - JSONB array of team names for view permission control (used when ViewPermissionSubjectType=Team)
        /// Example: ["Team-A", "Team-B"]
        /// Used with VisibleToTeams or InvisibleToTeams mode
        /// </summary>
        [SugarColumn(ColumnName = "view_teams", ColumnDataType = "jsonb", IsJson = true)]
        public string ViewTeams { get; set; }

        /// <summary>
        /// View Users - JSONB array of user IDs for view permission control (used when ViewPermissionSubjectType=User)
        /// Example: ["1935628742495965184", "1935628742495965185"]
        /// Used with VisibleToTeams or InvisibleToTeams mode
        /// </summary>
        [SugarColumn(ColumnName = "view_users", ColumnDataType = "jsonb", IsJson = true)]
        public string ViewUsers { get; set; }

        /// <summary>
        /// Operate Teams - JSONB array of team names that can perform operations (used when OperatePermissionSubjectType=Team)
        /// Example: ["Team-A", "Team-B"]
        /// Operations include: Create, Update, Delete, Assign
        /// </summary>
        [SugarColumn(ColumnName = "operate_teams", ColumnDataType = "jsonb", IsJson = true)]
        public string OperateTeams { get; set; }

        /// <summary>
        /// Operate Users - JSONB array of user IDs that can perform operations (used when OperatePermissionSubjectType=User)
        /// Example: ["1935628742495965184", "1935628742495965185"]
        /// Operations include: Create, Update, Delete, Assign
        /// </summary>
        [SugarColumn(ColumnName = "operate_users", ColumnDataType = "jsonb", IsJson = true)]
        public string OperateUsers { get; set; }

        /// <summary>
        /// Use Same Team For Operate - Indicates whether operate teams/users should use the same teams/users as view permission
        /// When true, OperateTeams/OperateUsers will be automatically synchronized with ViewTeams/ViewUsers based on the permission subject type
        /// </summary>
        [SugarColumn(ColumnName = "use_same_team_for_operate")]
        public bool UseSameTeamForOperate { get; set; } = false;

        /// <summary>
        /// Stage Progress Details (stored in JSONB format for better performance and querying)
        /// </summary>
        [SugarColumn(ColumnName = "stages_progress_json", ColumnDataType = "jsonb", IsJson = true)]
        public string StagesProgressJson { get; set; }

        /// <summary>
        /// System ID from external integration (Entity Mapping System ID)
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "system_id", IsNullable = true)]
        public string? SystemId { get; set; }

        /// <summary>
        /// Integration ID from external integration
        /// </summary>
        [SugarColumn(ColumnName = "integration_id", IsNullable = true)]
        public long? IntegrationId { get; set; }

        /// <summary>
        /// External Entity Type (e.g., "lead", "customer", "account") from external integration
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "entity_type", IsNullable = true)]
        public string? EntityType { get; set; }

        /// <summary>
        /// External Entity ID from external integration (e.g., Lead ID, Customer ID)
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "entity_id", IsNullable = true)]
        public string? EntityId { get; set; }

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
