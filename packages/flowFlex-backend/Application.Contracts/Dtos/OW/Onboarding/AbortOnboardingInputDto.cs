using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// Abort Onboarding Input DTO
    /// </summary>
    public class AbortOnboardingInputDto
    {
        /// <summary>
        /// Reason for aborting the onboarding (required)
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Reason { get; set; }

        /// <summary>
        /// Additional notes (optional)
        /// </summary>
        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}