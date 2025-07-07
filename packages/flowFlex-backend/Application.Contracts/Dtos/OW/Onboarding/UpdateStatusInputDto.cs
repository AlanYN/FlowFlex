using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// 更新状态输入DTO
    /// </summary>
    public class UpdateStatusInputDto
    {
        /// <summary>
        /// 新状态
        /// </summary>
        
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remarks { get; set; }
    }
}