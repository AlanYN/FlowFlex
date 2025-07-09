using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.WorkflowVersion
{
    /// <summary>
    /// 工作流版本输入DTO
    /// </summary>
    public class WorkflowVersionInputDto
    {
        /// <summary>
        /// 原始工作流ID
        /// </summary>

        public long OriginalWorkflowId { get; set; }

        /// <summary>
        /// 工作流名称
        /// </summary>

        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// 工作流描述
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// 是否默认工作流
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// 状态
        /// </summary>

        [StringLength(20)]
        public string Status { get; set; }

        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTimeOffset? StartDate { get; set; }

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
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 配置JSON
        /// </summary>
        public string ConfigJson { get; set; }

        /// <summary>
        /// 变更原因
        /// </summary>
        [StringLength(500)]
        public string ChangeReason { get; set; }

        /// <summary>
        /// 变更类型
        /// </summary>

        [StringLength(50)]
        public string ChangeType { get; set; }
    }
}
