using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Infrastructure.Configuration
{
    /// <summary>
    /// Security configuration options
    /// </summary>
    public class SecurityOptions
    {
        public const string SectionName = "Security";

        [Required]
        [MinLength(32, ErrorMessage = "JWT SecretKey must be at least 32 characters long")]
        public string JwtSecretKey { get; set; } = string.Empty;

        [Required]
        public string JwtIssuer { get; set; } = string.Empty;

        [Required]
        public string JwtAudience { get; set; } = string.Empty;

        [Range(1, 43200, ErrorMessage = "JWT expiry must be between 1 and 43200 minutes")]
        public int JwtExpiryMinutes { get; set; } = 1440;
    }
}