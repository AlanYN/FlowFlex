using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.User
{
    /// <summary>
    /// Parse JWT token request DTO
    /// </summary>
    public class ParseTokenRequestDto
    {
        /// <summary>
        /// JWT Token to parse
        /// </summary>
        [Required(ErrorMessage = "Token is required")]
        public string Token { get; set; }
    }
} 