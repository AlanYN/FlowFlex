using System;
using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.OW.User
{
    /// <summary>
    /// JWT Token information DTO
    /// </summary>
    public class JwtTokenInfoDto
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public long? UserId { get; set; }

        /// <summary>
        /// 用户邮箱
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// 租户ID
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Token是否有效
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Token颁发时间
        /// </summary>
        public DateTime? IssuedAt { get; set; }

        /// <summary>
        /// Token过期时间
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Token是否已过期
        /// </summary>
        public bool IsExpired { get; set; }

        /// <summary>
        /// 颁发者
        /// </summary>
        public string Issuer { get; set; }

        /// <summary>
        /// 受众
        /// </summary>
        public string Audience { get; set; }

        /// <summary>
        /// JWT ID
        /// </summary>
        public string JwtId { get; set; }

        /// <summary>
        /// 所有Claims信息
        /// </summary>
        public Dictionary<string, string> Claims { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 错误信息（如果解析失败）
        /// </summary>
        public string ErrorMessage { get; set; }
    }
} 