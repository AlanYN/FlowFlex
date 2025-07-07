using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.OW.Workflow
{
    /// <summary>
    /// Workflow export search parameters
    /// </summary>
    public class WorkflowExportSearch
    {
        /// <summary>
        /// 指定要导出的工作流ID列表
        /// </summary>
        public List<long> Ids { get; set; }

        /// <summary>
        /// 是否导出所有工作流
        /// </summary>
        public bool IsAll { get; set; }

        /// <summary>
        /// 是否只导出激活的工作流
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// 是否只导出默认工作流
        /// </summary>
        public bool? IsDefault { get; set; }

        /// <summary>
        /// 状态过滤
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 工作流名称关键字搜索
        /// </summary>
        public string NameKeyword { get; set; }
    }
}