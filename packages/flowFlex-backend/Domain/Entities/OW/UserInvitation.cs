using System.ComponentModel.DataAnnotations;
using FlowFlex.Domain.Entities;
using SqlSugar;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// User invitation entity for portal access
    /// </summary>
    [SugarTable("ff_user_invitations")]
    public class UserInvitation : OwEntityBase
    {
        /// <summary>
        /// Onboarding ID
        /// </summary>
        [Required]
        [SugarColumn(ColumnName = "onboarding_id")]
        public long OnboardingId { get; set; }

        /// <summary>
        /// Email address
        /// </summary>
        [Required]
        [StringLength(200)]
        [SugarColumn(ColumnName = "email")]
        public string Email { get; set; }

        /// <summary>
        /// Invitation token (unique identifier for the invitation)
        /// </summary>
        [Required]
        [StringLength(100)]
        [SugarColumn(ColumnName = "invitation_token")]
        public string InvitationToken { get; set; }

        /// <summary>
        /// Invitation status (Pending, Active, Inactive, Expired, Used)
        /// </summary>
        [StringLength(20)]
        [SugarColumn(ColumnName = "status")]
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Encrypted portal access token (contains onboarding ID and invitation token)
        /// </summary>
        [StringLength(500)]
        [SugarColumn(ColumnName = "encrypted_access_token")]
        public string? EncryptedAccessToken { get; set; }

        /// <summary>
        /// Invitation sent date (nullable - only set when invitation is actually sent)
        /// </summary>
        [SugarColumn(ColumnName = "sent_date")]
        public DateTimeOffset? SentDate { get; set; }

        /// <summary>
        /// Token expiry date (null means no expiry)
        /// </summary>
        [SugarColumn(ColumnName = "token_expiry")]
        public DateTimeOffset? TokenExpiry { get; set; }

        /// <summary>
        /// Last access date (when user clicked the invitation link)
        /// </summary>
        [SugarColumn(ColumnName = "last_access_date")]
        public DateTimeOffset? LastAccessDate { get; set; }

        /// <summary>
        /// Short URL identifier (MD5 hash for compact invitation links)
        /// </summary>
        [StringLength(32)]
        [SugarColumn(ColumnName = "short_url_id")]
        public string? ShortUrlId { get; set; }

        /// <summary>
        /// User ID (if user exists)
        /// </summary>
        [SugarColumn(ColumnName = "user_id")]
        public long? UserId { get; set; }

        /// <summary>
        /// Send count
        /// </summary>
        [SugarColumn(ColumnName = "send_count")]
        public int SendCount { get; set; } = 0;

        /// <summary>
        /// Invitation URL
        /// </summary>
        [StringLength(500)]
        [SugarColumn(ColumnName = "invitation_url")]
        public string InvitationUrl { get; set; }

        /// <summary>
        /// Notes
        /// </summary>
        [StringLength(1000)]
        [SugarColumn(ColumnName = "notes")]
        public string Notes { get; set; }

        /// <summary>
        /// Navigation property to Onboarding
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public virtual Onboarding Onboarding { get; set; }

        /// <summary>
        /// Navigation property to User
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public virtual User User { get; set; }
    }
}