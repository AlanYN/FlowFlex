using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// Assign onboarding input DTO
    /// </summary>
    public class AssignOnboardingInputDto
    {
        /// <summary>
        /// 负责人ID
        /// </summary>
        public long? AssigneeId { get; set; }

        /// <summary>
        /// 负责人姓名
        /// </summary>
        [StringLength(100)]
        public string AssigneeName { get; set; }

        /// <summary>
        /// 负责团队
        /// </summary>
        [StringLength(100)]
        public string Team { get; set; }

        /// <summary>
        /// 分配原因
        /// </summary>
        [StringLength(500)]
        public string Reason { get; set; }
    }
}