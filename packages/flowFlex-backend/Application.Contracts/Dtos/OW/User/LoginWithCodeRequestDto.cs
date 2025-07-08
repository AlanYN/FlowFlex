using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.User
{
    /// <summary>
    /// Login with verification code request DTO
    /// </summary>
    public class LoginWithCodeRequestDto
    {
        /// <summary>
        /// Email address
        /// </summary>
        [Required(ErrorMessage = "Email cannot be empty")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Verification code
        /// </summary>
        [Required(ErrorMessage = "Verification code cannot be empty")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be 6 digits")]
        public string VerificationCode { get; set; } = string.Empty;
    }
}