using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// Move to stage input DTO
    /// </summary>
    public class MoveToStageInputDto
    {
        /// <summary>
        /// 目标Stage ID
        /// </summary>
        
        public long StageId { get; set; }

        /// <summary>
        /// 移动原因/备注
        /// </summary>
        [StringLength(500)]
        public string Reason { get; set; }

        /// <summary>
        /// 是否强制移动（跳过验证）
        /// </summary>
        public bool ForceMove { get; set; } = false;
    }
}