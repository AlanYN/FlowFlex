using System;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Workflow
{
    /// <summary>
    /// Duplicate workflow input DTO
    /// </summary>
    public class DuplicateWorkflowInputDto
    {
        /// <summary>
        /// 新工作流名称
        /// </summary>

        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// 新工作流描述
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// 是否复制阶段
        /// </summary>
        public bool CopyStages { get; set; } = true;

        /// <summary>
        /// 是否设为默认
        /// </summary>
        public bool SetAsDefault { get; set; } = false;

        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTimeOffset? StartDate { get; set; }

        /// <summary>
        /// 结束日期
        /// </summary>
        public DateTimeOffset? EndDate { get; set; }
    }
}