using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.StaticField
{
    /// <summary>
    /// 静态字段值输入DTO
    /// </summary>
    public class StaticFieldValueInputDto
    {
        /// <summary>
        /// ID（更新时使用）
        /// </summary>
        public long? Id { get; set; }

        /// <summary>
        /// Onboarding ID
        /// </summary>
        [Required(ErrorMessage = "Onboarding ID不能为空")]
        public long OnboardingId { get; set; }

        /// <summary>
        /// Stage ID
        /// </summary>
        [Required(ErrorMessage = "Stage ID不能为空")]
        public long StageId { get; set; }

        /// <summary>
        /// 静态字段名称
        /// </summary>
        [Required(ErrorMessage = "字段名称不能为空")]
        [StringLength(100, ErrorMessage = "字段名称长度不能超过100个字符")]
        public string FieldName { get; set; }

        /// <summary>
        /// 字段ID（动态属性ID）
        /// </summary>
        public long? FieldId { get; set; }

        /// <summary>
        /// 字段显示名称
        /// </summary>
        [StringLength(200, ErrorMessage = "字段显示名称长度不能超过200个字符")]
        public string DisplayName { get; set; }

        /// <summary>
        /// 字段标签（用于操作日志显示）
        /// </summary>
        [StringLength(200, ErrorMessage = "字段标签长度不能超过200个字符")]
        public string FieldLabel { get; set; }

        /// <summary>
        /// 字段值（JSON格式）
        /// </summary>
        public string FieldValueJson { get; set; } = string.Empty;

        /// <summary>
        /// 字段类型
        /// </summary>
        [StringLength(50, ErrorMessage = "字段类型长度不能超过50个字符")]
        public string FieldType { get; set; } = "text";

        /// <summary>
        /// 是否必填
        /// </summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// 提交状态
        /// </summary>
        [StringLength(20, ErrorMessage = "状态长度不能超过20个字符")]
        public string Status { get; set; } = "Draft";

        /// <summary>
        /// 完成率
        /// </summary>
        [Range(0, 100, ErrorMessage = "完成率必须在0-100之间")]
        public int CompletionRate { get; set; } = 0;

        /// <summary>
        /// 数据来源
        /// </summary>
        [StringLength(50, ErrorMessage = "数据来源长度不能超过50个字符")]
        public string Source { get; set; } = "customer_portal";

        /// <summary>
        /// IP地址
        /// </summary>
        [StringLength(45, ErrorMessage = "IP地址长度不能超过45个字符")]
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// 用户代理
        /// </summary>
        [StringLength(500, ErrorMessage = "用户代理长度不能超过500个字符")]
        public string UserAgent { get; set; } = string.Empty;

        /// <summary>
        /// 验证结果
        /// </summary>
        [StringLength(20, ErrorMessage = "验证结果长度不能超过20个字符")]
        public string ValidationStatus { get; set; } = "Pending";

        /// <summary>
        /// 验证错误信息
        /// </summary>
        [StringLength(1000, ErrorMessage = "验证错误信息长度不能超过1000个字符")]
        public string ValidationErrors { get; set; } = string.Empty;
    }

    /// <summary>
    /// 批量保存静态字段值输入DTO
    /// </summary>
    public class BatchStaticFieldValueInputDto
    {
        /// <summary>
        /// Onboarding ID
        /// </summary>
        [Required(ErrorMessage = "Onboarding ID不能为空")]
        public long OnboardingId { get; set; }

        /// <summary>
        /// Stage ID
        /// </summary>
        [Required(ErrorMessage = "Stage ID不能为空")]
        public long StageId { get; set; }

        /// <summary>
        /// 静态字段值列表
        /// </summary>
        [Required(ErrorMessage = "静态字段值列表不能为空")]
        public List<StaticFieldValueInputDto> FieldValues { get; set; } = new List<StaticFieldValueInputDto>();

        /// <summary>
        /// 是否覆盖现有值
        /// </summary>
        public bool OverwriteExisting { get; set; } = true;

        /// <summary>
        /// 数据来源
        /// </summary>
        [StringLength(50, ErrorMessage = "数据来源长度不能超过50个字符")]
        public string Source { get; set; } = "customer_portal";

        /// <summary>
        /// IP地址
        /// </summary>
        [StringLength(45, ErrorMessage = "IP地址长度不能超过45个字符")]
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// 用户代理
        /// </summary>
        [StringLength(500, ErrorMessage = "用户代理长度不能超过500个字符")]
        public string UserAgent { get; set; } = string.Empty;
    }
}