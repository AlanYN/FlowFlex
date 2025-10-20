using System;
using System.Collections.Generic;
using FlowFlex.Domain.Shared.Enums.OW;
using Newtonsoft.Json;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// Onboarding output DTO
    /// </summary>
    public class OnboardingOutputDto
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 所属Workflow主键ID
        /// </summary>
        public long WorkflowId { get; set; }

        /// <summary>
        /// Workflow名称
        /// </summary>
        public string WorkflowName { get; set; }

        /// <summary>
        /// 当前所在Stage主键ID
        /// </summary>
        public long? CurrentStageId { get; set; }

        /// <summary>
        /// 当前Stage名称
        /// </summary>
        public string CurrentStageName { get; set; }

        /// <summary>
        /// 当前进度（Stage序号）
        /// </summary>
        public int CurrentStageOrder { get; set; }

        /// <summary>
        /// 客户/线索ID
        /// </summary>
        public string LeadId { get; set; }

        /// <summary>
        /// 客户/线索名称
        /// </summary>
        public string LeadName { get; set; }

        /// <summary>
        /// 客户/线索邮箱
        /// </summary>
        public string LeadEmail { get; set; }

        /// <summary>
        /// 客户/线索电话
        /// </summary>
        public string LeadPhone { get; set; }

        /// <summary>
        /// 联系人姓名
        /// </summary>
        public string ContactPerson { get; set; }

        /// <summary>
        /// 联系人邮箱
        /// </summary>
        public string ContactEmail { get; set; }

        /// <summary>
        /// CRM Lead的Life Cycle Stage ID
        /// </summary>
        public long? LifeCycleStageId { get; set; }

        /// <summary>
        /// CRM Lead的Life Cycle Stage名称
        /// </summary>
        public string LifeCycleStageName { get; set; }

        /// <summary>
        /// Onboarding状态
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 总体完成率（0-100）
        /// </summary>
        public decimal CompletionRate { get; set; }

        /// <summary>
        /// 开始日期
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public DateTimeOffset? StartDate { get; set; }

        /// <summary>
        /// 预计完成日期
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public DateTimeOffset? EstimatedCompletionDate { get; set; }

        /// <summary>
        /// 实际完成日期
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public DateTimeOffset? ActualCompletionDate { get; set; }

        /// <summary>
        /// 当前负责人ID
        /// </summary>
        public long? CurrentAssigneeId { get; set; }

        /// <summary>
        /// 当前负责人姓名
        /// </summary>
        public string CurrentAssigneeName { get; set; }

        /// <summary>
        /// 当前负责团队
        /// </summary>
        public string CurrentTeam { get; set; }

        /// <summary>
        /// Stage更新人ID
        /// </summary>
        public long? StageUpdatedById { get; set; }

        /// <summary>
        /// Stage更新人姓名
        /// </summary>
        public string StageUpdatedBy { get; set; }

        /// <summary>
        /// Stage更新人邮箱
        /// </summary>
        public string StageUpdatedByEmail { get; set; }

        /// <summary>
        /// Stage更新时间
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public DateTimeOffset? StageUpdatedTime { get; set; }

        /// <summary>
        /// 当前Stage开始时间
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public DateTimeOffset? CurrentStageStartTime { get; set; }

        /// <summary>
        /// 当前Stage预计结束时间 (基于开始时间 + 预计天数计算) - UTC时间
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public DateTimeOffset? CurrentStageEndTime { get; set; }

        /// <summary>
        /// 当前Stage预计完成天数
        /// </summary>
        public decimal? CurrentStageEstimatedDays { get; set; }

        /// <summary>
        /// Timeline天数
        /// </summary>
        public int TimelineDays { get; set; }

        /// <summary>
        /// Timeline显示文本
        /// </summary>
        public string TimelineDisplay { get; set; }

        /// <summary>
        /// 目标完成日期（ETA）
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public DateTimeOffset? TargetCompletionDate { get; set; }

        /// <summary>
        /// 是否逾期
        /// </summary>
        public bool IsOverdue { get; set; }

        /// <summary>
        /// 优先级
        /// </summary>
        public string Priority { get; set; }

        /// <summary>
        /// 是否已设置优先级
        /// </summary>
        public bool IsPrioritySet { get; set; }

        /// <summary>
        /// Ownership - User ID who owns this onboarding
        /// </summary>
        public long? Ownership { get; set; }

        /// <summary>
        /// Ownership Name - User name who owns this onboarding
        /// </summary>
        public string OwnershipName { get; set; }

        /// <summary>
        /// Ownership Email - User email who owns this onboarding
        /// </summary>
        public string OwnershipEmail { get; set; }

        /// <summary>
        /// 动态扩展字段（JSON）
        /// </summary>
        public string CustomFieldsJson { get; set; }

        /// <summary>
        /// 备注信息
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Permission Subject Type - Team or User based permissions
        /// </summary>
        public PermissionSubjectTypeEnum PermissionSubjectType { get; set; }

        /// <summary>
        /// View Permission Mode - Public/VisibleToTeams/InvisibleToTeams/Private
        /// </summary>
        public ViewPermissionModeEnum ViewPermissionMode { get; set; }

        /// <summary>
        /// View Teams - List of team names for view permission control (used when PermissionSubjectType=Team)
        /// </summary>
        public List<string> ViewTeams { get; set; }

        /// <summary>
        /// View Users - List of user IDs for view permission control (used when PermissionSubjectType=User)
        /// </summary>
        public List<string> ViewUsers { get; set; }

        /// <summary>
        /// Operate Teams - List of team names that can perform operations (used when PermissionSubjectType=Team)
        /// </summary>
        public List<string> OperateTeams { get; set; }

        /// <summary>
        /// Operate Users - List of user IDs that can perform operations (used when PermissionSubjectType=User)
        /// </summary>
        public List<string> OperateUsers { get; set; }

        /// <summary>
        /// Stage进度详情
        /// </summary>
        public List<OnboardingStageProgressDto> StagesProgress { get; set; } = new List<OnboardingStageProgressDto>();

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        public string CreateBy { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTimeOffset ModifyDate { get; set; }

        /// <summary>
        /// 修改人
        /// </summary>
        public string ModifyBy { get; set; }
    }
}