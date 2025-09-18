using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.QuestionnaireAnswer
{
    /// <summary>
    /// 问卷答案输入DTO
    /// </summary>
    public class QuestionnaireAnswerInputDto
    {
        /// <summary>
        /// 入职ID
        /// </summary>
        [Required(ErrorMessage = "入职ID不能为空")]
        public long OnboardingId { get; set; }

        /// <summary>
        /// 阶段ID
        /// </summary>
        [Required(ErrorMessage = "阶段ID不能为空")]
        public long StageId { get; set; }

        /// <summary>
        /// 问卷ID
        /// </summary>
        public long? QuestionnaireId { get; set; }

        /// <summary>
        /// 答案JSON
        /// </summary>
        [Required(ErrorMessage = "答案内容不能为空")]
        [MaxLength(100000, ErrorMessage = "答案内容不能超过100000个字符")] // 增加到100KB
        public string AnswerJson { get; set; } = string.Empty;

        /// <summary>
        /// 状态
        /// </summary>
        [MaxLength(20, ErrorMessage = "状态长度不能超过20个字符")]
        public string? Status { get; set; }

        /// <summary>
        /// 完成率
        /// </summary>
        [Range(0, 100, ErrorMessage = "完成率必须在0-100之间")]
        public decimal? CompletionRate { get; set; }

        /// <summary>
        /// 当前章节索引
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "当前章节索引必须大于等于0")]
        public int? CurrentSectionIndex { get; set; }

        /// <summary>
        /// 用户代理
        /// </summary>
        [MaxLength(50, ErrorMessage = "用户代理长度不能超过50个字符")]
        public string? UserAgent { get; set; }
    }

    /// <summary>
    /// 问卷答案更新DTO
    /// </summary>
    public class QuestionnaireAnswerUpdateDto
    {
        /// <summary>
        /// 答案JSON
        /// </summary>
        [MaxLength(10000, ErrorMessage = "答案内容不能超过10000个字符")]
        public string? AnswerJson { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        [MaxLength(20, ErrorMessage = "状态长度不能超过20个字符")]
        public string? Status { get; set; }

        /// <summary>
        /// 完成率
        /// </summary>
        [Range(0, 100, ErrorMessage = "完成率必须在0-100之间")]
        public decimal? CompletionRate { get; set; }

        /// <summary>
        /// 当前章节索引
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "当前章节索引必须大于等于0")]
        public int? CurrentSectionIndex { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [MaxLength(1000, ErrorMessage = "备注长度不能超过1000个字符")]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// 问卷答案审核DTO
    /// </summary>
    public class QuestionnaireAnswerReviewDto
    {
        /// <summary>
        /// 答案ID列表
        /// </summary>
        [Required(ErrorMessage = "答案ID列表不能为空")]
        public List<long> AnswerIds { get; set; } = new();

        /// <summary>
        /// 新状态
        /// </summary>
        [Required(ErrorMessage = "状态不能为空")]
        [MaxLength(20, ErrorMessage = "状态长度不能超过20个字符")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 审核备注
        /// </summary>
        [MaxLength(1000, ErrorMessage = "审核备注长度不能超过1000个字符")]
        public string? ReviewNotes { get; set; }

        /// <summary>
        /// 审核人ID
        /// </summary>
        public long? ReviewerId { get; set; }
    }
}