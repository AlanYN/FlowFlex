using System;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.Dtos;

namespace FlowFlex.Application.Contracts.Dtos.OW.StageCompletion
{
    /// <summary>
    /// 阶段完成日志分页/条件查询请求
    /// </summary>
    public class StageCompletionLogQueryRequest : PageQueryRequest
    {
        /// <summary>
        /// 入职ID（可选）
        /// </summary>
        public long? OnboardingId { get; set; }

        /// <summary>
        /// 阶段ID（可选）
        /// </summary>
        public long? StageId { get; set; }

        /// <summary>
        /// 日志类型（可选）
        /// </summary>
        public string? LogType { get; set; }

        /// <summary>
        /// 是否成功（可选）
        /// </summary>
        public bool? Success { get; set; }

        /// <summary>
        /// 开始时间（可选）
        /// </summary>
        public DateTimeOffset? StartDate { get; set; }

        /// <summary>
        /// 结束时间（可选）
        /// </summary>
        public DateTimeOffset? EndDate { get; set; }
    }

    /// <summary>
    /// 分页请求基类
    /// </summary>
    public class PageQueryRequest
    {
        [Range(1, int.MaxValue)]
        public int PageIndex { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 10;
    }
}