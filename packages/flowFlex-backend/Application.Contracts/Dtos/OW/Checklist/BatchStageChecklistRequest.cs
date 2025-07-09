using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Checklist
{
    /// <summary>
    /// 批量查询Stage清单请求DTO
    /// </summary>
    public class BatchStageChecklistRequest
    {
        /// <summary>
        /// Stage ID列表
        /// </summary>
        [Required(ErrorMessage = "Stage ID列表不能为空")]
        public List<long> StageIds { get; set; } = new List<long>();
    }
} 