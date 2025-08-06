using System;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.QuestionnaireAnswer
{
    /// <summary>
    /// 问卷答案输出DTO
    /// </summary>
    public class QuestionnaireAnswerOutputDto
    {
        /// <summary>
        /// 答案ID
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
        public long StageId { get; set; }

        /// <summary>
        /// 问卷ID
        /// </summary>
        public long? QuestionnaireId { get; set; }

        /// <summary>
        /// 答案JSON
        /// </summary>
        public string AnswerJson { get; set; } = string.Empty;

        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 完成率
        /// </summary>
        public decimal CompletionRate { get; set; }

        /// <summary>
        /// 当前章节索引
        /// </summary>
        public int CurrentSectionIndex { get; set; }

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
        public string? ReviewNotes { get; set; }

        /// <summary>
        /// 版本号
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// 是否最新版本
        /// </summary>
        public bool IsLatest { get; set; }

        /// <summary>
        /// 用户代理
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// IP地址
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// 设备信息
        /// </summary>
        public string? DeviceInfo { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        public string CreateBy { get; set; } = string.Empty;

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTimeOffset ModifyDate { get; set; }

        /// <summary>
        /// 修改人
        /// </summary>
        public string ModifyBy { get; set; } = string.Empty;
    }
}