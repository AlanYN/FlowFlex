using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.OW.Checklist
{
    /// <summary>
    /// 批量查询Stage清单响应DTO
    /// </summary>
    public class BatchStageChecklistResponse
    {
        /// <summary>
        /// Stage ID到清单列表的映射
        /// </summary>
        public Dictionary<long, List<ChecklistOutputDto>> StageChecklists { get; set; } = new Dictionary<long, List<ChecklistOutputDto>>();
    }
}