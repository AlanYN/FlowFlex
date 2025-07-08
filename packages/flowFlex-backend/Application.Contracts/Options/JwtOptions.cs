namespace FlowFlex.Application.Contracts.Options
{
    /// <summary>
    /// JWT配置选项
    /// </summary>
    public class JwtOptions
    {
        /// <summary>
        /// 密钥
        /// </summary>
        public string SecretKey { get; set; }

        /// <summary>
        /// 发行�?
        /// </summary>
        public string Issuer { get; set; }

        /// <summary>
        /// 接收�?
        /// </summary>
        public string Audience { get; set; }

        /// <summary>
        /// 过期时间（分钟）
        /// </summary>
        public int ExpiryMinutes { get; set; } = 60;
    }
}
