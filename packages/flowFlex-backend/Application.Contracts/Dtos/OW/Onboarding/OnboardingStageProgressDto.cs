using System;
using System.Collections.Generic;

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
        /// 完成人ID
        /// </summary>
        public long? CompletedById { get; set; }

        /// <summary>
        /// 完成人姓名
        /// </summary>
        public string CompletedBy { get; set; }

        /// <summary>
        /// 预计完成天数（支持小数）
        /// </summary>
        public decimal? EstimatedDays { get; set; }

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
        /// Visible in Portal - Controls whether this stage is visible in the portal
        /// </summary>
        public bool VisibleInPortal { get; set; } = true;

        /// <summary>
        /// Attachment Management Needed - Indicates whether file upload is required for this stage
        /// </summary>
        public bool AttachmentManagementNeeded { get; set; } = false;

        /// <summary>
        /// Stage组件配置列表
        /// 定义Stage包含的组件及其顺序
        /// </summary>
        public List<FlowFlex.Domain.Shared.Models.StageComponent> Components { get; set; } = new List<FlowFlex.Domain.Shared.Models.StageComponent>();
    }
}