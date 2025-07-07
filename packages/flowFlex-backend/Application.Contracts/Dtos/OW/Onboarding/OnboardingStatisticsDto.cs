using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// Onboarding statistics DTO
    /// </summary>
    public class OnboardingStatisticsDto
    {
        /// <summary>
        /// 总数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 进行中数量
        /// </summary>
        public int InProgressCount { get; set; }

        /// <summary>
        /// 已完成数量
        /// </summary>
        public int CompletedCount { get; set; }

        /// <summary>
        /// 暂停数量
        /// </summary>
        public int PausedCount { get; set; }

        /// <summary>
        /// 已取消数量
        /// </summary>
        public int CancelledCount { get; set; }

        /// <summary>
        /// 逾期数量
        /// </summary>
        public int OverdueCount { get; set; }

        /// <summary>
        /// 平均完成率
        /// </summary>
        public decimal AverageCompletionRate { get; set; }

        /// <summary>
        /// 按状态统计
        /// </summary>
        public Dictionary<string, int> StatusStatistics { get; set; }

        /// <summary>
        /// 按优先级统计
        /// </summary>
        public Dictionary<string, int> PriorityStatistics { get; set; }

        /// <summary>
        /// 按团队统计
        /// </summary>
        public Dictionary<string, int> TeamStatistics { get; set; }

        /// <summary>
        /// 按阶段统计
        /// </summary>
        public Dictionary<string, int> StageStatistics { get; set; }
    }
}