using System;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.StageCompletion
{
    /// <summary>
    /// 阶段完成日志输出DTO
    /// </summary>
    public class StageCompletionLogOutputDto
    {
        /// <summary>
        /// 日志ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 租户ID
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// 入职ID
        /// </summary>
        public long OnboardingId { get; set; }

        /// <summary>
        /// 阶段ID
        /// </summary>
        public long? StageId { get; set; }

        /// <summary>
        /// 操作类型
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// 日志类型
        /// </summary>
        public string LogType { get; set; } = string.Empty;

        /// <summary>
        /// 日志数据
        /// </summary>
        public StageCompletionLogDataDto Data { get; set; } = new();

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 网络状态
        /// </summary>
        public string? NetworkStatus { get; set; }

        /// <summary>
        /// 响应时间(毫秒)
        /// </summary>
        public int? ResponseTime { get; set; }

        /// <summary>
        /// API端点
        /// </summary>
        public string? Endpoint { get; set; }

        /// <summary>
        /// 用户代理
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// HTTP方法
        /// </summary>
        public string? Method { get; set; }

        /// <summary>
        /// IP地址
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// 客户端信息
        /// </summary>
        public string? ClientInfo { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        public string CreateBy { get; set; } = string.Empty;
    }
}