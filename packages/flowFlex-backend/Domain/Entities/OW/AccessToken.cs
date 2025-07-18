using System.ComponentModel.DataAnnotations;
using SqlSugar;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// Access Token Entity for managing JWT tokens
    /// </summary>
    [SugarTable("ff_access_tokens")]
    public class AccessToken : OwEntityBase
    {
        /// <summary>
        /// JWT ID (jti claim) - unique identifier for the token
        /// </summary>
        [Required]
        [StringLength(100)]
        [SugarColumn(ColumnName = "jti")]
        public string Jti { get; set; } = string.Empty;

        /// <summary>
        /// User ID who owns the token
        /// </summary>
        [Required]
        [SugarColumn(ColumnName = "user_id")]
        public long UserId { get; set; }

        /// <summary>
        /// User email
        /// </summary>
        [Required]
        [StringLength(100)]
        [SugarColumn(ColumnName = "user_email")]
        public string UserEmail { get; set; } = string.Empty;

        /// <summary>
        /// Token value (hashed for security)
        /// </summary>
        [Required]
        [StringLength(500)]
        [SugarColumn(ColumnName = "token_hash")]
        public string TokenHash { get; set; } = string.Empty;

        /// <summary>
        /// When the token was issued
        /// </summary>
        [SugarColumn(ColumnName = "issued_at")]
        public DateTimeOffset IssuedAt { get; set; }

        /// <summary>
        /// When the token expires
        /// </summary>
        [SugarColumn(ColumnName = "expires_at")]
        public DateTimeOffset ExpiresAt { get; set; }

        /// <summary>
        /// Whether the token is currently active
        /// </summary>
        [SugarColumn(ColumnName = "is_active")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Token type (login, refresh, portal-access)
        /// </summary>
        [StringLength(20)]
        [SugarColumn(ColumnName = "token_type")]
        public string TokenType { get; set; } = "login";

        /// <summary>
        /// When the token was revoked (if applicable)
        /// </summary>
        [SugarColumn(ColumnName = "revoked_at")]
        public DateTimeOffset? RevokedAt { get; set; }

        /// <summary>
        /// Reason for revocation (logout, refresh, expired)
        /// </summary>
        [StringLength(50)]
        [SugarColumn(ColumnName = "revoke_reason")]
        public string RevokeReason { get; set; } = string.Empty;

        /// <summary>
        /// Last time this token was used
        /// </summary>
        [SugarColumn(ColumnName = "last_used_at")]
        public DateTimeOffset? LastUsedAt { get; set; }

        /// <summary>
        /// IP address where token was issued
        /// </summary>
        [StringLength(45)]
        [SugarColumn(ColumnName = "issued_ip")]
        public string IssuedIp { get; set; } = string.Empty;

        /// <summary>
        /// User agent when token was issued
        /// </summary>
        [StringLength(500)]
        [SugarColumn(ColumnName = "user_agent")]
        public string UserAgent { get; set; } = string.Empty;
    }
} 