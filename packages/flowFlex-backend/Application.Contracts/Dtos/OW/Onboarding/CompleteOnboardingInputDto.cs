namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// 完成Onboarding输入DTO
    /// </summary>
    public class CompleteOnboardingInputDto
    {
        /// <summary>
        /// 完成备注
        /// </summary>
        public string? CompletionNotes { get; set; }

        /// <summary>
        /// 完成评分 (1-5)
        /// </summary>
        public int? Rating { get; set; }

        /// <summary>
        /// 反馈
        /// </summary>
        public string? Feedback { get; set; }
    }
}