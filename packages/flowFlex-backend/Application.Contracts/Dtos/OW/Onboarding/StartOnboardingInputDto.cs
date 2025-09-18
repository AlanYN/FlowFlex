using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// Start Onboarding Input DTO
    /// </summary>
    public class StartOnboardingInputDto
    {
        /// <summary>
        /// Reason for starting the onboarding (optional)
        /// </summary>
        [StringLength(500)]
        public string? Reason { get; set; }

        /// <summary>
        /// Additional notes (optional)
        /// </summary>
        [StringLength(1000)]
        public string? Notes { get; set; }

        /// <summary>
        /// Reset progress to stage 1 (default: false)
        /// </summary>
        public bool ResetProgress { get; set; } = false;
    }
}