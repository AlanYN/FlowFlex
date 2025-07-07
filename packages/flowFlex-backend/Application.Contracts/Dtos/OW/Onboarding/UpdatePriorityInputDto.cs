using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// 更新优先级输入DTO
    /// </summary>
    public class UpdatePriorityInputDto
    {
        /// <summary>
        /// 优先级 (Low/Medium/High/Critical)
        /// </summary>
        
        public string Priority { get; set; } = string.Empty;

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remarks { get; set; }
    }
}