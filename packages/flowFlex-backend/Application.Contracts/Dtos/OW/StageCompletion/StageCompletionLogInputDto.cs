using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.StageCompletion
{
    /// <summary>
    /// 阶段完成日志输入DTO
    /// </summary>
    public class StageCompletionLogInputDto
    {
        /// <summary>
        /// 日志条目列表
        /// </summary>
        [Required(ErrorMessage = "日志条目列表不能为空")]
        public List<StageCompletionLogEntryDto> Entries { get; set; } = new();

        /// <summary>
        /// 批次ID (可选)
        /// </summary>
        [MaxLength(50, ErrorMessage = "批次ID长度不能超过50个字符")]
        public string? BatchId { get; set; }
    }

    /// <summary>
    /// 阶段完成日志条目DTO
    /// </summary>
    public class StageCompletionLogEntryDto
    {
        /// <summary>
        /// 操作类型
        /// </summary>
        [Required(ErrorMessage = "操作类型不能为空")]
        [MaxLength(100, ErrorMessage = "操作类型长度不能超过100个字符")]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// 日志类型
        /// </summary>
        [Required(ErrorMessage = "日志类型不能为空")]
        [MaxLength(50, ErrorMessage = "日志类型长度不能超过50个字符")]
        public string LogType { get; set; } = string.Empty;

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// 日志数据
        /// </summary>
        [Required(ErrorMessage = "日志数据不能为空")]
        public StageCompletionLogDataDto Data { get; set; } = new();

        /// <summary>
        /// IP地址
        /// </summary>
        [MaxLength(500, ErrorMessage = "IP地址长度不能超过500个字符")]
        public string? IpAddress { get; set; }

        /// <summary>
        /// 用户代理
        /// </summary>
        [MaxLength(500, ErrorMessage = "用户代理长度不能超过500个字符")]
        public string? UserAgent { get; set; }
    }

    /// <summary>
    /// 阶段完成日志数据DTO
    /// </summary>
    public class StageCompletionLogDataDto
    {
        /// <summary>
        /// 入职ID
        /// </summary>
        public long? OnboardingId { get; set; }

        /// <summary>
        /// 阶段ID
        /// </summary>
        public long? StageId { get; set; }

        /// <summary>
        /// 操作类型
        /// </summary>
        [MaxLength(200, ErrorMessage = "操作类型长度不能超过200个字符")]
        public string? ActionType { get; set; }

        /// <summary>
        /// 来源页面
        /// </summary>
        [MaxLength(100, ErrorMessage = "来源页面长度不能超过100个字符")]
        public string? SourcePage { get; set; }

        /// <summary>
        /// 操作是否成功
        /// </summary>
        public bool? Success { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        [MaxLength(2000, ErrorMessage = "错误信息长度不能超过2000个字符")]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 网络状态
        /// </summary>
        [MaxLength(20, ErrorMessage = "网络状态长度不能超过20个字符")]
        public string? NetworkStatus { get; set; }

        /// <summary>
        /// 响应时间(毫秒)
        /// </summary>
        public int? ResponseTime { get; set; }

        /// <summary>
        /// API端点
        /// </summary>
        [MaxLength(200, ErrorMessage = "API端点长度不能超过200个字符")]
        public string? Endpoint { get; set; }

        /// <summary>
        /// HTTP状态码
        /// </summary>
        public int? HttpStatus { get; set; }

        /// <summary>
        /// HTTP方法
        /// </summary>
        [MaxLength(10, ErrorMessage = "HTTP方法长度不能超过10个字符")]
        public string? Method { get; set; }

        /// <summary>
        /// 数据大小(字节)
        /// </summary>
        public long? DataSize { get; set; }

        /// <summary>
        /// 任务数量
        /// </summary>
        public int? TasksCount { get; set; }

        /// <summary>
        /// 总操作数
        /// </summary>
        public int? TotalOperations { get; set; }
    }
}