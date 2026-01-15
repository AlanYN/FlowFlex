using System;

namespace FlowFlex.Application.Contracts.Dtos.OW.StaticField
{
    /// <summary>
    /// 静态字段值输出DTO
    /// </summary>
    public class StaticFieldValueOutputDto
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 租户ID
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Onboarding ID
        /// </summary>
        public long OnboardingId { get; set; }

        /// <summary>
        /// Stage ID
        /// </summary>
        public long StageId { get; set; }

        /// <summary>
        /// 静态字段名称
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// 字段ID（动态属性ID）
        /// </summary>
        public long? FieldId { get; set; }

        /// <summary>
        /// 字段显示名称
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 字段值JSON数据
        /// </summary>
        public string FieldValueJson { get; set; }

        /// <summary>
        /// 字段类型
        /// </summary>
        public string FieldType { get; set; }

        /// <summary>
        /// 是否必填
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// 提交状态
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 完成率
        /// </summary>
        public int CompletionRate { get; set; }

        /// <summary>
        /// 提交时间
        /// </summary>
        public DateTimeOffset? SubmitTime { get; set; }

        /// <summary>
        /// 审核时间
        /// </summary>
        public DateTimeOffset? ReviewTime { get; set; }

        /// <summary>
        /// 审核人ID
        /// </summary>
        public long? ReviewerId { get; set; }

        /// <summary>
        /// 审核备注
        /// </summary>
        public string ReviewNotes { get; set; }

        /// <summary>
        /// 版本号
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// 是否最新版本
        /// </summary>
        public bool IsLatest { get; set; }

        /// <summary>
        /// 数据来源
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// IP地址
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// 用户代理
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// 验证结果
        /// </summary>
        public string ValidationStatus { get; set; }

        /// <summary>
        /// 验证错误信息
        /// </summary>
        public string ValidationErrors { get; set; }

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTimeOffset ModifyDate { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        public string CreateBy { get; set; }

        /// <summary>
        /// 修改人
        /// </summary>
        public string ModifyBy { get; set; }

        /// <summary>
        /// 创建人ID
        /// </summary>
        public long CreateUserId { get; set; }

        /// <summary>
        /// 修改人ID
        /// </summary>
        public long ModifyUserId { get; set; }
    }
}