using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// 批量更新状态输入DTO
    /// </summary>
    public class BatchUpdateStatusInputDto
    {
        /// <summary>
        /// Onboarding ID列表
        /// </summary>

        public List<long> Ids { get; set; } = new List<long>();

        /// <summary>
        /// 目标状态
        /// </summary>

        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remarks { get; set; }
    }
}