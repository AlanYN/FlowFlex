using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Stage
{
    /// <summary>
    /// Sort stages input DTO
    /// </summary>
    public class SortStagesInputDto
    {
        /// <summary>
        /// 工作流ID
        /// </summary>
        
        public long WorkflowId { get; set; }

        /// <summary>
        /// 阶段排序列表
        /// </summary>
        
        public List<StageOrderItem> StageOrders { get; set; }
    }

    /// <summary>
    /// Stage order item
    /// </summary>
    public class StageOrderItem
    {
        /// <summary>
        /// 阶段ID
        /// </summary>
        public long StageId { get; set; }

        /// <summary>
        /// 新的排序值
        /// </summary>
        public int Order { get; set; }
    }
}