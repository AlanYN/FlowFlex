using System;
using System.Collections.Generic;
using FlowFlex.Application.Contracts.Dtos.OW.Stage;

namespace FlowFlex.Application.Contracts.Dtos.OW.Workflow
{
    /// <summary>
    /// Workflow version detail DTO with stages
    /// </summary>
    public class WorkflowVersionDetailDto
    {
        /// <summary>
        /// 版本ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 原始工作流ID
        /// </summary>
        public long OriginalWorkflowId { get; set; }

        /// <summary>
        /// 工作流名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

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
        /// 版本号
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// 变更类型
        /// </summary>
        public string ChangeType { get; set; }

        /// <summary>
        /// 变更原因
        /// </summary>
        public string ChangeReason { get; set; }

        /// <summary>
        /// 阶段列表
        /// </summary>
        public List<StageOutputDto> Stages { get; set; } = new List<StageOutputDto>();
    }
}