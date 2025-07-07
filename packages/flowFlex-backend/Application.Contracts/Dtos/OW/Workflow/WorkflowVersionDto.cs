using System;

namespace FlowFlex.Application.Contracts.Dtos.OW.Workflow
{
    /// <summary>
    /// Workflow version history DTO
    /// </summary>
    public class WorkflowVersionDto
    {
        /// <summary>
        /// 版本ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 工作流名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 是否为默认
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTimeOffset StartDate { get; set; }

        /// <summary>
        /// 结束日期
        /// </summary>
        public DateTimeOffset? EndDate { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// 版本号
        /// </summary>
        public int Version { get; set; }
    }
}