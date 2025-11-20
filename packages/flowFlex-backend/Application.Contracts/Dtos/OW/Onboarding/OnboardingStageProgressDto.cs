using System;
using System.Collections.Generic;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Domain.Shared.Enums;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// Onboarding Stage Progress DTO - 每个阶段的进度信息
    /// </summary>
    public class OnboardingStageProgressDto
    {
        /// <summary>
        /// Stage ID
        /// </summary>
        public long StageId { get; set; }

        /// <summary>
        /// Stage名称
        /// </summary>
        public string StageName { get; set; }

        /// <summary>
        /// Stage描述
        /// </summary>
        public string StageDescription { get; set; }

        /// <summary>
        /// Stage顺序
        /// </summary>
        public int StageOrder { get; set; }

        /// <summary>
        /// 完成状态（Pending/InProgress/Completed/Skipped）
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 是否已完成
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTimeOffset? StartTime { get; set; }

        /// <summary>
        /// 完成时间
        /// </summary>
        public DateTimeOffset? CompletionTime { get; set; }

        /// <summary>
        /// 预计结束时间 (基于开始时间 + 预计天数计算) - UTC时间
        /// </summary>
        public DateTimeOffset? EndTime { get; set; }

        /// <summary>
        /// 完成人ID
        /// </summary>
        public long? CompletedById { get; set; }

        /// <summary>
        /// 完成人姓名
        /// </summary>
        public string CompletedBy { get; set; }

        /// <summary>
        /// 预计完成天数（支持小数）- 显示优先级：CustomEstimatedDays > Stage配置的EstimatedDays
        /// 如果用户设置了CustomEstimatedDays，则显示自定义值；否则显示Stage配置的默认值
        /// </summary>
        public decimal? EstimatedDays { get; set; }

        /// <summary>
        /// 用户自定义预计完成天数（支持小数）- 用于覆盖Stage配置
        /// 当此字段有值时，EstimatedDays将显示此自定义值
        /// </summary>
        public decimal? CustomEstimatedDays { get; set; }

        /// <summary>
        /// 用户自定义结束时间 - 覆盖计算的EndTime
        /// </summary>
        public DateTimeOffset? CustomEndTime { get; set; }

        /// <summary>
        /// 实际用时天数
        /// </summary>
        public int? ActualDays { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// 是否为当前阶段
        /// </summary>
        public bool IsCurrent { get; set; }

        /// <summary>
        /// 是否已保存
        /// </summary>
        public bool IsSaved { get; set; }

        /// <summary>
        /// 保存时间
        /// </summary>
        public DateTimeOffset? SaveTime { get; set; }

        /// <summary>
        /// 保存人ID
        /// </summary>
        public string SavedById { get; set; }

        /// <summary>
        /// 保存人姓名/邮箱
        /// </summary>
        public string SavedBy { get; set; }

        /// <summary>
        /// Visible in Portal - Controls whether this stage is visible in the portal
        /// </summary>
        public bool VisibleInPortal { get; set; } = true;

        /// <summary>
        /// Portal Permission - Defines the level of access in the customer portal (Viewable or Completable)
        /// Only applies when VisibleInPortal is true
        /// </summary>
        public PortalPermissionEnum? PortalPermission { get; set; }

        /// <summary>
        /// Attachment Management Needed - Indicates whether file upload is required for this stage
        /// </summary>
        public bool AttachmentManagementNeeded { get; set; } = false;

        /// <summary>
        /// Stage组件配置列表
        /// 定义Stage包含的组件及其顺序
        /// </summary>
        public List<FlowFlex.Domain.Shared.Models.StageComponent> Components { get; set; } = new List<FlowFlex.Domain.Shared.Models.StageComponent>();

        // === AI Summary fields for display ===
        public string AiSummary { get; set; }
        public DateTime? AiSummaryGeneratedAt { get; set; }
        public decimal? AiSummaryConfidence { get; set; }
        public string AiSummaryModel { get; set; }
        public string AiSummaryData { get; set; }

        /// <summary>
        /// Actions associated with this stage
        /// 与此阶段关联的动作列表
        /// </summary>
        public List<ActionTriggerMappingWithActionInfo> Actions { get; set; } = new List<ActionTriggerMappingWithActionInfo>();


        /// <summary>
        /// Permission information for this stage (STRICT MODE)
        /// 当前用户对该 Stage 的权限信息
        /// STRICT MODE: Stage permission = Workflow ∩ Stage (requires both levels)
        /// </summary>
        public FlowFlex.Application.Contracts.Dtos.OW.Permission.PermissionInfoDto Permission { get; set; }
    }
}