using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// Save Onboarding Stage DTO - 保存阶段请求DTO
    /// </summary>
    public class SaveOnboardingStageDto
    {
        /// <summary>
        /// Onboarding ID
        /// </summary>
        [Required]
        public long OnboardingId { get; set; }

        /// <summary>
        /// Stage ID to save
        /// </summary>
        [Required]
        public long StageId { get; set; }
    }
}