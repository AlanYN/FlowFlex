using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SqlSugar;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// User Entity
    /// </summary>
    [Table("ff_users")]
    [SugarTable("ff_users")]
    public class User : OwEntityBase
    {
        /// <summary>
        /// Username
        /// </summary>

        [MaxLength(50)]
        public string Username { get; set; }

        /// <summary>
        /// Email Address
        /// </summary>

        [MaxLength(100)]
        public string Email { get; set; }

        /// <summary>
        /// Password Hash
        /// </summary>

        [MaxLength(255)]
        public string PasswordHash { get; set; }

        /// <summary>
        /// Email Verified Status
        /// </summary>
        public bool EmailVerified { get; set; }

        /// <summary>
        /// Email Verification Code
        /// </summary>
        [MaxLength(6)]
        public string EmailVerificationCode { get; set; }

        /// <summary>
        /// Verification Code Expiry Time
        /// </summary>
        public DateTimeOffset? VerificationCodeExpiry { get; set; }

        /// <summary>
        /// User Status
        /// </summary>
        [MaxLength(20)]
        public string Status { get; set; }

        /// <summary>
        /// Last Login Time
        /// </summary>
        [SugarColumn(ColumnName = "last_login_date")]
        public DateTimeOffset? LastLoginDate { get; set; }

        /// <summary>
        /// Last Login IP Address
        /// </summary>
        [MaxLength(45)]
        [SugarColumn(ColumnName = "last_login_ip")]
        public string LastLoginIp { get; set; }
    }
}
