using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// 设置优先级输入DTO
    /// </summary>
    public class SetPriorityInputDto
    {
        /// <summary>
        /// 优先级 (Low/Medium/High/Critical)
        /// </summary>
        
        public string Priority { get; set; } = string.Empty;

        /// <summary>
        /// 设置原因
        /// </summary>
        public string? Reason { get; set; }
    }
}