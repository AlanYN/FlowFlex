using System;

namespace FlowFlex.Application.Contracts.Dtos.OW.User
{
    /// <summary>
    /// 用户DTO
    /// </summary>
    public class UserDto
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// 是否已验证邮箱
        /// </summary>
        public bool EmailVerified { get; set; }

        /// <summary>
        /// 最后登录时间
        /// </summary>
        public DateTimeOffset? LastLoginDate { get; set; }

        /// <summary>
        /// 用户状态
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }

        /// <summary>
        /// 用户团队
        /// </summary>
        public string Team { get; set; }
    }
}