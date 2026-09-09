using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.AdobeSign
{
    /// <summary>
    /// Input DTO for initiating an Adobe Sign e-signature request (OW-731)
    /// </summary>
    public class RequestAdobeSignInputDto
    {
        /// <summary>
        /// The Onboarding (Case) ID this signing request belongs to
        /// </summary>
        [Required]
        public long OnboardingId { get; set; }

        /// <summary>
        /// The Stage ID this signing request belongs to
        /// </summary>
        [Required]
        public long StageId { get; set; }

        /// <summary>
        /// The ID of the original PDF file in ff_onboarding_file to be signed
        /// </summary>
        [Required]
        public long SourceFileId { get; set; }

        /// <summary>
        /// List of signers (1–10). Each entry must have at least Email and Name.
        /// </summary>
        [Required]
        [MinLength(1, ErrorMessage = "At least one signer is required")]
        [MaxLength(10, ErrorMessage = "Maximum 10 signers allowed")]
        public List<AdobeSignerDto> Signers { get; set; }

        /// <summary>
        /// Signing order: Sequential (one by one in Order index) or Parallel (all at once)
        /// </summary>
        public string SigningOrder { get; set; } = "Sequential";

        /// <summary>
        /// Number of days before the agreement expires (7 / 14 / 30 / 60 / 90)
        /// </summary>
        [Range(1, 365)]
        public int ExpirationDays { get; set; } = 30;

        /// <summary>
        /// Optional message displayed to signers in the Adobe Sign email
        /// </summary>
        [StringLength(1000)]
        public string Message { get; set; }
    }
}
