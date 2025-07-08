using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.User
{
    /// <summary>
    /// Send verification code request DTO
    /// </summary>
    public class SendVerificationCodeRequestDto
    {
        /// <summary>
        /// Email address
        /// </summary>
        [Required(ErrorMessage = "Email cannot be empty")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email length cannot exceed 100 characters")]
        public string Email { get; set; }
    }
}