using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// Onboarding进度DTO
    /// </summary>
    public class OnboardingProgressDto
    {
        /// <summary>
        /// Onboarding ID
        /// </summary>
        public long OnboardingId { get; set; }

        /// <summary>
        /// 当前阶段ID
        /// </summary>
        public long? CurrentStageId { get; set; }

        /// <summary>
        /// 当前阶段名称
        /// </summary>
        public string? CurrentStageName { get; set; }

        /// <summary>
        /// 总阶段数
        /// </summary>
        public int TotalStages { get; set; }

        /// <summary>
        /// 已完成阶段数
        /// </summary>
        public int CompletedStages { get; set; }

        /// <summary>
        /// 完成百分比 (0-100)
        /// </summary>
        public decimal CompletionPercentage { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public DateTimeOffset? StartTime { get; set; }

        /// <summary>
        /// 预计完成时间
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public DateTimeOffset? EstimatedCompletionTime { get; set; }

        /// <summary>
        /// 实际完成时间
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public DateTimeOffset? ActualCompletionTime { get; set; }

        /// <summary>
        /// 是否逾期
        /// </summary>
        public bool IsOverdue { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 优先级
        /// </summary>
        public string? Priority { get; set; }

        /// <summary>
        /// 详细的阶段进度信息
        /// </summary>
        public List<OnboardingStageProgressDto> StagesProgress { get; set; } = new List<OnboardingStageProgressDto>();
    }
}