using FlowFlex.Domain.Entities.Base;
using SqlSugar;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// AI模型配置
    /// </summary>
    [SugarTable("ff_aimodel_config")]
    public class AIModelConfig : EntityBase
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        [Required]
        public long UserId { get; set; }

        /// <summary>
        /// AI提供商类型
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// API密钥
        /// </summary>
        [Required]
        [StringLength(2000)]
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// 基础URL
        /// </summary>
        [StringLength(1000)]
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// API版本
        /// </summary>
        [StringLength(20)]
        public string ApiVersion { get; set; } = string.Empty;

        /// <summary>
        /// 模型名称
        /// </summary>
        [StringLength(100)]
        public string ModelName { get; set; } = string.Empty;

        /// <summary>
        /// 温度参数
        /// </summary>
        public double Temperature { get; set; } = 0.7;

        /// <summary>
        /// 最大Token数
        /// </summary>
        public int MaxTokens { get; set; } = 4096;

        /// <summary>
        /// 是否启用流式响应
        /// </summary>
        public bool EnableStreaming { get; set; } = true;

        /// <summary>
        /// 是否为默认配置
        /// </summary>
        public bool IsDefault { get; set; } = false;

        /// <summary>
        /// 状态检查结果
        /// </summary>
        public bool IsAvailable { get; set; } = false;

        /// <summary>
        /// 最后检查时间
        /// </summary>
        public DateTime? LastCheckTime { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string Remarks { get; set; } = string.Empty;
    }
}