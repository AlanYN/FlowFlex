using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.Dtos.OW.WorkflowVersion
{
    /// <summary>
    /// 工作流版本查询请求DTO
    /// </summary>
    public class WorkflowVersionQueryRequest : QueryPageModel
    {
        /// <summary>
        /// 原始工作流ID
        /// </summary>
        public long? OriginalWorkflowId { get; set; }

        /// <summary>
        /// 工作流名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 版本号
        /// </summary>
        public int? Version { get; set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// 变更类型
        /// </summary>
        public string ChangeType { get; set; }

        /// <summary>
        /// 创建时间开始
        /// </summary>
        public DateTimeOffset? CreateDateStart { get; set; }

        /// <summary>
        /// 创建时间结束
        /// </summary>
        public DateTimeOffset? CreateDateEnd { get; set; }
    }
}
