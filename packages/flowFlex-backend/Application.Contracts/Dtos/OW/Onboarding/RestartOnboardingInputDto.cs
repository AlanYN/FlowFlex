namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// 重启Onboarding输入DTO
    /// </summary>
    public class RestartOnboardingInputDto
    {
        /// <summary>
        /// 重启原因
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// 是否重置进度
        /// </summary>
        public bool ResetProgress { get; set; } = true;

        /// <summary>
        /// 备注
        /// </summary>
        public string? Notes { get; set; }
    }
}