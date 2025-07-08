using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Infrastructure.Configuration
{
    /// <summary>
    /// 数据库配置选项
    /// </summary>
    public class DatabaseOptions
    {
        public const string SectionName = "Database";

        [Required]
        public string ConnectionString { get; set; } = string.Empty;

        [Required]
        public string ConfigId { get; set; } = "FlowFlex";

        [Required]
        public string DbType { get; set; } = "PostgreSQL";

        public int CommandTimeout { get; set; } = 30;

        public bool EnableSqlLogging { get; set; } = false;

        public bool EnablePerformanceLogging { get; set; } = true;

        public int MaxRetryCount { get; set; } = 3;

        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    }
}