using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Options
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

        /// <summary>
        /// API Key 加密密钥
        /// Must be exactly 32 characters for AES-256 encryption
        /// </summary>
        [Required]
        [StringLength(32, MinimumLength = 32, ErrorMessage = "EncryptionKey must be exactly 32 characters long")]
        public string EncryptionKey { get; set; } = string.Empty;

        /// <summary>
        /// API Key 加密初始向量
        /// Must be exactly 16 characters for AES encryption
        /// </summary>
        [Required]
        [StringLength(16, MinimumLength = 16, ErrorMessage = "EncryptionIV must be exactly 16 characters long")]
        public string EncryptionIV { get; set; } = string.Empty;
    }
}