using System;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// Onboarding create/update input DTO
    /// </summary>
    public class OnboardingInputDto
    {
        /// <summary>
        /// 所属Workflow主键ID（可选，不填时后端自动选择默认工作流）
        /// </summary>
        public long? WorkflowId { get; set; }

        /// <summary>
        /// 当前所在Stage主键ID
        /// </summary>
        public long? CurrentStageId { get; set; }

        /// <summary>
        /// 客户/线索ID
        /// </summary>
        
        [StringLength(100)]
        public string LeadId { get; set; }

        /// <summary>
        /// 客户/线索名称
        /// </summary>
        [StringLength(200)]
        public string LeadName { get; set; }

        /// <summary>
        /// 客户/线索邮箱
        /// </summary>
        [StringLength(200)]
        public string LeadEmail { get; set; }

        /// <summary>
        /// 客户/线索电话
        /// </summary>
        [StringLength(50)]
        public string LeadPhone { get; set; }

        /// <summary>
        /// 联系人姓名
        /// </summary>
        [StringLength(200)]
        public string ContactPerson { get; set; }

        /// <summary>
        /// 联系人邮箱
        /// </summary>
        [StringLength(200)]
        [EmailAddress(ErrorMessage = "联系人邮箱格式不正确")]
        public string ContactEmail { get; set; }

        /// <summary>
        /// CRM Lead的Life Cycle Stage ID
        /// </summary>
        public long? LifeCycleStageId { get; set; }

        /// <summary>
        /// CRM Lead的Life Cycle Stage名称
        /// </summary>
        [StringLength(100)]
        public string LifeCycleStageName { get; set; }

        /// <summary>
        /// Onboarding状态（Started/InProgress/Completed/Paused/Cancelled）
        /// </summary>
        [StringLength(20)]
        public string Status { get; set; } = "Started";

        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTimeOffset? StartDate { get; set; }

        /// <summary>
        /// 预计完成日期
        /// </summary>
        public DateTimeOffset? EstimatedCompletionDate { get; set; }

        /// <summary>
        /// 当前负责人ID
        /// </summary>
        public long? CurrentAssigneeId { get; set; }

        /// <summary>
        /// 当前负责人姓名
        /// </summary>
        [StringLength(100)]
        public string CurrentAssigneeName { get; set; }

        /// <summary>
        /// 当前负责团队
        /// </summary>
        [StringLength(100)]
        public string CurrentTeam { get; set; }

        /// <summary>
        /// Stage更新人ID
        /// </summary>
        public long? StageUpdatedById { get; set; }

        /// <summary>
        /// Stage更新人姓名
        /// </summary>
        [StringLength(100)]
        public string StageUpdatedBy { get; set; }

        /// <summary>
        /// Stage更新人邮箱
        /// </summary>
        [StringLength(200)]
        public string StageUpdatedByEmail { get; set; }

        /// <summary>
        /// Stage更新时间
        /// </summary>
        public DateTimeOffset? StageUpdatedTime { get; set; }

        /// <summary>
        /// 当前Stage开始时间
        /// </summary>
        public DateTimeOffset? CurrentStageStartTime { get; set; }

        /// <summary>
        /// 优先级（Low/Medium/High/Critical）
        /// </summary>
        [StringLength(20)]
        public string Priority { get; set; } = "Medium";

        /// <summary>
        /// 是否已设置优先级
        /// </summary>
        public bool IsPrioritySet { get; set; } = false;

        /// <summary>
        /// 动态扩展字段
        /// </summary>
        public string CustomFieldsJson { get; set; }

        /// <summary>
        /// 备注信息
        /// </summary>
        [StringLength(1000)]
        public string Notes { get; set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}