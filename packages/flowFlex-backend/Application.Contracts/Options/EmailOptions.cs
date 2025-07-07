namespace FlowFlex.Application.Contracts.Options
{
    /// <summary>
    /// 邮件服务配置选项
    /// </summary>
    public class EmailOptions
    {
        /// <summary>
        /// SMTP服务�?
        /// </summary>
        public string SmtpServer { get; set; }

        /// <summary>
        /// SMTP端口
        /// </summary>
        public int SmtpPort { get; set; }

        /// <summary>
        /// 是否启用SSL
        /// </summary>
        public bool EnableSsl { get; set; }

        /// <summary>
        /// 发件人邮�?
        /// </summary>
        public string FromEmail { get; set; }

        /// <summary>
        /// 发件人显示名�?
        /// </summary>
        public string FromName { get; set; }

        /// <summary>
        /// 用户�?
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// 验证码有效期（分钟）
        /// </summary>
        public int VerificationCodeExpiryMinutes { get; set; } = 10;
    }
} 
