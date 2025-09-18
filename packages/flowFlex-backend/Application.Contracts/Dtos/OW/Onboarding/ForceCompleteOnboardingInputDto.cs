using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// Force Complete Onboarding Input DTO
    /// </summary>
    public class ForceCompleteOnboardingInputDto
    {
        /// <summary>
        /// Reason for force completion (required)
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Reason { get; set; }

        /// <summary>
        /// Additional completion notes (optional)
        /// </summary>
        [StringLength(1000)]
        public string? CompletionNotes { get; set; }

        /// <summary>
        /// Rating (1-5 stars, optional)
        /// </summary>
        [Range(1, 5)]
        public int? Rating { get; set; }

        /// <summary>
        /// Feedback (optional)
        /// </summary>
        [StringLength(2000)]
        public string? Feedback { get; set; }
    }
}