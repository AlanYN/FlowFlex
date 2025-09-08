using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// Resume Onboarding Input DTO
    /// </summary>
    public class ResumeOnboardingInputDto
    {
        /// <summary>
        /// Reason for resuming (optional)
        /// </summary>
        [StringLength(500)]
        public string? Reason { get; set; }

        /// <summary>
        /// Additional notes (optional)
        /// </summary>
        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}