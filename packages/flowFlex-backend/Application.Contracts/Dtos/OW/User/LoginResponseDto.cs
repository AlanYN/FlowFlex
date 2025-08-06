namespace FlowFlex.Application.Contracts.Dtos.OW.User
{
    /// <summary>
    /// 登录响应DTO
    /// </summary>
    public class LoginResponseDto
    {
        /// <summary>
        /// 访问令牌
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// 令牌类型
        /// </summary>
        public string TokenType { get; set; } = "Bearer";

        /// <summary>
        /// 过期时间（秒）
        /// </summary>
        public int ExpiresIn { get; set; }

        /// <summary>
        /// 用户信息
        /// </summary>
        public UserDto User { get; set; }

        /// <summary>
        /// Application code
        /// </summary>
        public string AppCode { get; set; }

        /// <summary>
        /// Tenant ID
        /// </summary>
        public string TenantId { get; set; }
    }
}