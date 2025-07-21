using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.User
{
    /// <summary>
    /// Third-party login request DTO
    /// </summary>
    public class ThirdPartyLoginRequestDto
    {
        /// <summary>
        /// Application code from third-party system
        /// </summary>
        [Required(ErrorMessage = "AppCode is required")]
        [StringLength(100, ErrorMessage = "AppCode length cannot exceed 100 characters")]
        public string AppCode { get; set; } = string.Empty;

        /// <summary>
        /// Tenant ID from third-party system
        /// </summary>
        [Required(ErrorMessage = "TenantId is required")]
        [StringLength(100, ErrorMessage = "TenantId length cannot exceed 100 characters")]
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// Authorization token from third-party system
        /// </summary>
        [Required(ErrorMessage = "AuthorizationToken is required")]
        public string AuthorizationToken { get; set; } = string.Empty;
    }
} 