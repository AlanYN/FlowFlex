using System;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// 完成指定阶段输入DTO (支持跳过阶段完成)
    /// </summary>
    public class CompleteCurrentStageInputDto
    {
        /// <summary>
        /// 要完成的阶段ID（新版本字段，支持任意阶段，不限于当前阶段）
        /// 如果不提供，将使用CurrentStageId字段的值
        /// </summary>
        public long? StageId { get; set; }

        /// <summary>
        /// 当前阶段ID（旧版本字段，保持向后兼容性）
        /// 如果StageId未提供，将使用此字段作为要完成的阶段ID
        /// </summary>
        public long? CurrentStageId { get; set; }

        /// <summary>
        /// 获取实际要完成的阶段ID（优先使用StageId，否则使用CurrentStageId）
        /// </summary>
        public long GetTargetStageId()
        {
            if (StageId.HasValue && StageId.Value > 0)
            {
                return StageId.Value;
            }

            if (CurrentStageId.HasValue && CurrentStageId.Value > 0)
            {
                return CurrentStageId.Value;
            }

            throw new ArgumentException("Either StageId or CurrentStageId must be provided and greater than 0");
        }

        /// <summary>
        /// 完成备注（可选）
        /// </summary>
        [MaxLength(2000, ErrorMessage = "完成备注长度不能超过2000个字符")]
        public string? CompletionNotes { get; set; }

        /// <summary>
        /// 是否强制完成（忽略重复完成检查）
        /// </summary>
        public bool ForceComplete { get; set; } = false;

        /// <summary>
        /// Preferred language for AI summary output (e.g., zh-CN, en-US)
        /// </summary>
        public string? Language { get; set; }
    }
}