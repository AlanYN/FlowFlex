using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.AdobeSign
{
    /// <summary>
    /// A single signer in an Adobe Sign agreement
    /// </summary>
    public class AdobeSignerDto
    {
        /// <summary>
        /// Signer's email address (required, validated by Adobe Sign)
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        /// <summary>
        /// Signer's display name
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        /// <summary>
        /// Role in the agreement: Signer / Approver / CC
        /// </summary>
        public string Role { get; set; } = "Signer";

        /// <summary>
        /// Signing order index (used when SigningOrder = Sequential).
        /// Starts from 1. Signers with the same order sign simultaneously.
        /// </summary>
        public int Order { get; set; } = 1;

        /// <summary>
        /// Current status of this signer (populated in output DTOs):
        /// Awaiting / Signed / Declined / Cancelled
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Timestamp when this signer completed signing (null if not yet signed)
        /// </summary>
        public DateTimeOffset? SignedAt { get; set; }
    }
}
