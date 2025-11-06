using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW
{
    /// <summary>
    /// User invitation request DTO
    /// </summary>
    public class UserInvitationRequestDto
    {
        /// <summary>
        /// Onboarding ID
        /// </summary>
        [Required]
        public long OnboardingId { get; set; }

        /// <summary>
        /// Email addresses to invite
        /// </summary>
        [Required]
        [MinLength(1, ErrorMessage = "At least one email address is required")]
        public List<string> EmailAddresses { get; set; } = new List<string>();

        /// <summary>
        /// Base URL for generating invitation links (optional, will use default if not provided)
        /// </summary>
        public string? BaseUrl { get; set; }
    }

    /// <summary>
    /// User invitation response DTO
    /// </summary>
    public class UserInvitationResponseDto
    {
        /// <summary>
        /// Successfully sent invitations
        /// </summary>
        public List<string> SuccessfulInvitations { get; set; } = new List<string>();

        /// <summary>
        /// Failed invitations with error messages
        /// </summary>
        public Dictionary<string, string> FailedInvitations { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Total number of invitations sent
        /// </summary>
        public int TotalSent { get; set; }

        /// <summary>
        /// Total number of failed invitations
        /// </summary>
        public int TotalFailed { get; set; }
    }

    /// <summary>
    /// Portal user DTO
    /// </summary>
    public class PortalUserDto
    {
        /// <summary>
        /// User ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Email address
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Status (Pending, Active, Expired)
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Invitation sent date (nullable - only set when invitation is actually sent)
        /// </summary>
        public DateTimeOffset? SentDate { get; set; }

        /// <summary>
        /// Invitation token
        /// </summary>
        public string InvitationToken { get; set; }

        /// <summary>
        /// Last login date
        /// </summary>
        public DateTimeOffset? LastLoginDate { get; set; }
    }

    /// <summary>
    /// Portal access verification request DTO
    /// </summary>
    public class PortalAccessVerificationRequestDto
    {
        /// <summary>
        /// Invitation token
        /// </summary>
        [Required]
        public string Token { get; set; }

        /// <summary>
        /// Email address
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    /// <summary>
    /// Portal access verification response DTO
    /// </summary>
    public class PortalAccessVerificationResponseDto
    {
        /// <summary>
        /// Whether verification was successful
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Whether the invitation has expired
        /// </summary>
        public bool IsExpired { get; set; }

        /// <summary>
        /// Onboarding ID
        /// </summary>
        public long OnboardingId { get; set; }

        /// <summary>
        /// User email
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Access token for authentication
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Error message if verification failed
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Resend invitation request DTO
    /// </summary>
    public class ResendInvitationRequestDto
    {
        /// <summary>
        /// Onboarding ID
        /// </summary>
        [Required]
        public long OnboardingId { get; set; }

        /// <summary>
        /// Email address to resend invitation to
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Base URL for generating invitation links (optional, will use default if not provided)
        /// </summary>
        public string? BaseUrl { get; set; }
    }

    /// <summary>
    /// Token validation request DTO
    /// </summary>
    public class TokenValidationRequestDto
    {
        /// <summary>
        /// Invitation token to validate
        /// </summary>
        [Required]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Onboarding ID
        /// </summary>
        [Required]
        public long OnboardingId { get; set; }
    }

    /// <summary>
    /// Token validation response DTO
    /// </summary>
    public class TokenValidationResponseDto
    {
        /// <summary>
        /// Whether the token is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Email associated with the token (if valid)
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Error message (if validation failed)
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Portal access verification request DTO using short URL
    /// </summary>
    public class PortalAccessVerificationByShortUrlRequestDto
    {
        /// <summary>
        /// Email address for verification
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}