using System;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// Onboarding时间线DTO
    /// </summary>
    public class OnboardingTimelineDto
    {
        /// <summary>
        /// 时间线ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 事件类型
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// 事件描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 发生时间
        /// </summary>
        public DateTimeOffset EventTime { get; set; }

        /// <summary>
        /// 操作人ID
        /// </summary>
        public long? UserId { get; set; }

        /// <summary>
        /// 操作人姓名
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// 阶段ID
        /// </summary>
        public long? StageId { get; set; }

        /// <summary>
        /// 阶段名称
        /// </summary>
        public string? StageName { get; set; }

        /// <summary>
        /// 详细信息
        /// </summary>
        public string? Details { get; set; }
    }
}