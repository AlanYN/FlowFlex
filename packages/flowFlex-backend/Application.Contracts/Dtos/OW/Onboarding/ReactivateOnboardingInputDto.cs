using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// Reactivate Onboarding Input DTO
    /// </summary>
    public class ReactivateOnboardingInputDto
    {
        /// <summary>
        /// Reason for reactivation (required)
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Reason { get; set; }

        /// <summary>
        /// Additional notes (optional)
        /// </summary>
        [StringLength(1000)]
        public string? Notes { get; set; }

        /// <summary>
        /// Reset progress to stage 1 (deprecated: this parameter is no longer used)
        /// </summary>
        [Obsolete("This parameter is no longer used. StagesProgress fields are now preserved during reactivation.")]
        public bool ResetProgress { get; set; } = true;

        /// <summary>
        /// Preserve questionnaire answers (default: true)
        /// </summary>
        public bool PreserveAnswers { get; set; } = true;
    }
}