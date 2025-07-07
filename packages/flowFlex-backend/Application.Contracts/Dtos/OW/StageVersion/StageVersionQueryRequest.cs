using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.Dtos.OW.StageVersion
{
    /// <summary>
    /// 阶段版本查询请求DTO
    /// </summary>
    public class StageVersionQueryRequest : QueryPageModel
    {
        /// <summary>
        /// 工作流版本ID
        /// </summary>
        public long? WorkflowVersionId { get; set; }

        /// <summary>
        /// 原始阶段ID
        /// </summary>
        public long? OriginalStageId { get; set; }

        /// <summary>
        /// 阶段名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 工作流版本
        /// </summary>
        public string WorkflowVersion { get; set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool? IsActive { get; set; }

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
 